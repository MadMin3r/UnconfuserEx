using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SRE = System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.PE;
using UnConfuserEx.Protections;
using System.IO;
using UnConfuserEx.Protections.AntiTamper;
using log4net;
using de4dot.blocks;

namespace UnConfuserEx.Protections
{
    internal class AntiTamperRemover : IProtection
    {
        static ILog Logger = LogManager.GetLogger("AntiTamper");

        private enum DeriverType
        { 
            Normal,
            Dynamic
        }

        MethodDef? decryptMethod;

        string IProtection.Name => "AntiTamper";

        public bool IsPresent(ref ModuleDefMD module)
        {
            decryptMethod = GetDecryptMethod(module);

            return decryptMethod != null;
        }

        public bool Remove(ref ModuleDefMD module)
        {
            ImageSectionHeader? encryptedSection = GetEncryptedSection(module);
            if (encryptedSection == null)
            {
                Logger.Error("Failed to find encrypted section");
                return false;
            }
            Logger.Debug($"Found encrypted data in section {Encoding.UTF8.GetString(encryptedSection.Name)}");

            uint[]? initialKeys = GetInitialKeys();
            if (initialKeys == null)
            {
                Logger.Error("Failed to find initial keys in decrypt method");
                return false;
            }
            Logger.Debug($"Found initial decryption keys");

            (DeriverType? deriverType, List<Instruction>? derivation) = GetDeriverTypeAndDerivation();
            if (deriverType == null || derivation == null)
            {
                Logger.Error("[-] Failed to get the key deriver type and it's derivation");
                return false;
            }
            Logger.Debug($"Detected deriver type is {deriverType}");

            (uint[] dst, uint[] src) = PrepareKeyArrays(module, encryptedSection, initialKeys);

            IKeyDeriver deriver;
            if (deriverType == DeriverType.Normal)
            {
                deriver = new NormalDeriver();
            }
            else
            {
                deriver = new DynamicDeriver(derivation);
            }

            Logger.Debug($"Deriving decryption key");
            uint[] key = deriver.DeriveKey(dst, src);

            Logger.Debug($"Decrypting method bodies");
            return DecryptSection(ref module, key, encryptedSection);
        }

        private ImageSectionHeader? GetEncryptedSection(ModuleDefMD module)
        {
            int name = -1;
            
            var instrs = decryptMethod!.Body.Instructions;
            for (int i = 0; i < instrs.Count - 2; i++)
            {
                if (instrs[i].OpCode == OpCodes.Ldloc_S
                    && instrs[i + 1].OpCode == OpCodes.Ldc_I4
                    && instrs[i + 2].OpCode.FlowControl == FlowControl.Cond_Branch)
                {
                    name = (int)instrs[i + 1].Operand;
                    break;
                }
            }
            
            if (name == -1)
            {
                return null;
            }

            IList<ImageSectionHeader> sections = module.Metadata.PEImage.ImageSectionHeaders;
            foreach (var section in sections)
            {
                var sectionName = section.Name;
                var name1 = sectionName[0] | sectionName[1] << 8 | sectionName[2] << 16 | sectionName[3] << 24;
                var name2 = sectionName[4] | sectionName[5] << 8 | sectionName[6] << 16 | sectionName[7] << 24;

                if (name == (name1 * name2))
                {
                    return section;
                }

            }
            return null;
        }

        private uint[]? GetInitialKeys()
        {
            var instrs = decryptMethod!.Body.Instructions;
            int firstInstr = -1;
            for (int i = 0; i < instrs.Count - 1; i++)
            {
                if (instrs[i].OpCode == OpCodes.Ldc_I4
                    && instrs[i + 1].OpCode == OpCodes.Stloc_S)
                {
                    firstInstr = i;
                    break;
                }
            }

            if (firstInstr == -1)
            {
                return null;
            }

            uint[] keys = new uint[4];
            for (int i = 0; i < keys.Length; i++)
            {
                keys[i] = (uint)(int)instrs[firstInstr + (i * 2)].Operand;
            }

            return keys;
        }

        private (DeriverType?, List<Instruction>?) GetDeriverTypeAndDerivation()
        {
            /*
             *  Start of derivation
             *
             * 1F10     : ldc.i4.s  16
             * 32B5     : blt.s     IL_0151 
             * ????     : ??????            <<<< first derivation instr
             * 
             *  End of derivation
             * 
             * ????     ; ??????            <<<< last derivation instr
             * 1F40     : ldc.i4.s  64
             * 130E     : stloc.s   V_14
             * 1105     : ldloc.s   V_5
             * 
             */
            var instrs = decryptMethod!.Body.Instructions;

            var firstInstr = -1;
            for (int i = 0; i < instrs.Count - 1; i++)
            {
                if (instrs[i].OpCode == OpCodes.Ldc_I4_S
                    && instrs[i + 1].OpCode == OpCodes.Blt_S)
                {
                    firstInstr = i + 2;
                    break;
                }
            }

            if (firstInstr == -1)
            {
                return (null, null);
            }

            var lastInstr = -1;
            for (int i = firstInstr; i < instrs.Count - 2; i++)
            {
                if (instrs[i].OpCode == OpCodes.Stelem_I4
                    && instrs[i + 1].OpCode == OpCodes.Ldc_I4_S
                    && instrs[i + 2].OpCode == OpCodes.Stloc_S)
                {
                    lastInstr = i;
                    break;
                }
            }

            if (lastInstr == -1)
            {
                return (null, null);
            }

            List<Instruction> derivation = new();
            for (int i = 0; i <= (lastInstr - firstInstr); i++)
            {
                derivation.Add(instrs[firstInstr + i]);
            }

            // Normal deriver is 16 iterations of 10 instructions
            const int normalDeriationLength = 16 * 10;
            DeriverType type = (derivation.Count == normalDeriationLength) ? DeriverType.Normal : DeriverType.Dynamic;
            return (type, derivation);
        }

        private static MethodDef? GetDecryptMethod(ModuleDefMD module)
        {
            var cctor = module.GlobalType.FindStaticConstructor();

            if (cctor == null || !(cctor.HasBody))
                return null;

            IList<Instruction> instrs;

            // Check the first call in the cctor first
            if (cctor.Body.Instructions[0].OpCode == OpCodes.Call)
            {
                var method = cctor.Body.Instructions[0].Operand as MethodDef;

                instrs = method!.Body.Instructions;
                for (int i = 0; i < instrs.Count - 2; i++)
                {
                    if (instrs[i].OpCode == OpCodes.Ldtoken &&
                        instrs[i].Operand == module.GlobalType &&
                        instrs[i + 1].OpCode == OpCodes.Call &&
                        instrs[i + 1].Operand is MemberRef m &&
                        m.Name == "GetTypeFromHandle")
                    {
                        return method;
                    }
                }
            }

            return null;
        }

        private static (uint[], uint[]) PrepareKeyArrays(ModuleDefMD module, ImageSectionHeader encryptedSection, uint[] initialKeys)
        {
            uint z = initialKeys[0], x = initialKeys[1], c = initialKeys[2], v = initialKeys[3];

            var reader = module.Metadata.PEImage.CreateReader();
            IList<ImageSectionHeader> sections = module.Metadata.PEImage.ImageSectionHeaders;
            foreach (var section in sections)
            {
                if (section == encryptedSection)
                {
                    continue;
                }
                else
                {
                    var size = section.SizeOfRawData >> 2;
                    var loc = section.PointerToRawData;
                    reader.Position = loc;
                    for (int i = 0; i < size; i++)
                    {
                        var t = (z ^ reader.ReadUInt32()) + x + c * v;
                        z = x;
                        x = v;
                        v = t;
                    }
                }
            }
            uint[] dst = new uint[16], src = new uint[16];
            for (int i = 0; i < 16; i++)
            {
                dst[i] = v;
                src[i] = x;
                z = (x >> 5) | (x << 27);
                x = (c >> 3) | (c << 29);
                c = (v >> 7) | (v << 25);
                v = (z >> 11) | (z << 21);
            }

            return (dst, src);
        }

        private bool DecryptSection(ref ModuleDefMD module, uint[] key, ImageSectionHeader encryptedSection)
        {
            var reader = module.Metadata.PEImage.CreateReader();
            byte[] image = reader.ReadRemainingBytes();

            var size = encryptedSection.SizeOfRawData >> 2;
            var pos = encryptedSection.PointerToRawData;
            reader.Position = pos;
            uint[] result = new uint[size];
            for (uint i = 0; i < size; i++)
            {
                uint data = reader.ReadUInt32();
                result[i] = data ^ key[i & 0xf];
                key[i & 0xf] = (key[i & 0xf] ^ result[i]) + 0x3dbb2819;
            }
            byte[] byteResult = new byte[size << 2];
            Buffer.BlockCopy(result, 0, byteResult, 0, byteResult.Length);

            var stream = new MemoryStream(image)
            {
                Position = pos
            };
            stream.Write(byteResult, 0, byteResult.Length);

            ModuleDefMD newModule = ModuleDefMD.Load(stream);
            module = newModule;

            decryptMethod = GetDecryptMethod(module)!;

            module.GlobalType.Methods.Remove(decryptMethod);
            module.GlobalType.FindStaticConstructor().Body.Instructions.RemoveAt(0);

            return true;
        }
    }
}

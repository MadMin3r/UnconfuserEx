using dnlib.DotNet;
using dnlib.DotNet.Emit;
using MSILEmulator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using X86Emulator;

namespace UnConfuserEx.Protections.Delegates
{
    internal class RefProxyHandler
    {
        private MethodDef Handler;
        private X86Method? X86Method;
        private int[] NameChars = new int[5];
        private int[] Shifts = new int[4];

        public RefProxyHandler(ModuleDefMD module, MethodDef handler)
        {
            Handler = handler;

            var instrs = handler.Body.Instructions;
            var nameCharsFound = 0;
            var shiftsFound = 0;
            for (int i = 0; i < instrs.Count - 2; i++)
            {
                // Find the name indices and the shifts
                if (nameCharsFound == 5)
                {
                    break;
                }

                if (instrs[i].OpCode == OpCodes.Callvirt &&
                    instrs[i].Operand is IMethodDefOrRef md &&
                    md.Name.Contains("get_Name") &&
                    instrs[i + 1].IsLdcI4())
                {
                    NameChars[nameCharsFound++] = instrs[i + 1].GetLdcI4Value();
                }
                else if (instrs[i].IsLdcI4() &&
                    instrs[i].GetLdcI4Value() == 0x1f)
                {
                    Shifts[shiftsFound++] = instrs[i - 1].GetLdcI4Value();
                }
            }

            var method = instrs.Where(i => i.OpCode == OpCodes.Call).Skip(1).First().Operand as MethodDef;
            if (method!.IsNative)
            {
                X86Method = new X86Method(module, method);
            }
        }

        public MDToken GetMethodMDToken(FieldDef field)
        {
            var fieldSig = field.FieldSig.ExtraData;

            int key;
            if (field.FieldType is CModOptSig optSig)
            {
                key = (int)optSig.Modifier.MDToken.Raw;
            }
            else
            {
                throw new Exception("First field type wasn't an optional modifier - need to iterate");
            }

            key += ((field.Name.String[NameChars[0]] ^ (char)fieldSig[^1]) << Shifts[0]) +
                ((field.Name.String[NameChars[1]] ^ (char)fieldSig[^2]) << Shifts[1]) +
                ((field.Name.String[NameChars[2]] ^ (char)fieldSig[^4]) << Shifts[2]) +
                ((field.Name.String[NameChars[3]] ^ (char)fieldSig[^5]) << Shifts[3]);

            if (X86Method != null)
            {
                key = X86Method.Emulate(new int[] { key });
            }
            else
            {
                throw new NotImplementedException("Only x86 delegate removal is supported");
            }

            key *= GetFieldHash(field);

            return new MDToken(key);
        }

        public OpCode GetOpCode(FieldDef field, byte opKey)
        {
            var opCode = (Code)(field.Name.String[NameChars[4]] ^ opKey);
            return opCode.ToOpCode();
        }

        private int GetFieldHash(FieldDef field)
        {
            var customAttribute = field.CustomAttributes[0];

            var ctor = (MethodDef)customAttribute.Constructor;
            var arg = (int)customAttribute.ConstructorArguments[0].Value;

            var ilMethod = new ILMethod(ctor, 3, ctor.Body.Instructions.Count - 1);

            ilMethod.SetArg(1, arg);

            Context ctx = ilMethod.Emulate();

            return (int)ctx.Stack.Pop();
        }
    }
}

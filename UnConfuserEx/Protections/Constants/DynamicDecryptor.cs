using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using SRE = System.Reflection.Emit;

namespace UnConfuserEx.Protections.Constants
{
    internal class DynamicDecryptor : IDecryptor
    {
        private IList<Instruction> decryptInstructions;
        private Func<uint[], uint[], uint[]> decryptDelegate;

        public DynamicDecryptor(IList<Instruction> decryptInstructions)
        {
            this.decryptInstructions = decryptInstructions;
            decryptDelegate = GetDecryptDelegate();
        }

        private Func<uint[], uint[], uint[]> GetDecryptDelegate()
        {
            SortedSet<int> arrays = new();
            for (int i = 0; i < decryptInstructions.Count - 2; i++)
            {
                if (decryptInstructions[i + 2].OpCode == OpCodes.Ldelem_U4)
                {
                    if (decryptInstructions[i].OpCode == OpCodes.Ldloc_S)
                    {
                        arrays.Add(((Local)decryptInstructions[i].Operand).Index);
                    }
                    else if (decryptInstructions[i].OpCode.Value >= OpCodes.Ldloc_0.Value
                             && decryptInstructions[i].OpCode.Value <= OpCodes.Ldloc_3.Value)
                    {
                        arrays.Add(decryptInstructions[i].OpCode.Value - OpCodes.Ldloc_0.Value);
                    }
                }
            }
            int[] arrayIndices = arrays.ToArray();
            if (arrayIndices.Length != 2)
            {
                throw new Exception("There should be two key arrays used in a dynamic derivation");
            }

            var deriverMethod = new SRE.DynamicMethod("", typeof(uint[]), new Type[] { typeof(uint[]), typeof(uint[]) });
            var il = deriverMethod.GetILGenerator();

            // Setup locals
            Dictionary<int, int> locals = new();
            locals.Add(arrayIndices[0], 0);
            il.DeclareLocal(typeof(uint[])); // key
            locals.Add(arrayIndices[1], 1);
            il.DeclareLocal(typeof(uint[])); // temp

            // Store passed arrays in the expected locals
            il.Emit(SRE.OpCodes.Ldarg_0);
            il.Emit(SRE.OpCodes.Stloc_S, 0);
            il.Emit(SRE.OpCodes.Ldarg_1);
            il.Emit(SRE.OpCodes.Stloc_S, 1);

            // Derivation
            foreach (var instr in decryptInstructions)
            {
                var opcode = (SRE.OpCode)typeof(SRE.OpCodes).GetField(instr.OpCode.Code.ToString())!.GetValue(null)!;

                if (instr.Operand == null)
                {
                    if (instr.OpCode.OpCodeType == OpCodeType.Macro)
                    {
                        if (instr.IsLdloc())
                        {
                            var index = instr.OpCode.Value - OpCodes.Ldloc_0.Value;
                            int operand;
                            if (locals.ContainsKey(index))
                            {
                                operand = locals[index];
                            }
                            else
                            {
                                operand = locals.Count;
                                locals.Add(index, operand);

                                il.DeclareLocal(typeof(uint));
                            }
                            il.Emit(SRE.OpCodes.Ldloc_S, operand);
                        }
                        else if (instr.IsStloc())
                        {
                            var index = instr.OpCode.Value - OpCodes.Ldloc_0.Value;
                            int operand;
                            if (locals.ContainsKey(index))
                            {
                                operand = locals[index];
                            }
                            else
                            {
                                operand = locals.Count;
                                locals.Add(index, operand);

                                il.DeclareLocal(typeof(uint));
                            }
                            il.Emit(SRE.OpCodes.Stloc_S, operand);
                        }
                        else
                        {
                            il.Emit(opcode);
                        }
                    }
                    else
                    {
                        il.Emit(opcode);
                    }
                }
                else if (instr.Operand is Local local)
                {
                    var index = local.Index;
                    int operand;
                    if (locals.ContainsKey(index))
                    {
                        operand = locals[index];
                    }
                    else
                    {
                        operand = locals.Count;
                        locals.Add(index, operand);

                        il.DeclareLocal(typeof(uint));
                    }
                    il.Emit(opcode, operand);
                }
                else if (instr.Operand is sbyte sb)
                {
                    il.Emit(opcode, sb);
                }
                else if (instr.Operand is int i)
                {
                    il.Emit(opcode, i);
                }
                else if (instr.Operand is short s)
                {
                    il.Emit(opcode, s);
                }
                else if (instr.Operand is byte b)
                {
                    il.Emit(opcode, b);
                }
                else if (instr.Operand is long l)
                {
                    il.Emit(opcode, l);
                }
                else
                {
                    throw new Exception($"Unhandled operand type: { instr.Operand.GetType() }");
                }
            }

            // Return the temp array
            il.Emit(SRE.OpCodes.Ldloc_S, 1);
            il.Emit(SRE.OpCodes.Ret);

            var deriverDelegate = (Func<uint[], uint[], uint[]>)deriverMethod.CreateDelegate(typeof(Func<uint[], uint[], uint[]>));
            return deriverDelegate;
        }

        public byte[] DecryptData(uint[] data, uint[] key)
        {
            uint[] temp = new uint[key.Length];
            byte[] ret = new byte[data.Length << 2];
            int s = 0, d = 0;
            while (s < data.Length)
            {
                for (int j = 0; j < 16; j++)
                {
                    temp[j] = data[s + j];
                }

                temp = decryptDelegate.Invoke(key, temp);

                for (int j = 0; j < 16; j++)
                {
                    uint t = temp[j];
                    ret[d++] = (byte)t;
                    ret[d++] = (byte)(t >> 8);
                    ret[d++] = (byte)(t >> 16);
                    ret[d++] = (byte)(t >> 24);
                    key[j] ^= t;
                }
                s += 16;
            }

            return ret;
        }

    }
}

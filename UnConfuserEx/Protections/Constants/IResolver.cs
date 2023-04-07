using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Runtime.InteropServices;
using System.Text;

namespace UnConfuserEx.Protections.Constants
{
    internal abstract class IResolver
    {
        protected byte[]? data;

        public abstract void Resolve(MethodDef method, IList<MethodDef> instances);

        protected (int stringId, int numId, int objectId) GetIdsFromGetter(MethodDef getter)
        {
            var locals = getter.Body.Variables;
            var keyLocal = getter.Body.Variables.Locals[0];

            var indices = getter.Body.Instructions
                .Where(i => i.IsLdloc() && i.GetLocal(locals) == keyLocal)
                .Select(i => getter.Body.Instructions.IndexOf(i) + 1).ToList();

            var stringId = getter.Body.Instructions[indices[0]].GetLdcI4Value();
            var numId = getter.Body.Instructions[indices[1]].GetLdcI4Value();
            var objectId = getter.Body.Instructions[indices[2]].GetLdcI4Value();

            return (stringId, numId, objectId);
        }

        protected int GetNextInstanceInMethod(MethodDef getter, MethodDef method, out TypeSig? genericType)
        {
            var instrs = method.Body.Instructions;

            for (int i = 0; i < instrs.Count; i++)
            {
                if (instrs[i].OpCode == OpCodes.Call &&
                    instrs[i].Operand is MethodSpec ms &&
                    ms.Method.ResolveMethodDef() is MethodDef md &&
                    md.Equals(getter))
                {
                    genericType = ms.GenericInstMethodSig.GenericArguments[0];

                    if (instrs[i - 1].IsBr() &&
                        instrs[i - 1].Operand is Instruction target &&
                        target == instrs[i])
                    {
                        method.Body.Instructions.RemoveAt(i - 1);
                        i--;
                    }

                    return i - 1;
                }
            }
            genericType = null;
            return -1;
        }

        protected void FixStringConstant(MethodDef method, int instrOffset, int id)
        {
            uint count = (uint)(data![id] | (data[id + 1] << 8) | (data[id + 2] << 16) | (data[id + 3] << 24));
            //count = (count << 4) | (count >> 0x1C);
            string result = string.Intern(Encoding.UTF8.GetString(data, id + 4, (int)count));

            method.Body.Instructions[instrOffset].OpCode = OpCodes.Ldstr;
            method.Body.Instructions[instrOffset].Operand = result;
            method.Body.Instructions.RemoveAt(instrOffset + 1);
        }

        protected void FixNumberConstant(MethodDef method, int instrOffset, int id, TypeSig type)
        {
            switch (type.ElementType)
            {
                case ElementType.I4:
                    FixNumberConstant<int>(method, instrOffset, id);
                    method.Body.Instructions[instrOffset].OpCode = OpCodes.Ldc_I4;
                    break;
                case ElementType.R8:
                    FixNumberConstant<double>(method, instrOffset, id);
                    method.Body.Instructions[instrOffset].OpCode = OpCodes.Ldc_R8;
                    break;
                case ElementType.R4:
                    FixNumberConstant<Single>(method, instrOffset, id);
                    method.Body.Instructions[instrOffset].OpCode = OpCodes.Ldc_R4;
                    break;

                default:
                    throw new NotImplementedException($"Can't fix number constant. Type is {type.TypeName}");
            }
        }

        protected void FixNumberConstant<T>(MethodDef method, int instrOffset, int id)
        {
            T[] array = new T[1];
            Buffer.BlockCopy(data!, id, array, 0, Marshal.SizeOf(default(T)));

            method.Body.Instructions[instrOffset].Operand = array[0];
            method.Body.Instructions.RemoveAt(instrOffset + 1);
        }

        protected void FixObjectConstant(MethodDef method, int instrOffset, int id, TypeSig type)
        {
            int num0 = data![id] | (data[id + 1] << 8) | (data[id + 2] << 16) | (data[id + 3] << 24);
            uint num1 = (uint)(data![id + 4] | (data[id + 5] << 8) | (data[id + 6] << 16) | (data[id + 7] << 24));
            num1 = (num1 << 4) | (num1 >> 0x1C);

            throw new NotImplementedException("Object constant not handled");
        }

        protected void FixDefaultConstant(MethodDef method, int instrOffset, TypeSig type)
        {
            method.Body.Instructions[instrOffset].OpCode = OpCodes.Initobj;
            method.Body.Instructions[instrOffset].Operand = type.ToTypeDefOrRef();
            method.Body.Instructions.RemoveAt(instrOffset + 1);
        }
    }
}

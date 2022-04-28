using dnlib.DotNet;
using dnlib.DotNet.Emit;
using log4net;
using System;
using System.Collections.Generic;
using X86Emulator;

namespace UnConfuserEx.Protections.Constants
{
    internal class X86Resolver : IResolver
    {
        private static ILog Logger = LogManager.GetLogger("Constants");

        private ModuleDefMD Module;
        private byte[] Data;

        public X86Resolver(ModuleDefMD module, byte[] data)
        {
            Module = module;
            Data = data;
        }

        public void Resolve(MethodDef getter, IList<MethodDef> instances)
        {
            var x86MethodDef = (MethodDef)getter.Body.Instructions[5].Operand;

            var x86Method = new X86Method(Module, x86MethodDef);

            foreach (var method in instances)
            {
                int id = -1;
                TypeSig? genericType = null;
                var instrs = method.Body.Instructions;
                for (int i = 0; i < instrs.Count; i++)
                {
                    if (instrs[i].OpCode == OpCodes.Call
                        && instrs[i].Operand is MethodSpec m
                        && m.Method.ResolveMethodDef().Equals(getter))
                    {
                        id = (int)instrs[i - 1].Operand;
                        genericType = m.GenericInstMethodSig.GenericArguments[0];
                        break;
                    }
                }

                if (id == -1)
                    throw new Exception("Failed to get ID for constant decryption");


                id = x86Method.Emulate(new int[] { id });
                var type = (int)((uint)id >> 0x1E);
                id = (id & 0x3FFFFFFF) << 2;

                Logger.Debug($"X86 Constant getter:\n\tid: {id}\n\ttype: {type}\n\tgenericType: {genericType}");
            }
        }

    }
}

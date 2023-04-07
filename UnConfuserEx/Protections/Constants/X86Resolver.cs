using dnlib.DotNet;
using dnlib.DotNet.Emit;
using log4net;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using X86Emulator;

namespace UnConfuserEx.Protections.Constants
{
    internal class X86Resolver : IResolver
    {
        private static ILog Logger = LogManager.GetLogger("Constants");

        private ModuleDefMD Module;

        public X86Resolver(ModuleDefMD module, byte[] data)
        {
            Module = module;
            this.data = data;
        }

        public override void Resolve(MethodDef getter, IList<MethodDef> instances)
        {
            var x86MethodDef = (MethodDef)getter.Body.Instructions[5].Operand;
            var x86Method = new X86Method(Module, x86MethodDef);

            var (stringId, numId, objectId) = GetIdsFromGetter(getter);


            foreach (var method in instances)
            {
                if (ConstantsCFG.IsCFGPresent(method))
                {
                    new ConstantsCFG(method).RemoveFromMethod();
                }

                TypeSig? genericType;
                int instanceOffset = GetNextInstanceInMethod(getter, method, out genericType);

                while (instanceOffset != -1)
                {
                    var instrs = method.Body.Instructions;

                    var id = instrs[instanceOffset].GetLdcI4Value();
                    id = x86Method.Emulate(new int[] { id });
                    int type = (int)((uint)id >> 0x1e);
                    id = (id & 0x3fffffff) << 2;

                    try
                    {
                        if (type == stringId)
                        {
                            FixStringConstant(method, instanceOffset, id);
                        }
                        else if (type == numId)
                        {
                            FixNumberConstant(method, instanceOffset, id, genericType!);
                        }
                        else if (type == objectId)
                        {
                            FixObjectConstant(method, instanceOffset, id, genericType!);
                        }
                        else
                        {
                            FixDefaultConstant(method, instanceOffset, genericType!);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Failed to remove constants obfuscation from method ${method.FullName} ({ex.Message})");
                        return;
                    }


                    instanceOffset = GetNextInstanceInMethod(getter, method, out genericType);
                }

            }
        }

    }
}

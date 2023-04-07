using dnlib.DotNet;
using dnlib.DotNet.Emit;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace UnConfuserEx.Protections.Constants
{
    internal class NormalResolver : IResolver
    {
        private static ILog Logger = LogManager.GetLogger("Constants");

        public NormalResolver(byte[] data)
        {
            this.data = data;
        }

        public override void Resolve(MethodDef getter, IList<MethodDef> instances)
        {
            var key1 = (int)getter.Body.Instructions[5].Operand;
            var key2 = (int)getter.Body.Instructions[7].Operand;

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
                    id = (id * key1) ^ key2;
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
                
                method.Body.UpdateInstructionOffsets();
            }
        }
    }
}

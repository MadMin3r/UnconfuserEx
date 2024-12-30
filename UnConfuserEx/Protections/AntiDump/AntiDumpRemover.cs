using de4dot.blocks;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnConfuserEx.Protections.AntiDump
{
    internal class AntiDumpRemover : IProtection
    {
        public string Name => "AntiDump";

        MethodDef? antiDumpMethod;

        public bool IsPresent(ref ModuleDefMD module)
        {
            var cctor = module.GlobalType.FindStaticConstructor();

            if (cctor == null || !(cctor.HasBody) || cctor.Body.Instructions.Count == 0)
                return false;

            IList<Instruction> instrs;

            // Check the first call in the cctor first
            if (cctor.Body.Instructions[0].OpCode == OpCodes.Call)
            {
                var method = cctor.Body.Instructions[0].Operand as MethodDef;

                instrs = method!.Body.Instructions;
                if (instrs.Count > 6 &&
                    instrs[0].OpCode == OpCodes.Ldtoken &&
                    (TypeDef)instrs[0].Operand == module.GlobalType &&
                    instrs[5].OpCode == OpCodes.Call &&
                    ((IMethodDefOrRef)instrs[5].Operand).Name == "GetHINSTANCE")
                {
                    antiDumpMethod = method;
                    return true;
                }

            }

            instrs = cctor.Body.Instructions;

            // Then check the body itself
            if (instrs.Count > 6 &&
                instrs[0].OpCode == OpCodes.Ldtoken &&
                (TypeDef)instrs[0].Operand == module.GlobalType &&
                instrs[5].OpCode == OpCodes.Call &&
                ((IMethodDefOrRef)instrs[5].Operand).Name == "GetHINSTANCE")
            {
                antiDumpMethod = cctor;
                return true;
            }

            return false;
        }

        public bool Remove(ref ModuleDefMD module)
        {
            var cctor = module.GlobalType.FindStaticConstructor();

            if (antiDumpMethod == cctor)
            {
                // TODO: This may cause issues
                cctor.Body.Instructions.Clear();
                cctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            }
            else
            {
                cctor.Body.Instructions.RemoveAt(0);
            }

            return true;
        }
    }
}

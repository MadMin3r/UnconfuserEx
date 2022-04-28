using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnConfuserEx.Protections.AntiDebug
{
    internal class AntiDebugRemover : IProtection
    {
        public string Name => "AntiDebug";

        private bool ModuleCctorHasAntiDump = false;
        private List<MethodDef> AntiDebugMethods = new();


        public bool IsPresent(ref ModuleDefMD module)
        {
            if (module.GlobalType.FindStaticConstructor() is MethodDef cctor &&
                cctor.HasBody)
            {
                var instrs = cctor.Body.Instructions;

                ModuleCctorHasAntiDump = instrs[0].OpCode == OpCodes.Ldtoken &&
                                          (TypeDef)instrs[0].Operand == module.GlobalType &&
                                          instrs[5].OpCode == OpCodes.Call &&
                                          ((IMethodDefOrRef)instrs[5].Operand).Name == "GetHINSTANCE";

            }

            return ModuleCctorHasAntiDump;
        }

        public bool Remove(ref ModuleDefMD module)
        {
            if (ModuleCctorHasAntiDump)
            {
                var cctor = module.GlobalType.FindStaticConstructor() as MethodDef;

                cctor.Body.Instructions.Clear();
                cctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            }

            return true;
        }
    }
}

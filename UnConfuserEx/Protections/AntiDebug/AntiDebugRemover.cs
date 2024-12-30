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

        private enum AntiDebugType
        {
            Safe,
            Win32,
            Antinet
        };

        MethodDef? antiDebugMethod;
        AntiDebugType? antiDebugType;

        public bool IsPresent(ref ModuleDefMD module)
        {
            var cctor = module.GlobalType.FindStaticConstructor();

            if (cctor == null || !(cctor.HasBody) || cctor.Body.Instructions.Count == 0)
                return false;

            IList<Instruction> instrs;

            // Check the first call in the cctor
            if (cctor.Body.Instructions[0].OpCode == OpCodes.Call)
            {
                var method = cctor.Body.Instructions[0].Operand as MethodDef;

                instrs = method!.Body.Instructions;

                for (int i = 0; i < instrs.Count; i++)
                {

                    // Check for safe AntiDebug
                    if (instrs[i].OpCode == OpCodes.Ldstr &&
                        instrs[i].Operand is String str1 &&
                        str1 == "COR_ENABLE_PROFILING") 
                    {
                        antiDebugMethod = method;
                        antiDebugType = AntiDebugType.Safe;
                        return true;
                    }
                    // Check for win32 AntiDebug
                    else if (instrs[i].OpCode == OpCodes.Ldstr &&
                        instrs[i].Operand is String str2 &&
                        str2 == "_ENABLE_PROFILING")
                    {
                        antiDebugMethod = method;
                        antiDebugType = AntiDebugType.Win32;
                        return true;
                    }
                    // Check for antinet AntiDebug
                    else if (instrs[i].OpCode == OpCodes.Ldnull &&
                        instrs[i + 1].OpCode == OpCodes.Call &&
                        instrs[i + 1].Operand is MemberRef m &&
                        m.Name == "FailFast")
                    {
                        antiDebugMethod = method;
                        antiDebugType = AntiDebugType.Antinet;
                        return true;
                    }
                }

            }

            return false;
        }

        public bool Remove(ref ModuleDefMD module)
        {
            var cctor = module.GlobalType.FindStaticConstructor()!;

            // TODO: Could spend some effort to clean up all the other junk that gets injected
            // but this removes the AntiDebug so...

            // At the moment all of the AntiDebug can be removed this easily. Will keep the switch
            // here in case this is changed in the future.
            switch (antiDebugType)
            {
                case AntiDebugType.Safe:
                case AntiDebugType.Win32:
                case AntiDebugType.Antinet:
                    cctor.Body.Instructions.RemoveAt(0);
                    return true;
            }

            return false;
        }
    }
}

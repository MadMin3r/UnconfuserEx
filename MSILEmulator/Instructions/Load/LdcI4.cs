using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSILEmulator.Instructions.Load
{
    internal class LdcI4
    {
        public static void Emulate(Context ctx, Instruction instr)
        {
            int val = instr.GetLdcI4Value();

            ctx.Stack.Push(val);
        }
    }
}

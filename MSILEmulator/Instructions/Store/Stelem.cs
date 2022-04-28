using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSILEmulator.Instructions.Store
{
    internal class Stelem
    {
        public static void Emulate(Context ctx, Instruction instr)
        {
            int value = (int)ctx.Stack.Pop();
            int index = (int)ctx.Stack.Pop();
            var array = (int[])ctx.Stack.Pop();

            array[index] = value;
        }
    }
}

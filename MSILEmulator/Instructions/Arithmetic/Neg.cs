using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSILEmulator.Instructions.Arithmetic
{
    internal class Neg
    {
        public static void Emulate(Context ctx)
        {
            int val = (int)ctx.Stack.Pop();

            ctx.Stack.Push(-val);
        }
    }
}

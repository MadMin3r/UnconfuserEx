using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSILEmulator.Instructions.Logic
{
    internal class And
    {
        public static void Emulate(Context ctx)
        {
            int val2 = (int)ctx.Stack.Pop();
            int val1 = (int)ctx.Stack.Pop();

            ctx.Stack.Push(val1 & val2);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSILEmulator.Instructions.Logic
{
    internal class Shl
    {
        public static void Emulate(Context ctx)
        {
            var shift = (int)ctx.Stack.Pop();
            var val = (int)ctx.Stack.Pop();

            ctx.Stack.Push(val << shift);
        }
    }
}

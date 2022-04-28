using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSILEmulator.Instructions.Load
{
    internal class LdcI4_M1
    {
        public static void Emulate(Context ctx)
        {
            ctx.Stack.Push((int)-1);
        }
    }
}

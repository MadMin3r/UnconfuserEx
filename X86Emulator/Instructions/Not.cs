using SharpDisasm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace X86Emulator.Instructions
{
    internal class Not : Instruction
    {
        private Operand Operand;

        public Not(Operand operand)
        {
            Operand = operand;
        }

        public override void Emulate(Stack<int> stack, Registers registers)
        {
            int val = registers.GetValue(Operand.Base);

            val = ~val;

            registers.SetValue(Operand.Base, val);
        }
    }
}

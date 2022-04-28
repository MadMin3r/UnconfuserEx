using SharpDisasm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace X86Emulator.Instructions
{
    internal class Mov : Instruction
    {
        private Operand[] Operands;

        public Mov(Operand[] operands)
        {
            Operands = operands;
        }

        public override void Emulate(Stack<int> stack, Registers registers)
        {
            Operand to = Operands[0];
            Operand from = Operands[1];

            int val;

            if (from.Type == SharpDisasm.Udis86.ud_type.UD_OP_REG)
            {
                val = registers.GetValue(from.Base);
            }
            else
            {
                val = from.LvalSDWord;
            }

            registers.SetValue(to.Base, val);
        }
    }
}

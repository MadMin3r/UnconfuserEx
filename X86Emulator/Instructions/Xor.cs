using SharpDisasm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace X86Emulator.Instructions
{
    internal class Xor : Instruction
    {
        private Operand[] Operands;

        public Xor(Operand[] operands)
        {
            Operands = operands;
        }

        public override void Emulate(Stack<int> stack, Registers registers)
        {
            int dst = registers.GetValue(Operands[0].Base);
            int src;
            if (Operands[1].Type == SharpDisasm.Udis86.ud_type.UD_OP_REG)
            {
                src = registers.GetValue(Operands[1].Base);
            }
            else
            {
                src = Operands[1].LvalSDWord;
            }

            int result = src ^ dst;
            registers.SetValue(Operands[0].Base, result);
        }
    }
}

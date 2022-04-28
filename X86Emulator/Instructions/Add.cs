using SharpDisasm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace X86Emulator.Instructions
{
    internal class Add : Instruction
    {
        private Operand[] Operands;

        public Add(Operand[] operands)
        {
            Operands = operands;
        }

        public override void Emulate(Stack<int> stack, Registers registers)
        {
            int src;
            if (Operands[1].Type == SharpDisasm.Udis86.ud_type.UD_OP_REG)
            {
                src = registers.GetValue(Operands[1].Base);
            }
            else
            {
                src = Operands[1].LvalSDWord;
            }

            int dst;
            if (Operands[0].Type == SharpDisasm.Udis86.ud_type.UD_OP_REG)
            {
                dst = registers.GetValue(Operands[0].Base);
            }
            else
            {
                dst = Operands[0].LvalSDWord;
            }

            int result = dst + src;
            registers.SetValue(Operands[0].Base, result);
        }
    }
}

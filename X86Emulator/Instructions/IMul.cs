using SharpDisasm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace X86Emulator.Instructions
{
    internal class IMul : Instruction
    {
        private Operand[] Operands;

        public IMul(Operand[] operands)
        {
            Operands = operands;
        }

        public override void Emulate(Stack<int> stack, Registers registers)
        {
            if (Operands.Length == 1)
            {
                long eax = registers.GetValue(Registers.Register.EAX);
                long val;
                if (Operands[0].Type == SharpDisasm.Udis86.ud_type.UD_OP_REG)
                {
                    val = registers.GetValue(Operands[0].Base);
                }
                else
                {
                    val = Operands[0].LvalSDWord;
                }

                // Multiply value in eax with operand
                long result = eax * val;

                // Store result in EDX:EAX (top 32 in edx, bottom 32 in EAX)
                registers.SetValue(SharpDisasm.Udis86.ud_type.UD_R_EDX, (int)(result >> 32));
                registers.SetValue(SharpDisasm.Udis86.ud_type.UD_R_EAX, (int)(result));
            }
            else if (Operands.Length == 2)
            {
                // Multiply Operands[0] with Operands[1]
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

                // Store truncated result in Operands[0]
                int result = dst * src;
                registers.SetValue(Operands[0].Base, result);
            }
            else
            {
                // Multiply Operands[1] with Operands[2]
                int val0;
                if (Operands[1].Type == SharpDisasm.Udis86.ud_type.UD_OP_REG)
                {
                    val0 = registers.GetValue(Operands[1].Base);
                }
                else
                {
                    val0 = Operands[1].LvalSDWord;
                }

                int val1;
                if (Operands[2].Type == SharpDisasm.Udis86.ud_type.UD_OP_REG)
                {
                    val1 = registers.GetValue(Operands[2].Base);
                }
                else
                {
                    val1 = Operands[2].LvalSDWord;
                }

                // Store truncated result in Operands[0]
                int result = val0 * val1;
                registers.SetValue(Operands[0].Base, result);
            }
        }
    }
}

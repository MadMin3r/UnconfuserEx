using SharpDisasm.Udis86;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace X86Emulator
{
    internal class Registers
    {
        public int[] Values = new int[(int)Register.MAX];


        public int GetValue(Register register)
        {
            return Values[(int)register];
        }

        public int GetValue(ud_type register)
        {
            return register switch
            {
                ud_type.UD_R_EAX => Values[(int)Register.EAX],
                ud_type.UD_R_ECX => Values[(int)Register.ECX],
                ud_type.UD_R_EDX => Values[(int)Register.EDX],
                ud_type.UD_R_EBX => Values[(int)Register.EBX],
                ud_type.UD_R_ESP => Values[(int)Register.ESP],
                ud_type.UD_R_EBP => Values[(int)Register.EBP],
                ud_type.UD_R_ESI => Values[(int)Register.ESI],
                ud_type.UD_R_EDI => Values[(int)Register.EDI],
                _ => throw new EmulatorException($"Unhandled register type {register}"),
            };
        }

        public void SetValue(ud_type register, int value)
        {
            switch (register)
            {
                case ud_type.UD_R_EAX:
                    Values[(int)Register.EAX] = value;
                    break;
                case ud_type.UD_R_ECX:
                    Values[(int)Register.ECX] = value;
                    break;
                case ud_type.UD_R_EDX:
                    Values[(int)Register.EDX] = value;
                    break;
                case ud_type.UD_R_EBX:
                    Values[(int)Register.EBX] = value;
                    break;
                case ud_type.UD_R_ESP:
                    Values[(int)Register.ESP] = value;
                    break;
                case ud_type.UD_R_EBP:
                    Values[(int)Register.EBP] = value;
                    break;
                case ud_type.UD_R_ESI:
                    Values[(int)Register.ESI] = value;
                    break;
                case ud_type.UD_R_EDI:
                    Values[(int)Register.EDI] = value;
                    break;
                default:
                    throw new EmulatorException($"Unhandled register type {register}");
            };
        }

        public enum Register
        {
            EAX,
            ECX,
            EDX,
            EBX,
            ESP,
            EBP,
            ESI,
            EDI,

            MAX
        }
    }
}

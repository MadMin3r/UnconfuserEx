using dnlib.DotNet;
using dnlib.PE;
using System.Collections.Generic;
using X86Emulator.Instructions;
using System.Linq;
using SharpDisasm.Udis86;

namespace X86Emulator
{
    public class X86Method
    {
        private ModuleDefMD Module;
        private MethodDef Method;
        private List<Instruction> Instructions = new();
        private int NumArgs = 0;
        
        public X86Method(ModuleDefMD module, MethodDef method)
        {
            Module = module;
            Method = method;

            Initialize();
        }

        public int Emulate(int[] args)
        {
            if (args.Length != NumArgs)
            {
                throw new EmulatorException($"Incorrect number of args passed to method. Expected {NumArgs} got {args.Length}");
            }

            var stack = new Stack<int>();
            var registers = new Registers();

            if (args.Length == 1)
            {
                registers.SetValue(ud_type.UD_R_ECX, args[0]);
                stack.Push(args[0]);
            }
            else if (args.Length > 1)
            {
                registers.SetValue(ud_type.UD_R_ECX, args[0]);
                registers.SetValue(ud_type.UD_R_EDX, args[1]);
                foreach (var arg in args.Skip(2))
                {
                    stack.Push(arg);
                }
            }

            foreach (var instr in Instructions)
            {
                instr.Emulate(stack, registers);
            }

            return registers.GetValue(Registers.Register.EAX);
        }

        private void Initialize()
        {
            NumArgs = Method.Parameters.Count;
            var body = ReadNativeBody();

            var disassembler = new SharpDisasm.Disassembler(body, SharpDisasm.ArchitectureMode.x86_32);
            var instructions = disassembler.Disassemble();

            foreach (var instr in instructions)
            {
                if (instr.Mnemonic == ud_mnemonic_code.UD_Iret)
                {
                    // Need to remove the final pops before a ret
                    Instructions.RemoveRange(Instructions.Count - 3, 3);
                    break;
                }

                switch (instr.Mnemonic)
                {
                    case ud_mnemonic_code.UD_Ipush:
                        Instructions.Add(new Push(instr.Operands[0]));
                        break;
                    case ud_mnemonic_code.UD_Ipop:
                        Instructions.Add(new Pop(instr.Operands.Length == 0 ? null : instr.Operands[0]));
                        break;
                    case ud_mnemonic_code.UD_Imov:
                        Instructions.Add(new Mov(instr.Operands));
                        break;
                    case ud_mnemonic_code.UD_Inot:
                        Instructions.Add(new Not(instr.Operands[0]));
                        break;
                    case ud_mnemonic_code.UD_Iimul:
                        Instructions.Add(new IMul(instr.Operands));
                        break;
                    case ud_mnemonic_code.UD_Ineg:
                        Instructions.Add(new Neg(instr.Operands[0]));
                        break;
                    case ud_mnemonic_code.UD_Ixor:
                        Instructions.Add(new Xor(instr.Operands));
                        break;
                    case ud_mnemonic_code.UD_Isub:
                        Instructions.Add(new Sub(instr.Operands));
                        break;
                    case ud_mnemonic_code.UD_Iadd:
                        Instructions.Add(new Add(instr.Operands));
                        break;


                    case ud_mnemonic_code.UD_Inop:
                        // no-op ...
                        break;


                    default:
                        throw new EmulatorException($"Unhandled instruction type {instr.Mnemonic}");
                }
            }

        }

        private byte[] ReadNativeBody()
        {
            var reader = Module.Metadata.PEImage.CreateReader();
            var fileOffset = Module.Metadata.PEImage.ToFileOffset(Method.NativeBody.RVA) + 0x14;
            reader.Position = (uint)fileOffset;

            Module.TablesStream.TryReadMethodRow(Method.Rid + 1, out var nextMethod);
            var size = Module.Metadata.PEImage.ToFileOffset((RVA)nextMethod.RVA) - fileOffset;
            var bytes = ((nextMethod.ImplFlags & (ushort)MethodImplAttributes.Native) != 0) ? new byte[size] : new byte[2048];


            reader.ReadBytes(bytes, 0, bytes.Length);
            return bytes;
        }
    }
}
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using MSILEmulator.Instructions.Arithmetic;
using MSILEmulator.Instructions.Load;
using MSILEmulator.Instructions.Logic;
using MSILEmulator.Instructions.Store;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MSILEmulator
{
    public class ILMethod
    {
        private MethodDef? Method;
        private List<Instruction> Instructions;
        private Context Ctx;

        public ILMethod(MethodDef method)
        {
            Method = method;
            Instructions = Method.Body.Instructions.ToList();
            Ctx = new();
        }

        public ILMethod(MethodDef method, int start, int end)
        {
            Method = method;
            Instructions = Method.Body.Instructions.Skip(start).Take(end - start).ToList();
            Ctx = new();
        }

        public ILMethod(IEnumerable<Instruction> instructions)
        {
            Method = null;
            Instructions = instructions.ToList();
            Ctx = new();
        }

        public void SetArg(int index, object value)
        {
            Ctx.Args[index] = value;
        }

        public void SetLocal(int index, object value)
        {
            Ctx.Locals[index] = value;
        }

        public Context Emulate()
        {

            for (int i = 0; i < Instructions.Count; )
            {
                var instr = Instructions[i];

                i = EmulateInstruction(Ctx, instr, i);
            }

            return Ctx;
        }

        private int EmulateInstruction(Context ctx, Instruction instr, int index)
        {

            switch (instr.OpCode.Code)
            {

                // Load
                case Code.Ldc_I4:
                case Code.Ldc_I4_0:
                case Code.Ldc_I4_1:
                case Code.Ldc_I4_2:
                case Code.Ldc_I4_3:
                case Code.Ldc_I4_4:
                case Code.Ldc_I4_5:
                case Code.Ldc_I4_6:
                case Code.Ldc_I4_7:
                case Code.Ldc_I4_8:
                case Code.Ldc_I4_S:
                    LdcI4.Emulate(ctx, instr);
                    break;
                case Code.Ldc_I4_M1:
                    LdcI4_M1.Emulate(ctx);
                    break;

                case Code.Ldarg:
                case Code.Ldarg_0:
                case Code.Ldarg_1:
                case Code.Ldarg_2:
                case Code.Ldarg_3:
                case Code.Ldarg_S:
                    Ldarg.Emulate(ctx, instr);
                    break;

                case Code.Ldloc:
                case Code.Ldloc_0:
                case Code.Ldloc_1:
                case Code.Ldloc_2:
                case Code.Ldloc_3:
                case Code.Ldloc_S:
                    Ldloc.Emulate(ctx, instr);
                    break;

                case Code.Ldelem_U4:
                    Ldelem.EmulateU4(ctx, instr);
                    break;

                // Store
                case Code.Stloc:
                case Code.Stloc_0:
                case Code.Stloc_1:
                case Code.Stloc_2:
                case Code.Stloc_3:
                case Code.Stloc_S:
                    Stloc.Emulate(ctx, instr);
                    break;

                case Code.Stelem_I4:
                    Stelem.Emulate(ctx, instr);
                    break;

                case Code.Stfld:
                    // TODO: Keep fields in ctx in case they get referenced again?
                    //       For now just treat it as a no-op
                    break;

                // Arithmetic operators
                case Code.Add:
                    Add.Emulate(ctx);
                    break;
                case Code.Mul:
                    Mul.Emulate(ctx);
                    break;
                case Code.Neg:
                    Neg.Emulate(ctx);
                    break;
                case Code.Or:
                    Or.Emulate(ctx);
                    break;
                case Code.Sub:
                    Sub.Emulate(ctx);
                    break;

                // Logic operators
                case Code.And:
                    And.Emulate(ctx);
                    break;
                case Code.Not:
                    Not.Emulate(ctx);
                    break;
                case Code.Shl:
                    Shl.Emulate(ctx);
                    break;
                case Code.Shr:
                    Shr.Emulate(ctx);
                    break;
                case Code.Shr_Un:
                    Shr_Un.Emulate(ctx);
                    break;
                case Code.Xor:
                    Xor.Emulate(ctx);
                    break;

                default:
                    throw new EmulatorException($"Unhandled OpCode {instr.OpCode}");
            }

            return ++index;
        }

    }
}

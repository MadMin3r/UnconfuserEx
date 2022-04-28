using de4dot.blocks;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnConfuserEx.Protections.Constants
{
    internal class ConstantsCFG
    {
        private static TypeDef? CFGCtxStruct = null;

        private MethodDef Method;
        private Blocks @Blocks;
        private IList<Local> Locals;
        private Local CtxLocal;
        private HashSet<BaseBlock> Visited = new();

        public ConstantsCFG(MethodDef method)
        {
            Method = method;
            Locals = method.Body.Variables.Locals;
            CtxLocal = method.Body.Instructions[0].GetLocal(Locals);
            @Blocks = new Blocks(Method);
        }

        public void RemoveFromMethod()
        {
            RemoveFromBlock(@Blocks.MethodBlocks.GetAllBlocks()[0], new CFGCtx(0));

            IList<Instruction> instructions;
            IList<ExceptionHandler> exceptionHandlers;
            @Blocks.GetCode(out instructions, out exceptionHandlers);
            DotNetUtils.RestoreBody(Method, instructions, exceptionHandlers);
        }

        private void RemoveFromBlock(Block block, CFGCtx curCtx)
        {
            if (Visited.Contains(block))
            {
                return;
            }
            Visited.Add(block);

            List<Instr> instrs = block.Instructions;
            // Iterate through methods and find calls to next
            // Only replace with an ldc.i4 if the instruction after the call to next
            // is NOT a pop
            for (int i = 0; i < instrs.Count - 2; i++)
            {
                if (instrs[i].OpCode == OpCodes.Ldloca_S &&
                    instrs[i].Instruction.GetLocal(Locals) == CtxLocal)
                {
                    // This is most likely a call to the Rand method,
                    // but could be another constructor call
                    if (instrs[i + 1].IsLdcI4() &&
                        instrs[i + 2].IsLdcI4())
                    {
                        // Call to Rand
                        var f = (byte)instrs[i + 1].GetLdcI4Value();
                        var q = (uint)instrs[i + 2].GetLdcI4Value();

                        var rand = curCtx.Rand(f, q);
                        if (instrs[i + 4].OpCode != OpCodes.Pop)
                        {
                            // This is a value that's actually going to be used
                            // The instruction before should be an ldc.i4 and after should be a xor
                            if (instrs[i - 1].IsLdcI4() &&
                                instrs[i + 4].OpCode == OpCodes.Xor)
                            {
                                var xorVal = (uint)instrs[i - 1].GetLdcI4Value();
                                var key = (int)(xorVal ^ rand);
                                instrs.RemoveRange(i - 1, 6);
                                instrs.Insert(i - 1, new Instr(Instruction.CreateLdcI4(key)));
                            }
                        }
                        else
                        {
                            // This call to rand is just for switching up the values in the ctx
                            instrs.RemoveRange(i, 5);
                            i--;
                        }
                    }
                    else
                    {
                        // Call to constructor
                        var seed = (uint)instrs[i + 1].GetLdcI4Value();
                        curCtx = new CFGCtx(seed);

                        instrs.RemoveRange(i, 3);
                        i--;
                    }
                }
            }

            if (block.FallThrough != null)
            {
                RemoveFromBlock(block.FallThrough, new CFGCtx(curCtx));
            }

            if (block.FallThrough == null)
            {
                if (block.Parent is TryBlock tryBlock &&
                    block.LastInstr.IsLeave())
                {
                    RemoveFromBlock(tryBlock.TryHandlerBlocks[0].GetAllBlocks()[0], new CFGCtx(curCtx));
                }
                else if (block.Parent is HandlerBlock handlerBlock &&
                        block.LastInstr.OpCode == OpCodes.Endfinally)
                {
                    var index = Blocks.MethodBlocks.GetAllBlocks().IndexOf(block);
                    RemoveFromBlock(Blocks.MethodBlocks.GetAllBlocks()[index + 1], new CFGCtx(curCtx));
                }
                else if (block.Parent is FilterHandlerBlock filterHandlerBlock &&
                        block.LastInstr.OpCode == OpCodes.Endfilter)
                {
                    var index = Blocks.MethodBlocks.GetAllBlocks().IndexOf(block);
                    RemoveFromBlock(Blocks.MethodBlocks.GetAllBlocks()[index + 1], new CFGCtx(curCtx));
                }
                else if (block.LastInstr.OpCode != OpCodes.Ret)
                {

                }
            }

            if (block.Targets != null)
            {
                foreach (var target in block.Targets)
                {
                    RemoveFromBlock(target, new CFGCtx(curCtx));
                }
            }
        }

        public static bool IsCFGPresent(MethodDef method)
        {
            var instrs = method.Body.Instructions;

            // Constants CFG will always create the CFG context at the start of a method
            // Signature:
            // ldloca.s
            // ldc.i4 <seed>
            // call ctor

            if (instrs.Count > 3 &&
                instrs[0].OpCode == OpCodes.Ldloca_S &&
                instrs[1].IsLdcI4() &&
                instrs[2].OpCode == OpCodes.Call)
            {
                var ctor = instrs[2].Operand as MethodDef;
                
                if (ctor == null)
                {
                    return false;
                }

                if (CFGCtxStruct != null &&
                    ctor.DeclaringType == CFGCtxStruct)
                {
                    return true;
                }

                // Not sure if we can always guarantee the parameter will be named seed
                // so if this fails can try and get a more general method
                if (ctor.Name == ".ctor" &&
                    ctor.ParamDefs.Count == 1 &&
                    ctor.ParamDefs[0].Name == "seed")
                {
                    CFGCtxStruct = ctor.DeclaringType;
                    return true;
                }
                else if (ctor.Signature.ToString() == "System.Void (System.UInt32)")
                {
                    CFGCtxStruct = ctor.DeclaringType;
                    return true;
                }
            }

            return false;

        }

        private struct CFGCtx 
        {
            public uint a; 
            public uint b;
            public uint c;
            public uint d;

            public CFGCtx(uint seed)
            {
                seed = (a = seed * 0x21412321U);
                seed = (b = seed * 0x21412321U);
                seed = (c = seed * 0x21412321U);
                d = seed * 0x21412321U;
            }

            public CFGCtx(CFGCtx other)
            {
                a = other.a;
                b = other.b;
                c = other.c;
                d = other.d;
            }

            public uint Rand(byte f, uint q)
            {
                if ((f & 0x80) != 0)
                {
                    switch (f & 3)
                    {
                        case 0:
                            a = q;
                            break;
                        case 1:
                            b = q;
                            break;
                        case 2:
                            c = q;
                            break;
                        case 3:
                            d = q;
                            break;
                    }
                }
                else
                {
                    switch (f & 3)
                    {
                        case 0:
                            a ^= q;
                            break;
                        case 1:
                            b += q;
                            break;
                        case 2:
                            c ^= q;
                            break;
                        case 3:
                            d -= q;
                            break;
                    }
                }
                switch ((f >> 2) & 3)
                {
                    case 0:
                        return a;
                    case 1:
                        return b;
                    case 2:
                        return c;
                    default:
                        return d;
                }
            }

            public override string ToString()
            {
                return $"a: 0x{a:X8}, b: 0x{b:X8}, c: 0x{c:X8}, d: 0x{d:X8}";
            }

        }
    }
}

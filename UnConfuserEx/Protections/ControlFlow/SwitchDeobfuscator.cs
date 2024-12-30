using de4dot.blocks;
using de4dot.blocks.cflow;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using log4net;
using MSILEmulator;
using System.Collections.Generic;
using System.Linq;
using X86Emulator;

namespace UnConfuserEx.Protections.ControlFlow
{
    internal class SwitchDeobfuscator : BlockDeobfuscator
    {
        static ILog Logger = LogManager.GetLogger("ControlFlow");

        private ModuleDefMD Module;
        private List<Local> locals = new();
        private Block? switchBlock = null;
        private List<Block> switchCases = new();
        private Local? switchLocal = null;

        private int methodOffset = -1;
        private X86Method? x86Method = null;

        private ILMethod? ilMethod = null;
        private int? keyIndex = null;

        public SwitchDeobfuscator(ModuleDefMD module)
        {
            Module = module;
        }

        protected override bool Deobfuscate(Block block)
        {
            bool modified = false;

            if (block.LastInstr.OpCode == OpCodes.Switch && IsSwitchBlock(block))
            {
                modified = DeobfuscateSwitchBlock();
            }
            return modified;
        }

        private bool IsSwitchBlock(Block curBlock)
        {
            var InstrToBlock = new Dictionary<Instruction, Block>();
            foreach (var block in allBlocks)
            {
                foreach (var instr in block.Instructions.Select(i => i.Instruction))
                {
                    InstrToBlock[instr] = block;
                }
            }

            var instrs = curBlock.Instructions;
            for (int i = 0; i < instrs.Count; i++)
            {
                if (instrs[i].OpCode == OpCodes.Switch &&
                    instrs[i - 1].OpCode == OpCodes.Rem_Un &&
                    instrs[i - 2].IsLdcI4() &&
                    instrs[i].Operand is Instruction[] cases &&
                    cases.Length == instrs[i - 2].GetLdcI4Value())
                {
                    locals = blocks.Method.Body.Variables.Locals.ToList();
                    switchBlock = curBlock;
                    switchCases = switchBlock.Targets;
                    switchLocal = instrs[^4].Instruction.GetLocal(locals);

                    if (instrs[i - 5].OpCode == OpCodes.Call)
                    {
                        methodOffset = i - 5;
                        x86Method = new X86Method(Module, (MethodDef)instrs[i - 5].Operand);
                    }
                    else if (!switchBlock.FirstInstr.Instruction.IsLdcI4())
                    {
                        var decodeInstrsLength = switchBlock.Instructions.Count - 6;

                        ilMethod = new ILMethod(switchBlock.Instructions
                            .Skip(1)
                            .Take(decodeInstrsLength)
                            .Select(instr => instr.Instruction));
                        keyIndex = Utils.GetStoreLocalIndex(switchBlock.FirstInstr.Instruction);
                    }

                    return true;
                }
            }

            return false;
        }

        private bool DeobfuscateSwitchBlock()
        {
            bool ret = false;
            int processedCount = 0;

            List<Block> curBlocks = new();
            foreach (var block in allBlocks)
            {
                if (block.FallThrough == switchBlock)
                {
                    curBlocks.Add(block);
                }
            }

            bool modified;
            do
            {
                modified = false;

                
                // Sometimes with the x86 predicate, the ldc.i4 going into the switch block
                // doesn't get split into a separate block which can cause issues

                // TODO: Not entirely convinced this solution won't cause issues...
                if (x86Method != null && switchBlock!.FirstInstr.IsLdcI4())
                {
                    var key = switchBlock.FirstInstr.GetLdcI4Value();
                    var (nextKey, nextCase) = GetNextKeyAndCase(key);

                    var nextBlock = switchCases[nextCase];
                    UpdateKeyInBlock(nextBlock, nextKey);

                    switchBlock.Sources[0].SetNewFallThrough(nextBlock);
                    switchBlock.Instructions[0] = new Instr(Instruction.Create(OpCodes.Nop));

                    modified = true;

                }

                foreach (var block in curBlocks)
                {
                    if (block.LastInstr.IsLdcI4())
                    {
                        var key = block.LastInstr.GetLdcI4Value();
                        var (nextKey, nextCase) = GetNextKeyAndCase(key);

                        var nextBlock = switchCases[nextCase];
                        UpdateKeyInBlock(nextBlock, nextKey);

                        block.ReplaceLastInstrsWithBranch(1, nextBlock);

                        modified = true;
                        processedCount++;
                    }
                    else if (block.Instructions.Count >= 5 &&
                             block.Instructions[^1].OpCode == OpCodes.Xor &&
                             block.Instructions[^2].IsLdcI4() &&
                             block.Instructions[^4].IsLdcI4())
                    {
                        if (!block.Instructions[^5].IsLdcI4())
                        {
                            continue;
                        }
                        var key = block.Instructions[^5].GetLdcI4Value();
                        var mulVal = block.Instructions[^4].GetLdcI4Value();
                        var xorVal = block.Instructions[^2].GetLdcI4Value();

                        var switchVal = (key * mulVal) ^ xorVal;
                        var (nextKey, nextCase) = GetNextKeyAndCase(switchVal);

                        var nextBlock = switchCases[nextCase];
                        UpdateKeyInBlock(nextBlock, nextKey);

                        block.ReplaceLastInstrsWithBranch(5, nextBlock);

                        modified = true;
                        processedCount++;
                    }
                    else if (block.Sources.Count == 2 &&
                             block.Instructions.Count == 5 &&
                             block.Instructions[2].IsLdcI4() &&
                             block.LastInstr.OpCode == OpCodes.Xor)
                    {
                        if (!block.Instructions[1].IsLdcI4())
                        {
                            continue;
                        }
                        var key = block.Instructions[1].GetLdcI4Value();
                        var mulVal = block.Instructions[2].GetLdcI4Value();

                        var sources = new List<Block>(block.Sources);
                        foreach (var source in sources)
                        {
                            var xorVal = source.FirstInstr.GetLdcI4Value();

                            var switchVal = (key * mulVal) ^ xorVal;
                            var (nextKey, nextCase) = GetNextKeyAndCase(switchVal);

                            var nextBlock = switchCases[nextCase];
                            UpdateKeyInBlock(nextBlock, nextKey);

                            source.ReplaceLastInstrsWithBranch(source.Instructions.Count, nextBlock);
                        }

                        modified = true;
                        processedCount++;
                    }
                    else if (block.Sources.Count == 2 &&
                             block.Instructions.Count == 1 &&
                             block.Instructions[0].OpCode == OpCodes.Pop)
                    {
                        var sources = new List<Block>(block.Sources);
                        foreach (var source in sources)
                        {
                            var key = source.FirstInstr.GetLdcI4Value();

                            var (nextKey, nextCase) = GetNextKeyAndCase(key);

                            var nextBlock = switchCases[nextCase];
                            UpdateKeyInBlock(nextBlock, nextKey);

                            source.ReplaceLastInstrsWithBranch(source.Instructions.Count, nextBlock);
                        }

                        modified = true;
                        processedCount++;
                    }
                }

                if (modified)
                {
                    ret = true;
                }

            } while (modified);

            if (processedCount != curBlocks.Count)
            {
                Logger.Warn($"Not all obfuscated blocks were processed! Only processed {processedCount} out of {curBlocks.Count}");
            }

            return ret;
        }

        private (int nextKey, int nextCase) GetNextKeyAndCase(int key)
        {
            // x86 predicate
            if (x86Method != null)
            {
                var nextKey = x86Method.Emulate(new int[] { key });
                return (nextKey, nextKey % switchCases.Count);
            }
            // Expression Predicate
            else if (ilMethod != null)
            {
                ilMethod.SetLocal((int)keyIndex!, key);

                var nextKey = (int)ilMethod.Emulate().Stack.Pop();
                return (nextKey, nextKey % switchCases.Count);
            }
            // Normal predicate
            else
            {
                var xorKey = switchBlock!.FirstInstr.GetLdcI4Value();
                var nextKey = key ^ xorKey;
                return (nextKey, nextKey % switchCases.Count);
            }
        }

        private void UpdateKeyInBlock(Block block, int key)
        {
            if (block.IsConditionalBranch())
            {
                if (block.FallThrough == null || block.FallThrough.FallThrough == null)
                {
                    return;
                }

                if (block.FallThrough.FallThrough == switchBlock)
                {
                    block = block.FallThrough;
                }
                else
                {
                    block = block.FallThrough.FallThrough;
                }
            }

            if (block.LastInstr.OpCode == OpCodes.Switch)
            {
                UpdateKeyInBlock(block.FallThrough, key);
                return;
            }

            for (int i = 0; i < block.Instructions.Count; i++)
            {
                var instr = block.Instructions[i];
                if (instr.IsLdloc() && instr.Instruction.GetLocal(locals) == switchLocal)
                {
                    block.Replace(i, 1, Instruction.CreateLdcI4(key));
                    return;
                }
            }

        }

    }
}

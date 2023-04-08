using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using log4net;
using UnConfuserEx.Protections.ControlFlow;
using de4dot.blocks;
using de4dot.blocks.cflow;

namespace UnConfuserEx.Protections
{
    internal class ControlFlowRemover : IProtection
    {
        static ILog Logger = LogManager.GetLogger("ControlFlow");

        public string Name => "ControlFlow";

        private readonly IList<MethodDef> ObfuscatedMethods = new List<MethodDef>();

        public bool IsPresent(ref ModuleDefMD module)
        {
            /*
             * Go through all of the methods in the module
             * if they all contain a switch then it's present
             */
            foreach (var method in module.GetTypes().SelectMany(t => t.Methods))
            {
                if (IsMethodObfuscated(method))
                {
                    ObfuscatedMethods.Add(method);
                }
            }

            return ObfuscatedMethods.Any();
        }

        public bool Remove(ref ModuleDefMD module)
        {
            int numSolved = 0;
            int numFailed = 0;

            Logger.Debug($"Found {ObfuscatedMethods.Count} obfuscated methods");
            foreach (var method in ObfuscatedMethods)
            {
                try
                {
                    Logger.Debug($"Removing obfuscation from method {method.FullName}");

                    var deobfuscator = new BlocksCflowDeobfuscator();
                    var blocks = new Blocks(method);
                    blocks.RemoveDeadBlocks();
                    blocks.RepartitionBlocks();
                    blocks.UpdateBlocks();

                    blocks.Method.Body.SimplifyBranches();
                    blocks.Method.Body.OptimizeBranches();
                    
                    deobfuscator.Initialize(blocks);
                    deobfuscator.Add(new SwitchDeobfuscator(module));
                    deobfuscator.Deobfuscate();

                    blocks.RepartitionBlocks();

                    IList<Instruction> instructions;
                    IList<ExceptionHandler> exceptionHandlers;
                    blocks.GetCode(out instructions, out exceptionHandlers);
                    DotNetUtils.RestoreBody(method, instructions, exceptionHandlers);

                    if (IsMethodObfuscated(method))
                    {
                        throw new Exception("Method still obfuscated after deobfuscation");
                    }

                    numSolved++;
                }
                catch (Exception e)
                {
                    Logger.Error($"Failed to remove obfuscation for method {method.FullName} ({e.Message})");
                    Logger.Error(e.StackTrace);
                    numFailed++;
                }
            }

            var msg = $"Removed obfuscation from {numSolved} methods. Failed to remove from {numFailed} methods";
            if (numFailed > 0)
            {
                Logger.Error(msg);
            }
            else
            {
                Logger.Debug(msg);
            }
            return true;
        }

        private static bool IsMethodObfuscated(MethodDef method)
        {
            if (!method.HasBody || method.Body.Instructions.Count == 0)
                return false;


            return IsSwitchObfuscation(method.Body.Instructions.ToList());
        }

        public static bool IsSwitchObfuscation(List<Instruction> instrs)
        {
            if (instrs.Count < 3)
            {
                return false;
            }

            for (int i = 0; i < instrs.Count; i++)
            {
                if (instrs[i].OpCode == OpCodes.Switch
                    && instrs[i - 1].OpCode == OpCodes.Rem_Un
                    && instrs[i - 2].IsLdcI4()
                    && instrs[i].Operand is Instruction[] cases
                    && cases.Length == instrs[i - 2].GetLdcI4Value())
                {
                    return true;
                }
            }
            return false;
        }
    }
}

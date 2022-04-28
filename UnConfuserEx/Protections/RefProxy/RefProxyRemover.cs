using de4dot.blocks.cflow;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.MD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnConfuserEx.Protections.Delegates
{
    internal class RefProxyRemover : IProtection
    {
        public string Name => "RefProxy";

        private ModuleDefMD? Module;
        private List<MethodDef> HandlerMethods = new();
        private HashSet<TypeDef> Delegates = new();
        private HashSet<MethodDef> DelegateCtors = new();
        private Dictionary<MethodDef, RefProxyHandler> DelegateHandlers = new();
        private Dictionary<FieldDef, Tuple<OpCode, IMethodDefOrRef>> ResolvedDelegates = new();

        public bool IsPresent(ref ModuleDefMD module)
        {
            Module = module;

            // Check in the default module for methods with the signature
            // static void SMethod1(RuntimeFieldHandle field, byte opKey)

            foreach (var method in Module.GlobalType.Methods)
            {
                if (method.MethodSig.ToString() == "System.Void (System.RuntimeFieldHandle,System.Byte)")
                {
                    HandlerMethods.Add(method);
                }
            }

            return HandlerMethods.Any();
        }

        public bool Remove(ref ModuleDefMD module)
        {
            foreach (var handler in HandlerMethods)
            {
                var instances = GetAllInstances(handler);

                Delegates.UnionWith(instances.Select(instance => instance.DeclaringType));
                DelegateCtors.UnionWith(instances);

                var delegateHandler = new RefProxyHandler(module, handler);
                DelegateHandlers[handler] = delegateHandler;
            }

            ResolveAllDelegates();
            ReplaceDelegateInvocations();

            RemoveHandlers();
            RemoveDelegateClasses();

            return true;
        }

        private HashSet<MethodDef> GetAllInstances(MethodDef delegateHandler)
        {
            var placesUsed = new HashSet<MethodDef>();

            foreach (var method in Module!.GetTypes().SelectMany(m => m.Methods))
            {
                if (!method.HasBody)
                    continue;

                foreach (var instr in method.Body.Instructions)
                {
                    if (instr.OpCode == OpCodes.Call && instr.Operand is MethodDef called)
                    {
                        if (called == delegateHandler)
                        {
                            placesUsed.Add(method);
                        }
                    }
                }
            }

            return placesUsed;
        }

        private void ResolveAllDelegates()
        {
            foreach (var @delegate in DelegateCtors)
            {
                for (int i = 0; i < @delegate.Body.Instructions.Count - 2; i += 3)
                {
                    var field = (FieldDef)@delegate.Body.Instructions[i].Operand;
                    var opKey = (byte)@delegate.Body.Instructions[i + 1].GetLdcI4Value();
                    var handler = (MethodDef)@delegate.Body.Instructions[i + 2].Operand;

                    var token = DelegateHandlers[handler].GetMethodMDToken(field);
                    var opCode = DelegateHandlers[handler].GetOpCode(field, opKey);

                    if (token.Table == Table.MemberRef)
                    {
                        var method = Module!.ResolveMemberRef(token.Rid);
                        ResolvedDelegates[field] = new(opCode, method);
                    }
                    else if (token.Table == Table.Method)
                    {
                        var method = Module!.ResolveMethod(token.Rid);
                        ResolvedDelegates[field] = new(opCode, method);
                    }
                    else
                    {
                        throw new NotImplementedException($"Unhandled token type: {token.Table}");
                    }
                }
            }
        }

        private void ReplaceDelegateInvocations()
        {
            var fieldStack = new Stack<FieldDef>();
            foreach (var method in Module!.GetTypes().SelectMany(m => m.Methods).Where(m => m.HasBody))
            {
                var instrsToRemove = new List<int>();

                var instrs = method.Body.Instructions;
                for (int i = 0; i < instrs.Count; i++)
                {
                    var instr = instrs[i];
                    if (instr.OpCode == OpCodes.Ldsfld &&
                        instr.Operand is FieldDef f &&
                        ResolvedDelegates.ContainsKey(f))
                    {
                        instrsToRemove.Add(i);
                        fieldStack.Push(f);
                    }
                    else if (instr.OpCode == OpCodes.Call &&
                        instr.Operand is MethodDef m)
                    {
                        // Normal delegate invocation
                        if (fieldStack.Count > 0 &&
                            m.DeclaringType == fieldStack.Peek().DeclaringType)
                        {
                            var field = fieldStack.Pop();
                            var (opCode, resolvedMethod) = ResolvedDelegates[field];

                            instr.OpCode = opCode;
                            instr.Operand = resolvedMethod;
                        }
                        // Static method inside one of the MulticastDelegates
                        else if (Delegates.Contains(m.DeclaringType))
                        {
                            var staticInvoke = (MethodDef)instr.Operand;
                            var invokeInstrs = staticInvoke.Body.Instructions;

                            // Two possibilities:
                            // This static method has not been fixed
                            if (invokeInstrs[0].OpCode == OpCodes.Ldsfld)
                            {
                                var field = (FieldDef)invokeInstrs[0].Operand;
                                var (opCode, resolvedMethod) = ResolvedDelegates[field];

                                instr.OpCode = opCode;
                                instr.Operand = resolvedMethod;
                            }
                            // This static method has already been fixed
                            else
                            {
                                var invokeInstr = staticInvoke.Body.Instructions[staticInvoke.Parameters.Count];

                                instrs[i].OpCode = invokeInstr.OpCode;
                                instrs[i].Operand = invokeInstr.Operand;
                            }
                        }

                    }
                }

                if (instrsToRemove.Count > 0)
                {
                    if (fieldStack.Count > 0)
                    {
                        throw new Exception("Delegate Field stack not empty!");
                    }

                    instrsToRemove.Reverse();

                    foreach (var instrIndex in instrsToRemove)
                    {
                        instrs[instrIndex].OpCode = instrs[instrIndex + 1].OpCode;
                        instrs[instrIndex].Operand = instrs[instrIndex + 1].Operand;

                        instrs.RemoveAt(instrIndex + 1);
                    }
                }
            }
        }

        private void RemoveHandlers()
        {
            foreach (var handler in HandlerMethods)
            {
                handler.DeclaringType.Methods.Remove(handler);
            }
        }

        private void RemoveDelegateClasses()
        {
            foreach (var type in DelegateCtors.Select(ctor => ctor.DeclaringType))
            {
                Module!.Types.Remove(type);
            }
        }

    }
}

namespace TorchLiteInstrumenter
{
    using System.Collections.Generic;
    using System.Linq;
    using Mono.Cecil;
    using Mono.Cecil.Cil;
    using Mono.Cecil.Rocks;
    using System;
    /// <summary>
    /// Instrumenter for instrumenting async code.
    /// </summary>
    public class AsyncInstrumenter : IInstrumenter
    {
        /// <inheritdoc/>
        public bool Instrument(IEnumerable<MethodDefinition> methods)
        {
            bool instrumented = false;
            foreach (var method in methods)
            {
                instrumented |= this.Instrument(method);
            }

            return instrumented;
        }

        /// <summary>
        /// Instrument a method so that "Call System.Runtime.CompilerServices*::get_IsCompleted()" alwasy returns false.
        /// </summary>
        /// <param name="method">Method definition.</param>
        /// <returns>True, if the method is instrumented.</returns>
        public bool Instrument(MethodDefinition method)
        {
            bool instrumented = false;
            var processor = method.Body.GetILProcessor();
            method.Body.SimplifyMacros();
            method.Body.InitLocals = true;

            foreach (var instruction in method.Body.Instructions.ToList())
            {
                if (instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt)
                {
                    var calleeRef = (MethodReference)instruction.Operand;
                    var calleeName = $"{calleeRef.DeclaringType.FullName}::{calleeRef.Name}";

                    if (Constants.MethodPrefixBlackList.Any(x => calleeName.StartsWith(x)))
                    {
                        continue;
                    }

                    var isGetIsCompleted = calleeRef.Name == "get_IsCompleted"
                        && instruction.ToString().Contains("System.Runtime.CompilerServices");

                    if (isGetIsCompleted)
                    {
                        var patch = new List<Instruction>()
                        {
                            processor.Create(OpCodes.Pop),
                            processor.Create(OpCodes.Ldc_I4_0),
                        };
                        processor.InsertAfter(instruction, patch);
                        instrumented = true;
                        Console.WriteLine("Enfore an async function");
                    }
                }
            }

            method.Body.OptimizeMacros();
            return instrumented;
        }
    }
}

namespace TorchLiteInstrumenter
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using Mono.Cecil;
    using Mono.Cecil.Cil;
    using Mono.Cecil.Rocks;

    /// <summary>
    /// Instruments code to track field accesses.
    /// The instrumentation logic is simple, but it uses two local variables for each access.
    /// </summary>
    internal class InstrumenterV2 : IInstrumenter
    {
        private readonly MethodReference beforeFieldWriteCallbackRef;
        private readonly MethodReference afterFieldWriteCallbackRef;
        private readonly MethodReference beforeFieldReadCallbackRef;
        private readonly MethodReference beforeMethodCallCallbackRef;
        private readonly MethodReference AfterInstructionCallbackRef;

        private readonly TypeReference objectType;

        private readonly string callbackTypeName = "CallbacksV2";
        private readonly string beforeFieldReadCallbackName = "BeforeFieldRead";
        private readonly string beforeFieldWriteCallbackName = "BeforeFieldWrite";
        private readonly string afterFieldWriteCallbackName = "AfterFieldWrite";
        private readonly string beforeMethodCallCallbackName = "BeforeMethodCall";
        private readonly string AfterInstructionCallbackName = "AfterInstructionCall";

        public InstrumenterV2(ModuleDefinition moduleToBeInstrumented, ModuleDefinition callbackModule)
        {
            var fieldTrackerCallbackType = callbackModule.Types.Single(t => t.Name == this.callbackTypeName);

            this.beforeFieldWriteCallbackRef = moduleToBeInstrumented.ImportReference(
                fieldTrackerCallbackType.Methods.Single(x => x.Name == this.beforeFieldWriteCallbackName).Resolve());
            // this.afterFieldWriteCallbackRef = moduleToBeInstrumented.ImportReference(
            //    fieldTrackerCallbackType.Methods.Single(x => x.Name == this.afterFieldWriteCallbackName).Resolve());
            this.beforeFieldReadCallbackRef = moduleToBeInstrumented.ImportReference(
                fieldTrackerCallbackType.Methods.Single(x => x.Name == this.beforeFieldReadCallbackName).Resolve());
            this.beforeMethodCallCallbackRef = moduleToBeInstrumented.ImportReference(
                fieldTrackerCallbackType.Methods.Single(x => x.Name == this.beforeMethodCallCallbackName).Resolve());
            this.AfterInstructionCallbackRef = moduleToBeInstrumented.ImportReference(
                fieldTrackerCallbackType.Methods.Single(x => x.Name == this.AfterInstructionCallbackName).Resolve());
            this.objectType = moduleToBeInstrumented.ImportReference(typeof(object));
        }

        public bool Instrument(IEnumerable<MethodDefinition> methods)
        {
            bool instrumented = false;
            foreach (var method in methods)
            {
                instrumented |= this.Instrument(method);
            }

            return instrumented;
        }

        public bool Instrument(MethodDefinition method)
        {
            // Console.WriteLine("processing function body of " + method.FullName);
            // do not instrument compiler generated methods
            if (method.FullName.Contains("System.Void Radical.Messaging.MessageBroker::Subscribe(")
                || method.FullName.Contains("Radical.Validation.ComparableEnsureExtension::IsGreaterThen")
                || method.FullName.Contains("Radical.Conversions.CastExtensions::As(")
                || method.FullName.Contains("System.RuntimeMethodHandle.InvokeMethod(")
                // || method.FullName.Contains("MahApps.Metro.Tests.TestHelpers.WindowHelpers.CreateInvisibleWindowAsync")
                || method.FullName.Contains("MahApps.Metro.Tests.TestHelpers.WindowHelpers")
                || (method.FullName.Contains("NetMQ.NetMQFrame") &&(method.IsConstructor))
                )
            {
                //Console.WriteLine("Ignore function body of " + method.FullName);
                return false;
            }
            

            var methodName = $"{method.DeclaringType.FullName}::{method.Name}";
            bool instrumented = false;
            var processor = method.Body.GetILProcessor();
            method.Body.SimplifyMacros();
            method.Body.InitLocals = true;




            foreach (var instruction in method.Body.Instructions.ToList())
            {
                try
                {
                    if ((instruction.OpCode == OpCodes.Stfld || instruction.OpCode == OpCodes.Stsfld))
                    {
                        
                        FieldDefinition fieldDef = null;
                        try
                        {
                            fieldDef = instruction.Operand is FieldDefinition ?
                                (FieldDefinition)instruction.Operand :
                                ((FieldReference)instruction.Operand).Resolve();
                        }
                        catch (Exception e)
                        {
                            // Console.WriteLine("Ignore unresolved field");
                            continue;
                        }

                        if (fieldDef.DeclaringType.IsValueType) continue; // struct field

                        if (fieldDef.FieldType.ToString().Contains("System.Runtime.CompilerServices"))
                        {
                            if (fieldDef.ToString().Contains("isVolatile"))
                                Console.WriteLine("Ingore " + fieldDef.ToString());
                            continue;
                        }



                        var fieldName = $"{fieldDef.DeclaringType.FullName}::{fieldDef.Name}";//
                        if (fieldDef.Name.Contains("<>")) continue; // compiler generated fields
                        


                        FieldReference fieldRef = instruction.Operand is FieldReference ?
                            (FieldReference)instruction.Operand :
                            null;
                        
                        var isStaticField = fieldDef.IsStatic;
                        // var isValueType = fieldDef.FieldType.IsValueType;

                        // create two local variables: one to store the new value, and one for the object instance 
                        var newValueVariableDef = new VariableDefinition(this.objectType);
                        method.Body.Variables.Add(newValueVariableDef);

                        VariableDefinition objVariableDef = null;
                        if (!isStaticField)
                        {
                            objVariableDef = new VariableDefinition(this.objectType);
                            method.Body.Variables.Add(objVariableDef);
                        }

                        var patch = this.GetPatchForBeforeFieldWrite(
                            processor,
                            fieldDef,
                            fieldRef,
                            objVariableDef,
                            newValueVariableDef,
                            methodName,
                            instruction.Offset);

                        processor.InsertBeforeAndUpdateReference(method, instruction, patch);


                        patch = this.GetPatchAfterInstruction(processor, methodName, instruction.Offset);
                        processor.InsertAfter(instruction, patch);
                        method.UpdateInstructionReferences(instruction, patch.Last(), true);
                    }
                    else if ((instruction.OpCode == OpCodes.Ldfld || instruction.OpCode == OpCodes.Ldsfld))
                    {
                        FieldDefinition fieldDef = null;
                        try
                        {
                            fieldDef = instruction.Operand is FieldDefinition ?
                                (FieldDefinition)instruction.Operand :
                                ((FieldReference)instruction.Operand).Resolve();
                        }
                        catch (Exception e)
                        {
                            //Console.WriteLine("Ignore unresolved field");
                            continue;
                        }

                        if (fieldDef == null) continue;
                        if (fieldDef.DeclaringType.IsValueType) continue; // struct field
                        if (fieldDef.FieldType.ToString().Contains("System.Runtime.CompilerServices"))
                        {
                            Console.WriteLine("Ingore " + fieldDef.ToString());
                            continue; // compiler generated
                        }


                        var fieldName = $"{fieldDef.DeclaringType.FullName}::{fieldDef.Name}";
                        if (fieldDef.Name.Contains("<>")) continue; // compiler generated fields
                        
                        

                        FieldReference fieldRef = instruction.Operand is FieldReference ?
                            (FieldReference)instruction.Operand :
                            null;

                        var isStaticField = fieldDef.IsStatic;
                        // var isValueType = fieldDef.FieldType.IsValueType;

                        // create a local variable to store the instance object
                        VariableDefinition objVariableDef = null;
                        if (!isStaticField)
                        {
                            objVariableDef = new VariableDefinition(this.objectType);
                            method.Body.Variables.Add(objVariableDef);
                        }

                        var patch = this.GetPatchForFieldRead(
                            processor,
                            fieldDef,
                            fieldRef,
                            objVariableDef,
                            methodName,
                            instruction.Offset);
                        processor.InsertBeforeAndUpdateReference(method, instruction, patch);

                        patch = this.GetPatchAfterInstruction(processor, methodName, instruction.Offset);
                        processor.InsertAfter(instruction, patch);
                        method.UpdateInstructionReferences(instruction, patch.Last(), true);

                        instrumented = true;
                    }
                    else if (instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt || instruction.OpCode == OpCodes.Newobj)
                    {
                        var calleeRef = (MethodReference)instruction.Operand;

                        if ((calleeRef.DeclaringType.IsValueType) 
                            && (!SpecialValuetypeMethod(calleeRef))
                            )
                        {
                            // Console.WriteLine(calleeRef.ToString() + " is in struct ? ");
                            continue; // method in struct
                        }

                        if (calleeRef.DeclaringType.IsValueType)
                        {
                            //Console.WriteLine("Valuetype " + calleeRef.ToString());
                        }

                        var calleeName = $"{calleeRef.DeclaringType.FullName}::{calleeRef.Name}";
                        // if (calleeName.Contains("<>")) continue; // compiler generated methods
                        /*
                        var isGetIsCompleted = calleeRef.Name == "get_IsCompleted"
                            && instruction.ToString().Contains("System.Runtime.CompilerServices");

                        if (isGetIsCompleted)
                        {
                            var asyncpatch = new List<Instruction>()
                            {
                                processor.Create(OpCodes.Pop),
                                processor.Create(OpCodes.Ldc_I4_0),
                            };
                            processor.InsertAfter(instruction, asyncpatch);
                            instrumented = true;
                            Console.WriteLine("enfore an async function");
                            continue;
                        }
                        */
                        var signature = calleeRef.GetResolvedMethodSignature();

                        
                        var isNewObj = instruction.OpCode == OpCodes.Newobj;
                        Instruction patchStart = null;
                        VariableDefinition instanceVarDef = null;

                        if (!isNewObj && calleeRef.HasThis)
                        {
                            instanceVarDef = new VariableDefinition(method.Module.TypeSystem.Object);
                            method.Body.Variables.Add(instanceVarDef);

                            var loadThisInstruction = MSILHelper.LocateLoadThisInstruction(processor, instruction);
                            var patch1 = new List<Instruction>()
                        {
                            processor.Create(OpCodes.Dup),
                            processor.Create(OpCodes.Stloc, instanceVarDef),
                        };
                            processor.InsertAfter(loadThisInstruction, patch1);
                            patchStart = patch1[0];
                        }

                        var patchTarget = instruction;
                        var isValueType = false;
                        if (patchTarget.Previous != null && patchTarget.Previous.OpCode == OpCodes.Constrained)
                        {
                            patchTarget = patchTarget.Previous;
                            if (patchTarget.Operand is GenericInstanceType)
                            {
                                isValueType = ((GenericInstanceType)patchTarget.Operand).IsValueType;
                            }
                        }

                        var patch = this.GetPatchForMethodCall(
                            processor,
                            instanceVarDef,
                            methodName,
                            calleeRef,
                            instruction.Offset,
                            isValueType);

                        if (patchStart == null) patchStart = patch[0];

                        processor.InsertBefore(patchTarget, patch);
                        //method.UpdateInstructionReferences(patchTarget, patchStart, !SpecialValuetypeMethod(calleeRef));
                        //method.UpdateInstructionReferences(patchTarget, patchStart, !calleeRef.ToString().Contains("System.Threading.Tasks.TaskFactory::StartNew"));
                        method.UpdateInstructionReferences(patchTarget, patchStart, true);

                        patch = this.GetPatchAfterInstruction(processor, methodName, instruction.Offset);
                        processor.InsertAfter(instruction, patch);
                        method.UpdateInstructionReferences(instruction, patch.Last(), true);

                        instrumented = true;
                    }
                }catch(Exception e_)
                {
                    Console.WriteLine(e_);
                }
            }

            
            var beginpatch = this.GetPatchForBeginEnd(
                processor,
                methodName,
                "Begin"
                );

            processor.InsertBeforeAndUpdateReference(method, method.Body.Instructions[0], beginpatch);
            // processor.InsertBefore(method.Body.Instructions[0], beginpatch);
            

            var endpatch = this.GetPatchForBeginEnd(
                processor,
                methodName,
                "End"
                );
            processor.InsertBeforeAndUpdateReference(method, method.Body.Instructions[method.Body.Instructions.Count - 1], endpatch);
            // processor.InsertBefore(method.Body.Instructions[method.Body.Instructions.Count - 1], endpatch);

            method.Body.OptimizeMacros();

            return instrumented;
        }

        List<Instruction> GetPatchForBeforeFieldWrite(
            ILProcessor processor,
            FieldDefinition fieldDef,
            FieldReference fieldRef,
            VariableDefinition objVariableDef,
            VariableDefinition newValueVariableDef,
            string callerMethodName,
            int ilOffset)
        {
            var isStaticField = fieldDef.IsStatic;
            // var isValueType = fieldDef.FieldType.IsValueType;
            var needsBoxing = fieldDef.FieldType.IsValueType || fieldDef.ContainsGenericParameter;
            var declaringType = fieldRef != null ? fieldRef.DeclaringType : fieldDef.DeclaringType;
            var fieldType = fieldRef != null ? fieldRef.FieldType : fieldDef.FieldType;
            var fieldName = $"{fieldDef.DeclaringType.FullName}::{fieldDef.Name}"; 

            List<Instruction> patch = new List<Instruction>()
                    {
                    // top of the stack has the new value.
                        needsBoxing ? processor.Create(OpCodes.Box, fieldType) : processor.Create(OpCodes.Nop),
                        processor.Create(OpCodes.Stloc, newValueVariableDef),

                        // for non-static field, top of stack has the instance object.
                        isStaticField ? processor.Create(OpCodes.Nop) : processor.Create(OpCodes.Stloc, objVariableDef),

                        // Now call the callback.
                        // arg0: instance: object
                        isStaticField ? processor.Create(OpCodes.Ldnull) : processor.Create(OpCodes.Ldloc, objVariableDef),

                        // arg: field name: string
                        processor.Create(OpCodes.Ldstr, fieldName),

                        // arg: old value: object
                        isStaticField ? processor.Create(OpCodes.Nop) : processor.Create(OpCodes.Ldloc, objVariableDef),
                        isStaticField ? processor.Create(OpCodes.Nop) : processor.Create(OpCodes.Castclass, declaringType),
                        processor.Create(isStaticField ? OpCodes.Ldsfld : OpCodes.Ldfld, fieldRef??fieldDef),
                        needsBoxing ? processor.Create(OpCodes.Box, fieldType) : processor.Create(OpCodes.Nop),

                        // arg: new value : object
                        processor.Create(OpCodes.Ldloc, newValueVariableDef),

                        // arg: caller method name: string
                        processor.Create(OpCodes.Ldstr, callerMethodName),

                        // arg: ILOffset
                        processor.Create(OpCodes.Ldc_I4, ilOffset),

                        // call the callback
                        processor.Create(OpCodes.Call, this.beforeFieldWriteCallbackRef),

                        // finally, restore the stack
                        isStaticField ? processor.Create(OpCodes.Nop) : processor.Create(OpCodes.Ldloc, objVariableDef),
                        isStaticField ? processor.Create(OpCodes.Nop) : processor.Create(OpCodes.Castclass, declaringType),
                        processor.Create(OpCodes.Ldloc, newValueVariableDef),
                        needsBoxing ? processor.Create(OpCodes.Unbox_Any, fieldType) : processor.Create(OpCodes.Castclass, fieldType),
                    };

            return patch;
        }

        List<Instruction> GetPatchForAfterFieldWrite(
            ILProcessor ilProcessor, 
            FieldDefinition fieldDef, 
            FieldReference fieldRef,
            VariableDefinition objVariableDef, 
            string callerMethodName, 
            int ilOffset)
        {
            var isStaticField = fieldDef.IsStatic;
            var needsBoxing = fieldDef.FieldType.IsValueType || fieldDef.ContainsGenericParameter;
            var declaringType = fieldRef != null ? fieldRef.DeclaringType : fieldDef.DeclaringType;
            var fieldType = fieldRef != null ? fieldRef.FieldType : fieldDef.FieldType;
            var fieldName = $"{fieldDef.DeclaringType.FullName}::{fieldDef.Name}";

            var patch = new List<Instruction>()
            {
                // parent object
                isStaticField ? ilProcessor.Create(OpCodes.Ldnull) : ilProcessor.Create(OpCodes.Ldloc, objVariableDef),

                //param1: field name
                ilProcessor.Create(OpCodes.Ldstr, fieldName),
    
                // param: field value
                isStaticField ? ilProcessor.Create(OpCodes.Nop) 
                    : ilProcessor.Create(OpCodes.Ldloc, objVariableDef),
                isStaticField ? ilProcessor.Create(OpCodes.Nop) : ilProcessor.Create(OpCodes.Castclass, declaringType),
                ilProcessor.Create(isStaticField ? OpCodes.Ldsfld : OpCodes.Ldfld, fieldRef??fieldDef),
                needsBoxing ? ilProcessor.Create(OpCodes.Box, fieldType) : ilProcessor.Create(OpCodes.Nop),

                // param3: caller method name
                ilProcessor.Create(OpCodes.Ldstr, callerMethodName),

                // param: ILOffset
                ilProcessor.Create(OpCodes.Ldc_I4, ilOffset),

                // finally, call the callback
                ilProcessor.Create(OpCodes.Call, afterFieldWriteCallbackRef),
            };
            return patch;
        }

        List<Instruction> GetPatchForFieldRead(
            ILProcessor processor,
            FieldDefinition fieldDef,
            FieldReference fieldRef,
            VariableDefinition objVariableDef,
            string callerMethodName,
            int iloffset)
        {
            var isStaticField = fieldDef.IsStatic;
            var needsBoxing = fieldDef.FieldType.IsValueType || fieldDef.ContainsGenericParameter;
            var declaringType = fieldRef != null ? fieldRef.DeclaringType : fieldDef.DeclaringType;
            var fieldType = fieldRef != null ? fieldRef.FieldType : fieldDef.FieldType;
            string fieldName = $"{fieldDef.DeclaringType.FullName}::{fieldDef.Name}"; 

            var patch = new List<Instruction>()
                    {
                        isStaticField ? processor.Create(OpCodes.Nop) : processor.Create(OpCodes.Stloc, objVariableDef),
                        isStaticField ? processor.Create(OpCodes.Ldnull) : processor.Create(OpCodes.Ldloc, objVariableDef),
                        ////isStaticField ? processor.Create(OpCodes.Nop) : processor.Create(OpCodes.Castclass, fieldDef.DeclaringType),
                        processor.Create(OpCodes.Ldstr, fieldName),
                        isStaticField ? processor.Create(OpCodes.Nop) : processor.Create(OpCodes.Ldloc, objVariableDef),
                        isStaticField ? processor.Create(OpCodes.Nop) : processor.Create(OpCodes.Castclass, declaringType),
                        processor.Create(isStaticField ? OpCodes.Ldsfld : OpCodes.Ldfld, fieldRef != null ? fieldRef : fieldDef),
                        needsBoxing ? processor.Create(OpCodes.Box, fieldType) : processor.Create(OpCodes.Nop),
                        processor.Create(OpCodes.Ldstr, callerMethodName),
                        processor.Create(OpCodes.Ldc_I4, iloffset),
                        processor.Create(OpCodes.Call, this.beforeFieldReadCallbackRef),
                        isStaticField ? processor.Create(OpCodes.Nop) : processor.Create(OpCodes.Ldloc, objVariableDef),
                        isStaticField ? processor.Create(OpCodes.Nop) : processor.Create(OpCodes.Castclass, declaringType),
                    };

            return patch;
        }

        List<Instruction> GetPatchForMethodCall(
            ILProcessor processor,
            VariableDefinition instanceVarDef,
            string callerMethodName,
            MethodReference calleeRef,
            int ilOffset,
            bool needsBoxing)
        {
            var calleeName = $"{calleeRef.DeclaringType.FullName}::{calleeRef.Name}";
            needsBoxing |= calleeRef.DeclaringType.IsValueType;

            var patch = new List<Instruction>()
                        {
                            instanceVarDef == null ? processor.Create(OpCodes.Ldnull) 
                                : processor.Create(needsBoxing ? OpCodes.Ldloca : OpCodes.Ldloc, instanceVarDef),
                            processor.Create(OpCodes.Ldstr, callerMethodName),
                            processor.Create(OpCodes.Ldc_I4, ilOffset),
                            processor.Create(OpCodes.Ldstr, calleeName),
                            processor.Create(OpCodes.Call, this.beforeMethodCallCallbackRef),
                        };

            return patch;
        }

        List<Instruction> GetPatchAfterInstruction(
            ILProcessor processor,
            string callerMethodName, 
            int ilOffset
            )
        {
            var patch = new List<Instruction>()
                        {
                            processor.Create(OpCodes.Ldnull),
                            processor.Create(OpCodes.Ldstr, "FinishIP"),
                            processor.Create(OpCodes.Ldc_I4, ilOffset),
                            processor.Create(OpCodes.Ldstr, callerMethodName),
                            processor.Create(OpCodes.Call, this.AfterInstructionCallbackRef),
                        };

            return patch;
        }

        List<Instruction> GetPatchForBeginEnd(
            ILProcessor processor,
            string methodName,
            string key // "begin" or "end"
            )
        {
            var patch = new List<Instruction>()
                        {
                            processor.Create(OpCodes.Ldnull),
                            processor.Create(OpCodes.Ldstr, methodName),
                            processor.Create(OpCodes.Ldc_I4, 0),
                            processor.Create(OpCodes.Ldstr, methodName + "-" +key),
                            processor.Create(OpCodes.Call, this.beforeMethodCallCallbackRef),
                        };

            return patch;
        }

        private bool SpecialValuetypeMethod(MemberReference mr)
        {
            if (!mr.ToString().Contains("TaskAwaiter")) return false;
            if (!mr.ToString().Contains("::GetResult()")) return false;
            return true;
        }
    }

}

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TorchLiteInstrumenter
{
    public class InstrumenterV1 : IInstrumenter
    {

        private readonly string fieldUsageCallbackTypeName = "CallbacksV1";
        private readonly string onFieldReadMethodName = "BeforeFieldRead";
        private readonly string onFieldWriteMethodName = "BeforeFieldWrite";
        private readonly string onStaticFieldWriteMethodName = "BeforeStaticFieldWrite";
 
        private MethodReference onFieldWriteMethodRef;
        private MethodReference onStaticFieldWriteMethodRef;
        private MethodReference onFieldReadMethodRef;

        public InstrumenterV1(ModuleDefinition moduleToBeInstrumented, ModuleDefinition callbackModule)
        {
            var fieldTrackerCallbackType = callbackModule.Types.Single(t => t.Name == fieldUsageCallbackTypeName);
            onFieldWriteMethodRef = moduleToBeInstrumented.ImportReference(
                fieldTrackerCallbackType.Methods.Single(x => x.Name == onFieldWriteMethodName).Resolve());
            onStaticFieldWriteMethodRef = moduleToBeInstrumented.ImportReference(
                fieldTrackerCallbackType.Methods.Single(x => x.Name == onStaticFieldWriteMethodName).Resolve());
            onFieldReadMethodRef = moduleToBeInstrumented.ImportReference(
                fieldTrackerCallbackType.Methods.Single(x => x.Name == onFieldReadMethodName).Resolve());
        }

        public bool Instrument(IEnumerable<MethodDefinition> methods)
        {
            bool instrumented = false;
            foreach (var method in methods)
            {
                instrumented |= Instrument(method);
            }

            return instrumented;
        }

        public bool Instrument(MethodDefinition method)
        {
            string thisModulePath = Path.GetDirectoryName(method.Module.FileName);

            if (method.FullName.Contains("<>"))
            {
                // ignore compiler generated methods
                return false;
            }

            ILProcessor ilProcessor = method.Body.GetILProcessor();
            bool instrumented = false;
            method.Body.SimplifyMacros();

            foreach (var instruction in method.Body.Instructions.ToList())
            {
                if (instruction.ToString().Contains("<>"))
                {
                    // ignore compiler generated instructions
                    continue;
                }

                if ((instruction.OpCode == OpCodes.Stfld || instruction.OpCode == OpCodes.Stsfld) && instruction.Operand is FieldDefinition)
                {
                    FieldDefinition fieldDef = (FieldDefinition)instruction.Operand;
                    if (fieldDef.IsStatic)
                    {
                        var patch = GetPatchForStaticWriteInterception(ilProcessor, fieldDef, null, method, instruction);
                        ilProcessor.InsertBeforeAndUpdateReference(method, instruction, patch);
                    }
                    else
                    {
                        var loadThisInstruction = MSILHelper.LocateLoadThisInstruction(ilProcessor, instruction);

                        var patch1 = GetPatch1ForWriteInterception(ilProcessor, fieldDef, null, method, instruction);
                        ilProcessor.InsertAfter(loadThisInstruction, patch1);
                        //patch1.Reverse();
                        //patch1.ForEach(x => ilProcessor.InsertAfter(loadThisInstruction, x));
                        method.UpdateInstructionReferences(patch1.Last().Next, patch1[0]);

                        var patch2 = GetPatch2ForWriteInterception(ilProcessor, fieldDef, null, method, instruction);
                        ilProcessor.InsertBefore(instruction, patch2);
                    }

                    instrumented = true;
                }
                else if ((instruction.OpCode == OpCodes.Ldfld || instruction.OpCode == OpCodes.Ldsfld) && instruction.Operand is FieldDefinition)
                {
                    FieldDefinition fieldDef = (FieldDefinition)instruction.Operand;
                    bool isStaticField = instruction.OpCode == OpCodes.Ldsfld;
                    var loadThisInstruction = MSILHelper.LocateLoadThisInstruction(ilProcessor, instruction);

                    var patch = GetPatchForInterceptingBeforeRead(ilProcessor, fieldDef, method, instruction);
                    ilProcessor.InsertAfter(loadThisInstruction, patch);
                    instrumented = true;
                }
            }

            method.Body.OptimizeMacros();

            return instrumented;
        }

        private List<Instruction> GetPatchForStaticWriteInterception(ILProcessor ilProcessor, FieldDefinition fieldDef, VariableDefinition parentObjVariable, MethodDefinition method, Instruction instruction)
        {
            var patch = new List<Instruction>();

            // the newValue is at the top of the stack at this point.
            if (fieldDef.FieldType.IsValueType)
            {
                patch.Add(ilProcessor.Create(OpCodes.Box, fieldDef.FieldType));
            }

            patch.Add(ilProcessor.Create(OpCodes.Ldsfld, fieldDef));
            if (fieldDef.FieldType.IsValueType)
            {
                patch.Add(ilProcessor.Create(OpCodes.Box, fieldDef.FieldType));
            }

            //param1: field name
            patch.Add(ilProcessor.Create(OpCodes.Ldstr, fieldDef.FullName));

            // param3: caller method name
            patch.Add(ilProcessor.Create(OpCodes.Ldstr, InstrumentationHelper.MethodSignatureWithoutReturnType(method.FullName)));

            //param4: ILOffset
            patch.Add(ilProcessor.Create(OpCodes.Ldc_I4, instruction.Offset));

            // finally, call the callback
            patch.Add(ilProcessor.Create(OpCodes.Call, onStaticFieldWriteMethodRef));

            if (fieldDef.FieldType.IsValueType)
            {
                patch.Add(ilProcessor.Create(OpCodes.Unbox_Any, fieldDef.FieldType));
            }

            return patch;

        }
        private List<Instruction> GetPatch1ForWriteInterception(ILProcessor ilProcessor, FieldDefinition fieldDef, VariableDefinition parentObjVariable, MethodDefinition method, Instruction instruction)
        {
            var patch = new List<Instruction>();
            
            // the "this" object is at the top of the stack at this point.
            patch.Add(ilProcessor.Create(OpCodes.Dup));
            patch.Add(ilProcessor.Create(OpCodes.Dup));
            patch.Add(ilProcessor.Create(OpCodes.Ldfld, fieldDef));

            if (fieldDef.FieldType.IsValueType)
            {
                patch.Add(ilProcessor.Create(OpCodes.Box, fieldDef.FieldType));
            }
            return patch;
        }
        private List<Instruction> GetPatch2ForWriteInterception(ILProcessor ilProcessor, FieldDefinition fieldDef, VariableDefinition parentObjVariable, MethodDefinition method, Instruction instruction)
        {
            var patch = new List<Instruction>();

            // the newvalue is at the top of the stack at this point.

            if (fieldDef.FieldType.IsValueType)
            {
                patch.Add(ilProcessor.Create(OpCodes.Box, fieldDef.FieldType));
            }

            //param1: field name
            patch.Add(ilProcessor.Create(OpCodes.Ldstr, fieldDef.FullName));

            // param3: caller method name
            patch.Add(ilProcessor.Create(OpCodes.Ldstr, InstrumentationHelper.MethodSignatureWithoutReturnType(method.FullName)));

            //param4: ILOffset
            patch.Add(ilProcessor.Create(OpCodes.Ldc_I4, instruction.Offset));

            // finally, call the callback
            patch.Add(ilProcessor.Create(OpCodes.Call, onFieldWriteMethodRef));

            if (fieldDef.FieldType.IsValueType)
            {
                patch.Add(ilProcessor.Create(OpCodes.Unbox_Any, fieldDef.FieldType));
            }

            return patch;
        }

        private List<Instruction> GetPatchForInterceptingBeforeRead(ILProcessor ilProcessor, FieldDefinition fieldDef, MethodDefinition method, Instruction instruction)
        {
            var patch = new List<Instruction>();

            if (fieldDef.IsStatic)
            {
                patch.Add(ilProcessor.Create(OpCodes.Ldnull));
                patch.Add(ilProcessor.Create(OpCodes.Ldsfld, fieldDef));
            } else 
            {
                patch.Add(ilProcessor.Create(OpCodes.Dup));
                patch.Add(ilProcessor.Create(OpCodes.Ldfld, fieldDef));
            }

            if (fieldDef.FieldType.IsValueType)
            {
                patch.Add(ilProcessor.Create(OpCodes.Box, fieldDef.FieldType));
            }

            patch.Add(ilProcessor.Create(OpCodes.Ldstr, fieldDef.FullName));
            patch.Add(ilProcessor.Create(OpCodes.Ldstr, InstrumentationHelper.MethodSignatureWithoutReturnType(method.FullName)));
            patch.Add(ilProcessor.Create(OpCodes.Ldc_I4, instruction.Offset));
            patch.Add(ilProcessor.Create(OpCodes.Call, onFieldReadMethodRef));
            
            if (fieldDef.IsStatic)
            {
                patch.Add(ilProcessor.Create(OpCodes.Pop));
            }

            return patch;
        }
    }
}

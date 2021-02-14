using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace TorchLiteInstrumenter
{
    /// <summary>
    /// Implements various Cecil helper methods.
    /// </summary>
    public static class CecilExtensions
    {
        public static void UpdateInstructionReferences(this MethodDefinition method, Instruction oldInstruction, Instruction newInstruction, bool exceptionHanldersOnly = false)
        {
            if (!exceptionHanldersOnly)
            {
                var instructionsToUpdate = method.Body.Instructions
                    .Where(x => x.Operand is Instruction && ((Instruction)x.Operand) == oldInstruction);
                foreach (var instruction in instructionsToUpdate)
                {
                    instruction.Operand = newInstruction;
                }
            }

            foreach (var eh in method.Body.ExceptionHandlers)
            {
                if (eh.TryStart == oldInstruction) eh.TryStart = newInstruction;
                if (eh.TryEnd == oldInstruction) eh.TryEnd = newInstruction;
                if (eh.HandlerStart == oldInstruction) eh.HandlerStart = newInstruction;
                if (eh.HandlerEnd == oldInstruction) eh.HandlerEnd = newInstruction;
                if (eh.FilterStart == oldInstruction) eh.FilterStart = newInstruction;
            }
        }

        public static void InsertAfter(this ILProcessor ilProcessor, Instruction target, IEnumerable<Instruction> instructionsToInsert)
        {
            Instruction current = target;
            foreach (var i in instructionsToInsert)
            {
                ilProcessor.InsertAfter(current, i);
                current = current.Next;
            }
        }

        public static void InsertBefore(this ILProcessor ilProcessor, Instruction target, IEnumerable<Instruction> instructionsToInsert)
        {
            foreach (var i in instructionsToInsert)
            {
                ilProcessor.InsertBefore(target, i);
            }
        }

        public static void InsertBeforeAndUpdateReference(this ILProcessor ilProcessor, MethodDefinition method, Instruction target, List<Instruction> instructionsToInsert)
        {
            ilProcessor.InsertBefore(target, instructionsToInsert);
            method.UpdateInstructionReferences(target, instructionsToInsert[0]);
        }

        public static void InsertBeforeAndUpdateReference(this ILProcessor ilProcessor, MethodDefinition method, Instruction target, Instruction instructionsToInsert)
        {
            ilProcessor.InsertBeforeAndUpdateReference(method, target, new List<Instruction>() { instructionsToInsert });
        }

        public static void InsertAfterAndUpdateReference(this ILProcessor ilProcessor, MethodDefinition method, Instruction target, List<Instruction> instructionsToInsert)
        {
            ilProcessor.InsertAfter(target, instructionsToInsert);
            method.UpdateInstructionReferences(target, instructionsToInsert[0]);
        }

        public static void InsertAfterAndUpdateReference(this ILProcessor ilProcessor, MethodDefinition method, Instruction target, Instruction instructionsToInsert)
        {
            ilProcessor.InsertAfterAndUpdateReference(method, target, new List<Instruction>() { instructionsToInsert });
        }

        public static Instruction Clone(this Instruction i)
        {
            if (i.Operand == null)
            {
                return Instruction.Create(i.OpCode);
            }
            else if (i.Operand is MethodReference)
            {
                return Instruction.Create(i.OpCode, i.Operand as MethodReference);
            }
            else if (i.Operand is TypeReference)
            {
                return Instruction.Create(i.OpCode, i.Operand as TypeReference);
            }
            else if (i.Operand is ParameterDefinition)
            {
                return Instruction.Create(i.OpCode, i.Operand as ParameterDefinition);
            }
            else if (i.Operand is Instruction)
            {
                return Instruction.Create(i.OpCode, i.Operand as Instruction);
            }
            else if (i.Operand is byte)
            {
                return Instruction.Create(i.OpCode, (byte)i.Operand);
            }
            else if (i.Operand is CallSite)
            {
                return Instruction.Create(i.OpCode, i.Operand as CallSite);
            }
            else if (i.Operand is double)
            {
                return Instruction.Create(i.OpCode, (double)i.Operand);
            }
            else if (i.Operand is FieldReference)
            {
                return Instruction.Create(i.OpCode, i.Operand as FieldReference);
            }
            else if (i.Operand is float)
            {
                return Instruction.Create(i.OpCode, (float)i.Operand);
            }
            else if (i.Operand is Instruction[])
            {
                return Instruction.Create(i.OpCode, i.Operand as Instruction[]);
            }
            else if (i.Operand is int)
            {
                return Instruction.Create(i.OpCode, (int)i.Operand);
            }
            else if (i.Operand is long)
            {
                return Instruction.Create(i.OpCode, (long)i.Operand);
            }
            else if (i.Operand is sbyte)
            {
                return Instruction.Create(i.OpCode, (sbyte)i.Operand);
            }
            else if (i.Operand is string)
            {
                return Instruction.Create(i.OpCode, i.Operand as string);
            }
            else if (i.Operand is VariableDefinition)
            {
                return Instruction.Create(i.OpCode, i.Operand as VariableDefinition);
            }

            throw new Exception("Cannot clone instruction");
        }

        // ref: https://stackoverflow.com/questions/4968755/mono-cecil-call-generic-base-class-method-from-other-assembly
        public static TypeReference MakeGenericType(this TypeReference self, params TypeReference[] arguments)
        {
            if (self.GenericParameters.Count != arguments.Length)
            {
                throw new ArgumentException();
            }

            var instance = new GenericInstanceType(self);
            foreach (var argument in arguments)
            {
                instance.GenericArguments.Add(argument);
            }

            return instance;
        }

        public static MethodReference MakeGeneric(this MethodReference self, params TypeReference[] arguments)
        {
            var reference = new MethodReference(self.Name, self.ReturnType)
            {
                DeclaringType = MakeGenericType(self.DeclaringType, arguments),
                HasThis = self.HasThis,
                ExplicitThis = self.ExplicitThis,
                CallingConvention = self.CallingConvention,
            };

            foreach (var parameter in self.Parameters)
            {
                reference.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
            }

            foreach (var generic_parameter in self.GenericParameters)
            {
                reference.GenericParameters.Add(new GenericParameter(generic_parameter.Name, reference));
            }

            return reference;
        }

        public static bool IsVoidType(this TypeReference self)
        {
            return self.FullName.Equals("System.Void");
        }

        public static bool HasSameFullName(this TypeReference self, Type type) => string.Equals(self.FullName, type.FullName);

        private static readonly string GetPrefix = "get_";
        private static readonly string SetPrefix = "set_";

        /// <summary>
        /// Get method signature of method definition that complies with how
        /// CCI generates a method signature.
        /// </summary>
        /// <param name="method">Method to get signature</param>
        /// <returns>
        /// Method signature that complies with CCI method signature of the
        /// definition of the specified method, e.g., List&lt;T&gt;.Add(T);
        /// (generic parameters are as what they defined).
        /// </returns>
        public static string GetMethodSignature(this MethodDefinition method)
        {
            string methodSignature;
            if (method.Name.StartsWith(GetPrefix))
            {
                methodSignature = $"{method.Name.Substring(GetPrefix.Length)}.get";
            }
            else if (method.Name.StartsWith(SetPrefix))
            {
                methodSignature = $"{method.Name.Substring(SetPrefix.Length)}.set";
            }
            else
            {
                methodSignature = method.Name;
            }

            if (method.HasGenericParameters)
            {
                var genericParameters = method.GenericParameters.Select(genericParameter => genericParameter.GetTypeSignature());
                methodSignature = $"{methodSignature}<{string.Join(",", genericParameters)}>";
            }

            var parameters = method.Parameters.Select(parameter =>
            {
                TypeReference resolvedParameterType = parameter.GetTypeWithGenericResolved();
                if (resolvedParameterType.IsByReference)
                {
                    ByReferenceType byReferenceType = (ByReferenceType)resolvedParameterType;
                    string prefix;
                    if (parameter.IsOut)
                    {
                        prefix = "out";
                    }
                    else if (parameter.IsIn)
                    {
                        prefix = "in";
                    }
                    else
                    {
                        prefix = "ref";
                    }

                    return $"{prefix}{byReferenceType.ElementType.GetResolvedTypeSignature()}";
                }

                if (parameter.IsParams())
                {
                    return $"params{resolvedParameterType.GetResolvedTypeSignature()}";
                }

                return resolvedParameterType.GetResolvedTypeSignature();
            });
            return $"{method.DeclaringType.GetTypeSignature()}.{methodSignature}({string.Join(",", parameters)})";
        }

        /// <summary>
        /// Get method signature of invoked method. It will be resolved with
        /// generic arguments while the method is invoked.
        /// </summary>
        /// <param name="method">Method to get resolved signature</param>
        /// <returns>
        /// Method signature with generic parameters resolved, e.g.,
        /// List&lt;string&gt;.Add(string).
        /// Generic parameters are resolved to argument values.
        /// </returns>
        public static string GetResolvedMethodSignature(this MethodReference method)
        {
            string typeSignature = method.DeclaringType.GetResolvedTypeSignature();
            string methodSignature;
            if (method.Name.StartsWith(GetPrefix))
            {
                methodSignature = $"{method.Name.Substring(GetPrefix.Length)}.get";
            }
            else if (method.Name.StartsWith(SetPrefix))
            {
                methodSignature = $"{method.Name.Substring(SetPrefix.Length)}.set";
            }
            else
            {
                methodSignature = method.Name;
            }

            if (method.IsGenericInstance)
            {
                GenericInstanceMethod genericInstance = (GenericInstanceMethod)method;
                var genericParameters = genericInstance.GenericArguments.Select(genericArgument => genericArgument.GetResolvedTypeSignature());
                methodSignature = genericParameters.Count() != 0 ? $"{methodSignature}<{string.Join(",", genericParameters)}>" : methodSignature;
            }

            IEnumerable<string> parameters;
            if (method.IsGenericInstance)
            {
                GenericInstanceMethod genericInstanceMethod = (GenericInstanceMethod)method;
                parameters = method.Parameters.Select(parameter =>
                {
                    TypeReference resolvedParameterType = parameter.GetTypeWithGenericResolved(genericInstanceMethod);
                    if (resolvedParameterType.IsByReference)
                    {
                        ByReferenceType byReference = resolvedParameterType as ByReferenceType;
                        string prefix;
                        if (parameter.IsOut)
                        {
                            prefix = "out";
                        }
                        else if (parameter.IsIn)
                        {
                            prefix = "in";
                        }
                        else
                        {
                            prefix = "ref";
                        }

                        return $"{prefix}{byReference.ElementType.GetResolvedTypeSignature()}";
                    }

                    if (parameter.IsParams())
                    {
                        return $"params{resolvedParameterType.GetResolvedTypeSignature()}";
                    }

                    return resolvedParameterType.GetResolvedTypeSignature();
                });
            }
            else
            {
                parameters = method.Parameters.Select(parameter =>
                {
                    TypeReference resolvedParameterType = parameter.GetTypeWithGenericResolved();
                    if (resolvedParameterType.IsByReference)
                    {
                        ByReferenceType byReference = resolvedParameterType as ByReferenceType;
                        string prefix;
                        if (parameter.IsOut)
                        {
                            prefix = "out";
                        }
                        else if (parameter.IsIn)
                        {
                            prefix = "in";
                        }
                        else
                        {
                            prefix = "ref";
                        }

                        return $"{prefix}{byReference.ElementType.GetResolvedTypeSignature()}";
                    }

                    if (parameter.IsParams())
                    {
                        return $"params{resolvedParameterType.GetResolvedTypeSignature()}";
                    }

                    return resolvedParameterType.GetResolvedTypeSignature();
                });
            }

            return $"{typeSignature}.{methodSignature}({string.Join(",", parameters)})";
        }

        /// <summary>
        /// Get method signature of invoked method. It will be resolved with
        /// generic arguments while the method is invoked.
        /// </summary>
        /// <param name="method">Method to get resolved signature</param>
        /// <param name="methodDef">Method definition of method parameter</param>
        /// <returns>
        /// Method signature with generic parameters resolved, e.g., List&lt;string&gt;
        /// Generic parameters are resolved to argument values
        /// </returns>
        public static string GetResolvedMethodSignature(this MethodReference method, MethodDefinition methodDef)
        {
            string typeSignature = GetResolvedTypeSignature(method.DeclaringType);
            string methodSignature;
            if (method.Name.StartsWith(GetPrefix))
            {
                methodSignature = $"{method.Name.Substring(GetPrefix.Length)}.get";
            }
            else if (method.Name.StartsWith(SetPrefix))
            {
                methodSignature = $"{method.Name.Substring(SetPrefix.Length)}.set";
            }
            else
            {
                methodSignature = method.Name;
            }

            if (method.IsGenericInstance)
            {
                GenericInstanceMethod genericInstance = (GenericInstanceMethod)method;
                var genericParameters = genericInstance.GenericArguments.Select(genericArgument => genericArgument.GetResolvedTypeSignature());
                methodSignature = genericParameters.Count() != 0 ? $"{methodSignature}<{string.Join(",", genericParameters)}>" : methodSignature;
            }

            IList<string> parameters = new List<string>();
            if (method.IsGenericInstance)
            {
                GenericInstanceMethod genericInstanceMethod = (GenericInstanceMethod)method;
                for (int i = 0; i < method.Parameters.Count; i++)
                {
                    ParameterDefinition parameter = method.Parameters[i];
                    ParameterDefinition parameterOfMethodDef = methodDef.Parameters[i];
                    TypeReference resolvedParameterType = parameter.GetTypeWithGenericResolved(genericInstanceMethod);
                    if (resolvedParameterType.IsByReference)
                    {
                        ByReferenceType byReference = resolvedParameterType as ByReferenceType;
                        string prefix;
                        if (parameterOfMethodDef.IsOut)
                        {
                            prefix = "out";
                        }
                        else if (parameterOfMethodDef.IsIn)
                        {
                            prefix = "in";
                        }
                        else
                        {
                            prefix = "ref";
                        }

                        parameters.Add($"{prefix}{byReference.ElementType.GetResolvedTypeSignature()}");
                    }
                    else if (parameterOfMethodDef.IsParams())
                    {
                        parameters.Add($"params{resolvedParameterType.GetResolvedTypeSignature()}");
                    }
                    else
                    {
                        parameters.Add(resolvedParameterType.GetResolvedTypeSignature());
                    }
                }
            }
            else
            {
                for (int i = 0; i < method.Parameters.Count; i++)
                {
                    ParameterDefinition parameter = method.Parameters[i];
                    ParameterDefinition parameterOfMethodDef = methodDef.Parameters[i];
                    TypeReference resolvedParameterType = parameter.GetTypeWithGenericResolved();
                    if (resolvedParameterType.IsByReference)
                    {
                        ByReferenceType byReference = resolvedParameterType as ByReferenceType;
                        string prefix;
                        if (parameterOfMethodDef.IsOut)
                        {
                            prefix = "out";
                        }
                        else if (parameterOfMethodDef.IsIn)
                        {
                            prefix = "in";
                        }
                        else
                        {
                            prefix = "ref";
                        }

                        parameters.Add($"{prefix}{byReference.ElementType.GetResolvedTypeSignature()}");
                    }
                    else if (parameterOfMethodDef.IsParams())
                    {
                        parameters.Add($"params{resolvedParameterType.GetResolvedTypeSignature()}");
                    }
                    else
                    {
                        parameters.Add(resolvedParameterType.GetResolvedTypeSignature());
                    }
                }
            }

            return $"{typeSignature}.{methodSignature}({string.Join(",", parameters)})";
        }

        /// <summary>
        /// Get simple method signature that complies with CCI implementation which
        /// is there is no parameter shown.
        /// </summary>
        /// <param name="method">Method to get simple signature</param>
        /// <returns>Simple method signature complies with CCI</returns>
        public static string GetSimpleMethodSignature(this MethodReference method)
        {
            List<TypeReference> typeHierarchy = new List<TypeReference>();
            string typeNamespace = string.Empty;
            for (TypeReference iter = method.DeclaringType; !(iter is null); iter = iter.DeclaringType)
            {
                typeHierarchy.Add(iter);
                typeNamespace = iter.Namespace;
            }

            typeHierarchy.Reverse();
            List<string> splitMethodSignature = new List<string>();
            if (!string.IsNullOrEmpty(typeNamespace))
            {
                splitMethodSignature.Add(typeNamespace);
            }

            foreach (TypeReference t in typeHierarchy)
            {
                string typeName;
                if (t.HasGenericParameters)
                {
                    typeName = t.Name.Split('`')[0];
                }
                else
                {
                    typeName = t.Name;
                }

                splitMethodSignature.Add(typeName);
            }

            String methodName;
            if (method.Name.StartsWith(GetPrefix))
            {
                methodName = $"{method.Name.Substring(GetPrefix.Length)}.get";
            }
            else if (method.Name.StartsWith(SetPrefix))
            {
                methodName = $"{method.Name.Substring(SetPrefix.Length)}.set";
            }
            else
            {
                methodName = method.Name;
            }

            splitMethodSignature.Add(methodName);
            return string.Join(".", splitMethodSignature);
        }

        /// <summary>
        /// Get type signature with generic parameter resolved
        /// </summary>
        /// <param name="type">Type to get resolved signature</param>
        /// <returns>Type signature with generic resolved, e.g., </returns>
        public static string GetResolvedTypeSignature(this TypeReference type)
        {
            bool isArray = false;
            int arrayRank = 0;
            TypeReference elementType = type;
            if (type.IsArray)
            {
                ArrayType arrayType = type as ArrayType;
                elementType = arrayType.ElementType;
                isArray = true;
                arrayRank = arrayType.Rank;
            }

            if (elementType is GenericParameter)
            {
                string typeName = elementType.Name;
                if (isArray)
                {
                    typeName = $"{typeName}[";
                    for (int i = 0; i < arrayRank - 1; i++)
                    {
                        typeName = $"{typeName},";
                    }

                    typeName = $"{typeName}]";
                }

                return typeName;
            }

            List<TypeReference> typeHierarchy = new List<TypeReference>();
            string typeNamespace = string.Empty;
            for (TypeReference iter = elementType; !(iter is null); iter = iter.DeclaringType)
            {
                typeHierarchy.Add(iter);
                typeNamespace = iter.Namespace;
            }

            typeHierarchy.Reverse();
            List<string> splitCanonicalName = new List<string>();
            if (!string.IsNullOrEmpty(typeNamespace))
            {
                splitCanonicalName.Add(typeNamespace);
            }

            int genericParamCount = 0;
            foreach (TypeReference t in typeHierarchy)
            {
                TypeReference resolvedType = t;
                string typeName;
                if (elementType.IsGenericInstance)
                {
                    GenericInstanceType genericInstance = elementType as GenericInstanceType;
                    var nameTokens = resolvedType.Name.Split('`');
                    typeName = nameTokens[0];
                    int numThisGenericParams = nameTokens.Length == 1 ? 0 : Convert.ToInt32(nameTokens[1]);
                    List<string> genericParameters = new List<string>();
                    for (int i = 0; i < numThisGenericParams; i++)
                    {
                        genericParameters.Add(GetResolvedTypeSignature(genericInstance.GenericArguments[genericParamCount + i]));
                    }

                    genericParamCount += numThisGenericParams;
                    typeName = genericParameters.Count != 0 ? $"{typeName}<{string.Join(",", genericParameters)}>" : typeName;
                }
                else
                {
                    typeName = resolvedType.Name;
                }

                if (t == elementType && isArray)
                {
                    typeName = $"{typeName}[";
                    for (int i = 0; i < arrayRank - 1; i++)
                    {
                        typeName = $"{typeName},";
                    }

                    typeName = $"{typeName}]";
                }

                splitCanonicalName.Add(typeName);
            }

            return string.Join(".", splitCanonicalName);
        }

        /// <summary>
        /// This is the same method as Microsoft.Torch.Instrumenter.Cci.SignatureHelper.GetTypeSignature(ITypeReference)
        /// </summary>
        /// <param name="type"></param>
        /// <returns>
        /// E.g., Case1.Class1, Case2.Class1&lt;A&gt;
        /// </returns>
        public static string GetTypeSignature(this TypeReference type)
        {
            if (type is GenericParameter)
            {
                return type.Name;
            }

            List<TypeReference> typeHierarchy = new List<TypeReference>();
            string typeNamespace = string.Empty;
            for (TypeReference iter = type; !(iter is null); iter = iter.DeclaringType)
            {
                typeHierarchy.Add(iter);
                typeNamespace = iter.Namespace;
            }

            typeHierarchy.Reverse();
            List<string> splitCanonicalName = new List<string>();
            if (!string.IsNullOrEmpty(typeNamespace))
            {
                splitCanonicalName.Add(typeNamespace);
            }

            if (type is GenericInstanceType)
            {
                GenericInstanceType genericInstanceType = type as GenericInstanceType;
                int genericArgumentIndex = 0;
                foreach (var t in typeHierarchy)
                {
                    string typeName;
                    if (t.HasGenericParameters)
                    {
                        var nameTokens = t.Name.Split('`');
                        typeName = nameTokens[0];
                        int numThisGenericParams = nameTokens.Length == 1 ? 0 : Convert.ToInt32(nameTokens[1]);
                        List<string> genericArguments = new List<string>();
                        for (int i = 0; i < numThisGenericParams; i++)
                        {
                            genericArguments.Add(genericInstanceType.GenericArguments[genericArgumentIndex + i].GetTypeSignature());
                        }

                        typeName = genericArguments.Count != 0 ? $"{typeName}<{string.Join(",", genericArguments)}>" : typeName;
                        genericArgumentIndex += numThisGenericParams;
                    }
                    else if (t == genericInstanceType)
                    {
                        typeName = t.Name.Split('`')[0];
                        List<string> genericArguments = new List<string>();
                        for (; genericArgumentIndex < genericInstanceType.GenericArguments.Count; genericArgumentIndex++)
                        {
                            genericArguments.Add(genericInstanceType.GenericArguments[genericArgumentIndex].GetTypeSignature());
                        }

                        typeName = genericArguments.Count != 0 ? $"{typeName}<{string.Join(",", genericArguments)}>" : typeName;
                    }
                    else
                    {
                        typeName = t.Name;
                    }

                    splitCanonicalName.Add(typeName);
                }
            }
            else
            {
                int genericParameterIndex = 0;
                foreach (var t in typeHierarchy)
                {
                    string typeName;
                    if (t.HasGenericParameters)
                    {
                        typeName = t.Name.Split('`')[0];
                        List<string> genericParameters = new List<string>();
                        for (; genericParameterIndex < t.GenericParameters.Count; genericParameterIndex++)
                        {
                            genericParameters.Add(t.GenericParameters[genericParameterIndex].GetTypeSignature());
                        }

                        typeName = genericParameters.Count != 0 ? $"{typeName}<{string.Join(",", genericParameters)}>" : typeName;
                    }
                    else
                    {
                        typeName = t.Name;
                    }

                    splitCanonicalName.Add(typeName);
                }
            }

            return string.Join(".", splitCanonicalName);
        }

        public static TypeReference GetTypeWithGenericResolved(this ParameterDefinition definition)
        {
            return GetTypeWithGenericResolved(definition, null);
        }

        public static TypeReference GetTypeWithGenericResolved(this ParameterDefinition definition, GenericInstanceMethod genericInstanceMethod)
        {
            var typeWithGenericResolved = definition.ParameterType;
            bool isByReference = false;
            bool isArray = false;
            int arrayRank = 0;
            if (typeWithGenericResolved.IsByReference)
            {
                isByReference = true;
                ByReferenceType byReferenceType = (ByReferenceType)typeWithGenericResolved;
                typeWithGenericResolved = byReferenceType.ElementType;
            }

            if (typeWithGenericResolved.IsArray)
            {
                isArray = true;
                ArrayType arrayType = (ArrayType)typeWithGenericResolved;
                arrayRank = arrayType.Rank;
                typeWithGenericResolved = arrayType.ElementType;
            }

            if (typeWithGenericResolved.IsGenericInstance)
            {
                GenericInstanceType genericInstanceType = (GenericInstanceType)typeWithGenericResolved;
                typeWithGenericResolved = genericInstanceType.GetTypeWithGenericResolved(((MethodReference)definition.Method).DeclaringType as GenericInstanceType, genericInstanceMethod);
            }

            if (typeWithGenericResolved.IsGenericParameter)
            {
                GenericParameter genericParam = typeWithGenericResolved as GenericParameter;
                var declaringMethod = (MethodReference)definition.Method;
                if (genericParam.DeclaringType != null && declaringMethod.DeclaringType is GenericInstanceType genericInstanceTypeOfParam)
                {
                    var position = genericInstanceTypeOfParam.ElementType.GenericParameters.GetIndexByName(typeWithGenericResolved.Name);
                    if (position != -1)
                    {
                        typeWithGenericResolved = genericInstanceTypeOfParam.GenericArguments[position];
                    }
                }
                else if (genericParam.DeclaringMethod != null && genericInstanceMethod != null)
                {
                    var position = declaringMethod.GetElementMethod().GenericParameters.GetIndexByName(typeWithGenericResolved.Name);
                    if (position != -1)
                    {
                        typeWithGenericResolved = genericInstanceMethod.GenericArguments[position];
                    }
                }
            }

            if (isArray)
            {
                typeWithGenericResolved = new ArrayType(typeWithGenericResolved, arrayRank);
            }

            if (isByReference)
            {
                typeWithGenericResolved = new ByReferenceType(typeWithGenericResolved);
            }

            return typeWithGenericResolved;
        }

        internal static TypeReference GetTypeWithGenericResolved(
            this GenericInstanceType genericInstanceParameter,
            GenericInstanceType genericInstanceType,
            GenericInstanceMethod genericInstanceMethod)
        {
            GenericInstanceType newGenericInstanceType = new GenericInstanceType(genericInstanceParameter.ElementType);
            foreach (var genericArg in genericInstanceParameter.GenericArguments)
            {
                TypeReference newGenericArg = genericArg;
                bool isByReference = false;
                bool isArray = false;
                int arrayRank = 0;
                if (newGenericArg.IsByReference)
                {
                    isByReference = true;
                    ByReferenceType byReferenceType = (ByReferenceType)newGenericArg;
                    newGenericArg = byReferenceType.ElementType;
                }

                if (newGenericArg.IsArray)
                {
                    isArray = true;
                    ArrayType arrayType = (ArrayType)newGenericArg;
                    arrayRank = arrayType.Rank;
                    newGenericArg = arrayType.ElementType;
                }

                if (newGenericArg.IsGenericInstance)
                {
                    newGenericArg = ((GenericInstanceType)newGenericArg).GetTypeWithGenericResolved(genericInstanceType, genericInstanceMethod);
                }

                if (newGenericArg.IsGenericParameter)
                {
                    GenericParameter tmp = newGenericArg as GenericParameter;
                    if (tmp.DeclaringType != null && genericInstanceType != null)
                    {
                        int position = genericInstanceType.ElementType.GenericParameters.GetIndexByName(newGenericArg.Name);
                        if (position != -1)
                        {
                            newGenericArg = genericInstanceType.GenericArguments[position];
                        }
                    }
                    else if (tmp.DeclaringMethod != null && genericInstanceMethod != null)
                    {
                        int position = genericInstanceMethod.GetElementMethod().GenericParameters.GetIndexByName(newGenericArg.Name);
                        if (position != -1)
                        {
                            newGenericArg = genericInstanceMethod.GenericArguments[position];
                        }
                    }
                }

                if (isArray)
                {
                    newGenericArg = new ArrayType(newGenericArg, arrayRank);
                }

                if (isByReference)
                {
                    newGenericArg = new ByReferenceType(newGenericArg);
                }

                newGenericInstanceType.GenericArguments.Add(newGenericArg);
            }

            return newGenericInstanceType;
        }

        internal static bool IsParams(this ParameterDefinition parameter)
        {
            if (parameter.HasCustomAttributes)
            {
                foreach (var attribute in parameter.CustomAttributes)
                {
                    if (string.Equals(attribute.AttributeType.FullName, "System.ParamArrayAttribute"))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static int GetIndexByName(this Collection<GenericParameter> collection, string name)
        {
            for (int i = 0; i < collection.Count; i++)
            {
                if (string.Equals(collection[i].FullName, name, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}

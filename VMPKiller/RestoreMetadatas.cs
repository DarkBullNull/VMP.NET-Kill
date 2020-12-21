using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using OpCode = System.Reflection.Emit.OpCode;
using OpCodes = dnlib.DotNet.Emit.OpCodes;

namespace VMPKiller
{
    public class RestoreMetadatas
    {
        private bool VMPVersionIsOld;
        private MethodInfo methodInfo { get; }
        private Assembly assembly { get; }
        private ModuleDefMD moduleDef { get; }
        public RestoreMetadatas(ref ModuleDefMD moduleDef, string pathFile)
        {
            this.moduleDef = moduleDef;
            assembly = Assembly.LoadFile(pathFile);
            methodInfo = assembly.EntryPoint;
            DetectVersion(ref moduleDef);
            if (VMPVersionIsOld) // if true - version VMP is old
            {
                RestoreDelegatesVariant(pathFile);
            }
            else
            {
                RestoreMethodsModuleVariant(pathFile);
            }
            DecryptStrings();
            InlineCallMethods();


        }
        void DecryptStrings()
        {
            int counterStrings = 0;
            foreach (var type in moduleDef.Types)
            {
                foreach (var method in type.Methods)
                {
                    if (method.Body == null) continue;
                    for (int instructionIndex = 0; instructionIndex < method.Body.Instructions.Count; instructionIndex++)
                    {
                        
                        if (method.Body.Instructions[instructionIndex].OpCode == OpCodes.Call && method.Body.Instructions[instructionIndex].Operand is MethodDef)
                        {
                            var callOperand = (MethodDef) method.Body.Instructions[instructionIndex].Operand;
                            if (callOperand.ReturnType.TypeName == "String" && callOperand.IsStatic)
                            {
                                if (callOperand.Body.Instructions[0].OpCode == OpCodes.Newobj &&
                                    callOperand.Body.Instructions[2].IsLdcI4() &&
                                    callOperand.Body.Instructions[3].OpCode == OpCodes.Call &&
                                    (callOperand.Body.Instructions[4].OpCode == OpCodes.Castclass && callOperand.Body.Instructions[4].Operand.ToString().Contains("System.String")) &&
                                    callOperand.Body.Instructions[5].OpCode == OpCodes.Ret)
                                {
                                    counterStrings++;
                                    method.Body.Instructions[instructionIndex].OpCode = OpCodes.Ldstr;
                                    method.Body.Instructions[instructionIndex].Operand = InvokeDecryptMethod(
                                        callOperand.Module.Name, callOperand.DeclaringType2.Name,
                                        callOperand.Name.String);

                                }
                            }
                        }
                    }
                    method.Body.UpdateInstructionOffsets();
                }
            }
            Console.WriteLine($"Decrypted strings: {counterStrings}");
        }
        string InvokeDecryptMethod(string moduleName, string typeName, string methodName)
        {
            string decryptedString;
            var module = methodInfo.Module.Assembly.GetModule(moduleName);
            foreach (var type in module.GetTypes())
            {
                if (type.Name == typeName)
                {
                    foreach (var methodType in type.GetRuntimeMethods())
                    {
                        if (methodType.Name == methodName)
                        {
                            decryptedString = (string)methodType.Invoke(null, new object[] {  });
                            return decryptedString;
                        }
                    }
                }
            }
            return "NULL";
        }
        void RestoreDelegatesVariant(string pathFile)
        {
            int delegateNumber = 0;
            string declaringTypeName = string.Empty;
            string fieldName = string.Empty;

            foreach (var type in moduleDef.Types)
            {
                if (type.HasMethods && type.IsDelegate)
                {
                    foreach (var method in type.Methods)
                    {
                        if (method.HasBody)
                        {
                            if (method.Body.HasInstructions)
                            {
                                if (method.Body.Instructions[0].OpCode == OpCodes.Ldsfld &&
                                    method.Body.Instructions[1].OpCode.ToString().Contains("ldc"))
                                {
                                    delegateNumber = method.Body.Instructions[1].GetLdcI4Value();
                                    dynamic ldsfldOperand = method.Body.Instructions[0]?.Operand;
                                    if (ldsfldOperand is FieldDef)
                                    {
                                        declaringTypeName = ldsfldOperand.DeclaringType.Name;
                                        fieldName = ldsfldOperand.Name;
                                    }
                                }
                            }

                            object[] delegatesArray = (object[]) assembly.ManifestModule.GetType(declaringTypeName)
                                .GetField(fieldName).GetValue(null);
                            var currentDelegate = (Delegate) delegatesArray[delegateNumber];

                            var m_owner = currentDelegate.Method
                                .GetType()
                                .GetField("m_owner", BindingFlags.NonPublic | BindingFlags.Instance)
                                ?.GetValue(currentDelegate.Method);
                            if (m_owner != null)
                            {
                                var m_resolver = m_owner
                                    .GetType()
                                    .GetField("m_resolver", BindingFlags.NonPublic | BindingFlags.Instance)
                                    ?.GetValue(m_owner);
                                if (m_resolver != null)
                                {
                                    var m_scope = m_resolver.GetType()
                                        .GetField("m_scope", BindingFlags.NonPublic | BindingFlags.Instance)
                                        ?.GetValue(m_resolver);
                                    List<object> m_tokens = (List<object>) m_scope.GetType()
                                        .GetField("m_tokens", BindingFlags.NonPublic | BindingFlags.Instance)
                                        .GetValue(m_scope);
                                    if (m_tokens[m_tokens.Count - 1] is RuntimeMethodHandle)
                                    {
                                        RuntimeMethodHandle calledMethod = (RuntimeMethodHandle) m_tokens[m_tokens.Count - 1];
                                        dynamic calledMethodMInfo = calledMethod.GetType()
                                            .GetField("m_value", BindingFlags.NonPublic | BindingFlags.Instance)
                                            ?.GetValue(calledMethod);
                                        if (calledMethodMInfo != null)
                                        {
                                            try
                                            {
                                                string fullName = calledMethodMInfo.GetType()
                                                    .GetProperty("FullName", BindingFlags.Instance | BindingFlags.NonPublic)
                                                    .GetValue(calledMethodMInfo).ToString();
                                                if (fullName.Contains(".ctor") && !fullName.Contains("System.Windows.Forms.Form..ctor"))
                                                {
                                                    method.Body.Instructions[method.Body.Instructions.Count - 2] =
                                                        Instruction.Create(OpCodes.Newobj, moduleDef.Import(calledMethodMInfo));
                                                    method.Body.Instructions[0] = Instruction.Create(OpCodes.Nop);
                                                    method.Body.Instructions[1] = Instruction.Create(OpCodes.Nop);
                                                    method.Body.Instructions[2] = Instruction.Create(OpCodes.Nop);
                                                    method.Body.UpdateInstructionOffsets();
                                                }
                                                else
                                                {
                                                    method.Body.Instructions[method.Body.Instructions.Count - 2] =
                                                        Instruction.Create(OpCodes.Call, moduleDef.Import(calledMethodMInfo));
                                                    method.Body.Instructions[0] = Instruction.Create(OpCodes.Nop);
                                                    method.Body.Instructions[1] = Instruction.Create(OpCodes.Nop);
                                                    method.Body.Instructions[2] = Instruction.Create(OpCodes.Nop);
                                                    method.Body.UpdateInstructionOffsets();
                                                }
                                            }
                                            catch (Exception e)
                                            {

                                            }
                                        }

                                        // * - this runtime method
                                        Console.WriteLine(delegateNumber + "*: " + calledMethodMInfo.GetType()
                                            .GetProperty("FullName", BindingFlags.Instance | BindingFlags.NonPublic)
                                            .GetValue(calledMethodMInfo));
                                    }
                                    else if (m_tokens[m_tokens.Count - 1] is RuntimeFieldHandle)
                                    {
                                        RuntimeFieldHandle calledField = (RuntimeFieldHandle) m_tokens[m_tokens.Count - 1];
                                        dynamic calledFieldFInfo = calledField.GetType()
                                            .GetField("m_ptr", BindingFlags.NonPublic | BindingFlags.Instance)
                                            ?.GetValue(calledField);
                                        if (calledFieldFInfo != null)
                                        {
                                            method.Body.Instructions[method.Body.Instructions.Count - 2] = 
                                                Instruction.Create(OpCodes.Ldsfld, moduleDef.Import(calledFieldFInfo));
                                            method.Body.Instructions[0] = Instruction.Create(OpCodes.Nop);
                                            method.Body.Instructions[1] = Instruction.Create(OpCodes.Nop);
                                            method.Body.Instructions[2] = Instruction.Create(OpCodes.Nop);
                                            method.Body.UpdateInstructionOffsets();
                                        }
                                        // * - this runtime field
                                        Console.WriteLine(delegateNumber + "*: " + calledFieldFInfo.GetType()
                                            .GetProperty("FullName", BindingFlags.Instance | BindingFlags.NonPublic)
                                            .GetValue(calledFieldFInfo));
                                    }
                                    else
                                    {
                                        Console.ForegroundColor = ConsoleColor.Cyan;
                                        Console.WriteLine("UNKNOWN");
                                        Console.ForegroundColor = ConsoleColor.Blue;
                                    }
                                }
                            }
                            else
                            {
                                method.Body.Instructions[0] = Instruction.Create(OpCodes.Nop);
                                method.Body.Instructions[1] = Instruction.Create(OpCodes.Nop);
                                method.Body.Instructions[2] = Instruction.Create(OpCodes.Nop);
                                method.Body.Instructions[method.Body.Instructions.Count - 2] =
                                    Instruction.Create(OpCodes.Call, moduleDef.Import(currentDelegate.Method));
                                method.Body.UpdateInstructionOffsets();
                                Console.WriteLine(delegateNumber + ": " + currentDelegate.Method);
                            }

                        }
                    }
                }
            }
        }

        void RestoreMethodsModuleVariant(string pathFile)
        {
            int delegateNumber = 0;
            string declaringTypeName = string.Empty;
            string fieldName = string.Empty;
            
            foreach (var method in moduleDef.Types[0].Methods)
            {
                if (method.HasBody)
                {
                    if (method.Body.HasInstructions)
                    {
                        if (method.Body.Instructions[0].OpCode == OpCodes.Ldsfld &&
                            method.Body.Instructions[1].OpCode.ToString().Contains("ldc"))
                        {
                            delegateNumber = method.Body.Instructions[1].GetLdcI4Value();
                            dynamic ldsfldOperand = method.Body.Instructions[0]?.Operand;
                            if (ldsfldOperand is FieldDef)
                            {
                                declaringTypeName = ldsfldOperand.DeclaringType.Name;
                                fieldName = ldsfldOperand.Name;

                            }
                            var delegatesArray = (object[]) assembly.ManifestModule.GetType(declaringTypeName)
                                .GetField(fieldName).GetValue(null);
                            var currentDelegate = (Delegate) delegatesArray[delegateNumber];

                            var m_owner = currentDelegate.Method
                                .GetType()
                                .GetField("m_owner", BindingFlags.NonPublic | BindingFlags.Instance)
                                ?.GetValue(currentDelegate.Method);
                            if (m_owner != null)
                            {
                                var m_resolver = m_owner
                                    .GetType()
                                    .GetField("m_resolver", BindingFlags.NonPublic | BindingFlags.Instance)
                                    ?.GetValue(m_owner);
                                if (m_resolver != null)
                                {
                                    var m_scope = m_resolver.GetType()
                                        .GetField("m_scope", BindingFlags.NonPublic | BindingFlags.Instance)
                                        ?.GetValue(m_resolver);
                                    List<object> m_tokens = (List<object>) m_scope.GetType()
                                        .GetField("m_tokens", BindingFlags.NonPublic | BindingFlags.Instance)
                                        .GetValue(m_scope);
                                    if (m_tokens[m_tokens.Count - 1] is RuntimeMethodHandle)
                                    {
                                        RuntimeMethodHandle calledMethod = (RuntimeMethodHandle) m_tokens[m_tokens.Count - 1];
                                        dynamic calledMethodMInfo = calledMethod.GetType()
                                            .GetField("m_value", BindingFlags.NonPublic | BindingFlags.Instance)
                                            ?.GetValue(calledMethod);
                                        if (calledMethodMInfo != null)
                                        {
                                            try
                                            {
                                                string fullName = calledMethodMInfo.GetType()
                                                    .GetProperty("FullName", BindingFlags.Instance | BindingFlags.NonPublic)
                                                    .GetValue(calledMethodMInfo).ToString();
                                                
                                                if (fullName.Contains(".ctor") && !fullName.Contains("System.Windows.Forms.Form..ctor"))
                                                {
                                                    method.Body.Instructions[method.Body.Instructions.Count - 2] =
                                                        Instruction.Create(OpCodes.Newobj, moduleDef.Import(calledMethodMInfo));
                                                    method.Body.Instructions[0] = Instruction.Create(OpCodes.Nop);
                                                    method.Body.Instructions[1] = Instruction.Create(OpCodes.Nop);
                                                    method.Body.Instructions[2] = Instruction.Create(OpCodes.Nop);
                                                    method.Body.UpdateInstructionOffsets();
                                                }
                                                else
                                                {
                                                    method.Body.Instructions[method.Body.Instructions.Count - 2] =
                                                        Instruction.Create(OpCodes.Call, moduleDef.Import(calledMethodMInfo));
                                                    method.Body.Instructions[0] = Instruction.Create(OpCodes.Nop);
                                                    method.Body.Instructions[1] = Instruction.Create(OpCodes.Nop);
                                                    method.Body.Instructions[2] = Instruction.Create(OpCodes.Nop);
                                                    method.Body.UpdateInstructionOffsets();
                                                }
                                            }
                                            catch (Exception e)
                                            {

                                            }
                                        }

                                        // * - this runtime method
                                        Console.WriteLine(delegateNumber + "*: " + calledMethodMInfo.GetType()
                                            .GetProperty("FullName", BindingFlags.Instance | BindingFlags.NonPublic)
                                            .GetValue(calledMethodMInfo));
                                    }
                                    else if (m_tokens[m_tokens.Count - 1] is RuntimeFieldHandle)
                                    {
                                        RuntimeFieldHandle calledField = (RuntimeFieldHandle) m_tokens[m_tokens.Count - 1];
                                        dynamic calledFieldFInfo = calledField.GetType()
                                            .GetField("m_ptr", BindingFlags.NonPublic | BindingFlags.Instance)
                                            ?.GetValue(calledField);
                                        if (calledFieldFInfo != null)
                                        {
                                            method.Body.Instructions[method.Body.Instructions.Count - 2] = 
                                                Instruction.Create(OpCodes.Ldsfld,
                                                    moduleDef.Import(calledFieldFInfo));
                                            method.Body.Instructions[0] = Instruction.Create(OpCodes.Nop);
                                            method.Body.Instructions[1] = Instruction.Create(OpCodes.Nop);
                                            method.Body.Instructions[2] = Instruction.Create(OpCodes.Nop);
                                            method.Body.UpdateInstructionOffsets();
                                        }
                                        // * - this runtime field
                                        Console.WriteLine(delegateNumber + "*: " + calledFieldFInfo.GetType()
                                            .GetProperty("FullName", BindingFlags.Instance | BindingFlags.NonPublic)
                                            .GetValue(calledFieldFInfo));
                                    }
                                    else
                                    {
                                        Console.ForegroundColor = ConsoleColor.Cyan;
                                        Console.WriteLine("UNKNOWN");
                                        Console.ForegroundColor = ConsoleColor.Blue;
                                    }
                                }
                            }
                            else
                            {
                                method.Body.Instructions[0] = Instruction.Create(OpCodes.Nop);
                                method.Body.Instructions[1] = Instruction.Create(OpCodes.Nop);
                                method.Body.Instructions[2] = Instruction.Create(OpCodes.Nop);
                                method.Body.Instructions[method.Body.Instructions.Count - 2] =
                                    Instruction.Create(OpCodes.Call, moduleDef.Import(currentDelegate.Method));
                                method.Body.UpdateInstructionOffsets();
                                Console.WriteLine(delegateNumber + ": " + currentDelegate.Method);
                            }
                            
                        }
                        
                    }

                    
                }
            }
        }

        void InlineCallMethods()
        {
            int counterRestoredMetadatas = 0;
            foreach (var type in moduleDef.Types)
            {
                foreach (var method in type.Methods)
                {
                    if (method.HasBody)
                    {
                        for (int instructionIndex = 0; instructionIndex < method.Body.Instructions.Count; instructionIndex++)
                        {
                            if (method.Body.Instructions[instructionIndex].OpCode == OpCodes.Call)
                            {
                                if (method.Body.Instructions[instructionIndex].Operand is MethodDef operandCall)
                                {
                                    if (operandCall.HasBody)
                                    {
                                        if (operandCall.Body.Instructions[0].OpCode == OpCodes.Nop &&
                                            operandCall.Body.Instructions[1].OpCode == OpCodes.Nop &&
                                            operandCall.Body.Instructions[2].OpCode == OpCodes.Nop)
                                        {
                                            counterRestoredMetadatas++;
                                            var methodCall =
                                                operandCall.Body.Instructions[operandCall.Body.Instructions.Count - 2];
                                            method.Body.Instructions[instructionIndex].OpCode = methodCall.OpCode;
                                            method.Body.Instructions[instructionIndex].Operand = methodCall.Operand;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            Console.WriteLine($"Restored metadatas: {counterRestoredMetadatas}");
            
        }
        

        void DetectVersion(ref ModuleDefMD moduleDef)
        {
            foreach (var type in moduleDef.Types)
            {
                if (type.IsDelegate)
                {
                    if (type.Methods.Count == 2)
                    {
                        VMPVersionIsOld = false;
                        return;
                    }
                    else
                    {
                        VMPVersionIsOld = true;
                        return;
                    }
                }
            }
        }
    }
}

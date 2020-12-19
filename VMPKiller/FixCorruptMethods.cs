using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace VMPKiller
{
    public class FixCorruptMethods
    {
        private ModuleDefMD moduleDef { get; }
        public FixCorruptMethods(ref ModuleDefMD moduleDef)
        {
            this.moduleDef = moduleDef;
            FixingMethods();
        }
        
        void FixingMethods()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            foreach (var type in moduleDef.Types)
            {
                var ctor = type.FindDefaultConstructor();
                if (ctor != null)
                {
                    if (ctor.HasBody)
                    {
                        // fix CRC calculate
                        if (ctor.Body.Instructions.Count == 50)
                        {
                            Console.WriteLine("Fix CRC calculate");
                            type.Methods[1].Body.Instructions.Add(Instruction.Create(OpCodes.Nop));
                            type.Methods[1].Body.Instructions.Add(Instruction.Create(OpCodes.Nop));
                            type.Methods[1].Body.Instructions.Add(Instruction.Create(OpCodes.Nop));
                            type.Methods[1].Body.Instructions.Add(Instruction.Create(OpCodes.Nop));
                            type.Methods[1].Body.Instructions.Add(Instruction.Create(OpCodes.Nop));
                            type.Methods[1].Body.Instructions[0] = Instruction.Create(OpCodes.Ldc_I4_0);
                            type.Methods[1].Body.Instructions[1] = Instruction.Create(OpCodes.Stloc_0);
                            type.Methods[1].Body.Instructions[2] = Instruction.Create(OpCodes.Ldc_I4_0);
                            type.Methods[1].Body.Instructions[3] = Instruction.Create(OpCodes.Stloc_1);
                            type.Methods[1].Body.Instructions[4] = Instruction.Create(OpCodes.Br, type.Methods[1].Body.Instructions[27]);
                            type.Methods[1].Body.Instructions[5] = Instruction.Create(OpCodes.Ldsfld, type.Fields[0]);
                            type.Methods[1].Body.Instructions[6] = Instruction.Create(OpCodes.Ldarga_S, type.Methods[1].Parameters[1]);
                            type.Methods[1].Body.Instructions[7] = Instruction.Create(OpCodes.Call, moduleDef.Import(typeof(System.IntPtr).GetMethod("ToInt64", new Type[] { })));
                            type.Methods[1].Body.Instructions[8] = Instruction.Create(OpCodes.Ldloc_1);
                            type.Methods[1].Body.Instructions[9] = Instruction.Create(OpCodes.Conv_I8);
                            type.Methods[1].Body.Instructions[10] = Instruction.Create(OpCodes.Add);
                            type.Methods[1].Body.Instructions[11] = Instruction.Create(OpCodes.Newobj, moduleDef.Import(typeof(System.IntPtr).GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, CallingConventions.Standard | CallingConventions.HasThis, new Type[] { typeof(Int64) }, new []{ new ParameterModifier(),  })));
                            type.Methods[1].Body.Instructions[12] = Instruction.Create(OpCodes.Call, moduleDef.Import(typeof(System.Runtime.InteropServices.Marshal).GetMethod("ReadByte", new Type[] { typeof(IntPtr) }))); // IntPtr == native int
                            type.Methods[1].Body.Instructions[13] = Instruction.Create(OpCodes.Ldloc_0);
                            type.Methods[1].Body.Instructions[14] = Instruction.Create(OpCodes.Xor);
                            type.Methods[1].Body.Instructions[15] = Instruction.Create(OpCodes.Ldc_I4, 0xFF);
                            type.Methods[1].Body.Instructions[16] = Instruction.Create(OpCodes.And);
                            type.Methods[1].Body.Instructions[17] = Instruction.Create(OpCodes.Ldelem_U4);
                            type.Methods[1].Body.Instructions[18] = Instruction.Create(OpCodes.Ldloc_0);
                            type.Methods[1].Body.Instructions[19] = Instruction.Create(OpCodes.Ldc_I4_8);
                            type.Methods[1].Body.Instructions[20] = Instruction.Create(OpCodes.Shr_Un);
                            type.Methods[1].Body.Instructions[21] = Instruction.Create(OpCodes.Xor);
                            type.Methods[1].Body.Instructions[22] = Instruction.Create(OpCodes.Stloc_0);
                            type.Methods[1].Body.Instructions[23] = Instruction.Create(OpCodes.Ldloc_1);
                            type.Methods[1].Body.Instructions[24] = Instruction.Create(OpCodes.Ldc_I4_1);
                            type.Methods[1].Body.Instructions[25] = Instruction.Create(OpCodes.Add);
                            type.Methods[1].Body.Instructions[26] = Instruction.Create(OpCodes.Stloc_1);
                            type.Methods[1].Body.Instructions[27] = Instruction.Create(OpCodes.Ldloc_1);
                            type.Methods[1].Body.Instructions[28] = Instruction.Create(OpCodes.Conv_I8);
                            type.Methods[1].Body.Instructions[29] = Instruction.Create(OpCodes.Ldarg_2);
                            type.Methods[1].Body.Instructions[30] = Instruction.Create(OpCodes.Conv_U8);
                            type.Methods[1].Body.Instructions[31] = Instruction.Create(OpCodes.Blt, type.Methods[1].Body.Instructions[5]);
                            type.Methods[1].Body.Instructions[32] = Instruction.Create(OpCodes.Ldloc_0);
                            type.Methods[1].Body.Instructions[33] = Instruction.Create(OpCodes.Not);
                            type.Methods[1].Body.Instructions[34] = Instruction.Create(OpCodes.Ret);
                            type.Methods[1].Body.UpdateInstructionOffsets();
                            type.Methods[1].Body.Instructions[4] = Instruction.Create(OpCodes.Br, type.Methods[1].Body.Instructions[27]);
                        }
                    }
                }

                foreach (var method in type.Methods)
                {
                    if (method.Parameters.Count == 4)
                    {
                        if (method.Parameters[1].Type.ToString().Contains("System.IO.Stream") &&
                            method.Parameters[2].Type.ToString().Contains("System.IO.Stream") &&
                            method.Parameters[3].Type.ToString().Contains("System.Int64"))
                        {
                            Console.WriteLine("Method-decrypt nulling...");
                            method.MethodBody = new CilBody(true, new List<Instruction>() { Instruction.Create(OpCodes.Ret)}, new List<ExceptionHandler>(), new List<Local>());
                        }
                    }
                }
                
                foreach (var nestedType in type.NestedTypes)
                {
                    foreach (var method in nestedType.Methods)
                    {
                        if (method.Parameters.Count == 3)
                        {
                            if (method.HasBody)
                            {
                                if (method.Body.Instructions[0].OpCode == OpCodes.Ldarg_0 &&
                                    method.Body.Instructions[1].OpCode == OpCodes.Ldfld &&
                                    method.Body.Instructions[2].OpCode == OpCodes.Brfalse_S &&
                                    method.Body.Instructions[3].OpCode == OpCodes.Ldarg_0 &&
                                    method.Body.Instructions[4].OpCode == OpCodes.Ldfld &&
                                    method.Body.Instructions[5].OpCode == OpCodes.Ldarg_2 &&
                                    method.Body.Instructions[6].OpCode == OpCodes.Bne_Un_S &&
                                    method.Body.Instructions[7].OpCode == OpCodes.Ldarg_0 &&
                                    method.Body.Instructions[8].OpCode == OpCodes.Ldfld &&
                                    method.Body.Instructions[9].OpCode == OpCodes.Ldarg_1 &&
                                    method.Body.Instructions[10].OpCode == OpCodes.Bne_Un_S &&
                                    method.Body.Instructions[11].OpCode == OpCodes.Ret)
                                {
                                    Console.WriteLine("The restoration of the integrity cycles");
                                    while (method.Body.Instructions.Count != 56 - 1)
                                    {
                                        method.Body.Instructions.Add(Instruction.Create(OpCodes.Nop));
                                    }
                                    MethodDef methodDefForCall = null;
                                    for (int i = 40; i < method.Body.Instructions.Count; i++)
                                    {
                                        if (method.Body.Instructions[i].OpCode == OpCodes.Call)
                                        {
                                            methodDefForCall = (MethodDef) method.Body.Instructions[i].Operand;
                                        }
                                    }
                                    method.Body.Instructions[41] = Instruction.Create(OpCodes.Ldc_I4_0);
                                    method.Body.Instructions[42] = Instruction.Create(OpCodes.Stloc_1);
                                    method.Body.Instructions[43] = Instruction.Create(OpCodes.Br, method.Body.Instructions[53]); // need later update
                                    method.Body.Instructions[44] = Instruction.Create(OpCodes.Ldarg_0);
                                    method.Body.Instructions[45] = Instruction.Create(OpCodes.Ldfld, (IField)method.Body.Instructions[40].Operand);
                                    method.Body.Instructions[46] = Instruction.Create(OpCodes.Ldloc_1);
                                    method.Body.Instructions[47] = Instruction.Create(OpCodes.Ldelema, (TypeDef)method.Body.Instructions[39].Operand);
                                    method.Body.Instructions[48] = Instruction.Create(OpCodes.Call, methodDefForCall);
                                    method.Body.Instructions[49] = Instruction.Create(OpCodes.Ldloc_1);
                                    method.Body.Instructions[50] = Instruction.Create(OpCodes.Ldc_I4_1);
                                    method.Body.Instructions[51] = Instruction.Create(OpCodes.Add);
                                    method.Body.Instructions[52] = Instruction.Create(OpCodes.Stloc_1);
                                    method.Body.Instructions[53] = Instruction.Create(OpCodes.Ldloc_1);
                                    method.Body.Instructions[54] = Instruction.Create(OpCodes.Ldloc_0);
                            
                                    method.Body.UpdateInstructionOffsets();
                                    method.Body.Instructions[43] = Instruction.Create(OpCodes.Br, method.Body.Instructions[53]);
                                    method.Body.Instructions.Add(Instruction.Create(OpCodes.Blt_Un, method.Body.Instructions[44]));
                                    method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
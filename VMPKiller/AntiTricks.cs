using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Runtime;
using System.Threading.Tasks;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace VMPKiller
{
    class AntiTricks
    {
        public AntiTricks(ref ModuleDefMD moduleDef, string folderPathFile)
        {
            AntiDebug(ref moduleDef, folderPathFile);
        }

        void AntiDebug(ref ModuleDefMD moduleDef, string folderPathFile)
        {
            int skipFirstInvoke = 0;
            foreach (var type in moduleDef.Types)
            {
                foreach (var method in type.Methods)
                {
                    if (method.Parameters.Count == 3 && method.Parameters[1].Type.TypeName == "MethodBase" && method.Parameters[2].Type.TypeName == "Boolean")
                    {
                        for (int i = 0; i < method.Body.Instructions.Count; i++)
                        {
                            if (method.Body.Instructions[i].Operand?.ToString().Contains("Invoke") ?? false)
                            {
                                if (skipFirstInvoke++ == 1)
                                {
                                    var indexInvoke = method.Body.Instructions.IndexOf(method.Body.Instructions[i]);
                                    var lastInstruction = method.Body.Instructions[indexInvoke + 2]; // stub, will be reinitialized later

                                    Local getLocal;

                                    if (method.Body.Instructions[indexInvoke + 1].IsStloc())
                                    {
                                        if (method.Body.Instructions[indexInvoke + 1].OpCode == OpCodes.Stloc_0 )
                                        {
                                            getLocal = method.Body.Variables.Locals[0];
                                        }
                                        else if (method.Body.Instructions[indexInvoke + 1].OpCode == OpCodes.Stloc_1 )
                                        {
                                            getLocal = method.Body.Variables.Locals[1];
                                        }
                                        else if (method.Body.Instructions[indexInvoke + 1].OpCode == OpCodes.Stloc_2 )
                                        {
                                            getLocal = method.Body.Variables.Locals[2];
                                        }
                                        else if (method.Body.Instructions[indexInvoke + 1].OpCode == OpCodes.Stloc_3 )
                                        {
                                            getLocal = method.Body.Variables.Locals[3];
                                        }
                                        else
                                        {
                                            getLocal = method.Body.Instructions[indexInvoke + 1].GetLocal(null);
                                        }
                                        
                                    }
                                    else if (method.Body.Instructions[indexInvoke + 3].OpCode == OpCodes.Stloc_S)
                                    {
                                        getLocal = method.Body.Instructions[indexInvoke + 3].GetLocal(null);
                                        indexInvoke += 2;
                                    }
                                    else
                                    {
                                        getLocal = method.Body.Instructions[indexInvoke + 5].GetLocal(null); // skip VMP-mutations
                                        indexInvoke += 4;
                                    }
                                    
                                    // ### CRC-Check ###
                                    Console.WriteLine("CRC-check bypassing...");
                                    int indexPreInvoke = indexInvoke;
                                    Local getLocalObjectInvokeArgs;
                                    dynamic opcodeLdlocS;
                                    while (true)
                                    {
                                        if (method.Body.Instructions[indexPreInvoke].IsLdloc())
                                        {
                                            opcodeLdlocS = method.Body.Instructions[indexPreInvoke].Operand;
                                            if (opcodeLdlocS.Type?.FullName == "System.Object[]")
                                            {
                                                getLocalObjectInvokeArgs = method.Body.Variables[opcodeLdlocS.Index];
                                                break;
                                            }
                                        }
                                        indexPreInvoke--;
                                    }
                                    while (true)
                                    {
                                        if (method.Body.Instructions[indexPreInvoke].IsLdarg())
                                        {
                                            method.Body.Instructions.Insert(indexPreInvoke + 1, Instruction.Create(OpCodes.Callvirt, moduleDef.Import(typeof(System.Reflection.MemberInfo).GetMethod("get_Name", new Type[] { }))));
                                            method.Body.Instructions.Insert(indexPreInvoke + 2, Instruction.Create(OpCodes.Ldstr, "CreateFile"));
                                            method.Body.Instructions.Insert(indexPreInvoke + 3, Instruction.Create(OpCodes.Call, moduleDef.Import(typeof(System.String).GetMethod("op_Equality", new Type[] { typeof(string), typeof(string) }))));
                                            method.Body.Instructions.Insert(indexPreInvoke + 4, Instruction.Create(OpCodes.Brfalse_S, lastInstruction));
                                            method.Body.Instructions.Insert(indexPreInvoke + 5, Instruction.Create(OpCodes.Ldloc_S, getLocalObjectInvokeArgs));
                                            method.Body.Instructions.Insert(indexPreInvoke + 6, Instruction.Create(OpCodes.Ldc_I4_0));
                                            method.Body.Instructions.Insert(indexPreInvoke + 7, Instruction.Create(OpCodes.Ldstr, folderPathFile));
                                            method.Body.Instructions.Insert(indexPreInvoke + 8, Instruction.Create(OpCodes.Stelem_Ref));
                                            method.Body.Instructions.Insert(indexPreInvoke + 9, Instruction.Create(OpCodes.Ldarg_1));
                                            indexInvoke += 9;
                                            break;
                                        }
                                        indexPreInvoke--;
                                    }
                                    
                                    
                                    Console.WriteLine("Anti-debug bypassing...");
                                    // ### NtQueryInformationProcess ###
                                    method.Body.Instructions.Insert(indexInvoke + 2, Instruction.Create(OpCodes.Ldarg_1));
                                    method.Body.Instructions.Insert(indexInvoke + 3, Instruction.Create(OpCodes.Callvirt, moduleDef.Import(typeof(System.Reflection.MemberInfo).GetMethod("get_Name", new Type[] { }))));
                                    method.Body.Instructions.Insert(indexInvoke + 4, Instruction.Create(OpCodes.Ldstr, "NtQueryInformationProcess"));
                                    method.Body.Instructions.Insert(indexInvoke + 5, Instruction.Create(OpCodes.Call, moduleDef.Import(typeof(System.String).GetMethod("op_Equality", new Type[] { typeof(string), typeof(string) }))));
                                    method.Body.Instructions.Insert(indexInvoke + 6, Instruction.Create(OpCodes.Brfalse_S, lastInstruction));
                                    method.Body.Instructions.Insert(indexInvoke + 7, Instruction.Create(OpCodes.Ldc_I4_1));
                                    method.Body.Instructions.Insert(indexInvoke + 8, Instruction.Create(OpCodes.Box, moduleDef.Import(typeof(System.Int32))));
                                    method.Body.Instructions.Insert(indexInvoke + 9, Instruction.Create(OpCodes.Stloc_S, getLocal));

                                    // ### is_Attached ###
                                    method.Body.Instructions.Insert(indexInvoke + 10, Instruction.Create(OpCodes.Ldarg_1));
                                    method.Body.Instructions.Insert(indexInvoke + 11, Instruction.Create(OpCodes.Callvirt, moduleDef.Import(typeof(System.Reflection.MemberInfo).GetMethod("get_Name", new Type[] { }))));
                                    method.Body.Instructions.Insert(indexInvoke + 12, Instruction.Create(OpCodes.Ldstr, "get_IsAttached"));
                                    method.Body.Instructions.Insert(indexInvoke + 13, Instruction.Create(OpCodes.Call, moduleDef.Import(typeof(System.String).GetMethod("op_Equality", new Type[] { typeof(string), typeof(string) }))));
                                    method.Body.Instructions.Insert(indexInvoke + 14, Instruction.Create(OpCodes.Brfalse_S, lastInstruction));
                                    method.Body.Instructions.Insert(indexInvoke + 15, Instruction.Create(OpCodes.Ldc_I4_0));
                                    method.Body.Instructions.Insert(indexInvoke + 16, Instruction.Create(OpCodes.Box, moduleDef.Import(typeof(System.Boolean))));
                                    method.Body.Instructions.Insert(indexInvoke + 17, Instruction.Create(OpCodes.Stloc_S, getLocal));

                                    // ### IsLogging ###
                                    method.Body.Instructions.Insert(indexInvoke + 18, Instruction.Create(OpCodes.Ldarg_1));
                                    method.Body.Instructions.Insert(indexInvoke + 19, Instruction.Create(OpCodes.Callvirt, moduleDef.Import(typeof(System.Reflection.MemberInfo).GetMethod("get_Name", new Type[] { }))));
                                    method.Body.Instructions.Insert(indexInvoke + 20, Instruction.Create(OpCodes.Ldstr, "IsLogging"));
                                    method.Body.Instructions.Insert(indexInvoke + 21, Instruction.Create(OpCodes.Call, moduleDef.Import(typeof(System.String).GetMethod("op_Equality", new Type[] { typeof(string), typeof(string) }))));
                                    method.Body.Instructions.Insert(indexInvoke + 22, Instruction.Create(OpCodes.Brfalse_S, lastInstruction));
                                    method.Body.Instructions.Insert(indexInvoke + 23, Instruction.Create(OpCodes.Ldc_I4_0));
                                    method.Body.Instructions.Insert(indexInvoke + 24, Instruction.Create(OpCodes.Box, moduleDef.Import(typeof(System.Boolean))));
                                    method.Body.Instructions.Insert(indexInvoke + 25, Instruction.Create(OpCodes.Stloc_S, getLocal));
                                    // ### IsDebuggerPresent ###
                                    method.Body.Instructions.Insert(indexInvoke + 26, Instruction.Create(OpCodes.Ldarg_1));
                                    method.Body.Instructions.Insert(indexInvoke + 27, Instruction.Create(OpCodes.Callvirt, moduleDef.Import(typeof(System.Reflection.MemberInfo).GetMethod("get_Name", new Type[] { }))));
                                    method.Body.Instructions.Insert(indexInvoke + 28, Instruction.Create(OpCodes.Ldstr, "IsDebuggerPresent"));
                                    method.Body.Instructions.Insert(indexInvoke + 29, Instruction.Create(OpCodes.Call, moduleDef.Import(typeof(System.String).GetMethod("op_Equality", new Type[] { typeof(string), typeof(string) }))));
                                    method.Body.Instructions.Insert(indexInvoke + 30, Instruction.Create(OpCodes.Brfalse_S, lastInstruction));
                                    method.Body.Instructions.Insert(indexInvoke + 31, Instruction.Create(OpCodes.Ldc_I4_0));
                                    method.Body.Instructions.Insert(indexInvoke + 32, Instruction.Create(OpCodes.Box, moduleDef.Import(typeof(System.Boolean))));
                                    method.Body.Instructions.Insert(indexInvoke + 33, Instruction.Create(OpCodes.Stloc_S, getLocal));

                                    // ### CheckRemoteDebuggerPresent ###
                                    method.Body.Instructions.Insert(indexInvoke + 34, Instruction.Create(OpCodes.Ldarg_1));
                                    method.Body.Instructions.Insert(indexInvoke + 35, Instruction.Create(OpCodes.Callvirt, moduleDef.Import(typeof(System.Reflection.MemberInfo).GetMethod("get_Name", new Type[] { }))));
                                    method.Body.Instructions.Insert(indexInvoke + 36, Instruction.Create(OpCodes.Ldstr, "CheckRemoteDebuggerPresent"));
                                    method.Body.Instructions.Insert(indexInvoke + 37, Instruction.Create(OpCodes.Call, moduleDef.Import(typeof(System.String).GetMethod("op_Equality", new Type[] { typeof(string), typeof(string) }))));
                                    method.Body.Instructions.Insert(indexInvoke + 38, Instruction.Create(OpCodes.Brfalse_S, lastInstruction));
                                    method.Body.Instructions.Insert(indexInvoke + 39, Instruction.Create(OpCodes.Ldc_I4_0));
                                    method.Body.Instructions.Insert(indexInvoke + 40, Instruction.Create(OpCodes.Box, moduleDef.Import(typeof(System.Boolean))));
                                    method.Body.Instructions.Insert(indexInvoke + 41, Instruction.Create(OpCodes.Stloc_S, getLocal));


                                    var invoke = (MemberRef)method.Body.Instructions[indexInvoke].Operand;
                                    
                                    method.Body.UpdateInstructionOffsets(); // update offsets

                                    if (invoke.GetParamCount() == 5) // new version VMP (build 1261)
                                    {
                                        // lastInstruction = this pointer, when need jump
                                        lastInstruction = method.Body.Instructions[indexInvoke - 6]; // On Ldarg.1
                                        method.Body.Instructions[indexInvoke - 11] = Instruction.Create(OpCodes.Brfalse_S, lastInstruction); // create Instruction
                                    }
                                    else // old version VMP (build 1213)
                                    {
                                        // lastInstruction = this pointer, when need jump
                                        lastInstruction = method.Body.Instructions[indexInvoke - 3]; // On Ldarg.1
                                        method.Body.Instructions[indexInvoke - 8] = Instruction.Create(OpCodes.Brfalse_S, lastInstruction); // create Instruction
                                    }
                                    
                                    lastInstruction = method.Body.Instructions[indexInvoke + 10];
                                    method.Body.Instructions[indexInvoke + 6] = Instruction.Create(OpCodes.Brfalse_S, lastInstruction);

                                    lastInstruction = method.Body.Instructions[indexInvoke + 18];
                                    method.Body.Instructions[indexInvoke + 14] = Instruction.Create(OpCodes.Brfalse_S, lastInstruction);

                                    lastInstruction = method.Body.Instructions[indexInvoke + 26];
                                    method.Body.Instructions[indexInvoke + 22] = Instruction.Create(OpCodes.Brfalse_S, lastInstruction);

                                    lastInstruction = method.Body.Instructions[indexInvoke + 34];
                                    method.Body.Instructions[indexInvoke + 30] = Instruction.Create(OpCodes.Brfalse_S, lastInstruction);

                                    lastInstruction = method.Body.Instructions[indexInvoke + 42];
                                    method.Body.Instructions[indexInvoke + 38] = Instruction.Create(OpCodes.Brfalse_S, lastInstruction);


                                    i += 20;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

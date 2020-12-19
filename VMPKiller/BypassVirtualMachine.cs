using System;
using System.Reflection.Emit;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.MD;
using dnlib.DotNet.Writer;
using dnlib.PE;
using Metadata = dnlib.DotNet.MD.Metadata;
using MethodAttributes = System.Reflection.MethodAttributes;
using OpCode = System.Reflection.Emit.OpCode;
using OpCodes = dnlib.DotNet.Emit.OpCodes;

namespace VMPKiller
{
    public class BypassVirtualMachine
    {
        public BypassVirtualMachine(ref ModuleDefMD moduleDef)
        {
            AntiVMInit(ref moduleDef);
        }
        private void AntiVMInit(ref ModuleDefMD moduleDefMD)
        {
            foreach (var type in moduleDefMD.Types)
            {
                // Bypass EnumSystemFirmwareTables()
                if (type.Methods.Count == 1 || type.Fields.Count == 5)
                {
                    foreach (var method in type.Methods)
                    {
                        if (method.HasBody)
                        {
                            var countLdcI4_1 = 0;
                            foreach (var instruction in method.Body.Instructions)
                                // Bypass EnumSystemFirmwareTables()
                                if (instruction.OpCode.Code == Code.Ldc_I4_1) // check signature (count Ldc_I4_1 == 13)
                                {
                                    countLdcI4_1++;
                                }
                                else if (instruction.IsLdcI4() && instruction.GetLdcI4Value() == 1)
                                {
                                    countLdcI4_1++;
                                }

                            if (countLdcI4_1 == 13)
                            {
                                Console.WriteLine("Bypass Anti-Virtual-Machine: \nBypassing(EnumSystemFirmwareTables)");
                                // C:\Users\skorp\Desktop\vmpKill\clean.vmpAVM1.dem-cleaned.exe
                                for (var indexInstructions = 0;
                                    indexInstructions < method.Body.Instructions.Count - 2;
                                    indexInstructions++)
                                    method.Body.Instructions[indexInstructions] = Instruction.Create(OpCodes.Nop);

                                method.Body.Instructions[method.Body.Instructions.Count - 2] =
                                    Instruction.Create(OpCodes.Ldc_I4, 0);
                            }
                        }
                    }
                }
                // Bypass CPUID 0x40000000 && 0x40000010
                if (type.Fields.Count == 2 && type.Methods.Count == 2)
                {
                    foreach (var method in type.Methods)
                    {
                        if (method.HasBody)
                        {
                            for (int indexInstructions = 0; indexInstructions < method.Body.Instructions.Count; indexInstructions++)
                            {
                                if (method.Body.Instructions[indexInstructions].OpCode == OpCodes.Ldtoken && 
                                    method.Body.Instructions[indexInstructions + 3].OpCode == OpCodes.Castclass && 
                                    method.Body.Instructions[indexInstructions + 6].OpCode == OpCodes.Initobj)
                                {
                                    Console.WriteLine("Bypass CPUID 0x40000000 && 0x40000010");
                                    method.Body.Instructions[0] = new Instruction(OpCodes.Ldc_I4_4);
                                    method.Body.Instructions[1] = new Instruction(OpCodes.Newarr, moduleDefMD.Import(typeof(System.Int32)));
                                    method.Body.Instructions[2] = new Instruction(OpCodes.Dup);
                                    method.Body.Instructions[3] = new Instruction(OpCodes.Ldc_I4_0);
                                    method.Body.Instructions[4] = new Instruction(OpCodes.Ldc_I4, 0x206A7);
                                    method.Body.Instructions[5] = new Instruction(OpCodes.Stelem_I4);
                                    method.Body.Instructions[6] = new Instruction(OpCodes.Dup);
                                    method.Body.Instructions[7] = new Instruction(OpCodes.Ldc_I4_1);
                                    method.Body.Instructions[8] = new Instruction(OpCodes.Ldc_I4, 0x3100800);
                                    method.Body.Instructions[9] = new Instruction(OpCodes.Stelem_I4);
                                    method.Body.Instructions[10] = new Instruction(OpCodes.Dup);
                                    method.Body.Instructions[11] = new Instruction(OpCodes.Ldc_I4_2);
                                    method.Body.Instructions[12] = new Instruction(OpCodes.Ldc_I4, 0x1F9AE3BF);
                                    method.Body.Instructions[13] = new Instruction(OpCodes.Stelem_I4);
                                    method.Body.Instructions[14] = new Instruction(OpCodes.Dup);
                                    method.Body.Instructions[15] = new Instruction(OpCodes.Ldc_I4_3);
                                    method.Body.Instructions[16] = new Instruction(OpCodes.Ldc_I4, -0x40140401);
                                    method.Body.Instructions[17] = new Instruction(OpCodes.Stelem_I4);
                                    method.Body.Instructions[18] = new Instruction(OpCodes.Ret);
                                    for (int indexCleaningInstruction = 19; indexCleaningInstruction < method.Body.Instructions.Count;)
                                    {
                                        if (method.Body.Instructions[indexCleaningInstruction] != null)
                                        {
                                            method.Body.Instructions.RemoveAt(indexCleaningInstruction);
                                        }
                                    }

                                    for (int exceptionIterator = method.Body.ExceptionHandlers.Count - 1;
                                        exceptionIterator >= 0;
                                        exceptionIterator--)
                                    {
                                       method.Body.ExceptionHandlers.RemoveAt(exceptionIterator);
                                    }
                                    method.Body.OptimizeBranches();
                                    method.Body.UpdateInstructionOffsets();
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            
        }
    }
}
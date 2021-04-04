using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.DotNet.Writer;
using dnlib.IO;
using dnlib.PE;

namespace VMPKiller
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "########################";
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[build from 20.12.2020]\nVMP.NET Killer (dev: https://github.com/DarkBullNull/)");
            Console.WriteLine("Please see the guide before using it");
            Console.WriteLine("If there are any errors, please create an issue");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Enter path file (drag and drop): ");
            var pathFile =  Console.ReadLine()?.Replace("\"", "");
            
            Console.WriteLine("Select options:\n" +
                              "\t 1 - Bypass anti-VM (1213 build)\n" +
                              "\t 2 - Bypass CRC and anti-debug\n" +
                              "\t 3 - Bypass all\n" +
                              "\t 4 - Method Call Hiding remove\n");
            Int32.TryParse(Console.ReadLine(), out var userParams);
            
            Console.ForegroundColor = ConsoleColor.Blue;

            ModuleDefMD moduleDef = ModuleDefMD.Load(pathFile);
            Controller controller = new Controller(ref moduleDef, pathFile, userParams);
            
            var nativeModuleWriter = new dnlib.DotNet.Writer.NativeModuleWriterOptions(moduleDef, false);
            nativeModuleWriter.Logger = DummyLogger.NoThrowInstance;
            nativeModuleWriter.MetadataOptions.Flags = MetadataFlags.PreserveAll |
                                                       MetadataFlags.KeepOldMaxStack |
                                                       MetadataFlags.PreserveExtraSignatureData |
                                                       MetadataFlags.PreserveBlobOffsets |
                                                       MetadataFlags.PreserveUSOffsets |
                                                       MetadataFlags.PreserveStringsOffsets;
            nativeModuleWriter.Cor20HeaderOptions.Flags = ComImageFlags.ILOnly;
            
            Console.WriteLine("Saving...");
            var newFilePath = pathFile.Substring(0, pathFile.Length - 4) + ".justify.exe";
            moduleDef.NativeWrite(newFilePath, nativeModuleWriter);

            var patchCrcMetadata = new PatchCRCMetadata(newFilePath);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Done!");
            Thread.Sleep(5000);
        }
        
    }
}

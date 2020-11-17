using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.DotNet.Writer;

namespace VMPKiller
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "########################";
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("VMP.NET Killer (dev: https://github.com/DarkBullNull/)");
            Console.WriteLine("Please see the guide before using it");
            Console.WriteLine("If there are any errors, please create an issue");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Enter path file (drag and drop): ");
            var pathFile =  Console.ReadLine();
            
            Console.WriteLine("Select options:\n" +
                              "\t 1 - Bypass anti-VM\n" +
                              "\t 2 - Bypass CRC and anti-debug\n" +
                              "\t 3 - Bypass all\n");
            Int32.TryParse(Console.ReadLine(), out var userParams);
            
            Console.ForegroundColor = ConsoleColor.Blue;
            ModuleDefMD moduleDef = ModuleDefMD.Load(pathFile);
            var imageProtectedVanillaFile = Path.GetDirectoryName(pathFile) + @"\vmp.exe";
            var test = moduleDef.Cor20HeaderFlags;
            var nativeModuleWriter = new dnlib.DotNet.Writer.NativeModuleWriterOptions(moduleDef, false);
            Controller controller = new Controller(ref moduleDef, imageProtectedVanillaFile, userParams);
            
            nativeModuleWriter.Logger = DummyLogger.NoThrowInstance;
            nativeModuleWriter.MetadataOptions.Flags = MetadataFlags.PreserveAll |
                                                       MetadataFlags.KeepOldMaxStack |
                                                       MetadataFlags.PreserveExtraSignatureData |
                                                       MetadataFlags.PreserveBlobOffsets |
                                                       MetadataFlags.PreserveUSOffsets |
                                                       MetadataFlags.PreserveStringsOffsets;
            nativeModuleWriter.Cor20HeaderOptions.Flags = new ComImageFlags();
            
            Console.WriteLine("Saving...");
            moduleDef.NativeWrite(pathFile.Substring(0, pathFile.Length - 4) + ".justify.exe", nativeModuleWriter);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Done!");
            Thread.Sleep(10000);
        }
    }
}

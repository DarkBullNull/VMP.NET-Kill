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
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Enter path file (drag and drop): ");
            Console.ForegroundColor = ConsoleColor.DarkRed;
            var pathFile =  Console.ReadLine();
            var imageProtectedVanillaFile = Path.GetDirectoryName(pathFile) + @"\vmp.exe";
            Console.ForegroundColor = ConsoleColor.Yellow;
            ModuleDefMD moduleDef = ModuleDefMD.Load(pathFile);
            Controller controller = new Controller(ref moduleDef, imageProtectedVanillaFile);
            var nativeModuleWriter = new dnlib.DotNet.Writer.NativeModuleWriterOptions(moduleDef, false);
            nativeModuleWriter.Logger = DummyLogger.NoThrowInstance;
            nativeModuleWriter.MetadataOptions.Flags = MetadataFlags.PreserveAll | MetadataFlags.KeepOldMaxStack | MetadataFlags.PreserveExtraSignatureData | MetadataFlags.PreserveBlobOffsets | MetadataFlags.PreserveUSOffsets | MetadataFlags.PreserveStringsOffsets;
            nativeModuleWriter.Cor20HeaderOptions.Flags = new ComImageFlags();
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("Saving...");
            moduleDef.NativeWrite(pathFile.Substring(0, pathFile.Length - 4) + ".justify.exe", nativeModuleWriter);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Done!");
            Thread.Sleep(800);
        }
    }
}

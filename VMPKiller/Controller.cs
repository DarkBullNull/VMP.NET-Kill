using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dnlib.DotNet;

namespace VMPKiller
{
    public class Controller
    {
        public Controller(ref ModuleDefMD moduleDef, string pathFile, int userParams)
        {
            var imageProtectedVanillaFile = Path.GetDirectoryName(pathFile) + @"\vmp.exe";
            if (userParams == 1)
            {
                var bypassVirtualMachine = new BypassVirtualMachine(ref moduleDef);
            }
            else if (userParams == 2)
            {
                var aTricks = new AntiTricks(ref moduleDef, imageProtectedVanillaFile);
                var tryFixCorruptMethods = new FixCorruptMethods(ref moduleDef);
            }
            else if (userParams == 3)
            {
                var aTricks = new AntiTricks(ref moduleDef, imageProtectedVanillaFile);
                var bypassVirtualMachine = new BypassVirtualMachine(ref moduleDef);
                var tryFixCorruptMethods = new FixCorruptMethods(ref moduleDef);
            }
            else if (userParams == 4)
            {
                var tryShowRestoreMethods = new RestoreMetadatas(ref moduleDef, pathFile);
                Console.WriteLine("Continue?");
                Console.ReadKey();
            }
            
        }
    }
}

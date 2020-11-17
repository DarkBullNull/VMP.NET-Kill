using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dnlib.DotNet;

namespace VMPKiller
{
    public class Controller
    {
        public Controller(ref ModuleDefMD moduleDef, string folderPathFile, int userParams)
        {
            if (userParams == 1)
            {
                BypassVirtualMachine bypassVirtualMachine = new BypassVirtualMachine(ref moduleDef);
            }
            else if (userParams == 2)
            {
                AntiTricks aTricks = new AntiTricks(ref moduleDef, folderPathFile);
            }
            else if (userParams == 3)
            {
                AntiTricks aTricks = new AntiTricks(ref moduleDef, folderPathFile);
                BypassVirtualMachine bypassVirtualMachine = new BypassVirtualMachine(ref moduleDef);
            }
        }
    }
}

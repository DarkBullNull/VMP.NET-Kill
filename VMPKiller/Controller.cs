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
        public Controller(ref ModuleDefMD moduleDef, string folderPathFile)
        {
            AntiTricks aTricks = new AntiTricks(ref moduleDef, folderPathFile);
        }


    }
}

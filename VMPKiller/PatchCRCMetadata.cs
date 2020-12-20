using System;
using System.IO;

namespace VMPKiller
{
    public class PatchCRCMetadata
    {
        byte[] searchPatternFirst =
        {
            0x02, 0x00, 0x02, 0x00, 0x02, 0x00, 0x00, 0x06, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x42
        }; // The array to search, if first byte != ILOnly, then patch on ILOnly
        byte[] searchPatternSecond =
        {
            0x02, 0x00, 0x02, 0x00, 0x06, 0x00, 0x00, 0x06, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x42
        };

        private byte[] searchPatternThird =
        {
            0x48, 0x00, 0x00, 0x00, 0x02, 0x00, 0x05, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0x02
        };

        public PatchCRCMetadata(string pathFile)
        {
            byte[] bytesData = File.ReadAllBytes(pathFile);
            var byteILOnlyPositionF = GetPositionAfterMatch(bytesData, searchPatternFirst);
            var byteILOnlyPositionS = GetPositionAfterMatch(bytesData, searchPatternSecond);
            var byteILOnlyPositionT = GetPositionAfterMatch(bytesData, searchPatternThird);
            if (byteILOnlyPositionF != 0)
            {
                Console.WriteLine("Found 0x02 byte! Patch .NET byte ILOnly...");
                bytesData[byteILOnlyPositionF] = 0x03;
                File.Delete(pathFile);
                File.WriteAllBytes(pathFile, bytesData);
                Console.WriteLine("Complete!");
            }
            else if (byteILOnlyPositionS != 0)
            {
                Console.WriteLine("Found 0x06 byte! Patch .NET byte");
                bytesData[byteILOnlyPositionS] = 0x03;
                File.Delete(pathFile);
                File.WriteAllBytes(pathFile, bytesData);
                Console.WriteLine("Complete!");
            }
            else if(byteILOnlyPositionT != 0)
            {
                Console.WriteLine("Found 0x02 byte! Patch .NET byte");
                bytesData[byteILOnlyPositionT + 16] = 0x03;
                File.Delete(pathFile);
                File.WriteAllBytes(pathFile, bytesData);
                Console.WriteLine("Complete!");
            }
            else
            {
                Console.WriteLine("No pattern found, patching manually!");
            }
        }
        
        int GetPositionAfterMatch(byte[] data, byte[]pattern)
        {
            for (int i = 0; i < data.Length - pattern.Length; i++)
            {
                bool match = true;
                for (int k = 0; k < pattern.Length; k++)
                {
                    if (pattern[k] == 0xFF)
                    {
                        continue; // 0xFF byte - skip (analogue ??)
                    }
                    if (data[i + k] != pattern[k])
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                {
                    return i;
                }
            }
            return 0;
        }
    }
}

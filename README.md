# VMP.NET-Kill

###### Guys, please read the README on github before you ask questions.
- [x] Remove mutations. For using, need unpack vmp anti-tamper. Tools: https://yadi.sk/d/OXEqQ_3UfcVLag (thank you wwh1004! https://github.com/wwh1004)
- [x] Bypass anti virtual-machine
- [x] Bypass anti-debugging
- [x] Bypass CRC
- [x] Unpacking. Tools: https://yadi.sk/d/DtpNdfpIOj_cZQ (thank you codecracker! https://github.com/CodeCrackerSND)

This program allows you to unpacking binary, bypass CRC checking, anti-debugging and anti Virtual-Machines. (VMProtect all versions)

# The first-file must be named: "vmp.exe" (orig CRC-file checksum)(and don't delete it!)

Instructions:
1) Use tools SMD, for unpacking vmp
2) if needed, use my tools for bypass vmp tricks
3) wait devirt :D


Fix CRC-checking (Look for a class with a similar signature):
```csharp
public uint method_0(IntPtr intptr_0, uint uint_1)
    {
        uint num = 0U;
        int num2 = 0;
        while ((long)num2 < (long)((ulong)uint_1))
        {
            num = (CURRENT_CLASS.uint_0[(int)(((uint)Marshal.ReadByte(new IntPtr(intptr_0.ToInt64() + (long)num2)) ^ num) & 255U)] ^ num >> 8);
            num2++;
        }
        return ~num;
    }
```

https://yougame.biz/threads/166893/ - guide on RU lang

https://www.sendspace.com/file/a7yptr - tools

### https://youtu.be/zvoY0UOsceM - video-guide

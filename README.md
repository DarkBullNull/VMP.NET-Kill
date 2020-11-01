# VMP.NET-Kill
This program allows you to bypass CRC checking and anti-debugging. (VMProtect v3.5 build 1213)
# Doesn't work when the app is packaged(need unpack).
# The first-file must be named: "vmp.exe" (orig CRC-file checksum)(and don't delete it!)

Fix CRC-checking (Token: 0x0600008B - Not always, there may be a different token. !You can find GClass7 - 99%!):
```csharp
public uint method_0(IntPtr intptr_0, uint uint_1)
    {
        uint num = 0U;
        int num2 = 0;
        while ((long)num2 < (long)((ulong)uint_1))
        {
            num = (GClass7.uint_0[(int)(((uint)Marshal.ReadByte(new IntPtr(intptr_0.ToInt64() + (long)num2)) ^ num) & 255U)] ^ num >> 8);
            num2++;
        }
        return ~num;
    }
```

https://yougame.biz/threads/166893/ - guide on RU lang

https://www.sendspace.com/file/a7yptr - tools

### https://youtu.be/zvoY0UOsceM - video-guide

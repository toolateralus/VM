using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using Newtonsoft.Json.Linq;
using VM.Hardware;

OS os = new();

Dictionary<byte[], Dictionary<int, Range>> MemoryDirectory = new();

while (true) 
{
    
    System.Console.WriteLine("Any key to write");
    Console.ReadKey();

    int address = 1; 
    int length = 32;

    var ram = os.MemoryManager.GetDevice();

    if (ram == null)
    {
        System.Console.WriteLine("RAM NOT FOUND");
        continue;
    }

    var deviceHandle = ram.Specifications["ID"] as byte[];

    if (deviceHandle == null)
    {
        System.Console.WriteLine("DEVICE_HANDLE NOT FOUND");
        continue;
    }

    if (MemoryDirectory.TryGetValue(deviceHandle, out var block))
    {
        System.Console.WriteLine($"MEMORY IN USE {string.Join(",\n", deviceHandle)}");
        continue;
    }
    else 
    {
        os.MemoryManager.Allocate(address, length, deviceHandle);
    }
   

    Console.Clear();

    System.Console.WriteLine(JObject.FromObject(os.MemoryManager).ToString());
}
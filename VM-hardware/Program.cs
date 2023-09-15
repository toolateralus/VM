using VM.Hardware;

OS os = new();

var ram = os.MemoryManager.Devices.Where(D=>D.GetType() == typeof(RamEmulator)).First();

os.MemoryManager.Allocate(1, 32, (ram.Key.Specifications["ID"] as byte[])!);
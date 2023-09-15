using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft;

namespace VM.Hardware
{


// we can implement some kind of hardware configuration file so we don't have to manually code the stuff, 
    // but during development that's fine.
    public class OS
    {
        public MemoryManager MemoryManager = new();
       
    }

    public interface IHardwareDriver{

    }
    public class MemoryManager
    {
        // we'll load these from a file or some kind of configuration when done
        public Dictionary<IMemoryDevice, List<IHardwareDriver>> Devices = new();
        public Dictionary<byte[], List<MemoryRegion>> Memory = new();
        public long TotalRAM, WordSize =4;
        public int Allocate(long address, long length, byte[] deviceID)
        {
            var device = Devices.Where(device => device.Key.Specifications["ID"] as byte[] == deviceID).First().Key;
            
            for (int i =0; i < length; i += (int)device.WordSize)
            {
                device.Write(i, new byte[device.WordSize]);
            }

            return 0;
        }
        public MemoryManager()
        {
            Devices = new()
            {
                {new RamEmulator(), new()}
            };

            foreach(var device in Devices)
            {
                var handle = device.Key.Specifications["ID"] as byte[];

                if (handle is null){
                    System.Console.WriteLine($"Memory device {device.GetType()} did not contain an appropriate byte[] handle, and was not initialized.");
                    return;
                }

                var size = device.Key.Capacity;
                var wordSize = device.Key.WordSize;

                TotalRAM += size;

                if (this.WordSize != wordSize) {
                    Console.WriteLine($"Device {device} : Incorrect word size for the operating system. this is a {WordSize * 8}-bit OS, and the device is {wordSize*8}-bit");
                }

                Memory.Add(handle, new());
            }
        }
    }
    /// <summary>
    /// for now, we're just gonna implement contiguous regions of memory, with no fragmentation supported
    /// </summary>
    public struct MemoryRegion
    {
        public readonly int Address, Length;
        public byte[] DeviceID;
        public MemoryRegion(byte[] id, int address, int length)
        {
            this.DeviceID = id;
            this.Address = address;
            this.Length = length;
        }
    }
    /// <summary>
    /// This enum controls the level of access a particular IHardware device emulator grants to the user.
    /// </summary>
    public enum UserPermissions
    {
        /// <summary>
        /// No access to the hardware resource
        /// </summary>
        None,
        /// <summary>
        /// Read-only access
        /// </summary>
        Read,
        /// <summary>
        ///  Write-only access
        /// </summary>
        Write,
        /// <summary>
        /// Read and write access
        /// </summary>
        ReadWrite,
        /// <summary>
        /// Control or manage the hardware resource
        /// </summary>
        Control,
        /// <summary>
        /// Full access to the hardware resource (including configuration)
        /// </summary>
        FullAccess
    }

        public interface IHardware
    {
        public abstract Dictionary<string, object> Specifications { get; set; }
        public abstract void Cycle(object[] parameters);
        public abstract long GetPowerConsumptionStats();
        public abstract long GetTemperatureStats();

        /// <summary>
        /// this is the interface for drivers
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public abstract int[] Initialize(params object[] parameters);
        /// <summary>
        /// this is the exit interface for drivers
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public abstract int[] Finalize(params object[] parameters);
    }
    public interface IMemoryDevice : IHardware
    {
        public abstract byte[][] Memory { get; set; }
        public abstract long OccupiedMemory_Bytes { get; set; }
        public abstract long WordSize { get; set; }
        /// <summary>
        /// This resets a region of memory at specified address
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public abstract bool Free(int address);
        public abstract long Capacity { get; set; }
        /// <summary>
        /// Reads the memory from the specified address.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public abstract byte[] Read(int address);
        /// <summary>
        /// Writes to the device at address with data.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="memory"></param>
        /// <returns></returns>
        public abstract bool Write(int address, byte[] memory);
        /// <summary>
        /// Returns an action that will be used to fully factory reset the memory device.
        /// </summary>
        /// <returns></returns>
        public abstract Action GetDeviceResetAction();
    }
    /// <summary>
    /// Enumerates the processing statuses of a processing device.
    /// </summary>
    public enum ProcessingStatus
    {
        /// <summary>
        /// The device is idle or not currently processing any tasks.
        /// </summary>
        Idle,

        /// <summary>
        /// The device is actively processing a task.
        /// </summary>
        Active,

        /// <summary>
        /// An error occurred during processing, and the device is in an error state.
        /// </summary>
        Error,

        /// <summary>
        /// The device is paused and not processing tasks.
        /// </summary>
        Paused
    }
    /// <summary>
    /// Represents an interface for processing devices.
    /// </summary>
    public interface IProcessingDevice : IHardware
    {
        /// <summary>
        /// Gets the current processing status of the device.
        /// </summary>
        ProcessingStatus Status { get; }

        /// <summary>
        /// Executes a processing task with the given input.
        /// </summary>
        /// <param name="input">Input data for the processing task.</param>
        /// <returns>Output data produced by the processing task.</returns>
        Task<object> ExecuteProcessingTask(object input);

        /// <summary>
        /// Pauses the processing device, temporarily halting task execution.
        /// </summary>
        void Pause();

        /// <summary>
        /// Resumes processing after a pause.
        /// </summary>
        void Resume();

        /// <summary>
        /// Resets the processing device, clearing any error states or pending tasks.
        /// </summary>
        void Reset();
    }
}
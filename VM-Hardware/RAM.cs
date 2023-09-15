using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic;
using VM.Hardware;

namespace VM.Hardware
{

    public enum DevicePrimitiveTypes
    {
        RAM,
        DISK,
        ROM,
        CPU,
        GPU,
        POWER,
        /// <summary>
        /// Network
        /// </summary>
        NIC,
        /// <summary>
        /// Audio
        /// </summary>
        PCM,
        /// <summary>
        /// Aux/Unforseen?
        /// </summary>
        CIRCUIT,
        BATTERY,
        PERIPHERAL,
    }

    public class RamEmulator :  IMemoryDevice
    {

        public Dictionary<string, object> Specifications { get; set; } = new() 
        {
            {"NAME", "default ram"},
            {"CAPACITY", "1 GB"},
            {"ID", Guid.NewGuid().ToByteArray()},
        };
        /// <summary>
        /// <code>
        /// Note : This is measured in bytes
        /// 
        /// ((capacity 1_073_741_824 B / regions 1_048_576) bytes) 1024 MB / 1 GB DEFAULT CAPACITY))
        /// 
        /// </code>
        /// </summary>
        public long Capacity { get; set; } = 1_073_741_824;
        /// <summary>
        /// <code>
        /// 32 bit 
        /// 
        /// This is measured in bytes, and has a default of 4.
        /// </code>
        /// </summary>
        public long WordSize { get; set; } = 4;
        public byte[][] Memory { get; set; } = new byte[0][];
        public long OccupiedMemory_Bytes { get; set; }

        public void Cycle(object[] parameters)
        {
            // we do nothing in the case of ram as of rn, i guess we could mess with some like latency and randomness here
        }

        public int[] Initialize(params object[] parameters)
        {
            return new[] { 0 };
        }

        public int[] Finalize(params object[] parameters)
        {
            return new[] { 0 }; //FlattenAndCloneMemory();
        }

        public int[] FlattenAndCloneMemory()
        {
            int totalIntegers = 0;
            for (int i = 0; i < Memory.Length; i++)
            {
                if (Memory[i] != null)
                {
                    totalIntegers += Memory[i].Length / sizeof(int);
                }
            }

            int[] data = new int[totalIntegers];
            int dataIndex = 0;

            for (int i = 0; i < Memory.Length; i++)
            {
                byte[] block = Memory[i];

                if (block != null)
                {
                    for (int j = 0; j < block.Length; j += sizeof(int))
                    {
                        int intValue = 0;
                        for (int k = 0; k < sizeof(int); k++)
                        {
                            if (j + k < block.Length)
                            {
                                intValue |= (block[j + k] << (8 * k));
                            }
                        }
                        data[dataIndex++] = intValue;
                    }
                }
            }

            return data;
        }

        public Action GetDeviceResetAction()
        {
            return () => { Memory = new byte[0][]; };
        }

        public long GetPowerConsumptionStats()
        {
            return (long)Math.Floor(0.1 * OccupiedMemory_Bytes);
        }

        public long GetTemperatureStats()
        {
            return (long)Math.Floor(0.01 * OccupiedMemory_Bytes);
        }

        public byte[] Read(long address)
        {
            return Memory[address];
        }
        public bool Write(long address, byte[] data)
        {
            Memory[address] = data;
            return true;
        }
        public bool Free(long address)
        {
            Memory[address] = null!;
            return true;
        }
    }

}
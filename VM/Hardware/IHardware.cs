using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


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
    public abstract int ID {get;set;}
    /// <summary>
    /// We will use this as a way to pass some crucial resources to our emulated drivers
    /// </summary>
    public abstract Dictionary<object, object> DriverInfo {get;set;}
    /// <summary>
    /// We'll use this just as a descrption for the specific piece of hardware, like serial no, manufacturer, name, architecture, whatever it may be.
    /// </summary>
    public abstract Dictionary<string, string> HardwareSpecs {get;set;}
    public abstract int[] Initialize(params object[] parameters);
    public abstract int[] Finalize(params object[] parameters);
}

public interface IMemoryDevice : IHardware
{
   
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

public interface IProcessingDevice : IHardware
{



}
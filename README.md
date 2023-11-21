# Welcome to Lemur VDK - Your Virtual Desktop Kit

## Essential Requirements
Before you begin, ensure you have the following prerequisites:

- **Operating System**: Windows 7 - Windows 11
- **IDE**: Visual Studio 2019 or newer for optimal WPF and C# support
- **.NET**: SDK and Runtime 6.0 or higher (7.0+ is also supported)

## For Linux Users
A Linux version is currently under development with AvaloniaUI. The UI is still basic, and contributions are highly appreciated.

## Recommended Tools
- **Visual Studio Code**: Any version is suitable

## Getting Started

### Cloning the Repository
To clone the repository, follow these steps:

1. Open Visual Studio and select 'Clone a Repository'.
2. Enter the repository URL: `https://github.com/toolateralus/lemur-vdk.git`.
3. Choose your preferred local path and initiate the cloning process.

### Building the Project
Once the project is cloned:

1. Ensure the repository structure is intact after cloning.
2. In Visual Studio, select 'Release' mode.
3. Right click on `lemur` in the Solution Explorer and click Build, or Build the project using the `Ctrl + Shift + B` shortcut.

**Important**: If using Ctrl+Shift+B, An expected error will occur : `Program does not contain a static 'Main' method suitable for an entry point`. This error is a known issue and should not affect the build process. This is from a deprecated and not yet removed dependency.

### Running Lemur VDK
After building:

1. Navigate to `lemur/lemur-vdk/bin/Release/net6.0/`.
2. Locate `lemur.exe`, right-click, and create a shortcut on your desktop for easy access.

**Note**: Replace `net6.0` with the correct .NET version if you are using a different one.

### First Run and Installation
On the first launch, the application will automatically install necessary JavaScript, JSON, and other configuration dependencies.

1. Launch `lemur.exe`.
2. Press new computer, optionally adding an integer ID, or defaulting to 0. this refers to virtual file system copies / 'installs' on your local disk.
- The install will be placed at `%Appdata%/Lemur/...` and will be called `computer0` or whatever index you chose.


**Note**: 
Lemur VDK's file system searches for files and directories by name, making it essential to use unique file names or qualified paths for accuracy.
Misuse can lead to unintended side effects, like retrieving the wrong `setup.ini` file if multiple files share the same name without a specific path.
you can provide semi qualified paths like `parent/myFile.txt` if you have several myFile.txt's

Your feedback and suggestions for improvements are always welcome.


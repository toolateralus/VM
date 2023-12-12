
# Welcome to Lemur VDK 
## Micro tutorial 
#### for windowing
- simply hover over the edge of a window to resize, you'll see different button bars appear when you're in the right spot. click and drag to resize.
- to drag, left click & drag. you can grab anywhere that doesn't handle a click event, which on a fresh app is anywhere, or on any app, the title bar.
#### for making applications
- make sure you check out the in-app documentation, just click navigate with the file explorer, double click a .md file, and click 'render .md' to view docs in-App. theyre rough & unfinished.
#### user data location
- computers get created in User/Appdata/Roaming/Lemur/..., this is your virtual file system.
### notes
- knowledge of wpf and javascript in a freestanding environment is useful

## Essential Requirements
Before you begin, ensure you have the following prerequisites:

- **Operating System**: Windows 7 - Windows 11
- **IDE**: Visual Studio 2019 or newer for optimal WPF and C# support
- **.NET**: SDK and Runtime 8.0+

## Recommended Tools
- **Visual Studio Code**: Any version is suitable
- **Visual Studio Community 2022**: this is very nice for creating complex XAML front ends, it's editor is hard to beat.
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

### Running Lemur VDK
After building:

1. Navigate to `lemur/lemur-vdk/bin/Release/net8.0/`.
2. Locate `lemur.exe`, right-click, and create a shortcut on your desktop for easy access.

**Note**: Replace `net8.0` with the correct .NET version if you are using a different one.

### First Run and Installation
On the first launch, the application will automatically install necessary JavaScript, JSON, and other configuration dependencies.

1. Launch `lemur.exe`.
2. Press Re\Install, optionally adding an integer ID, or defaulting to 0. this refers to virtual file system copies / 'installs' on your local disk.
- The install will be placed at `%Appdata%/Lemur/...` and will be called `computer0` or whatever index you chose.


**Note**: 
Lemur VDK's file system searches for files and directories by name, making it essential to use unique file names or qualified paths for accuracy.
Misuse can lead to unintended side effects, like retrieving the wrong `setup.ini` file if multiple files share the same name without a specific path.
you can provide semi qualified paths like `parent/myFile.txt` if you have several myFile.txt's




Your feedback and suggestions for improvements are always welcome.


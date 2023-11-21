# welcome to lemur-vdk!
a playground like virtual desktop, inspired by ComputerCraft (minecraft mod), aims to bring a similar functionality to a standalone desktop app.
featuring a custom JS/XAML implementation, we use WPF as a front end, which allows for quick development of flexible UI. 


** disclaimer, this only affects those who use network services from within the environment, which right now are limited to : 
  Uploading directories / files to host.
  Downloading directories / files from host.
  Sending 'messages' (base 64 encoded broadcast to listeners on 'ch')
  Recieving 'messages'
**

Note: This includes a pretty insecure local-network system, and you should only use it (the network/server) at your own risk.
It features the ability to upload/download directories and files to a 'server', which is hosted on your local network.
It's a TCP server with no security measures.

#### After building and running the startup / installation / first run, you'll get a copy of the documentation, including tutorials and startup info.

> # Welcome to _Lemur VDK_ - A 'Virtual Desktop Kit'
---
> ## Dependencies you will need : 
`Some version of Microsoft Windows between Windows 7 and Windows 11`
> Note :  there is a linux version in development using Avalonia but it's far behind this repo and the UI is non-existent. consider contributing :D. It's not an impossibility, in fact its quite realistic to port it.

`Visual Studio (preferably 2019+, I use 2022)` for me it has the best WPF and C# support.

`.NET SDK 6.0+` you can run on 7.0+ if you'd like

`.NET Runtime 6.0+` you can run on 7.0+ if you'd like

> ## Optionally, but reccomended : 

`Visual Studio Code` (any version, I use Insiders for pre-release features.)

---
   
> ## Building the repository
> Right now, our installer depends on building from source. it's a janky piece of code that needs to be replaced, but until that's done, cloning & building is quite simple and gives you the flexibility to mod the engine / vdk.

---
> ## Cloning
> Step 1. Open Visual Studio (VS22/ VS) and click 'Clone a Repository' or whatever the equivalent is.

> Step 2. in the URL bar, put `https://github.com/toolateralus/lemur-vdk.git`

> Step 3. set a desired path and press Clone. 

> note the repo structure should NOT change, especially the name and orientation of the folders.

> Step 4. At the top of the Visual Studio instance, once the project has loaded, there will be a dropdown that says either 'Release' or 'Debug' to the left of the green play buttons. choose 'Release' and then do the key chord `Ctrl + Shift + B` to build.

> Note: There `WILL` be exactly 1 expected error while building the solution : `Severity	Description	Project Rank
Error	Program does not contain a static 'Main' method suitable for an entry point	1`
> Do not worry about this. it will only happen once, and means nothing. its a wip.


> Step 5. You can navigate to the `lemur/lemur-vdk/bin/Release/net6.0/lemur.exe` file and right click the executable. select 'Send To' and then press desktop, or wherever you want a fast way to run the vdk.

> if you're not using 6.0 remember to replace it with the appropriate .net version.

> for contributors : please streamline this process! we can provide a install batch and bash script that does all of this, and cleans up after itself.
---
> ## First run - Installing the OS and Required Components.

> There are a number of javascript and json and other config dependencies that get installed on the first-run of the application. Don't worry, we have an installer that does all of this automatically.

> Step 1. Double click the executable and launch the app.
> you'll be greeted with a small window in the center of the screen your cursor is on. For 99.9% of cases, you can just start the computer at the ID 0. 
>> changing this ID will install new virtual file systems under those indices, such as Appdata/Lemur/computer0, computer1, and so on and so forth.

>> Step 2. (Only if step 1 failed:)
>> if for some reason you experience a failure on the first installation, you'll have to delete the computer0 directory under your `%Appdata%/Lemur/` directory

### Your newly installed system will be available at `%Appdata%/Lemur/computer0`. Just drop that in the navigation bar in the windows explorer and press enter.

> ## an important note about startup!

> See the `computer/utils/this.ins` or for new systems `computer/utils/QuickStart.md` file to learn more about skipping the startup selection screen and quickbooting into specific computers

---
## a very important note about the file system,
---
#### everything is always searched for by file name. 
###### you can search for directories like this too.
---
##### the sole function that is the core of ALL file system accesses

> `FileSystem.GetResourcePath(string path)` 

> considers these various arguments ALL valid, and point to the same directory, `"myApp.app"` 

The arguments are : 
> `"User/Appdata/Roaming/Lemur/computer0/home/apps/myApp.app"`, 
> `"/home/apps/myApp.app"`
> `"myApp.app"`

> The file system will return the first best-matching fit. this means that you should really make files have fully unique names, or always fetch them from a qualified path. 

- Why does this concern me?
> because all of your files are written, read, and searched for by this function. 

- Do I have to use this?
> probably not. beware it may take some significant effort to make it function as a normal File.Read() would in C#.

> #### _this can have unintended side effects if used improperly_, 
> such as when you have multiple files named like setup.ini, and don't access them by their parent directory explicitly. it will just return the first match.

## All searches begin at `computer/..` and recursively search subdirectories 100 deep. 

>  so the closer to that folder (i.e 'computer0'), the sooner that file will be selected. any guarantees outside of that cannot be made.
---


extra:

---

#### Why did you do this to the FileSystem?

>  it's my preference to streamline file system accesses, and to eliminate the frustrating issue of restructuring or refactoring projects and file systems and having dependencies be lost.

> hard-coded paths do not work great with IDE's and refactoring tools

> I know it's somewhat unconventional and I suppose I am open to reasonable arguments against it.

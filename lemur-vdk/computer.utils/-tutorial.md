## Welcome to _Lemur VDK_ - A 'Virtual Desktop Kit'



## Intro to programming in Lemur : 
### _In this tutorial we're going to :_

- learn about the file structure of _Lemur_ and how to setup dev tools.
- cover the types of executables (really jittables) allowed in _Lemur_
- describe some use cases to help you pick an app type for your program
- learn how to use the command line and text editor in _Lemur_

### setting up external development tools : 

to create & locate a computer, first you need to run the app.
first time runs an installation sequence, which will install the standard libraries, operating system (js portion), and a basic directory structure with samples and assets. This structure is not required but it is reccomended, for git-like usage over network. if you don't need compatability with other _Lemur_'s, you can move anything anywhere.

#### a very important note about the file system, everything is always searched for by file name. (you can search for directories like this too.)

`FileSystem.GetResourcePath(string path)`

 takes anything from 

`"User/Appdata/Roaming/Lemur/computer0/home/apps/myApp.app"`, 

`"/home/apps/myApp.app"`

   to

`"myApp.app"`

 and will return the best-matching fit. this means that you should really make files have fully unique names, or always fetch them from a qualified path. 

 #### _this can have unintended side effects if used improperly_, 

 #### such as when you have multiple files named like setup.ini, and don't access them by their parent directory explicitly. it will just return the first match.


#### note, you don't neccesarily _NEED_ vscode

- However, if you need/ are really used to things like LSP (red squigglys and suggestions, intellisense) support, you can just open up your
computer in vscode with a couple extensions. Extensions are pure preference, and vscode comes with a .js lsp, so it should automatically have excellent support.

- note that working in _Lemur_ is much like working in a free-standing environment, for kernel or embedded development. Meaning, you likely do not have access to many common, seemingly base-line language features. such as typical imports and exports, meaning if you need
third party software, it's likely you'll need to either copy paste it into a document and 'require' it (our node-like module system), or
just write it yourself.

- when I started this project, I felt un-able to develop without the lsp.
the less you use it, for this kind of tiny code and very small apps,
you can fill that void and confusion. However, while we aren't _CURRENTLY_ able to support things like
refactoring symbols and deep searches, or even swapping documents quickly and 
modifying file structure from in-ide, you should probably keep vscode on-deck.
it's also great for catching syntax errors.




# Creating a XAML/WPF/JS Application in Lemur :
### _In this tutorial we're going to :_

- learn how to create open and edit text files.

- learn the requirements for a '.app' application, setup our directories and files to create a valid .app project. 

- create a 'hello world' GUI app.


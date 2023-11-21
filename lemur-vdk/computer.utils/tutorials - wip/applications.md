
## Intro to programming in Lemur : 
#### _In this tutorial we're going to :_

- learn about the file structure of _Lemur_ and how to setup dev tools.

- cover the types of executables (i refer to them as `jittables` :D) allowed in _Lemur_

- describe some use cases to help you pick an app type for your program

### Setting up external development tools : 
###### See the intro to lemur tutorial to learn more about this process.

#### you don't absolutely _NEED_ vscode .. but..

> However, if you need/ are really used to things like LSP (red squigglys and suggestions, intellisense) support, you can just open up your
computer in vscode. Extensions are pure preference, and vscode comes with a `javascript lsp`, so it should already have excellent support.

>> note that working in _Lemur_ is much like working in a free-standing environment, for kernel or embedded development. Meaning, you likely do not have access to many common, seemingly base-line language features. such as typical imports and exports, meaning if you need third party software, it's likely you'll need to either copy paste it into a document and 'require' it (our node-like module system), or just write it yourself.

Not using vscode can be much more fun in terms of the emulation and experience, but vscode is practically neccesary to mid-level programming needs, like refactoring, very detailed syntax highlighting, and lsp support.

---
# Apps in Lemur
- There are three kinds of user-space application you can create in _Lemur_. You should be aware of the options even though there is a HEAVILY suggested option.
### The reccomended option
#### JS / WPF _Lemur Native_ app.
- This is the type of app the project was created to run !
- It features a free-standing javascript environment that we've implemented a bunch of WPF hooks and functionality into, so you can create WPF apps with java script as your backend 100% of the way.
> This has its limitations and resistances, but its our greatest goal to make this development process super intuitive, performant, and fun to use. for the beginner, intermediate, or expert!


#### JS / HTML Web app run in a browser container
- I don't know too much about web dev- so I don't have much to say here. the browser container is a modern browser, Microsoft Edge, and should be fully featured. There are some sample apps chat gpt wrote
for this setup. 
- libraries, connectivity and security are things I know nothing about here.

####  C# / WPF app compiled into the source - do not use - 

- This app has the most flexibility as in it can link with many
external libraries and code bases since it's not limited to a free standing environment. 
- This app performs generally better than the other kinds due to it being compiled in the native language of the project.
- This app has the highest implementation overhead - They can be complex to write and implement compared to the other types.

##### a message for contributors ...
> This app type should only be used when _NEEDED_ by expert users or contributors, simply because it breaks the spirit of the project.  there is a roleplay element to emulation - and we didn't aim to make a plain C#/WPF runtime container, the remaining apps that are C# / WPF are all in the process of being ported to JS.




> ###### "and we didn't aim to make a plain C#/WPF runtime container" 
> ###### if you want that, just go use WPF!


> As for performance concerns : we have the philosophy that `if we want to provide a service like the javascript framework, it must be useable to the extent of us never needing to reach outside of it for our own apps using it.` and if we do need to do so, we implement a way to do so from the framework. 
- We hope, in doing this, we create a trustworthy, reliable, and well featured and capable framework.

- Just know that right now- that is NOT exactly what this is. it's very rough and erroneous. it's most certainly usable and pretty featured,
but it lacks reliability.
- and by trustworthy, I just mean users can trust it will be use-able enough to suit their hobby programming interests. for any skill range.



##### Most cases won't need this app, and even _Lemur_ contributions and new system functionalites can almost always be implemented in javascript. 

##### new `system-app / shipped-apps` pull requests must be in js/wpf, even if it requires new C# backends... but we would love to feature any of your apps!! at any skill level!

---
---
# Creating a JS/WPF Application in Lemur :
### _In this tutorial we're going to :_

- learn how to create, open, and edit text files.
- learn the requirements for a '.app' application, setup our directories and files to create a valid .app project. 
- create a 'hello world' GUI app that runs when our computer starts.

### _In this tutorial, we expect you :_

- are familiar with Javascript

- understand the implications and meaning of the phrase `freestanding environment` when it comes to compilation (a term used in kernel and embedded development)

- `are going to be patient if features do not behave as expected!!!`, 
especally if you plan to implement anything above a small-medium scale!!

this is very much so a WIP and is very rough around the edges!! 

---
#### a note to programmers

`users are highly encouraged to contribute anywhere they want.` 
Avoid applying massive complex systems where unneccesary, or making
hard-to-read 'clever' code, make things dead simple and safe (especially c#)

something the repository itself does not do :D 
(not saying my code is great at all lol)

---
### Creating & Opening Text Files & Associated Project Directories.

- for us to define a valid JS/WPF application, we need to define a folder structure that the system will recognize as a 'jittable'. I use this term as a runtime paralell to executable.

here are the rules : 
> Your app folder must end in a `.app` extension.

> You must have both a `<app_name>.xaml.js` and  `<app_name>.xaml` files in this directory, with the same name as the `.app` folder

> Your app folder is your apps name, which will need to match the name of your JS and XAML files, and the name of the JS class.

> You may have a `icon.bmp` file that will be used as a desktop-icon for your application. note it must be named exactly this, and must be directly under your `.app` directory

Let's visualize what this structure should look like, for our app called `home.app` :
> note this is displayed in a format much like the 'tree' command in linux, where each child is displayed using └── or ├── in a hierarchical fashion.
```
home.app         (required)
├── home.xaml.js (required)
├── home.xaml    (required)
└── icon.bmp     (optional)
```
This is the bare-minimum, and you can have any `.js` files in there along with your apps code, just these are used for creating and jitting your app.

To create this, for the easiest option, just use vs-code for now.

- Open `vs code` and navigate to the folder `%Appdata%/Lemur/computer0` _(or whichever computer id you're using)_

- un-collapse the `home/apps/` directory and right click, select `New directory`

- name it `home.app`

- right click again, select `New file`, create a file called `home.xaml.js`, duplicate it, and rename the duplicated file `home.xaml`

> now, if we tried to run this app, (which we theoretically can even without restarting the _Lemur_ application, because everything is jitted when you press the desktop icon!) we'd get errors because the xaml file is completely empty, and no matter what xaml errors can cause runtime interference and crashing.

#### `Just as a rule of thumb` generally the application WILL NOT CRASH under any circumstances. even in extremely erroneous conditions, you will only crash the app with XAML errors, or running out of resources (CPU/RAM)


Now that we have our file structure sorted, and it looks like the tree diagram, we can start actually adding some code to our app. So far it's been boring as fudge, but now we get to write some actual code. Just to get us started on our XAML boiler plate, we can navigate to the `computer/utils/base_app` directory and just copy paste the example's xaml into our `home.xaml` file.

Theres also a starter `base_app.xaml.js` file, which gets us up and going with the appropriate class and constructor setup. 

this step (constructor, class, filename) `BEYOND CRUCIAL` for WPF to work with your JS backend.

Just for ease of use here, these are the provided xaml / js files.
```xaml
<!-- for use with base_app (template) -->
<!-- from computer/utils/base_app/base_app.xaml -->

<UserControl xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006" 
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:local="clr-namespace:System.Windows.Controls;assembly=PresentationFramework"
             mc:Ignorable="d" 
             d:DesignHeight="450" d:DesignWidth="800" Background="#474354" BorderBrush="White" BorderThickness="5">
    <Grid>
        <!-- We use a StackPanel for easy allignment -->
        <StackPanel Orientation="Vertical">
            <!-- Js will write to this label -->
            <TextBlock  Margin="2,2,2,2" Foreground="Wheat" Background="#52525e" x:Name="textBlock" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Text=""/>
            <!-- This is an input box we can fetch the contents of in JS -->
            <TextBox BorderBrush="Black" Margin="2,2,2,2" x:Name="textBox" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Text="C://>.."/>
            <!-- Js hooks into the event to handle it -->
            <Button BorderBrush="Black" Margin="2,2,2,2" x:Name="showImageBtn" HorizontalAlignment="Left" VerticalAlignment="Top" Content="Load Image"/>
            <!-- We use this to draw / render the image. can also be used for game development, see `game2.app` -->
            <Image Margin="10,10,10,10" x:Name="renderTarget" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Stretch="Fill"/>
        </StackPanel>
    </Grid>
</UserControl>
```
```javascript
/* #### ATTENTION!! READ THIS FIRST! ####


    ### MAJOR ALERT ###
    Theres a tutorial for making wpf/js apps in the documentation. see this first.

    your class that you will use with WPF for ui management MUST have the SAME NAME
    as the <app_name>.app folder, the <app_name>.xaml file, and the <app_name>.xaml.js file.

    if you read the tutorial you already knew that B)
    just a reminder!

    ### PURPOSE OF THIS DEMO ###
        provide a application that shows some easy interactions in a somewhat bare-metal JS/XAML application.
        make an easy copy-paste template that can be used to avoid boilerplate.

*/
class base_app {
    loadImage() {
        
        const text = interop.pushEvent(this.__ID, 'textBox', 'get_content', '')
        
        // reads bytes of file at path : 'text',
        // converts those bytes to a base64 string and
        // returns that string to us, or null if failed.
        const background = interop.base64FromFile(text);
        
        // bad read, file not found, etc.
        if (background === null || background.length === 0){
            print('failed to get file ' + text)
            return;
        }

        // call to the wpf application to search this classes XAML for 'renderTarget', and call a
        // 'draw_image' event on it with 'background' as data.
        // in the case of 'draw_image' you must provide a base64 string representing a common file-format image. (.png, .jpg, .bmp)
        // this is achieved with the `interop.base64FromFile(filePath)` function
        app.pushEvent(this.__ID, 'renderTarget', 'draw_image', background);
    }

    
    // ## this constructor is 100% mandatory.
    // ## `this.__ID` is a clunky but temporarily neccesary part of this system,
    // ## which is an identifier the WPF uses to know who is calling/requesting events/hooks and who to affect with those events/calls,
    // ## which elements to use etc.
    // ## anything that's not wrapped in the do not edit is free game, just to clarify.
    // ## feel free to move or remove the invasive comments.

    /* DO NOT EDIT */ constructor(id) { /* DO NOT EDIT */ 
    /* DO NOT EDIT*/ this.__ID = id;  /* END DO NOT EDIT*/

        // call to the wpf to search this class's XAML for an element 'showImageBtn' 
        // and subscribe the javascript function (which is present in this class) called 'loadImage' to the 'MouseDown'
        // event of that 'showImageBtn' control/element.
        app.eventHandler(this.__ID, 'showImageBtn', 'loadImage', XAML_EVENTS.MOUSE_DOWN);

        // call to the wpf to search this class's XAML for a control/element 'textBlock' and call the 'set_content' event
        // with that control as an argument, along with our string data we want to fill that label.
        // Note, in the case of setting text, and many other properties, theres several ways to do so.
        // this one is more limited but maybe more efficient because it does not rely on reflection.
        let string = "enter a file path (relative or absolute to any degree)";
        app.pushEvent(this.__ID, 'textBlock', 'set_content', string);

        let x = false;
        // an alternative to the call would be 

        // the guard is meant to just note that this isn't expected to be called
        // as is, youd remove the if and the above app.pushEvent call.
        if (x)
            app.setProperty(this.__ID, 'textBlock', 'Content', string)

        // and this can be used for basically any WPF property name, restricted only by javascript vs c# typing.
        // the bounds of this isn't well documented but outside of creating new elements at runtime,
        // A LOT is possible.

        // use the WPF documentation to find out more about what elements have which properties and how they can be setted/gotten.
    }
}
```


- ##### Now, with that code in our files, and especially _with those comments and code READ_, we have a basic skeleton to get us started and running in a window with some bare functionality.

The next step would be actually installing our app, and running it.
Now, theres a few ways to do this, we're just going to choose what
will likely be the most common and easy to use. First, we need to talk about 

#### `startup.js`

`startup.js` is a file located within each computer that, as the title suggests, is run at start-up.

It doesn't matter where this file is as long as it's present.
Why is that useful for us here? Well, we have provided some functionalities kind of geared towards use in start up. One of those features is setting an `installed apps directory.` There's already a tutorial on startup and scripting, see the documentation.
Specifically here, we will just use this info to understand if we put our application in the default `installed apps directory`, `home/apps/...`
our app will be installed next startup and we don't have to worry about it again.

Apps must be reinstalled every time the computer starts as theyre all dynamically created runtime objects and there is no way to save them.

So now, with our app in the `computer/home/apps/` directory, we can close and re-open our _Lemur_ machine to install, or we can just test directly by running the command
`install 'home.app`
which temporarily installs to this instance of the _Lemur_ machine
for use this run-time. 

You'll know your app is installed when its added as an icon on the desktop. right now this is basically the only way to start apps, outside of triggering from code.

Try double clicking your icon, and a window should open with your styling and elements. If you enter a path into the bar, being sure to rid of the junk text that defaults in there, like `background.png`
and press the button, you should see an image of blue mountains appear.

boom!

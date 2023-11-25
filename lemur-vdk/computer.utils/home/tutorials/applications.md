
---
# Making an app :
### _In this tutorial we're going to :_

- learn how to create, open, and edit text files.
- learn the requirements for a '.app' application, setup our directories and files to create a valid .app project. 
- create a 'hello world' GUI app that runs when our computer starts.

_In this tutorial, we expect you :_
---

- `are going to be patient if features do not behave as expected`, 
this project is very rough and is in very early development, so it is guaranteed you will run into things that do not work, do not work as expected, or are completely broken.

> This is a hobby project and has many flaws. it's very messy. but contributions (modifications, features, and especially bugfixes /  reworks of specific systems) are greatly appreciated. I am often available, and check github frequently.


Setting up the Application Directory
---

To define a valid JS/WPF application, you need to create a specific folder structure that the system recognizes as a 'jittable' runtime parallel to an executable.

Here are the rules:

1. Your app folder must have a `.app` extension.

2. Inside the `.app` folder, you should have the following files:

   - `<app_name>.xaml.js`: This file defines the JavaScript logic for your application. Replace `<app_name>` with your actual application name.

   - `<app_name>.xaml`: This file contains the XAML markup for your application's user interface. Replace `<app_name>` with your application's name.

   - `icon.bmp`: Optionally, you can include an `icon.bmp` file directly under your `.app` directory. This image will serve as the desktop icon for your application.

So our directory for our app called `home.app` would look like this :
```
home.app         (required)
├── home.xaml.js (required)
├── home.xaml    (required)
└── icon.bmp     (optional)
```

This is the bare-minimum, and you can have any files in there along with your apps code, these are just used for creating and jitting your app.

Ensure that the folder name, the names of the `.xaml` and `.xaml.js` files, and the name of the JavaScript class all match your application's name for proper recognition by the system.

This folder structure will make your application "jittable" and ready for installation, and then execution.



To create this, for the easiest option, just use vs-code for now.

- Open `vs code` and navigate to the folder `%Appdata%/Lemur/computer0` _(or whichever computer id you're using)_

- un-collapse the `home/apps/` directory and right click, select `New directory`

- name it `home.app`

- right click again, select `New file`, create a file called `home.xaml.js`, duplicate it, and rename the duplicated file `home.xaml`

> now, if we tried to run this app, (which we theoretically can even without restarting the _Lemur_ application, because everything is jitted when you press the desktop icon!) we'd get errors because the xaml file is completely empty, and no matter what xaml errors can cause runtime interference and crashing.

#### `Just as a rule of thumb` generally the application WILL NOT CRASH under any circumstances. even in extremely erroneous conditions, you will only crash the app with XAML errors, or running out of resources (CPU/RAM) anything outside of this is a bug, and there are many.

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
        provide an application that shows some easy interactions in a somewhat bare-metal JS/XAML application.
        make an easy copy-paste template that can be used to avoid boilerplate.
        provide obnoxiously extensive explanations of everything used, to educate beginners.

*/
class base_app {
    loadImage() {
        
        const text = interop.pushEvent(this.id, 'textBox', 'get_content', '')
        
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
        app.pushEvent(this.id, 'renderTarget', 'draw_image', background);
    }

    
    // ## this constructor is 100% mandatory.
    // ## `this.id` is a clunky but temporarily neccesary part of this system,
    // ## which is an identifier the WPF uses to know who is calling/requesting events/hooks and who to affect with those events/calls,
    // ## which elements to use etc.
    // ## anything that's not wrapped in the do not edit is free game, just to clarify.
    // ## feel free to move or remove the invasive comments.

    /* DO NOT EDIT */ constructor(id) { /* DO NOT EDIT */ 
    /* DO NOT EDIT*/ this.id = id;  /* END DO NOT EDIT*/

        // call to the wpf to search this class's XAML for an element 'showImageBtn' 
        // and subscribe the javascript function (which is present in this class) called 'loadImage' to the 'MouseDown'
        // event of that 'showImageBtn' control/element.
        app.eventHandler(this.id, 'showImageBtn', 'loadImage', XAML_EVENTS.MOUSE_DOWN);

        // call to the wpf to search this class's XAML for a control/element 'textBlock' and call the 'set_content' event
        // with that control as an argument, along with our string data we want to fill that label.
        // Note, in the case of setting text, and many other properties, theres several ways to do so.
        // this one is more limited but maybe more efficient because it does not rely on reflection.
        let string = "enter a file path (relative or absolute to any degree)";
        app.pushEvent(this.id, 'textBlock', 'set_content', string);

        let x = false;
        // an alternative to the call would be 

        // the guard is meant to just note that this isn't expected to be called
        // as is, youd remove the if and the above app.pushEvent call.
        if (x)
            app.setProperty(this.id, 'textBlock', 'Content', string)

        // and this can be used for basically any WPF property name, restricted only by javascript vs c# typing.
        // the bounds of this isn't well documented but outside of creating new elements at runtime,
        // A LOT is possible.

        // use the WPF documentation to find out more about what elements have which properties and how they can be setted/gotten.
    }
}
```

---
### Now, with that code in our files, and the comments read, we have a basic skeleton to get us started and running in a window with some bare functionality.

The next step would be actually installing our app, and running it.

Apps must be reinstalled every time the computer starts as theyre all dynamically created runtime objects and there is no way to save them.

To just make it easy, the computer defaults to searching the entire file structure at startup and 
linking & gathering any `.app` directories with valid contents.

You'll know your app is installed when its added as an icon on the desktop. right now this is basically the only way to start apps, outside of triggering from code.

Try double clicking your icon, and a window should open with your styling and elements. If you enter a path into the bar, being sure to rid of the junk text that defaults in there, like `background.png`
and press the button, you should see an image of blue mountains appear.

boom!

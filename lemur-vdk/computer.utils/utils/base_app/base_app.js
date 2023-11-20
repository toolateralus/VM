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
        // mostly everything is possible.

        // use the WPF documentation to find out more about what elements have which properties and how they can be setted/gotten.
    }
}
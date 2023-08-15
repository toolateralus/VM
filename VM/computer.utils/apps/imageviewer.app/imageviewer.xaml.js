// -- user -- name this class the (case sensitive) same as the name of the xaml file and this file. for generated code purposes.
class imageviewer {
    loadImage(sender, args) {
        const text = interop.pushEvent(this.__ID, 'textBox', 'get_content', '')
        
        const background = interop.fromFile(text);
        
        if (background === null || background.length === 0){
            print('failed to get file ' + text)
            return;
        }

        app.pushEvent(this.__ID, 'renderTarget', 'draw_image', background);
    }

    constructor(id) {

        /* DO NOT EDIT*/ this.__ID = id;  /* END DO NOT EDIT*/

        app.eventHandler(this.__ID, 'showImageBtn', 'loadImage', XAML_EVENTS.MOUSE_DOWN);
        app.pushEvent(this.__ID, 'textBlock', 'set_content', "enter a file path (relative or absolute to any degree)");
    }
}
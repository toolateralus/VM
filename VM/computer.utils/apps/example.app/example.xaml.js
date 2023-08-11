// -- user -- name this class the (case sensitive) same as the name of the xaml file and this file. for generated code purposes.
class example 
{
    btnPressed(sender, args){
        network.send(0, 1, "event");
    }
    constructor() {

        // you can replace 'example' with any word, letters only no numbers/punctuation

        /*readonly */ this.__ID = 'example{..}' /*readonly */

        app.eventHandler(this.__ID, 'shutdownButton' , 'btnPressed', XAML_EVENTS.MOUSE_DOWN);
        
        let image = image.fromFile('Background.png');

        app.pushEvent(this.__ID, 'renderTarget', 'draw_image', this.image);

        //app.PushEvent(^window ID, ^target control, ^event type, ^event data).
    }
}



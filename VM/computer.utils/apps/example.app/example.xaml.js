// -- user -- name this class the (case sensitive) same as the name of the xaml file and this file. for generated code purposes.
class example 
{
    btnPressed(sender, args){
        const text  = app.pushEvent(this.__ID, 'textBox', 'get_content', '')
        network.send(0, 1, text);
    }
    constructor() {
 
        /*const/readonly*/ this.__ID = 'example{..}' 

        app.eventHandler(this.__ID, 'shutdownButton' , 'btnPressed', XAML_EVENTS.MOUSE_DOWN);
        
        var background = image.fromFile('Background.png');
        
        app.pushEvent(this.__ID, 'renderTarget', 'draw_image', background);
        
        app.pushEvent(this.__ID, 'textBlock', 'set_content', "0.0.000.0");
    }
}



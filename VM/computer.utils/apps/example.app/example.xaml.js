// -- user -- name this class the (case sensitive) same as the name of the xaml file and this file. for generated code purposes.
class example 
{
    btnPressed(sender, args){
        network.send(0, 1, "event");
    }
    constructor() {
 
        /*const/readonly*/ this.__ID = 'example{..}' 

        app.eventHandler(this.__ID, 'shutdownButton' , 'btnPressed', XAML_EVENTS.MOUSE_DOWN);
        
        var background = image.fromFile('Background.png');
        
        app.pushEvent(this.__ID, 'renderTarget', 'draw_image', background);
        
    }
}



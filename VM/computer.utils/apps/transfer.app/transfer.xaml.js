// -- user -- name this class the (case sensitive) same as the name of the xaml file and this file. for generated code purposes.
class transfer {
    send(sender, args) {
        const content = app.pushEvent(this.__ID, 'text', 'get_content', '');
        print(content)
        const ch = app.pushEvent(this.__ID, 'ch', 'get_content', '');
        print(ch)

        if (content == undefined)
            print('was undefind')

        network.send(ch, 100, file.read(toString(content)));
    }

    constructor(id) {
        this.__ID = id;  
        
        /* 
            text
            ch
            send
            recieve 
        */


        app.eventHandler(this.__ID, 'send', 'send', XAML_EVENTS.MOUSE_DOWN);
        app.eventHandler(this.__ID, 'recieve', 'recieve', XAML_EVENTS.MOUSE_DOWN);

        var background = interop.fromFile('Background.png');

        app.pushEvent(this.__ID, 'renderTarget', 'draw_image', background);

        
    }
}
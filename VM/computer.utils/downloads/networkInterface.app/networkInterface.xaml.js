// -- user -- name this class the (case sensitive) same as the name of the xaml file and this file. for generated code purposes.
class networkInterface {
    btnPressed(sender, args) {
        network.connect(null);
        const text = app.pushEvent(this.__ID, 'msg', 'get_content', null)
        const ch = Number(app.pushEvent(this.__ID, 'ch', 'get_content', null))
        const reply = Number(app.pushEvent(this.__ID, 'replyCh', 'get_content', null))
        const connection = new Connection(ch, reply, text);
    }
    drawBackground(){
        var background = image.fromFile('Background.png');
        app.pushEvent(this.__ID, 'renderTarget', 'draw_image', background);
    }
    getEvents(){
        app.eventHandler(this.__ID, 'shutdownButton', 'btnPressed', XAML_EVENTS.MOUSE_DOWN);
    }
    constructor(id) {
        app.pushEvent(this.__ID, 'textBlock', 'set_content', 'CURRENT IP:'= network.ip());
        this.drawBackground();
        this.getEvents();
    }
}
class Connection{

    constructor(ch, replyCh, defaultMsg){
        this.CHANNEL = ch;
        this.REPLY = replyCh;
        this.DEFAULT = defaultMsg;
        this.timedOut = false;
        this.cancelled = false;
        this.timeout = timeout;
        this.startTimeout();
    }
    startTimeout(){
        setTimeout(() => { this.timedOut = true }, this.timeout)
    }
    async tryConnect(){
        while(true && !this.timedOut && !this.cancelled){
            network.send(this.CHANNEL, this.REPLY, this.DEFAULT);
            let response = "";
            response = await receive(this.REPLY)
        }
    }

}
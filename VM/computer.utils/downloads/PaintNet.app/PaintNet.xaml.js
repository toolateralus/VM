const Color = {
    RED: 0,
    ORANGE: 1,
    YELLOW: 2,
    LIME_GREEN: 3,
    GREEN: 4,
    SPRING_GREEN: 5,
    CYAN: 6,
    SKY_BLUE: 7,
    BLUE: 8,
    PURPLE: 9,
    MAGENTA: 10,
    PINK: 11,
    LIGHT_GRAY: 12,
    MEDIUM_GRAY: 13,
    DARK_GRAY: 14,
    BLACK: 15,
    WHITE: 16,
    RED_ORANGE: 17,
    GOLD: 18,
    DARK_GREEN: 19,
    TEAL: 20,
    NAVY: 21,
    DEEP_PURPLE: 22,
    DEEP_PINK: 23,
    MEDIUM_SPRING_GREEN: 24
};

// -- user -- name this class the (case sensitive) same as the name of the xaml file and this file. for generated code purposes.
class PaintNet {
    frameCt = 0;
    speed = 250;
    width = 24;
    bytesPerPixel = 4;
    data = [];
    newWidth = this.width;
    resizing = false;
    
    color1 = [255, 180, 255, 0];
    color2 = [200, 0, 90, 255];


    clean(color = 255) {
        this.width = this.newWidth;
        this.data = [];
        for (let y = 0; y < this.width; y++) {
            for (let x = 0; x < this.width; x++) {
                for (let index = 0; index < this.bytesPerPixel; index++) {
                    this.data.push(color);
                }
            }
        }
    }

    setWidth(width) {
        this.newWidth = width;
        this.resizing = true;
    }
    
    writePixel(x, y, color) {
        let index = (y * this.width + x) * this.bytesPerPixel;
        color.forEach(byte => {
            this.data[index] = byte;
            index++;
        });
        this.IsDirty = true;
    }
    
    captureTime(start) {
        if (start) 
        {
            this.startTime = new Date().getTime();
        } 
        else if (this.startTime !== 0 && this.frameCt % 10 === 0) 
        {
            const endTime = new Date().getTime();
            const instantFrameRate = endTime - this.startTime;
            app.pushEvent(this.__ID, 'framerateLabel', 'set_content', `frame time: ${instantFrameRate} ms`);
            this.startTime = 0;
        }
    }
    _render() {
        if (this.IsDirty === true)
        {
            this.captureTime(true);
            
            app.pushEvent(this.__ID, 'renderTarget', 'draw_pixels', this.data);

            this.frameCt++;

            if (this.resizing) {
                this.clean();
                this.resizing = false;
            }
            this.captureTime(false);
        }
    }

    lerpColors(a, b, t) {
        const result = new Uint8Array(4);
        for (let i = 0; i < 4; i++) {
            result[i] = Math.floor(b[i] * t + a[i] * (1 - t));
        }
        return result;
    }
    
    changeBrush(left, right){
        
        this.colorIndex ++;

        if (this.colorIndex > this.colors.length){
            this.colorIndex= 0;
        }

        const color = this.colors[this.colorIndex];
        const colors = [];

        for(let i = 0; i < 12; ++i){
            colors[i] = color;
        }
        
        app.pushEvent(this.__ID, 'brushColorImage', 'draw_pixels', colors);
    }
    
    onMouseMoved(X, Y){

        this.mouse.x = X;
        this.mouse.y = Y;
        // get the width/height of the current thing so we can scale the mouse
        const width = app.getProperty(this.__ID, 'renderTarget', 'ActualWidth');
        const height = app.getProperty(this.__ID, 'renderTarget', 'ActualHeight')

        if (this.mouse.right === true)
        {

            const X = Math.floor(this.mouse.x / width * this.width);
            const Y = Math.floor(this.mouse.y / height * this.width);

            const color = this.colors[this.colorIndex]
            
            this.writePixel(X, Y, color)

            app.pushEvent(this.__ID, 'renderTarget', 'draw_pixels', this.data);

            network.send(this.mp_ch, this.mp_reply, this.colorIndex)

            this.mpPendingAction = false;
        }
    }
    
    onMouseDown(left, right){
        this.mouse.right = right;
        this.mouse.left = left;
    }
    onConnect(){

        const isHost = app.getProperty(this.__ID, 'isHostCheckbox', 'IsChecked')

        const ch = app.getProperty(this.__ID, 'channelTxt', 'Text')
        const reply = app.getProperty(this.__ID, 'replyTxt', 'Text')
        
        if (isHost === undefined || ch === undefined || reply === undefined){
            print(`isHost : ${isHost} ch : ${ch} reply : ${reply}`)
            print('failed to get neccesary data from ui to connect. make sure your fields contain valid data.');
            return;
        }

        if (!network.IsConnected){
            print('You must connect to a network to use online multiplayer... attempting local {this machine}.')
        } 
        // Network receive seems to not be waiting properly, and if it does, it freezes every app.
        if (isHost === true){

            print(`Attempting to host on ${ch}::${reply}..`);

            network.send(ch, reply, 'REQUEST_GAME_START')

            const msg = network.receive(ch)
    
            if (msg === 'ACCEPT_GAME_START'){
                
                // starts game when the host right clicks.
                network.send(ch, reply, 'HOST_TURN')

                this.mpConnected = true; 
                this.mpPendingAction = true;
            }
            else{
                print('Failed to connect with another player.')
            }
        }
        else
        {
            print(`Trying to join game on ${ch}::${reply}..`);

            network.send(ch, reply, 'ACCEPT_GAME_START')

            const msg = network.receive(ch)
    
            if (msg === 'HOST_TURN'){
                // show waiting for player to go text.
                const move = network.receive(reply)
            }
            else{
                print('Failed to connect with another player.')
            }


        }
       
    }
    constructor(id) {
        // event handler hook handle
        this.__ID = id;
        
        this.IsDirty = true;

        this.colorIndex = 0;
        
        this.mouse = 
        {
            x: 0,
            y : 0,
            left : false,
            right : false
        }

       
        this.colors = [
            [255, 0, 0, 255],       // Red
            [255, 128, 0, 255],     // Orange
            [255, 255, 0, 255],     // Yellow
            [128, 255, 0, 255],     // Lime Green
            [0, 255, 0, 255],       // Green
            [0, 255, 128, 255],     // Spring Green
            [0, 255, 255, 255],     // Cyan
            [0, 128, 255, 255],     // Sky Blue
            [0, 0, 255, 255],       // Blue
            [128, 0, 255, 255],     // Purple
            [255, 0, 255, 255],     // Magenta
            [255, 0, 128, 255],     // Pink
            [192, 192, 192, 255],   // Light Gray
            [128, 128, 128, 255],   // Medium Gray
            [64, 64, 64, 255],      // Dark Gray
            [0, 0, 0, 255],         // Black
            [255, 255, 255, 255],   // White
            [255, 69, 0, 255],      // Red-Orange
            [255, 215, 0, 255],     // Gold
            [0, 128, 0, 255],       // Dark Green
            [0, 128, 128, 255],     // Teal
            [0, 0, 128, 255],       // Navy
            [128, 0, 128, 255],     // Purple
            [255, 20, 147, 255],    // Deep Pink
            [0, 250, 154, 255]      // Medium Spring Green
        ];
        // render loop
        app.eventHandler(this.__ID, 'this', '_render', XAML_EVENTS.RENDER);
        // brush color button click
        app.eventHandler(this.__ID, 'changeColorBtn', 'changeBrush', XAML_EVENTS.MOUSE_DOWN);
        app.eventHandler(this.__ID, 'connectBtn', 'onConnect', XAML_EVENTS.MOUSE_DOWN);
        
        // image mouse down/up in same method.
        app.eventHandler(this.__ID, 'this', 'onMouseDown', XAML_EVENTS.MOUSE_DOWN);
        app.eventHandler(this.__ID, 'this', 'onMouseDown', XAML_EVENTS.MOUSE_UP);
        
        // image mouse move
        app.eventHandler(this.__ID, 'this', 'onMouseMoved', XAML_EVENTS.MOUSE_MOVE);
        
        // sets the initial color array for rendering
        this.clean(170);

    }
    drawCircles(width, frameCt, speed){
        const halfWidth = width / 2;
        
        for (let y = 0; y < width; y++) {
            for (let x = 0; x < width; x++) {
                
                const distance = Math.sqrt((x - halfWidth) ** 2 + (y - halfWidth) ** 2)
                
                const scale = Math.sin(-distance + (frameCt * speed));

                const noisyValue = Math.cos(scale);
                
                const color = this.lerpColors(this.colors[x % this.colors.length], this.colors[y % this.colors.length], noisyValue);

                this.writePixel(x, y, color)
            }
        }
    }
}
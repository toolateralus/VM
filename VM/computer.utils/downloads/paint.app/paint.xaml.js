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
    DEEP_PINK: 22,
    MEDIUM_SPRING_GREEN: 23
};
class paint {
    //#region 
    clean(color) {
        this.width = this.newWidth;
        this.frameData = [];
        for (let y = 0; y < this.width; y++) {
            for (let x = 0; x < this.width; x++) {
                this.frameData.push(color[0])
                this.frameData.push(color[1])
                this.frameData.push(color[2])
                this.frameData.push(color[3])
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
            this.frameData[index] = byte;
            index++;
        });
        this.isDirty = true;
    }
    _render() {
        
        if (this.resizing) {
            this.clean(/*black*/ this.palette[15]);
            this.resizing = false;
        }
        if (this.isDirty === true)
        {
            app.pushEvent(this.__ID, 'renderTarget', 'draw_pixels', this.frameData);
            this.frameCt++;
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
        
        this.brushColorIndex ++;

        if (this.brushColorIndex >= this.palette.length){
            this.brushColorIndex= 0;
        }

        const color = this.palette[this.brushColorIndex];
        const colors = [];

        for(let i = 0; i < 12 * 12; ++i){
            colors[i] = color;
        }

        this.displayColorName();

    }
    displayColorName() {
        const colorName = Object.keys(Color).find(key => Color[key] == this.brushColorIndex);
        app.setProperty(this.__ID, 'colorNameLabel', 'Content', colorName);
    }

    onMouseMoved(X, Y){

        this.mouseState.x = X;
        this.mouseState.y = Y;

        const width = app.getProperty(this.__ID, 'renderTarget', 'ActualWidth')
        const height = app.getProperty(this.__ID, 'renderTarget', 'ActualHeight')

        if (this.mouseState.right === true)
        {
            const X = Math.floor(this.mouseState.x / width * this.width);
            const Y = Math.floor(this.mouseState.y / height * this.width);
            const color = this.palette[this.brushColorIndex]
            this.writePixel(X, Y, color)
            app.pushEvent(this.__ID, 'renderTarget', 'draw_pixels', this.frameData);
        }
    }
    onMouseDown(left, right){
        this.mouseState.right = right;
        this.mouseState.left = left;
    }
    
    setupUIEvents() {
        app.eventHandler(this.__ID, 'this', '_render', XAML_EVENTS.RENDER);
        app.eventHandler(this.__ID, 'this', '_physics', XAML_EVENTS.RENDER);
        
        // brush color button click
        app.eventHandler(this.__ID, 'changeColorBtn', 'changeBrush', XAML_EVENTS.MOUSE_DOWN);
        app.eventHandler(this.__ID, 'saveBtn', 'onConnect', XAML_EVENTS.MOUSE_DOWN);

        // save/load image UI
        app.eventHandler(this.__ID, 'saveBtn', 'onSave', XAML_EVENTS.MOUSE_DOWN);
        app.eventHandler(this.__ID, 'loadBtn', 'onLoad', XAML_EVENTS.MOUSE_DOWN);

        // image mouse down/up in same method.
        app.eventHandler(this.__ID, 'this', 'onMouseDown', XAML_EVENTS.MOUSE_DOWN);
        app.eventHandler(this.__ID, 'this', 'onMouseDown', XAML_EVENTS.MOUSE_UP);

        // image mouse move
        app.eventHandler(this.__ID, 'this', 'onMouseMoved', XAML_EVENTS.MOUSE_MOVE);
    }

    getIndexedColorData(){
        const data = [];
        for (let i = 0; i < this.frameData.length; i += 4) {
            const color = [
                this.frameData[i],
                this.frameData[i + 1],
                this.frameData[i + 2],
                this.frameData[i + 3]
            ];
        
            let index = -1;
        
            for (let j = 0; j < this.palette.length; j++) {
                if (
                    this.palette[j][0] === color[0] &&
                    this.palette[j][1] === color[1] &&
                    this.palette[j][2] === color[2] &&
                    this.palette[j][3] === color[3]
                ) {
                    index = j;
                    break;
                }
            }
        
            if (index === -1) {
                print('Color not found: ' + `${color}`);
                return;
            }
        
            data.push(index);
        }
        return data;
    }
    readIndexedColorData(data){
        const result = [];
        const input = JSON.parse(data);

        for (let i = 0; i < input.length; ++i){
            result[i + 0] = this.palette[input[i + 0]]
            result[i + 1] = this.palette[input[i + 1]]
            result[i + 2] = this.palette[input[i + 2]]
            result[i + 3] = this.palette[input[i + 3]]
        }
        return result;
    }

    onSave(){

        const path = app.getProperty(this.__ID, 'nameBox', 'Text')
        const data = this.getIndexedColorData();

        if (path !== undefined && typeof path === 'string' && data.length !== 0){
            file.write(path, JSON.stringify(data));
            print(`saved ${path}!`)
        }
        else{
            print(`failed to save : ${path} :: ${data}`);
        }
    }
    onLoad(){

        const path = app.getProperty(this.__ID, 'nameBox', 'Text')

        print(path);

        if (path === "" || path === undefined){
            print("you must provide a path to load from");
            return;
        }

        const json = file.read(path);

        const data = this.readIndexedColorData(json);
        
        if (data.length === 0){
            print('the file was read, but no data was found');
            return;
        }

        this.frameData = data;
    }

    //#endregion
    constructor(id) {
        // for the engine.
        this.__ID = id;

        // 24 color, 4bpp (a,r,g,b) color palette.
        this.palette = [
            [255, 255, 0, 0],       // Red
            [255, 255, 128, 0],     // Orange
            [255, 255, 255, 0],     // Yellow
            [255, 128, 255, 0],     // Lime Green
            [255, 0, 255, 0],       // Green
            [255, 0, 255, 128],     // Spring Green
            [255, 0, 255, 255],     // Cyan
            [255, 0, 128, 255],     // Sky Blue
            [255, 0, 0, 255],       // Blue
            [255, 128, 0, 255],     // Purple
            [255, 255, 0, 255],     // Magenta
            [255, 255, 0, 128],     // Pink
            [255, 192, 192, 192],   // Light Gray
            [255, 128, 128, 128],   // Medium Gray
            [255, 64, 64, 64],      // Dark Gray
            [255, 0, 0, 0],         // Black
            [255, 255, 255, 255],   // White
            [255, 255, 69, 0],      // Red-Orange
            [255, 255, 215, 0],     // Gold
            [255, 0, 128, 0],       // Dark Green
            [255, 0, 128, 128],     // Teal
            [255, 0, 0, 128],       // Navy
            [255, 255, 20, 147],    // Deep Pink
            [255, 0, 250, 154]      // Medium Spring Green
        ];
        
        // object representing mouse state
        this.mouseState = 
        {
            x: 0,
            y : 0,
            left : false,
            right : false
        }
        
        this.brushColorIndex = 0;
        
        this.bytesPerPixel = 4;
        this.width = 24;

        this.frameData = [];
        this.isDirty = true;
        
        this.resizing = false;
        this.newWidth = this.width;

        this.setupUIEvents();
        
        this.clean(this.palette[14]);

        this.displayColorName();
    }
}
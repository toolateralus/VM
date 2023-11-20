class animation
{
    time = 0;
    speed = 10;
    frameWidth = 64;
    bytesPerPixel = 4;
    data = [];
    newWidth = this.frameWidth;
    isResizing = false;
    colorScale = 0;

    updateSize() {
        // Extremely inefficient.
        this.frameWidth = this.newWidth;
        this.data = [];
        for (let y = 0; y < this.frameWidth; y++) {
            for (let x = 0; x < this.frameWidth; x++) {
                for (let index = 0; index < this.bytesPerPixel; index++) {
                    this.data.push(255);
                }
            }
        }
    }

    captureTime(start) {
        if (start) 
        {
            this.startTime = new Date().getTime();
        } 
        else if (this.startTime !== 0 && this.time % 10 === 0) 
        {
            const endTime = new Date().getTime();
            const ms = endTime - this.startTime;
            app.pushEvent(this.__ID, 'framerateLabel', 'set_content', `▲ (w) ̷  ▼ (s) {color shift value : ${this.colorScale}} res:${this.frameWidth}x${this.frameWidth} :${ms} ms  fps:${1 / ms * 1000}`);
            this.startTime = 0;
        }
    }

    setWidth(width) {
        this.newWidth = width;
        this.isResizing = true;
    }
    writePixel(x, y, color) {
        let index = (y * this.frameWidth + x) * this.bytesPerPixel;
        color.forEach(byte => {
            this.data[index] = byte;
            index++;
        });
    }
    draw() {
        this.captureTime(true);
        
        if (this.isResizing) {
            this.updateSize();
            this.isResizing = false;
        }

        if (this.shaders !== undefined && this.shaders !== null)
            this.shaders.forEach(shader =>
            {
                for (let y = 0; y < this.frameWidth; y++) 
                    for (let x = 0; x < this.frameWidth; x++) {
                        const color = shader.pixelShader(x, y, this.frameWidth, this.time, this.speed);
                        this.writePixel(x, y, color);
                    }
            });

        app.pushEvent(this.__ID, 'renderTarget', 'draw_pixels', this.data);
        
        this.time++;

        this.captureTime(false);
    }

    lerpColor(a, b, t) {
        let result = [];
        let i = 0;
        a.forEach(c => {
            let abLerp = b[i] * t + c * (1 - t);
            result.push(Math.floor(abLerp));
            i++;
        });
        return result;
    }

    clamp(min,max,value){
        return Math.min(max, Math.max(min, value))
    }
    pixelShaderFunction (x, y, resSqrt, time, speed) {
        const halfWidth = resSqrt / 2;
        const distance = Math.sqrt((x - halfWidth) ** 2 + (y - halfWidth) ** 2);
        const scale = Math.sin(distance - (time * speed / 100)) / 4 + 0.75;
        const cols = this.colors;

        if (!cols)
            return [Math.random(),Math.random(),Math.random(),Math.random()]

        const length = cols.length;
        
        const colora = this.colors[x % length];
        const colorb = this.colors[y % length];
        this.writePixel(x, y, this.lerpColor(colora, colorb, scale));
    }

    constructor(id) {
        
        /* DO NOT EDIT*/ this.__ID = id;  /* END DO NOT EDIT*/
        this.colors = [
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
            [255, 255, 69, 0],      // Red-Orange
            [255, 0, 128, 0],       // Dark Green
            [255, 0, 128, 128],     // Teal
            [255, 0, 0, 128],       // Navy
            [255, 255, 20, 147],    // Deep Pink
            [255, 0, 250, 154]      // Medium Spring Green
        ];

        this.shaders = [
            new shader(this.pixelShaderFunction),
        ];

        app.eventHandler(this.__ID, 'this', 'draw', XAML_EVENTS.RENDER);
        app.eventHandler(this.__ID, 'this', 'onKeyEvent', XAML_EVENTS.KEY_DOWN);
        this.updateSize();
    }


}

class shader
{
    constructor(pixelShader){
        this.pixelShader = pixelShader;
    }
    pixelShader(x, y, resSqrt, time, speed)
    {
        // x, y (pixel position)
        // width is the square root of the resolution
        // time is the count of frames that have elapsed since apps start
        // speed is a variable set in the animation, just a any purpose scalar
        return [Math.random(), Math.random(), Math.random(), Math.random()];
    }
    vertexShader = (/*not yet implemented*/) => {};
}
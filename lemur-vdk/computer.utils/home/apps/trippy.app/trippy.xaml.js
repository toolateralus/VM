// This needs to be finished ; it's in the middle of a refactor from it's own software renderer to the new 'gfx' module.
class trippy {
    resize() {
        this.width = this.newWidth;
    }
    captureTime(start) {
        if (start) 
        {
            this.startTime = new Date().getTime();
        } 
        else
        {
            const endTime = new Date().getTime();
            const ms = endTime - this.startTime;
            app.setProperty(this.__ID, 'framerateLabel', 'Content', `▲ (w) ̷  ▼ (s) {color shift value : ${this.colorScale}} res:${this.width}x${this.width} :${ms} ms  fps:${1 / ms * 1000}`);
            this.startTime = 0;
        }
    }
    setWidth(width) {
        this.newWidth = width;
        this.needsUpdating = true;
    }
    writePixel(x, y, color) {
        const packed = to_color(color);
        gfx.writePixel(this.gfx_ctx, Math.floor(x), Math.floor(y), packed);
    }
    draw() {
        gfx.clearColor(this.gfx_ctx, to_color(palette[Color.BLACK]))
        this.captureTime(true);
        let halfWidth = this.width / 2;
        for (let y = 0; y < this.width; y++) {
            for (let x = 0; x < this.width; x++) {
                //let distance = Math.sqrt((x - halfWidth) ** 2 + (y - halfWidth) ** 2)
                //let scale = Math.sin(distance - (this.frameCt * this.speed / 100)) / 4 + 0.75;
                //const colora = this.colors[x % this.colors.length];
                //const colorb = this.colors[y % this.colors.length];
                //this.lerpArrayFloored(colora, colorb, scale);
                this.writePixel(x, y, to_color(palette[Color.WHITE]))
            }
        }

        this.frameCt++;
        
        if (this.needsUpdating) {
            this.resize();
            this.needsUpdating = false;
        }

        this.captureTime(false);

        gfx.flushCtx(this.gfx_ctx);
    }
    lerpArrayFloored(a, b, t) {
        let result = [];
        let i = 0;
        a.forEach(c => {
            let abLerp = b[i] * t + c * (1 - t);
            result.push(Math.floor(abLerp));
            i++;
        });
        return result;
    }
    onKeyEvent(key, isDown){
        let dir = 0;

        if (key === 'W') 
            dir = 1;
        else if (key === 'S')
            dir = -1;
          
        this.colorScale += dir;

        for (let x = 0; x < this.width; ++x){
            for (let y = 0; y < this.width; ++y){
                let index = y * this.width + x;

                let color = palette[index];
                let original = this.colors_readonly[index];

                if (color !== null && color !== undefined){
                    color[0] = clamp(0, 255, original[0] + this.colorScale);
                    color[1] = clamp(0, 255, original[1] + this.colorScale);
                    color[2] = clamp(0, 255, original[2] + this.colorScale);
                    color[3] = clamp(0, 255, original[3] + this.colorScale);
                }
            }
        }       
    }
    constructor(id) {
        /* DO NOT EDIT*/ this.__ID = id;  /* END DO NOT EDIT*/
        this.frameCt = 0;
        this.speed = 10;
        this.width = 512;
        this.bytesPerPixel = 4;
        this.newWidth = this.width;
        this.needsUpdating = false;
        this.colorScale = 0;
        
       
        this.colors_readonly = [
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
        app.eventHandler(this.__ID, 'this', 'draw', XAML_EVENTS.RENDER);
        app.eventHandler(this.__ID, 'this', 'onKeyEvent', XAML_EVENTS.KEY_DOWN);

        this.gfx_ctx = gfx.createCtx(this.__ID, 'renderTexture', this.width, this.width);
    }
}
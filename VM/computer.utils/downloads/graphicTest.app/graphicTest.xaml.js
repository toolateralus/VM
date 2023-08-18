// -- user -- name this class the (case sensitive) same as the name of the xaml file and this file. for generated code purposes.
class graphicTest {
    frameCt = 0;
    speed = 10;
    width = 32;
    bytesPerPixel = 4;
    data = [];
    newWidth = this.width;
    needsUpdating = false;

    update() {
        this.width = this.newWidth;
        this.data = [];
        for (let y = 0; y < this.width; y++) {
            for (let x = 0; x < this.width; x++) {
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
        else if (this.startTime !== 0 && this.frameCt % 10 === 0) 
        {
            const endTime = new Date().getTime();
            const ms = endTime - this.startTime;
            app.pushEvent(this.__ID, 'framerateLabel', 'set_content', `${ms} ms  fps : ${1 / ms * 1000}`);
            this.startTime = 0;
        }
    }

    setWidth(width) {
        this.newWidth = width;
        this.needsUpdating = true;
    }
    writePixel(x, y, color) {
        let index = (y * this.width + x) * this.bytesPerPixel;
        color.forEach(byte => {
            this.data[index] = byte;
            index++;
        });
    }
    draw() {
        this.captureTime(true);
        let halfWidth = this.width / 2;
        for (let y = 0; y < this.width; y++) {
            for (let x = 0; x < this.width; x++) {
                let distance = Math.sqrt((x - halfWidth) ** 2 + (y - halfWidth) ** 2)
                let scale = Math.sin(distance - (this.frameCt * this.speed / 100)) / 4 + 0.75;
                const colora = this.colors[x % this.colors.length];
                const colorb = this.colors[y % this.colors.length];
                this.writePixel(x, y, this.lerpArrayFloored(colora, colorb, scale))
            }
        }

        app.pushEvent(this.__ID, 'renderTarget', 'draw_pixels', this.data);
        
        this.frameCt++;
        
        if (this.needsUpdating) {
            this.update();
            this.needsUpdating = false;
        }

        this.captureTime(false);
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
        app.eventHandler(this.__ID, 'this', 'draw', XAML_EVENTS.RENDER);
        this.update();
    }
}
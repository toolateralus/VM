// -- user -- name this class the (case sensitive) same as the name of the xaml file and this file. for generated code purposes.
class graphicTest {
    frames = 0;
    speed = 2;
    width = 32;
    bytesPerPixel = 4;
    data = [];
    newWidth = this.width;
    needsUpdating = false;
    color1 = [255, 0, 255, 0];
    color2 = [255, 0, 0, 255];
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
        let halfWidth = this.width / 2;
        for (let y = 0; y < this.width; y++) {
            for (let x = 0; x < this.width; x++) {
                let distance = Math.sqrt((x - halfWidth) ** 2 + (y - halfWidth) ** 2)
                let scale = Math.sin(distance - (this.frames * this.speed / 100)) / 4 + 0.75;
                this.writePixel(x, y, this.lerpArrayFloored(this.color1, this.color2, scale))
            }
        }
        app.pushEvent(this.__ID, 'renderTarget', 'draw_pixels', this.data);
        this.frames++;
        if (this.needsUpdating) {
            this.update();
            this.needsUpdating = false;
        }
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

        app.eventHandler(this.__ID, 'this', 'draw', XAML_EVENTS.RENDER);
        this.update();
    }
}
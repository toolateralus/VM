class paint {

    onMouseMoved(X, Y){

        this.mouseState.x = X;
        this.mouseState.y = Y;
        this.draw();
    }

    draw() {
        const width = app.getProperty('renderTarget', 'ActualWidth');
        const height = app.getProperty('renderTarget', 'ActualHeight');

        if (this.mouseState.right === true) {
            const radius = Math.floor(app.getProperty('thicknessSlider', 'Value') ?? 3);

            const msX = this.mouseState.x / width  * this.resolution;
            const msY = this.mouseState.y / height * this.resolution;
            const brush = this.brushIndex;
            const ctx = this.gfx_ctx;

            const sqrRad = radius * radius;

            for (let x = -radius; x <= radius; ++x) {
                for (let y = -radius; y < radius; ++y) {

                    const dist = (x * x) + (y * y);

                    if (dist < sqrRad) {
                        const pxX = Math.floor(x + msX);
                        const pxY = Math.floor(y + msY);

                        this.indexMap[pxX][pxY] = brush;

                        if (brush > palette.length) print(brush);

                        gfx.writePixelIndexed(ctx, pxX, pxY, brush);
                    }
                }
            }

            gfx.flushCtx(this.gfx_ctx);
        }
    }

    drawCached() {
        const width = app.getProperty('renderTarget', 'ActualWidth');
        const height = app.getProperty('renderTarget', 'ActualHeight');
        const ctx = this.gfx_ctx;

        for (let x = 0; x < this.resolution; ++x) {
            for (let y = 0; y < this.resolution; ++y) {
                const index = this.indexMap[x][y];
                gfx.writePixelIndexed(ctx, x, y, index);
            }
        }

        gfx.flushCtx(this.gfx_ctx);
    }
    onMouseDown(left, right) {
        this.mouseState.right = right;
        this.mouseState.left = left;
        this.draw();
    }
    onMouseLeave() {
        this.mouseState.right = false;
        this.mouseState.left = false;
    }
    onKeyDown() {
        if (Key.isDown('C')) {
            this.pickerOpen = 1 - this.pickerOpen;
            app.setProperty('colorPickerPanel', 'Visibility', this.pickerOpen)
        }
    }
    onSelectionChanged(index) {
        this.brushIndex = index;
    }
    onSavePressed() {
    	let filname = 'test.id';
        file.write(filname, JSON.stringify(this.indexMap));
        print(`Saved to "${filname}"`);
    }
    onLoadPressed() {
        this.indexMap = JSON.parse(file.read('test.id'));
        this.drawCached();
    }
    onClearPressed() {
        gfx.clearColorIndexed(this.gfx_ctx, Color.WHITE);
        gfx.flushCtx(this.gfx_ctx);
    }
    onFillPressed() { 
        gfx.clearColor(this.gfx_ctx, palette_indexed[this.brushIndex]);
        gfx.flushCtx(this.gfx_ctx);
    }
    constructor(id) {
        this.id = id;

        this.pickerOpen = 0;

        this.mouseState = 
        {
            x: 0,
            y : 0,
            left : false,
            right : false
        }

        app.setProperty('colorPickerPanel', 'Visibility', 0)

        this.brushIndex = 0;

        this.resolution = 256;

        this.indexMap = [[]];

        for (let i = 0; i < this.resolution; ++i) {
        	this.indexMap[i] = [];
            for (let j = 0; j < this.resolution; ++j) {
                this.indexMap[i][j] = Color.WHITE;
            }
        }

        this.gfx_ctx = gfx.createCtx(this.id, 'renderTarget', this.resolution, this.resolution);

        gfx.flushCtx(this.gfx_ctx);

        app.eventHandler('renderTarget', 'onMouseDown', XAML_EVENTS.MOUSE_DOWN);
        app.eventHandler('renderTarget', 'onMouseDown', XAML_EVENTS.MOUSE_UP);

        app.eventHandler('SaveButton', 'onSavePressed', XAML_EVENTS.MOUSE_DOWN);
        app.eventHandler('LoadButton', 'onLoadPressed', XAML_EVENTS.MOUSE_DOWN);
        app.eventHandler('ClearButton', 'onClearPressed', XAML_EVENTS.MOUSE_DOWN);
        app.eventHandler('FillButton', 'onFillPressed', XAML_EVENTS.MOUSE_DOWN);

        app.eventHandler('renderTarget', 'onMouseLeave', XAML_EVENTS.MOUSE_LEAVE);
        app.eventHandler('renderTarget', 'onMouseMoved', XAML_EVENTS.MOUSE_MOVE);

        app.eventHandler('renderTarget', 'onKeyDown', XAML_EVENTS.KEY_DOWN);

        app.eventHandler('colorPickerBox', 'onSelectionChanged', XAML_EVENTS.SELECTION_CHANGED);

    }
}
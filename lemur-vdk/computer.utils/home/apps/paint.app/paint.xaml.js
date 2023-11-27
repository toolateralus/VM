class paint {

    onMouseMoved(X, Y){

        this.mouseState.x = X;
        this.mouseState.y = Y;
        this.draw();
    }

    draw() {
        const width = app.getProperty(this.id, 'renderTarget', 'ActualWidth');
        const height = app.getProperty(this.id, 'renderTarget', 'ActualHeight');

        if (this.mouseState.right === true) {
            const radius = Math.floor(app.getProperty(this.id, 'thicknessSlider', 'Value') ?? 3);

            const msX = this.mouseState.x / width  * this.resolution;
            const msY = this.mouseState.y / height * this.resolution;
            const brush = this.brushIndex;
            const ctx = this.gfx_ctx;

            for (let x = -radius; x < radius; ++x) {
                for (let y = -radius; y < radius; ++y) {
                    const cosx = Math.cos(x);
                    const cosy = Math.sin(y);
                    const pxX = Math.floor(x + cosx + msX);
                    const pxY = Math.floor(x + cosy + msY);
                    gfx.writePixelIndexed(ctx, pxX, pxY, brush);

                }
            }

            gfx.flushCtx(this.gfx_ctx);
        }
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
            app.setProperty(this.id, 'colorPickerPanel', 'Visibility', this.pickerOpen)
        }
    }

    onSelectionChanged(index) {
        this.brushIndex = index;
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

        app.setProperty(this.id, 'colorPickerPanel', 'Visibility', 1)

        this.brushIndex = 0;

        this.resolution = 256;

        this.gfx_ctx = gfx.createCtx(this.id, 'renderTarget', this.resolution, this.resolution);

        gfx.flushCtx(this.gfx_ctx);

        app.eventHandler(this.id, 'renderTarget', 'onMouseDown', XAML_EVENTS.MOUSE_DOWN);
        app.eventHandler(this.id, 'renderTarget', 'onMouseDown', XAML_EVENTS.MOUSE_UP);


        app.eventHandler(this.id, 'renderTarget', 'onMouseLeave', XAML_EVENTS.MOUSE_LEAVE);
        app.eventHandler(this.id, 'renderTarget', 'onMouseMoved', XAML_EVENTS.MOUSE_MOVE);

        app.eventHandler(this.id, 'this', 'onKeyDown', XAML_EVENTS.KEY_DOWN);

        app.eventHandler(this.id, 'colorPickerBox', 'onSelectionChanged', XAML_EVENTS.SELECTION_CHANGED);

    }
}
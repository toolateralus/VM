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
            const radius = 3;
            for (let x = -radius; x < radius; ++x) {
                for (let y = -radius; y < radius; ++y) {
                    const pxX = Math.floor(x + this.mouseState.x / width * this.resolution);
                    const pxY = Math.floor(y + this.mouseState.y / height * this.resolution);
                    gfx.writePixel(this.gfx_ctx, pxX, pxY, to_color(palette[14]));
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
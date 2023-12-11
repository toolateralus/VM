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
			const rotation = 0;
			const primitive = Primitive.Circle;
			
			const halfRad = radius / 2;
			
			gfx.drawFilledShape(this.gfx_ctx, Math.floor(msX - halfRad), Math.floor(msY - halfRad), radius, radius, rotation, brush, primitive);

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
            app.setProperty('colorPickerPanel', 'Visibility', this.pickerOpen)
        }
    }
    onSelectionChanged(index) {
        this.brushIndex = index;
    }
    onSavePressed() {
    	gfx.flushCtx(this.gfx_ctx);
    	gfx.saveToImage(this.gfx_ctx, 'home/test.bmp');
    	print('saved to home/test.bmp');
    }
    onLoadPressed() {
    	gfx.clearColorIndexed(this.gfx_ctx, Color.BLACK);
        gfx.loadFromImage(this.gfx_ctx, 'home/test.bmp');
        gfx.flushCtx(this.gfx_ctx);
        print('loaded from home/test.bmp');
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
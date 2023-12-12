class paint {

    onMouseMoved(X, Y){

        this.mouseState.x = X;
        this.mouseState.y = Y;
        this.draw();
    }

    draw() {
        const width = App.getProperty('renderTarget', 'ActualWidth');
        const height = App.getProperty('renderTarget', 'ActualHeight');

        if (this.mouseState.right === true) {
            const radius = Math.floor(App.getProperty('thicknessSlider', 'Value') ?? 3);

            const msX = this.mouseState.x / width  * this.resolution;
            const msY = this.mouseState.y / height * this.resolution;
            const brush = this.brushIndex;
            const ctx = this.gfx_ctx;
			const rotation = 0;
			const primitive = Primitive.Circle;
			
			const halfRad = radius / 2;
			
			Graphics.drawFilledShape(this.gfx_ctx, Math.floor(msX - halfRad), Math.floor(msY - halfRad), radius, radius, rotation, brush, primitive);

            Graphics.flushCtx(this.gfx_ctx);
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
            App.setProperty('colorPickerPanel', 'Visibility', this.pickerOpen)
        }
    }
    onSelectionChanged(index) {
        this.brushIndex = index;
    }
    onSavePressed() {
    	Graphics.flushCtx(this.gfx_ctx);
    	Graphics.saveToImage(this.gfx_ctx, 'home/test.bmp');
    	print('saved to home/test.bmp');
    }
    onLoadPressed() {
    	Graphics.clearColorIndexed(this.gfx_ctx, Color.BLACK);
        Graphics.loadFromImage(this.gfx_ctx, 'home/test.bmp');
        Graphics.flushCtx(this.gfx_ctx);
        print('loaded from home/test.bmp');
    }
    onClearPressed() {
        Graphics.clearColorIndexed(this.gfx_ctx, Color.WHITE);
        Graphics.flushCtx(this.gfx_ctx);
    }
    onFillPressed() { 
        Graphics.clearColor(this.gfx_ctx, palette_indexed[this.brushIndex]);
        Graphics.flushCtx(this.gfx_ctx);
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

        App.setProperty('colorPickerPanel', 'Visibility', 0)

        this.brushIndex = 0;

        this.resolution = 256;

 
        this.gfx_ctx = Graphics.createCtx(this.id, 'renderTarget', this.resolution, this.resolution);

        Graphics.flushCtx(this.gfx_ctx);

        App.eventHandler('renderTarget', 'onMouseDown', XAML_EVENTS.MOUSE_DOWN);
        App.eventHandler('renderTarget', 'onMouseDown', XAML_EVENTS.MOUSE_UP);

        App.eventHandler('SaveButton', 'onSavePressed', XAML_EVENTS.MOUSE_DOWN);
        App.eventHandler('LoadButton', 'onLoadPressed', XAML_EVENTS.MOUSE_DOWN);
        App.eventHandler('ClearButton', 'onClearPressed', XAML_EVENTS.MOUSE_DOWN);
        App.eventHandler('FillButton', 'onFillPressed', XAML_EVENTS.MOUSE_DOWN);

        App.eventHandler('renderTarget', 'onMouseLeave', XAML_EVENTS.MOUSE_LEAVE);
        App.eventHandler('renderTarget', 'onMouseMoved', XAML_EVENTS.MOUSE_MOVE);

        App.eventHandler('renderTarget', 'onKeyDown', XAML_EVENTS.KEY_DOWN);

        App.eventHandler('colorPickerBox', 'onSelectionChanged', XAML_EVENTS.SELECTION_CHANGED);

    }
}
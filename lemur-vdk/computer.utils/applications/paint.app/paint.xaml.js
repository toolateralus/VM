class paint {
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

        this.gfx_ctx = new GraphicsContext(this.id, 'RenderingTarget', this.resolution, this.resolution);
		
		this.deferred = 0;
		
	
        App.eventHandler('RenderingTarget', 'onMouseDown', Event.MouseDown);
        App.eventHandler('RenderingTarget', 'onMouseDown', Event.MouseUp);
        App.eventHandler('SaveButton', 'onSavePressed', Event.MouseDown);
        App.eventHandler('LoadButton', 'onLoadPressed', Event.MouseDown);
        App.eventHandler('ClearButton', 'onClearPressed', Event.MouseDown);
        App.eventHandler('FillButton', 'onFillPressed', Event.MouseDown);
        App.eventHandler('RenderingTarget', 'onMouseLeave', Event.MouseLeave);
        App.eventHandler('RenderingTarget', 'onMouseMoved', Event.MouseMove);
        App.eventHandler('RenderingTarget', 'onKeyDown', Event.KeyDown);
        App.eventHandler('colorPickerBox', 'onSelectionChanged', Event.SelectionChanged);
        
        this.onClearPressed()

    }
    draw() {
        const width = App.getProperty('RenderingTarget', 'ActualWidth');
        const height = App.getProperty('RenderingTarget', 'ActualHeight');

        if (this.mouseState.right === true) {
            const radius = Math.floor(App.getProperty('thicknessSlider', 'Value') ?? 3);

            const msX = this.mouseState.x / width  * this.resolution;
            const msY = this.mouseState.y / height * this.resolution;
            const brush = this.brushIndex;
			const rotation = 0;
			
			const halfRad = Math.max(0.1, radius / 2);
			const x = Math.floor(msX - halfRad);
			const y = Math.floor(msY - halfRad)

			this.gfx_ctx.drawRect(x, y, radius, radius, rotation, brush);
			
		
        }
	
    }

//#region  events
    onMouseMoved(X, Y){
        this.mouseState.x = X;
        this.mouseState.y = Y;
        this.draw();
    	this.gfx_ctx.flush();
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
        if (Key.isDown('C') && Key.isDown('LeftCtrl')) {
            this.pickerOpen = 1 - this.pickerOpen;
            App.setProperty('colorPickerPanel', 'Visibility', this.pickerOpen)
        }
        if (Key.isDown('Space')) {
        	
        	if (Key.isDown('LeftShift')) {
        		this.onClearPressed();
        		return;
        	}
        	this.onFillPressed();
        }
        if (Key.isDown('B')) {
        	this.brushIndex = (this.brushIndex + 1) % palette.length;
        }
       
    }
    onSelectionChanged(index) {
        this.brushIndex = index;
    }
    onSavePressed() {
        this.gfx_ctx.flush();
        this.gfx_ctx.saveToImage('home/test.bmp');
        print('saved to home/test.bmp');
    }
    onLoadPressed() {
        this.gfx_ctx.loadFromImage('home/test.bmp');
        print('Loaded from home/test.bmp');
    }
    onClearPressed() {
        this.gfx_ctx.clearColor(Color.WHITE);
        this.gfx_ctx.flush();
    }
    onFillPressed() { 
        this.gfx_ctx.clearColor(this.brushIndex);
        this.gfx_ctx.flush();
    }

//#endregion
}
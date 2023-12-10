const {
    Point,
    GameObject,
    Scene,
} = require('game.js');

const { Profiler } = require('profiler.js');

class shapes {
    constructor(id) {

        this.captureBeginTime = 0;
        this.captureEndTime = 0;
        // for the engine.
        this.id = id;
        this.frameCt = 0;

		this.width = 256;

        this.gfx_ctx = gfx.createCtx(this.id, 'renderTarget', this.width, this.width);

        this.spawnScene();

        app.eventHandler('this', 'm_render', XAML_EVENTS.RENDER);
        app.eventHandler('playButton', 'onPlayClicked', XAML_EVENTS.MOUSE_DOWN);
        
        this.profiler = new Profiler();
        this.profiler.start();
        
        this.playing = false;
        
        
        //gfx.loadSkybox(this.gfx_ctx, 'icon.bmp');
    }
    
	onPlayClicked() {
		
		if (this.playing === true) {
			app.setProperty('playButton', 'Content', 'Play');
			this.playing = false;
		} else {
			app.setProperty('playButton', 'Content', 'Pause');
			this.playing = true;
		}
		
	}
	
    spawnScene() {
        const gameObjects = [];
		
		// 10,000 game objects.
        const countOfEach = 417; 
        for (let z = 0; z < countOfEach; ++z)
            for (let i = 0; i < palette.length; ++i) {
                const scale = new Point(50, 50);
                const pos = new Point(this.width * random(), this.width * random());
                let gO = new GameObject([], scale, pos);
                
                gO.drag = 1 - (i / 500);
                gO.colorIndex = i;
                gO.isMesh = true;
                gO.primitveIndex = Primitive.Rectangle;

                gameObjects.push(gO);
            }

        this.scene = new Scene(gameObjects);
    }

  
    m_render() {
    	
    	if (this.playing !== true)
    		return;
        
    	//gfx.drawSkybox(this.gfx_ctx);
        gfx.clearColor(this.gfx_ctx, Color.BLACK);

        this.profiler.set_marker('other');

		// run profiler 
		// draw results every 60 frames.
        if (this.frameCt % (60) == 0)
            this.fpsCounterFrame(false);
        else
            this.fpsCounterFrame(false);
        
        this.fpsCounterFrame(true);

        this.profiler.set_marker('profiler');

        this.frameCt++;
        
		let frequency = app.getProperty('frequencySlider', 'Value');
    	frequency = Math.floor(frequency);	
    	
    	if (this.lastFrequency !== frequency)
    	{
    		this.lastFrequency = frequency;	
    		app.setProperty('gameObjectCountLabel', 'Content', frequency);
    	}
    	
    	const gameObjects = this.scene.gOs;
    	for (let i = 0; i < frequency && i < gameObjects.length; ++i) {
    		const gO = gameObjects[i];
    		
			if (gO.isMesh !== true)
				return;
				
            const x = gO.pos.x;
            const y = gO.pos.y;

            const width = gO.scale.x;
            const height = gO.scale.y;

            const color = gO.colorIndex;
            const prim = gO.primitveIndex;

            const rot = gO.rotation;

    	    gO.velocity.y += 0.0981; 
            gO.update_physics();
            gO.confine_to_screen_space(this.width);
        
            

            if (gO.pos.y > (this.width - 25)) {
                gO.velocity.y = random() * -20;
                gO.velocity.x = (1.0 - random(2.0));
                gO.angular = 1.0;
            }
        
            if (gO.pos.x === 0 || gO.pos.x === this.width) {
                gO.velocity.x = -(gO.velocity.x * 2);
            }

            gfx.drawFilledShape(this.gfx_ctx, Math.floor(x), Math.floor(y), width, height, rot, color, prim);
	
        };

        this.profiler.set_marker('rendering');
        
        gfx.flushCtx(this.gfx_ctx);
        
        this.profiler.set_marker('uploading');

        
    }
    
    // false to begin a frame capture, false to end it and get the
    fpsCounterFrame(start) {
        if (start) 
        {
            this.captureBeginTime = new Date().getTime();
        } 
        else
        {
        	const time = new Date().getTime();
	        const elapsed = time - this.captureBeginTime;
            app.setProperty('framerateLabel', 'Content', `fps:${Math.floor(1 / elapsed * 1000)}`);
            this.captureBeginTime = 0;
            this.profiler.drawProfile();
        }
    }
}
const {
    Vec2,
    Node,
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
        
        this.bounds = {
            min : new Vec2(0, 0),
            max : new Vec2(this.width, this.width),
        };
        
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
        const nodes = [];
		
		// 10,000 game objects.
        const countOfEach = 417; 
        for (let z = 0; z < countOfEach; ++z)
            for (let i = 0; i < palette.length; ++i) {
                const scale = new Vec2(Math.floor(z % Math.max(1, i)), Math.floor(i * i));
                const pos = new Vec2(0 + i * i, i + z % this.width);
                let node = new Node(scale, pos);
                
                node.drag = 1 - (i / 500);
                node.colorIndex = i;
                node.isMesh = true;
                node.primitveIndex = Primitive.Rectangle;

                nodes.push(node);
            }

        this.scene = new Scene(nodes);
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
    		app.setProperty('nodeCountLabel', 'Content', frequency);
    	}
    	
    	const nodes = this.scene.nodes;
    	for (let i = 0; i < frequency && i < nodes.length; ++i) {
    		const node = nodes[i];
    		
			if (node.isMesh !== true)
				return;
				
            const x = node.pos.x;
            const y = node.pos.y;

            const width = node.scale.x;
            const height = node.scale.y;

            const color = node.colorIndex;
            const prim = node.primitveIndex;

            const rot = node.rotation;

    	    node.velocity.y += 0.0981; 
            node.update_physics();
            node.clamp_position(this.bounds.min, this.bounds.max);

                      

            if (node.pos.y > (this.width - 25)) {
                node.velocity.y = random() * -20;
                node.velocity.x = (1.0 - random(2.0));
                node.angular = 1.0;
            }
        
            if (node.pos.x === 0 || node.pos.x === this.width) {
                node.velocity.x = -(node.velocity.x * 2);
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
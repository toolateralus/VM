const {
    Point,
    GameObject,
    Scene,
} = require('game.js');

const { Profiler } = require('profiler.js');

class gamer {
    constructor(id) {

        this.captureBeginTime = 0;
        this.captureEndTime = 0;
        // for the engine.
        this.id = id;
        this.frameCt = 0;

		this.width = 2048;

        this.gfx_ctx = gfx.createCtx(this.id, 'renderTarget', this.width, this.width);

        this.spawnScene();

        app.eventHandler('this', 'm_render', XAML_EVENTS.RENDER);
        app.eventHandler('playButton', 'onPlayClicked', XAML_EVENTS.MOUSE_DOWN);
        
        this.profiler = new Profiler();
        this.profiler.start();
        
        this.playing = false;
        
        this.playerSpeed = 50;
        this.projectiles = [];
        //gfx.loadSkybox(this.gfx_ctx, 'icon.bmp');
    }
    
    handleInput() {
        this.player.velocity.y = 0;

        if (Key.isDown('E'))
            this.player.scale.add(1, 1);
        else if (Key.isDown('Q'))
            this.player.scale.sub(1, 1);

        if (Key.isDown('A')) 
            this.player.velocity.x = -this.playerSpeed;
        else if (Key.isDown('D'))
            this.player.velocity.x = this.playerSpeed;

        if (Key.isDown('W') || Key.isDown('S'))
            this.shoot();
        
    }

    shoot() {
        const bullet = new GameObject([], this.player.scale, new Point(this.player.pos.x - this.player.scale.x, this.player.pos.y - this.player.scale.x));
        bullet.isMesh = true;
        bullet.colorIndex = Color.RED;
        bullet.primitveIndex = Primitive.Rectangle;
        bullet.velocity.y = -100;
        bullet.drag = 0.999;
        this.scene.gOs.push(bullet);
        this.projectiles.push(bullet);

        const msDelay = 2000;
        const index = this.projectiles.length - 1;
        app.defer('destroyProjectile', msDelay, index);
    }
    destroyProjectile(index) {
        const projectile = this.projectiles[index];
        this.scene.gOs.splice(this.scene.gOs.indexOf(projectile), 1);
        this.projectiles.splice(index, 1);
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
		
        const go = new GameObject([], new Point(100, 100), new Point(Math.floor(this.width / 2), this.width));
        go.isMesh = true;
        go.colorIndex = Color.YELLOW;
        go.primitveIndex = Primitive.Rectangle;
        
        this.player = go;

        gameObjects.push(this.player);
        this.scene = new Scene(gameObjects);
    }
    m_render() {
    	
    	if (this.playing !== true)
    		return;
        
        this.handleInput();

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
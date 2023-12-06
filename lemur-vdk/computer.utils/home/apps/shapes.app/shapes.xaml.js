const {
    Point,
    Line, 
    GameObject,
    Scene,
    Renderer,
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

        this.setupUIEvents();
        this.profiler = new Profiler();
        this.profiler.start();
    }

    spawnScene() {
        const gameObjects = [];

        const ctOfEach = 120; 

        for (let z = 0; z < ctOfEach; ++z)
            for (let i = 0; i < palette.length; ++i) {
                const verts = create_square();
                const scale = new Point(25, 25);
                const pos = new Point(i * i, this.width - z);
                let gO = new GameObject(verts, scale, pos);
                
                gO.colorIndex = i;
                gO.isMesh = true;
                gO.primitveIndex = i % 3;

                gameObjects.push(gO);
            }

        this.scene = new Scene(gameObjects);
    }

    setupUIEvents() {
        app.eventHandler('this', 'm_render', XAML_EVENTS.RENDER);
    }
  
    fpsCounterFrame(start) {
        if (start) 
        {
            this.captureBeginTime = new Date().getTime();
        } 
        else
        {
            return new Date().getTime();
        }
    }
    m_render() {
        gfx.clearColor(this.gfx_ctx, Color.BLACK);

        this.profiler.set_marker('other');

        if (this.frameCt % (25 * 2) == 0) {
            let time = this.fpsCounterFrame(false);
            const elapsed = time - this.captureBeginTime;
            app.setProperty('framerateLabel', 'Content', `fps:${Math.floor(1 / elapsed * 1000)}`);
            this.captureBeginTime = 0;
            this.drawProfile();
        } else {
            this.fpsCounterFrame(false);
        }
        this.fpsCounterFrame(true);

        this.profiler.set_marker('profiler');

        this.frameCt++;
        
        const gos = this.scene.GameObjects();

        gos.forEach(gO => {
			
            const x = gO.pos.x;
            const y = gO.pos.y;

            const width = gO.scale.x;
            const height = gO.scale.y;

            const color = gO.colorIndex;
            const prim = gO.primitveIndex;

            if (gO.isMesh === true) {

                try {
                    gfx.drawFilledShape(this.gfx_ctx, Math.floor(x), Math.floor(y), width, height, color, prim);
                }
                catch (e) {
                    print(`${e} \n\n error drawing filled shape at ${x}, ${y}`);
                }

            }

        });

        this.profiler.set_marker('rendering');
        
        gfx.flushCtx(this.gfx_ctx);
        this.profiler.set_marker('uploading');

        gos.forEach(gO => {

        	gO.velocity.y += 0.181; 
        	gO.update_physics();
        	gO.confine_to_screen_space(this.width);
        	
        	if (gO.pos.y > (this.width - 15)) {
        		gO.velocity.y = random() * -20;
        		gO.velocity.x = (1 - random());
        	}
        	
        	if (gO.pos.x < 15 || gO.pos.x > this.width - 15) {
        		gO.velocity.x = -(gO.velocity.x * 2);
        	}

        	
        });
        
		
        this.profiler.set_marker('collision');
    }

    collisionRes(body_a, body_b) {
        if (body_a.collides(body_b.x, body_b.y)){
            print (`${body_a} collided with ${body_b}`)
        }
    }

    drawProfile() {
        const results = this.profiler.sample_average();

        const profilerWidth = app.getProperty('ProfilerPanel', 'ActualWidth') / 2;
        const fpsWidth = app.getProperty('framerateLabel', 'ActualWidth');

        const actualWidth = profilerWidth - fpsWidth;
        
        let totalTime = 0;

        for (const label in results)
            totalTime += results[label];

        const xFactor = actualWidth / totalTime;

        for (const label in results) {
            const time = results[label];
            app.setProperty(label, 'Content', `${time / 10_000} ms ${label}`);
            app.setProperty(label, 'Width', time * xFactor);
        }
    }
    
    
}
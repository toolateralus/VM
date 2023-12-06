const {
    Point,
    Line, 
    GameObject,
    Scene,
    Renderer,
} = require('game.js');
const { Profiler } = require('profiler.js');

class bouncy_cubes {
    constructor(id) {

        this.captureBeginTime = 0;
        this.captureEndTime = 0;
        // for the engine.
        this.id = id;
        this.frameCt = 0;

		// right now we just use a square resolution.
		this.width = 512;

        const gfx_ctx = gfx.createCtx(this.id, 'renderTarget', this.width, this.width);

        this.renderer = new Renderer(this.width, gfx_ctx);

        const gameObjects = [];

		// creates a square for each color in the indexed palette, 24.
		
		
		for (let z = 0; z < 6; ++z)
        for (let i = 0; i < palette.length; ++i) {
            const verts = create_square();
            const scale = new Point(25, 25);
            const pos = new Point(i * i, this.renderer.width - z);
            let gO = new GameObject(verts, scale, pos);
            verts.forEach(v => v.color = i);
            gameObjects.push(gO);
        }

        this.scene = new Scene(gameObjects);

        // setup events (including render/physics loops)
        this.setupUIEvents();
        this.profiler = new Profiler();
        this.profiler.start();

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

        this.renderer.m_drawScene(this.scene, this.gfx_ctx);
        this.profiler.set_marker('rendering');
        
        gfx.flushCtx(this.renderer.gfx_ctx);
        this.profiler.set_marker('uploading');

		const gos = this.scene.GameObjects();

        gos.forEach(gO => {
        	gO.velocity.y += 0.181; 
        	gO.update_physics();
        	gO.confine_to_screen_space(this.renderer.width);
        	
        	if (gO.pos.y > (this.renderer.width - 15)) {
        		gO.velocity.y = random() * -20;
        		gO.velocity.x = (1 - random());
        	}
        	
        	if (gO.pos.x < 15 || gO.pos.x > this.renderer.width - 15) {
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
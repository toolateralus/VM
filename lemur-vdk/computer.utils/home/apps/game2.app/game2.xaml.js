const {
    Point,
    Line, 
    GameObject,
    Scene,
    Renderer,
} = require('game.js');
const { Profiler } = require('profiler.js');


// THIS IS THE WPF CLASS! (it chooses the file name associated class.)
class game2 {
    constructor(id) {

        this.captureBeginTime = 0;
        this.captureEndTime = 0;
        // for the engine.
        this.id = id;
        this.frameCt = 0;

        const gfx_ctx = gfx.createCtx(this.id, 'renderTarget', 512, 512);

        this.renderer = new Renderer(512, gfx_ctx);

        var gameObjects = [];

        const half_width = 512 / 2;

        for (let i = 0; i < palette.length; ++i) {
            const verts = create_square();
            // obj scale.
            const scale = new Point(5 * i, 5 * i);
            // start position
            const pos = new Point(half_width + i, half_width + i);

            let gO = new GameObject(verts, scale, pos);

            verts.forEach(v => v.color = i);

            gameObjects.push(gO);
        }

        this.scene = new Scene(gameObjects);

        // setup events (including render/physics loops)
        this.setupUIEvents();

        this.profiler = new Profiler();
        this.profiler.start();

        // start drawing, the Renderer only draws when it's marked as dirty.
        this.renderer.isDirty = true;

    }

    setupUIEvents() {
        app.eventHandler(this.id, 'this', 'm_render', XAML_EVENTS.RENDER);
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
            app.setProperty(this.id, 'framerateLabel', 'Content', `fps:${Math.floor(1 / elapsed * 1000)}`);
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

        this.scene.GameObjects().forEach(gO => { gO.velocity.y += 0.01; gO.update_physics();});
        this.scene.GameObjects().forEach(gO => { gO.confine_to_screen_space(this.renderer.width); });
        this.scene.GameObjects().forEach(gO => { this.scene.GameObjects().forEach(gO1 => { this.collisionRes(gO, gO1); }) });

        this.profiler.set_marker('collision');
    }

    collisionRes(body_a, body_b) {
        if (body_a.collides(body_b.x, body_b.y)){
            print (`${body_a} collided with ${body_b}`)
        }
    }

    drawProfile() {
        const results = this.profiler.sample_average();

        const profilerWidth = app.getProperty(this.id, 'ProfilerPanel', 'ActualWidth') / 2;
        const fpsWidth = app.getProperty(this.id, 'framerateLabel', 'ActualWidth');

        const actualWidth = profilerWidth - fpsWidth;
        
        let totalTime = 0;

        for (const label in results)
            totalTime += results[label];

        const xFactor = actualWidth / totalTime;

        for (const label in results) {
            const time = results[label];
            app.setProperty(this.id, label, 'Content', `${time / 10_000} ms ${label}`);
            app.setProperty(this.id, label, 'Width', time * xFactor);
        }
    }
    
    
}
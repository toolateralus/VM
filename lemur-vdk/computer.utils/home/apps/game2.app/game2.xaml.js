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
    setupUIEvents() {
        app.eventHandler(this.__ID, 'this', 'm_render', XAML_EVENTS.RENDER);
        app.eventHandler(this.__ID, 'this', 'onKey', XAML_EVENTS.KEY_DOWN);
    }
    onKey(key, isDown) {
        
    }
    fpsCounterFrame(start) {
        if (start) 
        {
            this.captureBeginTime = new Date().getTime();
        } 
        else
        {
            const captureEndTime = new Date().getTime();
            const elapsed =captureEndTime - this.captureBeginTime;
            app.setProperty(this.__ID, 'framerateLabel', 'Content', `fps:${ Math.floor(1 / elapsed * 1000)}`);
            this.captureBeginTime = 0;
        }
    }
    m_render() {

        this.profiler.set_marker('other');
        this.frameCt++;
        
        // returns a bool indicating whether anything was actually drawn or not
        this.renderer.m_drawScene(this.scene, this.gfx_ctx);
        this.profiler.set_marker('rendering');
        
        gfx.flushCtx(this.renderer.gfx_ctx);
        this.profiler.set_marker('uploading');

        this.scene.GameObjects().forEach(gO => { gO.velocity.y += 0.01; gO.update_physics();});
        this.scene.GameObjects().forEach(gO => { gO.confine_to_screen_space(this.renderer.width); });
        this.scene.GameObjects().forEach(gO => { this.scene.GameObjects().forEach(gO1 => { this.collisionRes(gO, gO1); })});

        this.profiler.set_marker('collision');

        this.fpsCounterFrame(false);
        this.fpsCounterFrame(true);
        
        if (this.frameCt % 30 == 0)
            this.drawProfile();
    }
    collisionRes(body_a, body_b) {
        if (body_a.collides(body_b.x, body_b.y)){
            print (`${body_a} collided with ${body_b}`)
        }
    }
    drawProfile() {
        const results = this.profiler.sample_average();

        const profilerWidth = app.getProperty(this.__ID, 'ProfilerPanel', 'ActualWidth') / 2;
        const fpsWidth = app.getProperty(this.__ID, 'framerateLabel', 'ActualWidth');

        const actualWidth = profilerWidth - fpsWidth;
        
        let totalTime = 0;

        for (const label in results)
            totalTime += results[label];

        const xFactor = actualWidth / totalTime;

        for (const label in results) {
            const time = results[label];
            app.setProperty(this.__ID, label, 'Content', `${time / 10_000} ms ${label}`);
            app.setProperty(this.__ID, label, 'Width', time * xFactor);
        }
    }
    getsquare(size) {
        const v1 = new Point(-0.5, -0.5, Color.RED)
        const v2 = new Point(-0.5, 0.5,  Color.ORANGE)
        const v3 = new Point(0.5, 0.5,   Color.YELLOW)
        const v4 = new Point(0.5, -0.5,  Color.LIME_GREEN)
        const verts = [v1, v2, v3, v4];
        return verts;
    }
    constructor(id) {

      
        // for the engine.
        this.__ID = id;
        this.frameCt = 0;

        const gfx_ctx = gfx.createCtx(this.__ID, 'renderTarget', 512, 512);
        // 64x64 render surface.
        this.renderer = new Renderer(1024, gfx_ctx);

        // initialize the drawing surface

        this.moveSpeed = 1;

        // player init 
        // 4x4 (pixel) square sprite
       
        // make a player object
        
        var GameObjects = [];

        const half_width = 512 / 2;

        for (let i = 0; i < palette.length; ++i) {
            const verts = this.getsquare();
            // obj scale.
            const scale = new Point(5 * i, 5 * i);
            // start position
            const pos = new Point(half_width + i, half_width + i);
            
            this.player = new GameObject(verts, scale, pos);
            
            let gO = new GameObject(verts, scale, pos);

            verts.forEach(v => v.color = i);

            GameObjects.push(gO);
        }

        this.scene = new Scene(GameObjects);

        // setup events (including render/physics loops)
        this.setupUIEvents();

        this.profiler = new Profiler();
        this.profiler.start();

        // start drawing, the Renderer only draws when it's marked as dirty.
        this.renderer.isDirty = true;

    }
}
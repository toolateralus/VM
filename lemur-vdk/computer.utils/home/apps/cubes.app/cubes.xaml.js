const {
    Vec2,
    Node,
    Scene,
    Renderer,
} = require('game.js');
const { Profiler } = require('profiler.js');

class cubes {
    constructor(id) {
        this.id = id;
		this.width = 256;
        
        // spawn ct of each color, 1 == 24 spawns, 2 == 48 etc.
        this.spawnScene(5);
        
        this.bounds = {
            min : new Vec2(0, 0),
            max : new Vec2(this.width , this.width),
        };

        // create a context and pass it to the renderer. in this case, 
        // we won't be doing much with the ctx.
     	const gfx_ctx = gfx.createCtx(this.id, 'renderTarget', this.width, this.width);
        this.renderer = new Renderer(this.width, gfx_ctx);
        
        app.eventHandler('this', 'm_render', XAML_EVENTS.RENDER);
        
        this.profiler = new Profiler();
        this.profiler.start();
    }
    
    // spawn a number of randomly colored node's.
	spawnScene(countOfEach = 5) {
		const nodes = [];
		for (let z = 0; z < countOfEach; ++z) {
	        for (let i = 0; i < palette.length; ++i) {
	            const verts = create_square();
	            const scale = new Vec2(25, 25);
	            const pos = new Vec2(i * i, this.width - z);
	            let node = new Node(scale, pos, verts);
                
	            verts.forEach(v => v.color = i);
	            nodes.push(node);
       		}
   		}

        this.scene = new Scene(nodes);
	}
	
	// the 'this.profiler.set_marker('region_name') is used to
	// set various segments / splits in the profiler.
	// the profiler is just a latency averager.
	
    m_render() {
        this.profiler.set_marker('other');

		// draw objects
        this.renderer.m_drawScene(this.scene, this.gfx_ctx);
        this.profiler.set_marker('rendering');
        
        // display draw
        gfx.flushCtx(this.renderer.gfx_ctx);
        this.profiler.set_marker('uploading');

        this.m_update();
        this.profiler.set_marker('collision');
        
        // draws the profile gfx.
        this.profiler.drawProfile();
    }
    
    m_update(deltaTime = (8 / 1000)) {

    	this.scene.nodes.forEach(node => {
        	// gravity
        	node.velocity.y = 1 * deltaTime;
        	
        	node.update_physics(deltaTime);
        	let collided = node.clamp_position(this.bounds.min, this.bounds.max);
        	
        	// bounce;
        	if (collided) {
        		node.velocity.y = random() * -20 / deltaTime;
        		node.velocity.x = -(node.velocity.x * 2);
        	}
        });
    }
    
}
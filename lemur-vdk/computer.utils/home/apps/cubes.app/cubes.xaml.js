const { Vec2, Node, Scene, Renderer, } = require('game.js');
const { Profiler } = require('profiler.js');
class cubes {
    constructor(id) {
        this.scene = new Scene([]);
        this.id = id;
    	this.width = 256;
    	
    	this.bounds = {
    		min : new Vec2(0, 0), 
    		max : new Vec2(this.width , this.width) 
		};
		
        for (let z = 0; z < 5 * palette.length; ++z) {
            let node = new Node(new Vec2(25, 25), new Vec2(clamp(0, 24, z) * clamp(0, 48, z), this.width));
            node.set_vertices(create_square());
            node.vertices.forEach(v => v.color = z % palette.length);
            this.scene.nodes.push(node);
   		}
   		
     	const gfx_ctx = Graphics.createCtx(this.id, 'renderTarget', this.width, this.width);
        this.renderer = new Renderer(this.width, gfx_ctx);
        
        const __DEBUG__ = false;
        
        if (__DEBUG__) {
        	this.profiler = new Profiler();
        	this.profiler.start();
        	App.eventHandler('this', 'm_render_profiled', XAML_EVENTS.RENDER); 
        } else {
        	App.eventHandler('this', 'm_render', XAML_EVENTS.RENDER);
        	// remove works good, add is not working great yet, or at all.
        	App.removeChild('MainGrid', 'ProfilerPanel');
        	App.setRowSpan('renderTarget', 2);
        }
    }
    m_render() {
        this.renderer.m_drawScene(this.scene, this.gfx_ctx);
        Graphics.flushCtx(this.renderer.gfx_ctx);
        this.m_update(16 / 1000);
    }
    m_render_profiled() {
        this.profiler.set_marker('other');
        this.renderer.m_drawScene(this.scene, this.gfx_ctx);
        this.profiler.set_marker('rendering');
        Graphics.flushCtx(this.renderer.gfx_ctx);
        this.profiler.set_marker('uploading');
        this.m_update(16 / 1000);
        this.profiler.set_marker('collision');
        this.profiler.drawProfile();
    }
    m_update(deltaTime = 1 / 1000) {
    	this.scene.nodes.forEach(node => { 
    		node.velocity.y = 1 * deltaTime; // GRAVITY
        	node.update_physics(deltaTime);
        	node.clamp_position(this.bounds.min, this.bounds.max);
    	});
    }
}
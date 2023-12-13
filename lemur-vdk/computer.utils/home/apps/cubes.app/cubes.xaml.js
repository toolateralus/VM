const { Vec2, Node, Scene, Renderer, } = require('game.js');
const { Profiler } = require('profiler.js');
class cubes {
    constructor(id) {
        this.scene = new Scene([]);
        this.id = id;
    	this.width = 256;
    	
    	this.bounds = {
    		min : new Vec2(0, 0), 
    		max : new Vec2(this.width, this.width) 
		};
				
	   for (let z = 0; z < 5 * palette.length; ++z) {
		    const position = new Vec2(85  + z, 85 + z);
		    const scale = new Vec2(25, 25);
		    const node = new Node(scale, position);
		    node.set_vertices(create_square(z % 4));
		    node.vertices.forEach(v => v.color = z % palette.length);
		    
		    let distanceFromCenter = Math.sqrt(position.x * position.x + position.y * position.y);
		
			let frames = 0;
		
		    node.update = (deltaTime) => {
		    	let rotation = 0.025 * deltaTime;
		    	
		    	frames ++;
		    	
		    	if (frames % 200 < 100) {
		    		node.rotate(rotation);	
		    	} else {
		    		node.rotate(-rotation);
		    	}
		    	
	        	
	        	
		    };
		
		    this.scene.nodes.push(node);
		}

   		
     	const gfx_ctx = Graphics.createCtx(this.id, 'renderTarget', this.width, this.width);
        this.renderer = new Renderer(this.width, gfx_ctx);
        
        if (__DEBUG__) {
        	this.profiler = new Profiler();
        	this.profiler.start();
        	App.eventHandler('this', 'm_render_profiled', XAML_EVENTS.RENDER); 
        } else {
        	App.eventHandler('this', 'm_render', XAML_EVENTS.RENDER);
        	
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
    		if (typeof node.update === 'function') {
    			node.update(deltaTime);
    		}
    		node.velocity.y = 1 * deltaTime; // GRAVITY
        	node.update_physics(deltaTime);
        	node.clamp_position(this.bounds.min, this.bounds.max);
    	});
    }
}
const { Node, Vec2 } = require('game.js');
class shapes {
    constructor(id) {
        
        this.gfx_ctx = new GraphicsContext(id, 'RenderingTarget', 256, 256);
        App.eventHandler('this', 'm_Rendering', Event.Rendering);
        
        const player = new Node(new Vec2(25, 25), new Vec2(256, 256));
        player.velocity = new Vec2(0, 2500);
        player.drag = 1;
        
        this.min = new Vec2(0, 0);
        this.max = new Vec2(256, 256);
        
        this.player = player;
        
        App.defer('navigate', 1);
    }
    
   	navigate() {

	    const vel = this.player.velocity;
	    const dir = Math.floor(Math.random() * 6); // Generate a random direction (0 to 5)
	
		
	
	    switch (dir) {
	        case 0:
	            vel.set(0, 1);
	            break;
	        case 1:
	            vel.set(0, -1);
	            break;
	        case 2:
	            vel.set(1, 0);
	            break;
	        case 3:
	            vel.set(-1, 0);
	            break;
	        case 4:
	            vel.set(1, 1);
	            break;
	        case 5:
	            vel.set(-1, -1); 
	            break;
	    }
    
    	vel.mult(200);
    	
   		notify(dir)
    	App.defer('navigate', Math.floor(random() * 10000));
    
	}

    m_Rendering() {
    	this.player.clamp_position(this.min, this.max);
    	this.player.update_physics(16 / 1000);
    	this.gfx_ctx.clearColor(Color.WHITE)
        this.gfx_ctx.drawFilledShape(this.player.pos.x, this.player.pos.y, this.player.scale.x, this.player.scale.y, this.player.rotation, Color.WHITE, Primitive.Rectangle);
        this.gfx_ctx.flushCtx();
        sleep(24)
    }
}

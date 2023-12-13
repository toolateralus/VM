const { Node, Vec2 } = require('game.js');
class shapes {
    constructor(id) {
        this.gfx_ctx = new GraphicsContext(id, 'RenderingTarget', 256, 256);
        App.eventHandler('this', 'm_Rendering', Event.Rendering);
        this.player = new Node(new Vec2(25, 25), new Vec2(1, 1));
        this.player.velocity = new Vec2(25, 25);
        this.player.drag = 1;
        this.min = new Vec2(0, 0);
        this.max = new Vec2(256, 256);
        App.defer('navigate', 1);
    }
    
    navigate () {
		App.defer('navigate', 2500);
		this.player.velocity.set(this.player.velocity.y, this.player.velocity.x);
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

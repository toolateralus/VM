const {
    Vec2,
    Node,
    Scene,
} = require('game.js');
const { Profiler } = require('profiler.js');


class cubesOnline {
    constructor(id) {

        this.id = id;

        this.width = 512;
        this.playing = true;
        
        this.playerSpeed = 250;

        const nodes = [];

		const size = new Vec2(50, 50);
		const pos = new Vec2(Math.floor(this.width / 2), this.width - 100);
        const go = new Node(size, pos);
        
        go.isMesh = true;
        go.colorIndex = Color.YELLOW;
        go.primitveIndex = Primitive.Rectangle;

        this.player = go;

        nodes.push(this.player);
        this.scene = new Scene(nodes);

        Network.addListener('onMessage');

        this.gfx_ctx = new GraphicsContext(this.id, 'RenderingTarget', this.width, this.width);
        App.eventHandler('this', 'm_Rendering', Event.Rendering);

        this.startTime = 0;
        
        this.shotVelocity = -500;

        this.bounds = {
            min : new Vec2(0, 0),
            max : new Vec2(this.width, this.width),
        };

        this.player.onUpdate = (deltaTime) => {
            let w = Key.isDown('W');
            let a = Key.isDown('A');
            let s = Key.isDown('S');
            let d = Key.isDown('D');
    
            if (w || a || s || d) {
                let strafe = (a ? -1 : 0) + (d ? 1 : 0);
    
                if (w || s)
                    this.shoot();
    
                this.player.velocity.x = strafe * this.playerSpeed;
            }
        }

    }
    onCollision(node) {

        if (node.isProjectile !== true)
            return;
    
    	// remove
        const index = this.scene.nodes.indexOf(node);
        this.scene.nodes.splice(index, 1);
	
		
		if (Network.IsConnected || this.channel == this.opponent) {
			const packet = {
    	        type: 'bullet',
    	        bullet: node,
	        };

        	Network.send(this.opponent, this.channel, JSON.stringify(packet));
		}
		
        
    }
    onMessage(channel, reply, json) {
    	const data = JSON.parse(json).data;
        if (channel === this.channel) {
        	var msg = JSON.tryParse(data);
        	if (!msg.hasValue) {
        		return;
        	}
        	msg = msg.value;
        	if (!msg.type || msg.type !== 'bullet' || !msg.bullet) {
        		return;
        	}
        	const obj = msg.bullet;
            const bullet = new Node();
            bullet.copy(obj); // copies obj to bullet.
            this.scene.nodes.push(bullet);
        }
    }
    m_Rendering() {
        if (this.playing !== true)
            return;

        const time = Date.now();
        const deltaTime = (time - this.startTime) / 1000;

        this.update(deltaTime);
        
        this.channel = parseInt(App.getProperty('yourChannelBox', 'Text'));
        this.opponent = parseInt(App.getProperty('channelBox', 'Text'));

		this.startTime = time;
        
        this.draw();
    }
    draw() {
        this.gfx_ctx.clearColorIndex(Color.BLACK);

        const nodes = this.scene.nodes;

        for (let i = 0; i < nodes.length; ++i) {
            const node = nodes[i];

            if (node.isMesh !== true)
                return;

            const x = node.pos.x;
            const y = node.pos.y;

            const width = node.scale.x;
            const height = node.scale.y;

            const color = node.colorIndex;
            const prim = node.primitveIndex;

            const rot = node.rotation;

            this.gfx_ctx.drawFilledShape(Math.floor(x), Math.floor(y), width, height, rot, color, prim);
        };

        this.gfx_ctx.flushCtx();
    }
    update(deltaTime) {
        this.player.pos.y = this.width - 100;
        this.player.velocity.y = 0;

		if (Key.isDown('Escape')) {
			Key.clearFocus();
		}

        const destroyed = [];

        for (let i = 0; i < this.scene.nodes.length; ++i) {
        	const node = this.scene.nodes[i];

            if (typeof node.onUpdate === 'function')
                node.onUpdate(deltaTime); 
                
            node.velocity.y += 0.0981 * deltaTime;
            
            node.update_physics(deltaTime);
            
            if (node.clamp_position(this.bounds.min, this.bounds.max))
                this.onCollision(node);
        };

        const deltaTimeMillis = clamp(0, 16, Math.floor(deltaTime));

        sleep(16 - deltaTimeMillis);
    }
    shoot() {
    
    	if (this.scene.nodes.length > 1) {
    		return;
    	}
    
        const bullet = new Node(this.player.scale, new Vec2(this.player.pos.x, this.player.pos.y - this.player.scale.y));
            
        bullet.isMesh = true;
        bullet.colorIndex = Color.RED;
        bullet.primitveIndex = Primitive.Rectangle;
        bullet.isProjectile = true;
        bullet.drag = 0.999;
        bullet.velocity.y = this.shotVelocity;
        
        this.scene.nodes.push(bullet);
    }
}
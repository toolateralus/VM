class cubesOnline {
    constructor(id) {
    	notify(id);
        this.id = id;
        this.playerSpeed = 250;
        this.shotVelocity = new Vec2(500, 0);
        this.width = 512;
        const nodes = [];

        const size = new Vec2(50, 50);
        const position = new Vec2(256, 412);
        const go = new Node(size, position);
        
        go.isMesh = true;
        go.colorIndex = Color.YELLOW;
        go.primitveIndex = Primitive.Rectangle;
        
        this.player = go;

        nodes.push(this.player);
        describe(this.player);

        this.player.onUpdate = (deltaTime) => {
            this.player.position.y = this.width;
            this.player.velocity.y = 0;
			
            this.channel = parseInt(App.getProperty('yourChannelBox', 'Text'));
            this.opponent = parseInt(App.getProperty('channelBox', 'Text'));
            
            let w = Key.isDown('W');
            let a = Key.isDown('A');
            let s = Key.isDown('S');
            let d = Key.isDown('D');
            
            if (w || a || s || d) {
                let strafe = (a ? -1 : 0) + (d ? 1 : 0);
                if (w || s)
                    this.shoot();
                this.player.velocity.x = strafe * this.playerSpeed;
            };
			
        }

        this.targetControl = 'RenderingTarget';

        this.scene = new Scene(nodes);

        this.renderer = new ShapeRenderer(this.id, this.targetControl, // pid, render surface
                                                this.width, this.width,    // pixel width / height
                                                this.scene, this.on_collision);  // scene, collision callback

        App.eventHandler('this', 'renderLoop', Event.Rendering);
       // Network.addListener('onMessage');
    }
    renderLoop() {
        this.renderer.cycle(); // advance the renderer 1 frame.
    }
    on_collision(node) {
        if (node.isProjectile !== true)
            return;
    	// remove
        const index = this.scene.nodes.indexOf(node);
        this.scene.nodes.splice(index, 1);
	
		if (Network.IsConnected) {
			const packet = {
    	        type: 'bullet',
    	        bullet: node,
	        };
        	Network.send(this.opponent, this.channel, JSON.stringify(packet));
		}
    }
    onMessage(channel, _, json) {
        const incoming = JSON.parse(json);

        if (incoming.data === undefined)
            return;

    	const data = incoming.data;

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
    shoot() {

    	if (this.scene.nodes.length > 1) {
    		return;
    	}
    
        const bullet = new Node(this.player.scale, new Vec2(this.player.position.x, this.player.position.y - this.player.scale.y));
            
        bullet.isMesh = true;
        bullet.colorIndex = Color.RED;
        bullet.primitveIndex = Primitive.Rectangle;
        bullet.isProjectile = true;
        bullet.drag = 0.98;
        bullet.velocity = this.shotVelocity;
        
        this.scene.nodes.push(bullet);
    }
}
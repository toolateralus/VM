const {
    Point,
    GameObject,
    Scene,
} = require('game.js');
const { Profiler } = require('profiler.js');


class cubesOnline {
    constructor(id) {

        this.id = id;

        this.width = 512;
        this.playing = true;
        
        this.playerSpeed = 50;

        const gameObjects = [];

        const go = new GameObject([], new Point(50, 50), new Point(Math.floor(this.width / 2), this.width - 100));
        go.isMesh = true;
        go.colorIndex = Color.YELLOW;
        go.primitveIndex = Primitive.Rectangle;

        this.player = go;

        gameObjects.push(this.player);
        this.scene = new Scene(gameObjects);

        network.addListener('onMessage');

        this.gfx_ctx = gfx.createCtx(this.id, 'renderTarget', this.width, this.width);
        app.eventHandler('this', 'm_render', XAML_EVENTS.RENDER);
		app.eventHandler('connectBtn', 'onConnectPressed', XAML_EVENTS.MOUSE_DOWN);

        this.startTime = 0;
        
        this.shotVelocity = -500;
    }
    onCollision(gO) {

        if (gO.isProjectile !== true) {
            return;
        }
    
        const index = this.scene.gOs.indexOf(gO);
        this.scene.gOs.splice(index, 1);

        const packet = {
            type: 'bullet',
            bullet: gO,
        };

        network.send(this.opponent, this.channel, JSON.stringify(packet));
    }
	onConnectPressed () {
        network.connect('192.168.0.141');
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
            const bullet = new GameObject(obj.points, obj.scale, obj.pos);
            bullet.isMesh = obj.isMesh;
            bullet.colorIndex = (obj.colorIndex + 1) % palette.length;
            bullet.primitveIndex = obj.primitveIndex;
            bullet.velocity.x = -obj.velocity.x;
            bullet.velocity.y = -obj.velocity.y;
            bullet.drag = obj.drag;
            bullet.isProjectile = obj.isProjectile;
            this.scene.gOs.push(bullet);
        }
    }
    m_render() {
        if (this.playing !== true)
            return;

        const time = Date.now();
        const deltaTime = (time - this.startTime) / 1000;

        this.update(deltaTime);
        
        this.channel = parseInt(app.getProperty('yourChannelBox', 'Text'));
        this.opponent = parseInt(app.getProperty('channelBox', 'Text'));

		this.startTime = time;
        gfx.clearColor(this.gfx_ctx, Color.BLACK);

        const gameObjects = this.scene.gOs;

        for (let i = 0; i < gameObjects.length; ++i) {
            const gO = gameObjects[i];

            if (gO.isMesh !== true)
                return;

            const x = gO.pos.x;
            const y = gO.pos.y;

            const width = gO.scale.x;
            const height = gO.scale.y;

            const color = gO.colorIndex;
            const prim = gO.primitveIndex;

            const rot = gO.rotation;

            gfx.drawFilledShape(this.gfx_ctx, Math.floor(x), Math.floor(y), width, height, rot, color, prim);
        };

        gfx.flushCtx(this.gfx_ctx);
        
     
    }
    update(deltaTime) {
        this.player.pos.y = this.width - 100;
        this.player.velocity.y = 0;

		if (Key.isDown('Escape')) {
			Key.clearFocus();
		}

        const destroyed = [];

        this.scene.gOs.forEach (gO => {
            gO.velocity.y += 0.0981 * deltaTime;
            gO.update_physics(deltaTime);
            if (gO.confine_to_screen_space(this.width))
                destroyed.push(gO);
        });

        destroyed.forEach(gO => {
            this.onCollision(gO);
        });
    

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

        const deltaTimeMillis = clamp(0, 16, Math.floor(deltaTime));

        sleep(16 - deltaTimeMillis);
    }
    shoot() {
    
    	if (this.scene.gOs.length > 2) {
    		return;
    	}
    
        const bullet = new GameObject([], this.player.scale, new Point(this.player.pos.x - this.player.scale.x, this.player.pos.y - this.player.scale.x));
        bullet.isMesh = true;
        bullet.colorIndex = Color.RED;
        bullet.primitveIndex = Primitive.Rectangle;
        bullet.velocity.y = this.shotVelocity;
        bullet.isProjectile = true;
        bullet.drag = 0.999;
        
        this.scene.gOs.push(bullet);
    }
}
const {
    Point,
    GameObject,
    Scene,
} = require('game.js');
const { Profiler } = require('profiler.js');


class gamer {
    constructor(id) {

        this.id = id;
        

        this.width = 2048;
        this.playing = true;
        
        this.playerSpeed = 50;

        const gameObjects = [];

        const go = new GameObject([], new Point(100, 100), new Point(Math.floor(this.width / 2), this.width - 100));
        go.isMesh = true;
        go.colorIndex = Color.YELLOW;
        go.primitveIndex = Primitive.Rectangle;

        this.player = go;

        gameObjects.push(this.player);
        this.scene = new Scene(gameObjects);

        network.addListener('onMessage');

        this.gfx_ctx = gfx.createCtx(this.id, 'renderTarget', this.width, this.width);
        app.eventHandler('this', 'm_render', XAML_EVENTS.RENDER);
		app.eventHandler('connectBtn', 'onConnectPressed', XAML_EVENTS.RENDER);
		
        gfx.loadSkybox(this.gfx_ctx, 'icon.bmp');

        const Packet = {
            type: 'gameState',
            input_fire: false,
            input_strafe: 0,
        };

       
    }
    onCollision(gO) {

        if (gO.isProjectile !== true) {
            return;
        }
    
        const index = this.scene.gOs.indexOf(gO);
        this.scene.gOs.splice(index, 1);

        const packet = {
            type: 'bullet',
            vel: new Point(-gO.velocity.x, -gO.velocity.y),
            pos: gO.pos,
        };
        
        this.channel = parseInt(app.getProperty('yourChannelBox', 'Text'));
        this.opponent = parseInt(app.getProperty('channelBox', 'Text'));

        network.send(this.opponent, this.channel, JSON.stringify(packet));
    }
	onConnectPressed () {
	
	}
    
    onMessage(channel, reply, message) {
        const packet = JSON.parse(JSON.parse(message).data);
    	const packet = JSON.parse(json);
        if (channel === this.channel) {
        	var msg = JSON.tryParse(packet.data);
        	if (!msg.hasValue || !(msg = msg.value).type) {
        		return;
        	}
            const bullet = new GameObject([], this.player.scale, packet.pos);
            bullet.isMesh = true;
            bullet.colorIndex = Color.SPRING_GREEN;
            bullet.primitveIndex = Primitive.Rectangle;
            bullet.velocity = packet.vel;
            bullet.drag = 0.999;
            bullet.isProjectile = true;
            this.scene.gOs.push(bullet);
        }
    }
    m_render() {
        if (this.playing !== true)
            return;

        this.update();

        //gfx.drawSkybox(this.gfx_ctx);
        gfx.clearColor(this.gfx_ctx, Color.BLACK);

        this.frameCt++;

        const destroyed = [];
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

            gO.velocity.y += 0.0981;
            gO.update_physics();
            
            if (gO.confine_to_screen_space(this.width))
                destroyed.push(gO);

            gfx.drawFilledShape(this.gfx_ctx, Math.floor(x), Math.floor(y), width, height, rot, color, prim);
        };

        destroyed.forEach(gO => {
            this.onCollision(gO);
        });
    
        gfx.flushCtx(this.gfx_ctx);

    }
    update() {
        this.player.pos.y = this.width - 100;
        this.player.velocity.y = 0;

		if (Key.isDown('Escape')) {
			Key.clearFocus();
		}

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
    shoot() {
        const bullet = new GameObject([], this.player.scale, new Point(this.player.pos.x - this.player.scale.x, this.player.pos.y - this.player.scale.x));
        bullet.isMesh = true;
        bullet.colorIndex = Color.RED;
        bullet.primitveIndex = Primitive.Rectangle;
        bullet.velocity.y = -100;
        bullet.drag = 0.999;
        bullet.isProjectile = true;
        this.scene.gOs.push(bullet);
    }
}
const {
    Vec2,
    Node,
    Scene,
} = require('game.js');

const { Profiler } = require('profiler.js');

class shapes {
    constructor(id) {

        this.captureBeginTime = 0;
        this.captureEndTime = 0;
        // for the engine.
        this.id = id;
        this.frameCt = 0;

		this.width = 256;

        this.gfx_ctx = Graphics.createCtx(this.id, 'renderTarget', this.width, this.width);

        this.spawnScene();

        App.eventHandler('this', 'm_render', XAML_EVENTS.RENDER);
        App.eventHandler('playButton', 'onPlayClicked', XAML_EVENTS.MOUSE_DOWN);
        
        this.profiler = new Profiler();
        this.profiler.start();
        
        this.playing = false;
        
        this.bounds = {
            min : new Vec2(0, 0),
            max : new Vec2(this.width, this.width),
        };
    }
    
	onPlayClicked() {
		
		if (this.playing === true) {
			App.setProperty('playButton', 'Content', 'Play');
			this.playing = false;
		} else {
			App.setProperty('playButton', 'Content', 'Pause');
			this.playing = true;
		}
	}
	
    spawnScene() {
        const nodes = [];

        const generateFractal = (scale, pos, depth) => {
            if (depth === 0) {
                let node = new Node(scale, pos);
                node.colorIndex = pos.x;
                node.isMesh = true;
                node.primitveIndex = Primitive.Rectangle;
                nodes.push(node);
            } else {
                const childScale = new Vec2(scale.x / 2, scale.y / 2);
                const childPos1 = new Vec2(pos.x - childScale.x, pos.y - childScale.y);
                const childPos2 = new Vec2(pos.x + childScale.x, pos.y - childScale.y);
                const childPos3 = new Vec2(pos.x - childScale.x, pos.y + childScale.y);
                const childPos4 = new Vec2(pos.x + childScale.x, pos.y + childScale.y);

                generateFractal(childScale, childPos1, depth - 1);
                generateFractal(childScale, childPos2, depth - 1);
                generateFractal(childScale, childPos3, depth - 1);
                generateFractal(childScale, childPos4, depth - 1);
            }
        };

        const depth = 4;
        const scale = new Vec2(this.width, this.width);
        const pos = new Vec2(this.width / 2, this.width / 2);
        generateFractal(scale, pos, depth);

        this.scene = new Scene(nodes);
    }

  
    m_render() {
    	
    	if (this.playing !== true)
    		return;

        Graphics.clearColor(this.gfx_ctx, Color.BLACK);

        this.profiler.set_marker('other');

        this.profiler.set_marker('profiler');

        this.frameCt++;
        
		let frequency = App.getProperty('frequencySlider', 'Value');
    	frequency = Math.floor(frequency);	
    	
    	if (this.lastFrequency !== frequency)
    	{
    		this.lastFrequency = frequency;	
    		App.setProperty('nodeCountLabel', 'Content', frequency);
    	}
    	
    	const nodes = this.scene.nodes;
    	for (let i = 0; i < frequency && i < nodes.length; ++i) {
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

    	    node.velocity.y += 0.0981; 
            node.update_physics();
            node.clamp_position(this.bounds.min, this.bounds.max);

                      

            if (node.pos.y > (this.width - 25)) {
                node.velocity.y = random() * -20;
                node.velocity.x = (1.0 - random(2.0));
                node.angular = 1.0;
            }
        
            if (node.pos.x === 0 || node.pos.x === this.width) {
                node.velocity.x = -(node.velocity.x * 2);
            }

            Graphics.drawFilledShape(this.gfx_ctx, Math.floor(x), Math.floor(y), width, height, rot, color, prim);
	
        };

        this.profiler.set_marker('rendering');
        
        Graphics.flushCtx(this.gfx_ctx);
        
        this.profiler.set_marker('uploading');
        this.profiler.drawProfile();
    }
}
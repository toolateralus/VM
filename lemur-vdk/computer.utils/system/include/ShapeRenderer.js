const {
    Vec2,
    Node,
    Scene,
} = require('game.js');

class ShapeRenderer {
    constructor(id, target, width, height, scene, collisionCallback) {
        this.scene = scene ?? new Scene();
        this.bounds = {
            min: new Vec2(0, 0),
            max: new Vec2(width, height),
        };
        this.gfx_ctx = new GraphicsContext(id, target, width, height);
        this.startTime = 0;
        this.width = width;
        this.playing = true;

        if (typeof collisionCallback !== 'function')
            throw new Error('collisionCallback must be a function');

        this.collisionCallback = collisionCallback;
    }

    cycle() {
        if (!this.playing)
            return;

        const time = Date.now();
        const deltaTime = (time - this.startTime) / 1000;
        const deltaTimeMillis = clamp(0, 16, Math.floor(deltaTime));
        sleep(16 - deltaTimeMillis);
        this.update(deltaTime);
        this.startTime = time;
        this.drawScene();
    }

    drawScene() {
        this.gfx_ctx.clearColorIndex(Color.BLACK);

        for (const node of this.scene.nodes) {
            if (!node.isMesh)
                throw new Error('Invalid node type');

            if (!node.position)
                throw new Error('Invalid position');

            const { x, y } = node.position;
            if (isNaN(x) || isNaN(y))
                throw new Error('Invalid x or y :: NaN');

            if (!node.scale)
                throw new Error('Invalid scale');

            const { x: width, y: height } = node.scale;
            if (!node.colorIndex)
                throw new Error('Invalid color');

            const color = node.colorIndex;

            if (!node.primitveIndex)
                throw new Error('Invalid primitive');

            const prim = node.primitveIndex;

            if (!node.rotation)
                throw new Error('Invalid rotation');

            const rot = node.rotation;

            if (width == null || width == undefined ||
                height == null || height == undefined)
                throw new Error('Invalid width or height');

            if (isNaN(x) || isNaN(y))
                throw new Error('Invalid x or y :: NaN');

            if (x == null || x == undefined ||
                y == null || y == undefined)
                throw new Error('Invalid x or y');

            print(`drawing : ${x} ${y} ${width} ${height} ${rot} ${color} ${prim}`);
            this.gfx_ctx.drawFilledShape(x, y, width, height, rot, color, prim);
        }
    }

    update(deltaTime) {
        for (const [index, node] of this.scene.nodes.entries()) {
            if (!node)
                throw new Error(`update::error : Invalid node in scene at index ${index}`);
            if (!node.position)
                throw new Error(`update::error : Invalid node position at index ${index}`);
            if (!node.velocity)
                throw new Error(`update::error : Invalid node velocity at index ${index}`);
            if (!node.scale)
                throw new Error(`update::error : Invalid node scale at index ${index}`);
            if (!node.rotation)
                throw new Error(`update::error : Invalid node rotation at index ${index}`);
            if (!node.velocity)
                throw new Error(`update::error : Invalid node velocity at index ${index}`);

            if (Key.isDown('Escape'))
                Key.clearFocus();

            if (typeof node.onUpdate === 'function')
                node.onUpdate(deltaTime);

            node.velocity.y += 0.0981 * deltaTime;

            node.update_physics(deltaTime);

            if (node.clamp_position(this.bounds.min, this.bounds.max))
                this.collisionCallback(node);
        }
    }
    update(deltaTime) {
        
        for (let i = 0; i < this.scene.nodes.length; ++i) {
            const node = this.scene.nodes[i];

            if (node === undefined || node === null)
                throw new Error(`update::error : Invalid node in scene at index ${i}`);
            if (node === undefined || node === null)
                throw new Error(`update::error : Invalid node position at index ${i}`);
            if (node === undefined || node === null)
                throw new Error(`update::error : Invalid node velocity at index ${i}`);
            if (node === undefined || node === null)
                throw new Error(`update::error : Invalid node scale at index ${i}`);
            if (node === undefined || node === null)
                throw new Error(`update::error : Invalid node rotation at index ${i}`);
            if (node === undefined || node === null)
                throw new Error(`update::error : Invalid node velocity at index ${i}`);
		
            if (Key.isDown('Escape'))
			    Key.clearFocus();
        
            if (typeof node.onUpdate === 'function')
                node.onUpdate(deltaTime); 
                
            node.velocity.y += 0.0981 * deltaTime;
            
            node.update_physics(deltaTime);
            
            if (node.clamp_position(this.bounds.min, this.bounds.max))
                this.collisionCallback(node);
        
        };

    }
    cycle() {
        if (this.playing !== true)
            return;
        const time = Date.now();
        const deltaTime = (time - this.startTime) / 1000;
        const deltaTimeMillis = clamp(0, 16, Math.floor(deltaTime));
        sleep(16 - deltaTimeMillis);
        this.update(deltaTime);
        this.startTime = time;
        this.draw_scene();
    }
    draw_scene() {
        this.gfx_ctx.clearColorIndex(Color.BLACK);

        const nodes = this.scene.nodes;

        for (let i = 0; i < nodes.length; ++i) {
            const node = nodes[i];

            if (node.isMesh !== true) 
                throw new Error('Invalid node type');
            

            if (node.position === undefined || node.position === null) 
                throw new Error('Invalid position');
            

            var x = node.position.x;
            var y = node.position.y;

            if (isNaN(x) || isNaN(y)) {
                throw new Error('Invalid x or y :: NaN');
            }

            if (node.scale === undefined || node.scale === null) 
                throw new Error('Invalid scale');
            

            const width = node.scale.x;
            const height = node.scale.y;

            if (node.colorIndex === undefined || node.colorIndex === null) 
                throw new Error('Invalid color');
            

            const color = node.colorIndex;

            if (node.primitveIndex === undefined || node.primitveIndex === null) 
                throw new Error('Invalid primitive');
            

            const prim = node.primitveIndex;

            if (node.rotation === undefined || node.rotation === null) 
                throw new Error('Invalid rotation');
            
            const rot = node.rotation;

            if (width == null || width == undefined ||
                height == null || height == undefined) 
                throw new Error('Invalid width or height');
            
            if (x == null || x == undefined ||
                y == null || y == undefined) 
                throw new Error('Invalid x or y');
            
            
            if (__DEBUG__)
                print(`drawing : ${x} ${y} ${width} ${height} ${rot} ${color} ${prim}`);

            this.gfx_ctx.drawFilledShape(x, y, width, height, rot, color, prim);
        }

        this.gfx_ctx.flushCtx();
    }
}

// include some neccesary dependencies if needed.
return { ShapeRenderer : ShapeRenderer };
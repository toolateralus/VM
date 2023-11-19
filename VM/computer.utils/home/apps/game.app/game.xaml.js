class renderer {
    constructor(resolution){
        // renderer data
        this.bytesPerPixel = 4;
        this.width = resolution;
        this.frameData = [];
        this.resizing = false;
        this.newWidth = this.width;
        this.isDirty = true;
        
        // 24 color, 4bpp (a,r,g,b) color palette.
        this.palette = [
            [255, 255, 0, 0],       // Red 0
            [255, 255, 128, 0],     // Orange 1
            [255, 255, 255, 0],     // Yellow 2
            [255, 128, 255, 0],     // Lime Green 3
            [255, 0, 255, 0],       // Green 4
            [255, 0, 255, 128],     // Spring Green 5
            [255, 0, 255, 255],     // Cyan 6
            [255, 0, 128, 255],     // Sky Blue 7 
            [255, 0, 0, 255],       // Blue 8
            [255, 128, 0, 255],     // Purple 9 
            [255, 255, 0, 255],     // Magenta 10
            [255, 255, 0, 128],     // Pink 11
            [255, 192, 192, 192],   // Light Gray 12
            [255, 128, 128, 128],   // Medium Gray 13
            [255, 64, 64, 64],      // Dark Gray 14
            [255, 0, 0, 0],         // Black 15
            [255, 255, 255, 255],   // White 16
            [255, 255, 69, 0],      // Red-Orange 17
            [255, 255, 215, 0],     // Gold 18
            [255, 0, 128, 0],       // Dark Green 19
            [255, 0, 128, 128],     // Teal 20
            [255, 0, 0, 128],       // Navy 21
            [255, 255, 20, 147],    // Deep Pink 22
            [255, 0, 250, 154]      // Medium Spring Green 23
        ];
    }
    
    getRender(){
        if (this.frameData.length == 0){
            return [[0,0,0,0]]
        }
        return this.frameData;
    }

    clean(color) {
        this.width = this.newWidth;
        this.frameData = [];
        for (let y = 0; y < this.width; y++) {
            for (let x = 0; x < this.width; x++) {
                this.frameData.push(color[0])
                this.frameData.push(color[1])
                this.frameData.push(color[2])
                this.frameData.push(color[3])
            }
        }
    }

    setWidth(width) {
        this.newWidth = width;
        this.resizing = true;
    }

    lerpColors(a, b, t) {
        const result = new Uint8Array(4);
        for (let i = 0; i < 4; i++) {
            result[i] = Math.floor(b[i] * t + a[i] * (1 - t));
        }
        return result;
    }

    writePixel(x, y, color) {
        let index = (y * this.width + x) * this.bytesPerPixel;
        
        this.frameData[index + 0] = color[0];
        this.frameData[index + 1] = color[1];
        this.frameData[index + 2] = color[2];
        this.frameData[index + 3] = color[3];
    
        this.isDirty = true;
    }

    _drawScene(scene) {
        
        if (this.resizing) {
            this.clean(this.palette[15]);
            this.resizing = false;
            return true;
        }
        if (this.isDirty)
        {
            this.clean(this.palette[15]);
            const gameObjects = scene.gameObjects();

            // all objects in scene
            for (let z = 0; z < gameObjects.length; ++z)
            {
                const gameObject = gameObjects[z];

                const min_x = Math.floor(gameObject.pos.x - (gameObject.scale.x / 2));
                const min_y = Math.floor(gameObject.pos.y - (gameObject.scale.y / 2));
                const max_x = Math.floor(gameObject.pos.x + (gameObject.scale.x / 2));
                const max_y = Math.floor(gameObject.pos.y + (gameObject.scale.y / 2));

                for (let y = min_y; y < max_y; y++) {
                    for (let x = min_x; x < max_x; x++) {
                        // enable for wrapping of y.. y??
                        //const wrappedY = (y + this.width) % this.width; 
                        //this.writePixel(x, wrappedY, this.palette[16]);

                        this.writePixel(x, y, this.palette[16]);
                    }
                }
            }
            this.isDirty = false;
            return true;
        }
        return false;
    }
}

// THIS IS THE WPF CLASS! (it chooses the file name associated class.)
class game {
    
    setupUIEvents() {
        app.eventHandler(this.__ID, 'this', '_render', XAML_EVENTS.RENDER);
        app.eventHandler(this.__ID, 'this', 'onKey', XAML_EVENTS.KEY_DOWN);
    }

    onKey(key, isDown) {
        if (key === 'W')
            this.player.velocity.y = -this.moveSpeed;
    
        if (key === 'A')
            this.player.velocity.x = -this.moveSpeed;
    
        if (key === 'S')
            this.player.velocity.y = this.moveSpeed;
        
        if (key === 'D')
            this.player.velocity.x = this.moveSpeed;
    
        this.renderer.isDirty = true
    }

    _render() {
        // returns a bool indicating whether anything was actually drawn or not
        if (this.renderer._drawScene(this.scene) === true){
            app.pushEvent(this.__ID, 'renderTarget', 'draw_pixels', this.renderer.getRender());
        }
        this.player.update_physics()
        this.player.confine_to_screen_space(this.renderer.width);
    }

    _getSquare(size){
        const v1 = new point(0, 0, this.renderer.palette[0])
        const v2 = new point(0, size, this.renderer.palette[1])
        const v3 = new point(size, 0, this.renderer.palette[3])
        const v4 = new point(size, size, this.renderer.palette[4])
        const verts = [v1, v2, v3, v4];
        return verts;
    }
    
    constructor(id) {
        // for the engine.
        this.__ID = id;
        
        // 64x64 render surface.
        this.renderer = new renderer(128);
        
        // initialize the drawing surface
        this.renderer.clean(this.renderer.palette[15]);

        // pixels per frame. .12 is pretty fast since we run at 30-100 fps, ~1-3 px/s 
        this.moveSpeed = 0.32;
        
        // player init 
        // 4x4 (pixel) square sprite

        const verts = this._getSquare(4);

        // obj scale.
        const scale = new point(1, 1);

        // start position
        const pos = new point(24, 24);

        // make a player object
        this.player = new gameObject(verts, scale, pos);
        this.scene = new scene([this.player]);

        // setup events (including render/physics loops)
        this.setupUIEvents();

        // start drawing, the renderer only draws when it's marked as dirty.
        this.renderer.isDirty = true;
    }
}

class scene {
    // array of all gameobjects in scene.
    gameObjects = () => this.gOs;
    constructor(gOs){
        this.gOs = gOs;
    }
}

class line {
    constructor(point1, point2) {
        this.point1 = point1;
        this.point2 = point2;
    }
    
    getClosestPoint(x, y) {
        const dx = this.point2.x - this.point1.x;
        const dy = this.point2.y - this.point1.y;
        const u = ((x - this.point1.x) * dx + (y - this.point1.y) * dy) / (dx * dx + dy * dy);
        return u >= 0 && u <= 1 ? this.point1 : u < 0 ? this.point1 : this.point2;
    }
}

class point {
    constructor(x, y, color) {
        this.x = x;
        this.y = y;
        this.color = color;
    }
    getColor() {
        return this.color;
    }
    add(x, y) {
        this.x += x;
        this.y += y;
        return this;
    }
    subtract(otherPoint) {
        this.x -= otherPoint.x;
        this.y -= otherPoint.y;
        return this;
    }
    mult(scalar) {
        this.x *= scalar;
        this.y *= scalar;
        return this;
    }
    divide(scalar) {
        this.x /= scalar;
        this.y /= scalar;
        return this;
    }
    distance(x, y) {
        const dx = x - this.x;
        const dy = y - this.y;
        return Math.sqrt(dx * dx + dy * dy);
    }
    dot(other) {
        return this.x * other.x + this.y * other.y;
    }
    magnitude() {
        return Math.sqrt(this.x * this.x + this.y * this.y);
    }
    normalize() {
        const mag = this.magnitude();
        if (mag !== 0) {
            this.x /= mag;
            this.y /= mag;
        }
        return this;
    }
    set(x,y){
        this.x = x;
        this.y = y;
    }
    
}

class gameObject {

    constructor(points, scale, pos) {
        this.scale = scale ??  new point(1,1);
        this.pos = pos ?? new point(0,0);
        this.points = points ?? [];
        this.edges = this.createEdges(this.points);
        this.velocity = new point(0,0);
        this._cachedColorRatio = undefined;
    }
    confine_to_screen_space(width){
        const min_x = 1, min_y = 1, max_x = width - 1, max_y = width -1;
        this.pos.x = Math.min(Math.max(this.pos.x, min_x), max_x);
        this.pos.y = Math.min(Math.max(this.pos.y, min_y), max_y);
    }
    distanceToPoint(x1, y1, x2, y2) {
        const dx = x2 - x1;
        const dy = y2 - y1;
        return Math.sqrt(dx ** 2 + dy ** 2);
    }
    getClosestPoints(x, y) {

        // sort edges by distance from query pt
        const sortedEdges = this.edges.slice().sort((edgeA, edgeB) => {
            const distanceA = edgeA.point1.distance(new point(x, y));
            const distanceB = edgeB.point1.distance(new point(x, y));
            return distanceA - distanceB;
        });
    
        // get closest
        const edge = sortedEdges[0];
        
        // get points
        const closest = edge.getClosestPoint(x, y);
        const other = closest === edge.point1 ? edge.point2 : edge.point1;
    
        // get ratio
        const distanceClosestToQuery = closest.distance(new point(x, y));
        const distanceClosestToOther = closest.distance(other);
        this._cachedColorRatio = distanceClosestToQuery / (distanceClosestToQuery + distanceClosestToOther);
    
        return [closest, other];
    }
    getBlendedColor(points, lerpFunction){
        const A = points[0];
        const B = points[1];
        const blended = lerpFunction(A, B, this._cachedColorRatio);
        this._cachedColorRatio = 0;
        return blended;
    }
    collides(x, y) {
        let isInside = false;

        for (let i = 0, j = this.edges.length - 1; i < this.edges.length; j = i++) {
            const edge = this.edges[i];
            
            const x1 = edge.point1.x;
            const y1 = edge.point1.y;
            const x2 = edge.point2.x;
            const y2 = edge.point2.y;

            if ((y1 > y) !== (y2 > y) && x < ((x2 - x1) * (y - y1)) / (y2 - y1) + x1)
            {
                isInside = !isInside;
            }
        }

        return isInside;
    }
    createEdges(points) {
        const edges = [];

        for (let i = 0; i < points.length; ++i) {
            const pt1 = points[i];
            const pt2 = points[(i + 1) % points.length];
            edges.push(new line(pt1, pt2));
        }
        return edges;
    }
    update_physics() {

        this.pos.x += this.velocity.x;
        this.pos.y += this.velocity.y;

        this.velocity.x *= 0.95;
        this.velocity.y *= 0.95;
    }
    rotate(angle) {
        const cosAngle = Math.cos(angle);
        const sinAngle = Math.sin(angle);

        for (const point of this.points) {
            const x = point.x - this.pos.x;
            const y = point.y - this.pos.y;

            const rotatedX = x * cosAngle - y * sinAngle;
            const rotatedY = x * sinAngle + y * cosAngle;

            point.x = rotatedX + this.pos.x;
            point.y = rotatedY + this.pos.y;
        }
    }
}
class Renderer {
    constructor(resolution){
        // Renderer data
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
            const GameObjects = scene.GameObjects();

            // all objects in Scene
            for (let z = 0; z < GameObjects.length; ++z)
            {
                const gameObject = GameObjects[z];

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
    
        this.Renderer.isDirty = true
    }

    _render() {
        // returns a bool indicating whether anything was actually drawn or not
        if (this.Renderer._drawScene(this.Scene) === true){
            app.pushEvent(this.__ID, 'renderTarget', 'draw_pixels', this.Renderer.getRender());
        }
        this.player.update_physics()
        this.player.confine_to_screen_space(this.Renderer.width);
    }

    _getSquare(size){
        const v1 = new Point(0, 0, this.Renderer.palette[0])
        const v2 = new Point(0, size, this.Renderer.palette[1])
        const v3 = new Point(size, 0, this.Renderer.palette[3])
        const v4 = new Point(size, size, this.Renderer.palette[4])
        const verts = [v1, v2, v3, v4];
        return verts;
    }
    
    constructor(id) {
        // for the engine.
        this.__ID = id;
        
        // 64x64 render surface.
        this.Renderer = new Renderer(128);
        
        // initialize the drawing surface
        this.Renderer.clean(this.Renderer.palette[15]);

        // pixels per frame. .12 is pretty fast since we run at 30-100 fps, ~1-3 px/s 
        this.moveSpeed = 0.32;
        
        // player init 
        // 4x4 (pixel) square sprite

        const verts = this._getSquare(4);

        // obj scale.
        const scale = new Point(1, 1);

        // start position
        const pos = new Point(24, 24);

        // make a player object
        this.player = new GameObject(verts, scale, pos);
        this.Scene = new Scene([this.player]);

        // setup events (including render/physics loops)
        this.setupUIEvents();

        // start drawing, the Renderer only draws when it's marked as dirty.
        this.Renderer.isDirty = true;
    }
}

class Scene {
    // array of all GameObjects in Scene.
    GameObjects = () => this.gOs;
    constructor(gOs){
        this.gOs = gOs;
    }
}

class Line {
    constructor(Point1, Point2) {
        this.Point1 = Point1;
        this.Point2 = Point2;
    }
    
    getClosestPoint(x, y) {
        const dx = this.Point2.x - this.Point1.x;
        const dy = this.Point2.y - this.Point1.y;
        const u = ((x - this.Point1.x) * dx + (y - this.Point1.y) * dy) / (dx * dx + dy * dy);
        return u >= 0 && u <= 1 ? this.Point1 : u < 0 ? this.Point1 : this.Point2;
    }
}

class Point {
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

class GameObject {

    constructor(Points, scale, pos) {
        this.scale = scale ??  new Point(1,1);
        this.pos = pos ?? new Point(0,0);
        this.Points = Points ?? [];
        this.edges = this.createEdges(this.Points);
        this.velocity = new Point(0,0);
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
            const distanceA = edgeA.Point1.distance(new Point(x, y));
            const distanceB = edgeB.Point1.distance(new Point(x, y));
            return distanceA - distanceB;
        });
    
        // get closest
        const edge = sortedEdges[0];
        
        // get Points
        const closest = edge.getClosestPoint(x, y);
        const other = closest === edge.Point1 ? edge.Point2 : edge.Point1;
    
        // get ratio
        const distanceClosestToQuery = closest.distance(new Point(x, y));
        const distanceClosestToOther = closest.distance(other);
        this._cachedColorRatio = distanceClosestToQuery / (distanceClosestToQuery + distanceClosestToOther);
    
        return [closest, other];
    }
    getBlendedColor(Points, lerpFunction){
        const A = Points[0];
        const B = Points[1];
        const blended = lerpFunction(A, B, this._cachedColorRatio);
        this._cachedColorRatio = 0;
        return blended;
    }
    collides(x, y) {
        let isInside = false;

        for (let i = 0, j = this.edges.length - 1; i < this.edges.length; j = i++) {
            const edge = this.edges[i];
            
            const x1 = edge.Point1.x;
            const y1 = edge.Point1.y;
            const x2 = edge.Point2.x;
            const y2 = edge.Point2.y;

            if ((y1 > y) !== (y2 > y) && x < ((x2 - x1) * (y - y1)) / (y2 - y1) + x1)
            {
                isInside = !isInside;
            }
        }

        return isInside;
    }
    createEdges(Points) {
        const edges = [];

        for (let i = 0; i < Points.length; ++i) {
            const pt1 = Points[i];
            const pt2 = Points[(i + 1) % Points.length];
            edges.push(new Line(pt1, pt2));
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

        for (const Point of this.Points) {
            const x = Point.x - this.pos.x;
            const y = Point.y - this.pos.y;

            const rotatedX = x * cosAngle - y * sinAngle;
            const rotatedY = x * sinAngle + y * cosAngle;

            Point.x = rotatedX + this.pos.x;
            Point.y = rotatedY + this.pos.y;
        }
    }
}
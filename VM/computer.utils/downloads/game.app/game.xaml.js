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
            [255, 255, 0, 0],       // Red
            [255, 255, 128, 0],     // Orange
            [255, 255, 255, 0],     // Yellow
            [255, 128, 255, 0],     // Lime Green
            [255, 0, 255, 0],       // Green
            [255, 0, 255, 128],     // Spring Green
            [255, 0, 255, 255],     // Cyan
            [255, 0, 128, 255],     // Sky Blue
            [255, 0, 0, 255],       // Blue
            [255, 128, 0, 255],     // Purple
            [255, 255, 0, 255],     // Magenta
            [255, 255, 0, 128],     // Pink
            [255, 192, 192, 192],   // Light Gray
            [255, 128, 128, 128],   // Medium Gray
            [255, 64, 64, 64],      // Dark Gray
            [255, 0, 0, 0],         // Black
            [255, 255, 255, 255],   // White
            [255, 255, 69, 0],      // Red-Orange
            [255, 255, 215, 0],     // Gold
            [255, 0, 128, 0],       // Dark Green
            [255, 0, 128, 128],     // Teal
            [255, 0, 0, 128],       // Navy
            [255, 255, 20, 147],    // Deep Pink
            [255, 0, 250, 154]      // Medium Spring Green
        ];
    }
    getRender(){
        if (this.frameData.length == 0){
            return [[0,0,0,0]]
        }
        else return this.frameData;
    }
    //#region Rendering
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
        color.forEach(byte => {
            this.frameData[index] = byte;
            index++;
        });
        this.isDirty = true;
    }
    _drawScene(scene) {
        
        if (this.resizing) {
            this.clean(/*black background*/ this.palette[15]);
            this.resizing = false;
            return true;
        }
        if (this.isDirty)
        {

            const gameObjects = scene.gameObjects();

            // iterate through gameObjects
            for (let z = 0; z < gameObjects.length; ++z)
            {
                const gameObject = gameObjects[z];
                const pos = gameObject.pos;
                const size = new point()
                // draw sprite
                for (let _y = 0; _y < gameObject.size.x; _y++) 
                {
                    for (let _x = 0; _x < gameObject.size.h; _x++) 
                    {
                        
                    }
                }
            }
           
            if (poly.collides(x, y) === true) 
            {
                const points = poly.getClosestPoints(x, y);

                /* -- end user -- 
                    to easily manipulate polygon drawing, you can make a custom 'shader' 
                    by making a new function to pass in here, and modifying the polygon 
                    blend function to take in more parameters, such as screen space coords, 
                    texture coords, gameObject position, more colors, noise inputs, masks/maps , etc.
                */
                
                const blended = poly.getBlendedColor(points, this.lerpColors);
                this.writePixel(x, y, blended);
            } 
            else 
            {
                // draw background here
                const color = this.palette[14];
                this.writePixel(x, y, color);
            }

          
            
            this.isDirty = false;
            return true;
        }
        return false;
    }
}
class game {
    
    setupUIEvents() {
        app.eventHandler(this.__ID, 'this', '_render', XAML_EVENTS.RENDER);
        app.eventHandler(this.__ID, 'this', '_physics', XAML_EVENTS.PHYSICS);
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
        this.player.move()
        print(`velocity :: ${JSON.stringify(this.player.velocity)}`)
    }
    // simply uncomment this to get a physics loop
    //_physics(){}
    _render() {
        // returns a bool indicating whether anything was actually drawn or not
        if (this.renderer._drawScene(this.scene) === true){
            app.pushEvent(this.__ID, 'renderTarget', 'draw_pixels', this.renderer.getRender());

        }
    }
    _getSquare(size){
        const v1 = new point(0, 0, this.renderer.palette[0])
        const v2 = new point(0, size, this.renderer.palette[1])
        const v3 = new point(size, size, this.renderer.palette[3])
        const v4 = new point(size, 0, this.renderer.palette[2])
        const verts = [v1, v2, v3, v4];
        return verts;
    }
    constructor(id) {
        // for the engine.
        this.__ID = id;
        
        this.renderer = new renderer(36);
        
        // initialize the drawing surface
        this.renderer.clean(this.renderer.palette[13/*black*/]);

        
        this.moveSpeed = 0.01;
        
        // make a player object
        const verts = this._getSquare(16);
        const scale = new point(1, 1);
        const pos = new point(24, 24);
        this.player = new gameObject(verts, scale, pos);
        
        // make our scene
        this.scene = new scene([this.player]);
        

        // setup events (including render/physics loops)
        this.setupUIEvents();

        // start drawing
        this.renderer.isDirty = true;
        this.sized = false;

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
    add(otherPoint) {
        this.x += otherPoint.x;
        this.y += otherPoint.y;
        return this;
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
    subtract(x, y) {
        this.x -= x;
        this.y -= y;
        return this;
    }
    mult(scalar) {
        this.x *= scalar;
        this.y *= scalar;
        return this;
    }
    mult(x, y) {
        this.x *= x;
        this.y *= y;
        return this;
    }
    divide(scalar) {
        this.x /= scalar;
        this.y /= scalar;
        return this;
    }
    divide(x, y) {
        this.x /= x;
        this.y /= y;
        return this;
    }
    distance(other) {
        const dx = other.x - this.x;
        const dy = other.y - this.y;
        return Math.sqrt(dx * dx + dy * dy);
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
    set(point){
        this.x = point.x;
        this.y = point.y;
    }
    
}

class gameObject {
    constructor(points, size, pos) {
        this.size = size
        this.pos = pos ?? new point(0,0);
        this.points = this.applyScale(points, scale);
        this.edges = this.createEdges(this.points);
        this._cachedRatio = undefined;
        this.velocity = new point(0,0)
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
        this._cachedRatio = distanceClosestToQuery / (distanceClosestToQuery + distanceClosestToOther);
    
        return [closest, other];
    }
    getBlendedColor(points, lerpFunction){
        const A = points[0];
        const B = points[1];
        const blended = lerpFunction(A, B, this._cachedRatio);
        this._cachedRatio = 0;
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
    applyScale(points) {
        for (let i = 0; i < points.length; ++i) {
            const pt = points[i];
            points[i].x = pt.x * this.size.x;
            points[i].y = pt.y * this.size.y;
        }
        return points;
    }
    move() {

        this.pos.x += this.velocity.x;
        this.pos.y += this.velocity.y;
    
        for (const point of this.points) {
            point.x += this.velocity.x;
            point.y += this.velocity.y;
    
            point.x = Math.floor(point.x);
            point.y = Math.floor(point.y);
        }

        this.edges = this.createEdges(this.points)
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
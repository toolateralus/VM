
class renderer {
    
    constructor(resolution, gfxCtx) {
        // renderer data
        this.gfx_ctx = gfxCtx;

        if (this.gfx_ctx == undefined || this.gfx_ctx == null) {
            print('graphics context failed to initialize');
        }

        this.bytesPerPixel = 4;
        this.width = resolution;
        this.resizing = false;
        this.newWidth = this.width;
        this.isDirty = true;
        this.bgColor = this.packRGBA(this.palette[16]);

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
        const C = this.packRGBA(color);
        gfx.writePixel(this.gfx_ctx, Math.floor(x), Math.floor(y), C);
    }

    drawLine(line) {
        let steep = false;


        let x0 = line.point1.x;
        let x1 = line.point2.x;
        let y0 = line.point1.y;
        let y1 = line.point2.y;

        let c0 = line.point1.color;
        let c1 = line.point2.color;

        const distance = line.point1.sqrDist(line.point2);
        var color = c0;

        // steep
        if (Math.abs(x0 - x1) < Math.abs(y0 - y1)) {
            let t = x0;
            let t1 = x1;

            x0 = y0;
            x1 = y1;
            y0 = t;
            y1 = t1;

            steep = true;
        }

        // not steep
        if (x0 > x1) {
            let t = x0;
            let t1 = y0;

            x0 = x1;
            y0 = y1;
            x1 = t;
            y1 = t1;
        }

        let dx = x1 - x0;
        let dy = y1 - y0;
        let derror2 = Math.abs(dy) * 2;
        let error2 = 0;
        let y = y0;

        for (let x = x0; x <= x1; ++x) {

            if (steep) {
                const t = line.point1.sqrDistXY(y, x) / distance;
                this.writePixel(y, x, this.lerpColors(c0, c1, t));
            }
            else {
                const t = line.point1.sqrDistXY(x, y) / distance;
                this.writePixel(x, y, this.lerpColors(c0, c1, t));
            }

            error2 += derror2;

            if (error2 > dx) {
                y += (y1 > y0 ? 1 : -1);
                error2 -= dx * 2;
            }
        }
    }

    packRGBA(color) {
        var packedColor = (color[0] << 24) | (color[1] << 16) | (color[2] << 8) | color[3];
        return packedColor;
    }

    m_drawScene(scene) {

        gfx.clearColor(this.gfx_ctx, this.bgColor);

        const gameObjects = scene.gameObjects();

        // all objects in scene
        for (let z = 0; z < gameObjects.length; ++z) {
            const gameObject = gameObjects[z];
            const edges = gameObject.edges;

            edges.forEach(edge => {

                var p1 = new point(edge.point1.x * gameObject.scale.x, edge.point1.y * gameObject.scale.y, edge.point1.color);
                var p2 = new point(edge.point2.x * gameObject.scale.x, edge.point2.y * gameObject.scale.y, edge.point2.color);

                p1.addPt(gameObject.pos);
                p2.addPt(gameObject.pos);

                var ln = new line(p1, p2);
                this.drawLine(ln)
            });
        }
    }
}


class profiler {
    constructor() {
        this.segmentTime;
        this.endTime;
        this.markers = [];
        this.averages = [];
        this.stopwatch = new Stopwatch();
        this.segmentsCount = [];
    }
    start() {
        this.stopwatch.Start();
        this.segmentTime = 0;
    }
    set_marker(id) {
        const time = this.stopwatch.ElapsedTicks;
        const segmentDuration = time - this.segmentTime;
        this.markers[id] = segmentDuration;
        
        if (!this.segmentsCount.includes(id)) 
            this.segmentsCount[id] = 1;
        
        this.segmentsCount[id] += 1;

        if (!this.averages.includes(id)) 
            this.averages[id] = segmentDuration;

        const count = this.markers[id] < 1 ? this.markers[id] : 1;

        this.averages[id] = (this.averages[id] * count + segmentDuration) / (count + 1);

        this.segmentTime = time;
    }
    sample_immediate = () => this.markers;
    sample_average = () => this.averages;   
}

// THIS IS THE WPF CLASS! (it chooses the file name associated class.)
class elephantGame {

    setupUIEvents() {
        app.eventHandler(this.__ID, 'this', 'm_render', XAML_EVENTS.RENDER);
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
    }
    fpsCounterFrame(start) {
        if (start) 
        {
            this.captureBeginTime = new Date().getTime();
        } 
        else
        {
            const captureEndTime = new Date().getTime();
            const elapsed =captureEndTime - this.captureBeginTime;
            app.setProperty(this.__ID, 'framerateLabel', 'Content', `fps:${ Math.floor(1 / elapsed * 1000)}`);
            this.captureBeginTime = 0;
        }
    }
    m_render() {

        this.profiler.set_marker('other');
        this.frameCt++;
        
        // returns a bool indicating whether anything was actually drawn or not
        this.renderer.m_drawScene(this.scene, this.gfx_ctx);
        this.profiler.set_marker('rendering');
        
        gfx.flushCtx(this.renderer.gfx_ctx);
        this.profiler.set_marker('uploading');

        this.player.update_physics()
        this.player.confine_to_screen_space(this.renderer.width);
        
        this.profiler.set_marker('physics');
        this.fpsCounterFrame(false);
        this.fpsCounterFrame(true);
        
        if (this.frameCt % 30 == 0)
            this.drawProfile();
    }
    drawProfile() {
        const results = this.profiler.sample_average();

        const profilerWidth = app.getProperty(this.__ID, 'profilerPanel', 'ActualWidth') / 2;
        const fpsWidth = app.getProperty(this.__ID, 'framerateLabel', 'ActualWidth');

        const actualWidth = profilerWidth - fpsWidth;
        
        let totalTime = 0;

        for (const label in results) {
            totalTime += results[label];
        }

        const xFactor = actualWidth / totalTime;

        for (const label in results) {
            const time = results[label];
            app.setProperty(this.__ID, label, 'Content', `${time / 10_000} ms ${label}`);
            app.setProperty(this.__ID, label, 'Width', time * xFactor);
        }
    }
    _getSquare(size) {
        const v1 = new point(-0.5, -0.5, this.renderer.palette[0])
        const v2 = new point(-0.5, 0.5, this.renderer.palette[1])
        const v3 = new point(0.5, 0.5, this.renderer.palette[3])
        const v4 = new point(0.5, -0.5, this.renderer.palette[4])
        const verts = [v1, v2, v3, v4];
        return verts;
    }
    constructor(id) {

      
        // for the engine.
        this.__ID = id;
        this.frameCt = 0;

        const gfx_ctx = gfx.createCtx(this.__ID, 'renderTarget', 512, 512);
        // 64x64 render surface.
        this.renderer = new renderer(1024, gfx_ctx);

        // initialize the drawing surface
        // this.renderer.clean(this.renderer.palette[15]);

        this.moveSpeed = 1;

        // player init 
        // 4x4 (pixel) square sprite

        const verts = this._getSquare();

        // obj scale.
        const scale = new point(12, 12);

        // start position
        const pos = new point(24, 24);

        // make a player object
        this.player = new gameObject(verts, scale, pos);
        this.scene = new scene([this.player]);

        // setup events (including render/physics loops)
        this.setupUIEvents();

        this.profiler = new profiler();
        this.profiler.start();

        // start drawing, the renderer only draws when it's marked as dirty.
        this.renderer.isDirty = true;

    }
}

class scene {
    // array of all gameobjects in scene.
    gameObjects = () => this.gOs;
    constructor(gOs) {
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

    addPt(pt) {
        return this.add(pt.x, pt.y);
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
    sqrDistXY(x, y) {
        const dx = x - this.x;
        const dy = y - this.y;
        return dx * dx + dy * dy
    }
    sqrDist(pt) {
        return this.sqrDistXY(pt.x, pt.y);
    }
    distance(pt) {
        return Math.sqrt(this.sqrDist(pt));
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
    set(x, y) {
        this.x = x;
        this.y = y;
    }

}

class gameObject {

    constructor(points, scale, pos) {
        this.scale = scale ?? new point(1, 1);
        this.pos = pos ?? new point(0, 0);
        this.points = points ?? [];
        this.edges = this.createEdges(this.points);
        this.velocity = new point(0, 0);
        this._cachedColorRatio = undefined;
    }
    confine_to_screen_space(width) {
        const min_x = 1, min_y = 1, max_x = width - 1, max_y = width - 1;
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
    getBlendedColor(points, lerpFunction) {
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

            if ((y1 > y) !== (y2 > y) && x < ((x2 - x1) * (y - y1)) / (y2 - y1) + x1) {
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


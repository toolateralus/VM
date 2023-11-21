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
        return dx * dx + dy * dy;
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
class GameObject {

    constructor(Points, scale, pos) {
        this.scale = scale ?? new Point(1, 1);
        this.pos = pos ?? new Point(0, 0);
        this.Points = Points ?? [];
        this.edges = this.createEdges(this.Points);
        this.velocity = new Point(0, 0);
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
    getBlendedColor(Points, lerpFunction) {
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

            const x1 = edge.Point1.x * this.scale.x + this.pos.x;
            const y1 = edge.Point1.y * this.scale.y + this.pos.y;
            const x2 = edge.Point2.x * this.scale.x + this.pos.x;
            const y2 = edge.Point2.y * this.scale.y + this.pos.y;

            if ((y1 > y) !== (y2 > y) && x < ((x2 - x1) * (y - y1)) / (y2 - y1) + x1) {
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
class Scene {
    // array of all GameObjects in Scene.
    GameObjects = () => this.gOs;
    constructor(gOs) {
        this.gOs = gOs;
    }
}
class Renderer {

    constructor(resolution, gfxCtx) {
        // Renderer data
        this.gfx_ctx = gfxCtx;

        if (this.gfx_ctx == undefined || this.gfx_ctx == null) {
            print('graphics context failed to initialize');
        }

        this.bytesPerPixel = 4;
        this.width = resolution;
        this.resizing = false;
        this.newWidth = this.width;
        this.isDirty = true;

        this.bgColor = palette_indexed[Color.BLACK];
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
        gfx.writePixel(this.gfx_ctx, Math.floor(x), Math.floor(y), to_color(color));
    }

    drawLine(Line) {
        let steep = false;


        let x0 = Line.Point1.x;
        let x1 = Line.Point2.x;
        let y0 = Line.Point1.y;
        let y1 = Line.Point2.y;

        let c0 = Line.Point1.color;
        let c1 = Line.Point2.color;

        const distance = Line.Point1.sqrDist(Line.Point2);
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
                const t = Line.Point1.sqrDistXY(y, x) / distance;
                this.writePixel(y, x, this.lerpColors(c0, c1, t));
            }
            else {
                const t = Line.Point1.sqrDistXY(x, y) / distance;
                this.writePixel(x, y, this.lerpColors(c0, c1, t));
            }

            error2 += derror2;

            if (error2 > dx) {
                y += (y1 > y0 ? 1 : -1);
                error2 -= dx * 2;
            }
        }
    }

    m_drawScene(Scene) {

        gfx.clearColor(this.gfx_ctx, this.bgColor);

        const gameObjects = Scene.GameObjects();

        // all objects in Scene
        for (let z = 0; z < gameObjects.length; ++z) {
            const gO = gameObjects[z];
            const edges = gO.edges;

            edges.forEach(edge => {

                var p1 = new Point(edge.Point1.x * gO.scale.x, edge.Point1.y * gO.scale.y, edge.Point1.color);
                var p2 = new Point(edge.Point2.x * gO.scale.x, edge.Point2.y * gO.scale.y, edge.Point2.color);

                p1.addPt(gO.pos);
                p2.addPt(gO.pos);

                var ln = new Line(p1, p2);
                this.drawLine(ln);
            });
        }
    }
}
return {  
    Point : Point,
    Line : Line, 
    GameObject : GameObject,
    Scene : Scene,
    Renderer: Renderer,
};
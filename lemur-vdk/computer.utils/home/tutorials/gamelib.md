```Javascript
class Point {
    // you do NOT need to provide a color. only when using Point() for vertices with the provided line renderers, or your own vertex color / shading.
    constructor(x, y, color) {
        this.x = x;
        this.y = y;
        this.color = color;
    }
    
    // this class has many helper methods.
    // 'pt' or 'other' refers to another Point() instance.
    // x and y refer to numerical coordinates.

    // color is often not used

    getColor();
    
    addPt(pt);
    add(x, y);

    subtract(otherPoint);

    mult(scalar);
    divide(scalar);

    sqrDistXY(x, y);
    sqrDist(pt);

    distance(pt);
    dot(other);

    magnitude();
    normalize();
    set(x, y);
}
```

```Javascript
class GameObject {

    // points is a list of Point() objects representing the vertices of this GameObject.
    constructor(Points, scale, pos) {
        this.scale = scale ?? new Point(1, 1);
        this.pos = pos ?? new Point(0, 0);
        this.points = Points ?? [];
        this.edges = this.createEdges(this.points);
        this.velocity = new Point(0, 0);
        this._cachedColorRatio = undefined; // deprecated, do not use.
        this.rotation = 0;
        this.angular = 0;
        this.drag = 0.95;
    }

    // clamps the position of the game object between 0 and width, which should be in pixel space, ie the resolution of the renderer.
    confine_to_screen_space(width);

    // this function is not correct and should not be used, it is deprecated, but not yet removed.
    distanceToPoint(x1, y1, x2, y2);

    // returns the closest and second closest Point to Point(x,y);
    // in an array [closest, other];
    getClosestPoints(x, y)
    
    // blends [a, b] a two-element array Points, by their vertex colors using the provided interpolation function, and returns the blended value.
    getBlendedColor(Points, lerpFunction);

    // returns true if this gameObject's AABB (at position, of size scale) collides with Point(x, y)
    collides(x, y);

    // updates the 'edges' field with new lines based on the current vertex data.
    createEdges(Points);

    // applys an euler integrator to velocity and angular velocity,
    // and then drag.
    update_physics();

    // angle is a double in (degrees | radians)?
    rotate(angle);
}
```
this one's not quite neccesary, but is useful as the line renderer's use it as a unified way to accept a collection of GameObjects.
``` JavaScript
class Scene {
    // array of all GameObjects in Scene.
    GameObjects = () => this.gOs;
    constructor(gOs) {
        this.gOs = gOs;
    }
}
```

This renderer is mostly for drawing vertex based shapes in Scene objects, with vertex color.
```Javascript
class Renderer {
    constructor(resolution, gfxCtx) {
        // Renderer data
        this.gfx_ctx = gfxCtx;

        if (this.gfx_ctx == undefined || this.gfx_ctx == null) {
            print('graphics context failed to initialize');
            return;
        }

        this.bytesPerPixel = 4;
        this.width = resolution;
        this.resizing = false;
        this.newWidth = this.width;
        this.isDirty = true;

        this.bgColor = palette_indexed[Color.BLACK];
    }

    // calls for a resize, and sets a new resolution. do not directly set this.width.
    setWidth(width);

    // linear interpolation between two colors by T. [r,g,b,a]
    lerpColors(a, b, t);

    // wrapper to ease calling with unpacked color & unfloored double coordinates.
    writePixel(x, y, color) {
        gfx.writePixel(this.gfx_ctx, Math.floor(x), Math.floor(y), to_color(color));
    }

    // wrapper to ease calling with unfloored double coordinates.
    writePixelIndexed(x, y, index) {
        gfx.writePixelIndexed(this.gfx_ctx, Math.floor(x), Math.floor(y), index);
    }

    // draws a line with indexed color, used internally.
    // https://en.wikipedia.org/wiki/Bresenham's_line_algorithm
    // Bresenhams line drawing algorithm, adapted from stack overflow somewhere.
    drawLineIndexed(x0, x1, y0, y1, c0);
     
    // draws a line, used internally.
    drawLine(line);
    
    // draws the scene.
    m_drawScene(scene);

}
```
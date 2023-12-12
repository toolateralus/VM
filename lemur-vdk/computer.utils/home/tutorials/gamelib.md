```Javascript
class Vec2 {
    // you do NOT need to provide a color. only when using Vec2() for vertices with the provided line renderers, or your own vertex color / shading.
    constructor(x, y, color) {
        this.x = x;
        this.y = y;
        this.color = color;
    }
    
    // this class has many helper methods.
    // 'pt' or 'other' refers to another Vec2() instance.
    // x and y refer to numerical coordinates.

    // color is often not used

    getColor();
    
    addPt(pt);
    add(x, y);

    subtract(otherVec2);

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
class Node {

    constructor(scale, pos) {
        this.scale = scale ?? new Vec2(1, 1);
        this.pos = pos ?? new Vec2(0, 0);
        this.velocity = new Vec2(0, 0);

        this.rotation = 0;
        this.angular = 0;
        this.drag = 0.95;
    }

    // clamps the position of the game object between 0 and width, which should be in pixel space, ie the resolution of the renderer.
    clamp_position(min : Vec2, max : Vec2);

    update_physics(deltaTime : number);

    rotate(angle : number);
}
```
this one's not quite neccesary, but is useful as the line renderer's use it as a unified way to accept a collection of Nodes.
``` JavaScript
class Scene {
    // array of all Nodes in Scene.
    Nodes = () => this.nodes;
    constructor(nodes) {
        this.nodes = nodes;
    }
}
```

This renderer is mostly for drawing vertex based shapes in Scene objects, with vertex color.
```Javascript
class Renderer {
    constructor(resolution, GraphicsCtx) {
        // Renderer data
        this.gfx_ctx = GraphicsCtx;

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
        Graphics.writePixel(this.gfx_ctx, Math.floor(x), Math.floor(y), to_color(color));
    }

    // wrapper to ease calling with unfloored double coordinates.
    writePixelIndexed(x, y, index) {
        Graphics.writePixelIndexed(this.gfx_ctx, Math.floor(x), Math.floor(y), index);
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
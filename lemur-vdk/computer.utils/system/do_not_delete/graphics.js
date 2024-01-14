function to_color(color) {
    var packedColor = (color[0] << 24) | (color[1] << 16) | (color[2] << 8) | color[3];
    return packedColor;
}
function random_color() {
	let index = Math.floor(random() * palette.length);
	return index;
}
function create_square() {
    const v1 = new Vec2(-0.5, -0.5, Color.WHITE)
    const v2 = new Vec2(-0.5, 0.5, Color.WHITE)
    const v3 = new Vec2(0.5, 0.5, Color.WHITE)
    const v4 = new Vec2(0.5, -0.5, Color.WHITE)
    const verts = [v1, v2, v3, v4];
    return verts;
}
const Primitive = 
{
    Rectangle : 0,
    Triangle : 1,
    Circle : 2,
};
// should be in an 'app' module.
const Event = {
    MouseDown : 0,
    MouseUp : 1,
    MouseMove: 2,
    KeyDown : 3,
    KeyUp : 4,
    Loaded : 5,
    WindowClose : 6,
    Rendering : 7,
    MouseLeave: 9,
    SelectionChanged: 10,
};
const Color = {
    RED: 0,
    ORANGE: 1,
    YELLOW: 2,
    LIME_GREEN: 3,
    GREEN: 4,
    SPRING_GREEN: 5,
    CYAN: 6,
    SKY_BLUE: 7,
    BLUE: 8,
    PURPLE: 9,
    MAGENTA: 10,
    PINK: 11,
    LIGHT_GRAY: 12,
    MEDIUM_GRAY: 13,
    DARK_GRAY: 14,
    BLACK: 15,
    WHITE: 16,
    RED_ORANGE: 17,
    GOLD: 18,
    DARK_GREEN: 19,
    TEAL: 20,
    NAVY: 21,
    DEEP_PINK: 22,
    MEDIUM_SPRING_GREEN: 23
};
const palette_indexed = Object.values(Color).map(index => to_color(palette[index]));


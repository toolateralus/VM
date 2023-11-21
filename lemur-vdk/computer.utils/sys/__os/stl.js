//#region math
function clamp(min,max,value){
    return Math.min(max, Math.max(min, value))
}

function packRGBA(color) {
    var packedColor = (color[0] << 24) | (color[1] << 16) | (color[2] << 8) | color[3];
    return packedColor;
}

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

const palette = [
    [255, 255, 0, 0], // Red 0
    [255, 255, 128, 0], // Orange 1
    [255, 255, 255, 0], // Yellow 2
    [255, 128, 255, 0], // Lime Green 3
    [255, 0, 255, 0], // Green 4
    [255, 0, 255, 128], // Spring Green 5
    [255, 0, 255, 255], // Cyan 6
    [255, 0, 128, 255], // Sky Blue 7 
    [255, 0, 0, 255], // Blue 8
    [255, 128, 0, 255], // Purple 9 
    [255, 255, 0, 255], // Magenta 10
    [255, 255, 0, 128], // Pink 11
    [255, 192, 192, 192], // Light Gray 12
    [255, 128, 128, 128], // Medium Gray 13
    [255, 64, 64, 64], // Dark Gray 14
    [255, 0, 0, 0], // Black 15
    [255, 255, 255, 255], // White 16
    [255, 255, 69, 0], // Red-Orange 17
    [255, 255, 215, 0], // Gold 18
    [255, 0, 128, 0], // Dark Green 19
    [255, 0, 128, 128], // Teal 20
    [255, 0, 0, 128], // Navy 21
    [255, 255, 20, 147], // Deep Pink 22
    [255, 0, 250, 154] // Medium Spring Green 23
];
//#endregion math
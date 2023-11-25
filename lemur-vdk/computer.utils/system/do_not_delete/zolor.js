// indexed color enum.
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

// system wide basic color palette.
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

// indexed version of that array.
const palette_indexed = [
    to_color(palette[0]),  // Red 0
    to_color(palette[1]),  // Orange 1
    to_color(palette[2]),  // Yellow 2
    to_color(palette[3]),  // Lime Green 3
    to_color(palette[4]),  // Green 4
    to_color(palette[5]),  // Spring Green 5
    to_color(palette[6]),  // Cyan 6
    to_color(palette[7]),  // Sky Blue 7 
    to_color(palette[8]),  // Blue 8
    to_color(palette[9]),  // Purple 9 
    to_color(palette[10]), // Magenta 10
    to_color(palette[11]), // Pink 11
    to_color(palette[12]), // Light Gray 12
    to_color(palette[13]), // Medium Gray 13
    to_color(palette[14]), // Dark Gray 14
    to_color(palette[15]), // Black 15
    to_color(palette[16]), // White 16
    to_color(palette[17]), // Red-Orange 17
    to_color(palette[18]), // Gold 18
    to_color(palette[19]), // Dark Green 19
    to_color(palette[20]), // Teal 20
    to_color(palette[21]), // Navy 21
    to_color(palette[22]), // Deep Pink 22
    to_color(palette[23]) // Medium Spring Green 23
];

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

const palette_indexed = Object.values(Color).map(index => to_color(palette[index]));
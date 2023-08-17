function print(obj){
    interop.print(obj);
}
function alias(cmd, path){
    interop.alias(cmd, path)
}
function call(command) {
    interop.call(command);
}
function random(max = 1) {
    return interop.random(max);
}
function sleep(ms) {
    return interop.sleep();
}

function start(app) { interop.start(app) }

class OS {
    id = 0;

    constructor() {
        this.app_classes = [];
    }

    exit(code) {
        interop.exit(code);
    }

    computerID() {
        return this.id;
    }
}

const XAML_EVENTS = {
    MOUSE_DOWN: 0,
    MOUSE_UP: 1,
    MOUSE_MOVE: 2,
    KEY_DOWN: 3,
    KEY_UP: 4,
    LOADED: 5,
    WINDOW_CLOSE: 6,
    RENDER: 7,
    PHYSICS: 8,
};

function install(directory) {
    interop.install(directory)
}

class App {
    install(directory) {
        interop.install(directory)
    }
    eventHandler(id, control, method, type) {
        interop.eventHandler(id, control, method, type)
    }
    pushEvent(id, control, type, data) {
        interop.pushEvent(id, control, type, data);
    }
    getProperty(id, control, propertyName) {
        return interop.getProperty(id, control, propertyName);
    }
    setProperty(id, control, propertyName, value) {
        interop.setProperty(id, control, propertyName, value);
    }
}
let app = new App();

let os = new OS()
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
    return interop.sleep(ms);
}
function start(app) {
    interop.start(app)
}

// Todo: add a way to directly access the WPF Window's 'Resoure' Dictionary.'
// Would allow us to bypass the need to implement config readers/getters everywhere, at least for the UI.

function read() {
    return interop.read();
}
function install(directory) {
    interop.install(directory)
}
class OS {
    id = 0;

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
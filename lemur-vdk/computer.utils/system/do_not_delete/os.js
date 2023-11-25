// simple global 'system' functions. just easier.
function print(obj) {
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

// to be deprecated
function read() {
    return interop.read();
}

// to be deprecated
function install(directory) {
    interop.install(directory)
}

// to be deprecated
// pretty useless. many methods from interop can be moved into new types, such as OS, and we can 
// have a much better sense of organization.
class OS {
    id = 0;

    exit(code) {
        interop.exit(code);
    }

    computerID() {
        return this.id;
    }
}


// You cannot embed a typical enum and it's very easy to copy-paste them from C# to JS so we just do that.
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

// to be deprecated
// todo: remove this. this redundant and silly hack of wrappers came from a long time bug that's now fixed.
// each of these functions should be removed from `interop` and moved into their own type called App or something,
// then directly embedded.
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

const app = new App();
const os = new OS()
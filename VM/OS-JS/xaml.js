const XAML_EVENTS = {
    MOUSE_DOWN: 0,
    MOUSE_UP: 1,
    MOUSE_MOVE: 2,
    KEY_DOWN : 3,
    KEY_UP: 4,
    LOADED: 5,
    WINDOW_CLOSE: 6,
    RENDER: 7,
};
 
class App {
    install(directory)
    {
        interop.install(directory)
    }
    // window ID, target control ('this' for the app/window), target js method, event type (int/enum)
    eventHandler(id, control, method, type){
        interop.addEventHandler(id,control,method,type)
    }
    pushEvent(id, control, type, data){
        interop.pushEvent(id,control,type,data);
    }
}


let app = new App();
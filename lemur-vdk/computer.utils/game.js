

class Game {
    constructor(pid) {
        this.pid = pid;
        
        let g = App.getGLSurface(pid);

        this.g = g;

        const vertex = `
        
        `;

        const fragment = `
        
        `;

        const handle = g.compileShader(vertex, fragment);

        App.eventHandler('this', 'render', Event.Rendering);

        this.g.clearColor(0.0, 0.0, 0.0, 1.0);

        this.g.useShader(handle);
    }

    render() {
        this.g.drawRectangle(0, 0, 0.5, 0.5, Colors.RED);
    }



}
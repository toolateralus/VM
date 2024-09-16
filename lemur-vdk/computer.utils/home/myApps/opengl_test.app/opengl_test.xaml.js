class opengl_test {
	constructor(id) {
		this.frameCt = 0;
    	this.g = App.createGlSurface(id);
    	this.g.setDrawCallback(id, 'render');
    }
    render() {
    	this.frameCt++;
    	this.g.drawCube(0,0,-1, 0,this.frameCt % 360,0, 1,1,1);
    }
    
    
    
}
class opengl_test {
	constructor(id) {
		this.frameCt = 0;
    	this.g = App.createGlSurface(id);
    	this.g.setDrawCallback(id, 'render');
    }
    render() {
    	this.frameCt++;
    	const pos = new Vector3(0, 0, -1);
    	const rotation = new Vector3(0, this.frameCt % 360, 0);
    	const scale = new Vector3(1, 1, 1);
    	this.g.drawCube(pos, rotation, scale);
    }
    
    
    
}
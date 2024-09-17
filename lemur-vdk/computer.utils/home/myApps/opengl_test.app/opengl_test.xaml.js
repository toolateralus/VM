class opengl_test {
	constructor(id) {
		this.frameCt = 0;
    	this.g = App.createGLRenderer(id);
		this.g.setInitCallback(id, 'init');
    	this.g.setDrawCallback(id, 'render');

		this.mesh = new Mesh(canonical('cube.obj'));
		this.pid = id;
    }

	init() {
		let vSource = File.read('shader.vert');
    	let fSource = File.read('shader.frag');
		this.g.setShader(this.g.compileShader(vSource, fSource));
	}
    
    render() {
    	this.frameCt++;
    	this.g.uniformVec4("color", new Vector4(1.0, 0.5, 0, 1));
		
    	const pos = new Vector3(0, 0, -1);
    	const rotation = new Vector3(0, 0, 0);
    	const scale = new Vector3(1, 1, 1);

    	this.g.drawMesh(this.mesh, pos, rotation, scale);
    	this.g.uniformVec4("color", new Vector4(1.0, 0.5, 1.0, 1));
    	this.g.drawMesh(this.mesh, new Vector3(2,0,2), rotation, scale);
    }
}
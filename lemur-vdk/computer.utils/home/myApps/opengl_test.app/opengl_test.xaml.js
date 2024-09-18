class opengl_test {
	constructor(id) {
		this.frameCt = 0;
    	this.g = App.createGLRenderer(id);
		this.g.setInitCallback(id, 'init');
    	this.g.setDrawCallback(id, 'render');
		const mesh = new Mesh(canonical('cube.obj'));
    	const rotation = new Vector3(0, 0, 0);
    	const scale = new Vector3(1, 1, 1);
		this.meshes = [
			new MeshInfo(mesh, new Vector3(0, 0, -1), rotation, scale, new Vector4(1.0, 0.5, 0, 1)),
			new MeshInfo(mesh, new Vector3(2,0,2), rotation, scale, new Vector4(1.0, 0.5, 1.0, 1))
		];
		this.pid = id;
    }

	init() {
		let vSource = File.read('shader.vert');
    	let fSource = File.read('shader.frag');
		this.g.setShader(this.g.compileShader(vSource, fSource));
		this.texture = this.g.loadTexture(canonical('cubes.ico'));
		this.g.bindTexture(this.texture);
	}
    
    render() {
    	this.frameCt++;
    	const fwd = this.g.camera.Forward();
    	const speed = 0.1;
    	const rotSpeed = 2;
    	if (Key.isDown('W')) {
    		this.g.camera.Move(new Vector3(
	    		Convert.toFloat(fwd.X * speed),
	    		Convert.toFloat(fwd.Y * speed),
	    		Convert.toFloat(fwd.Z * speed)
	    	));
    	}
    	if (Key.isDown('S')) {
    		this.g.camera.Move(new Vector3(
	    		Convert.toFloat(fwd.X * -speed),
	    		Convert.toFloat(fwd.Y * -speed),
	    		Convert.toFloat(fwd.Z * -speed)
	    	));
    	}
    	if (Key.isDown('A')) {
    		this.g.camera.Rotate(new Vector3(
	    		0,
	    		Convert.toFloat(-rotSpeed),
	    		0
	    	));
    	}
    	if (Key.isDown('D')) {
    		this.g.camera.Rotate(new Vector3(
	    		0,
	    		Convert.toFloat(rotSpeed),
	    		0
	    	));
    	}
    	const rotX = Math.sin(Date.now() / 10000) * 0.01;
    	const rotZ = Math.cos(Date.now() / 10000) * 0.01;
    	for(const meshInfo of this.meshes) {
    		meshInfo.rotation.X += rotX;
    		meshInfo.rotation.Z += rotZ;
	    	this.g.uniformVec4("color", meshInfo.color);
	    	this.g.drawMesh(meshInfo.mesh, meshInfo.position, meshInfo.rotation, meshInfo.scale);
    	}
    }
}

class MeshInfo {
	constructor(mesh, position, rotation, scale, color) {
		this.mesh = mesh;
		this.position = position;
		this.rotation = rotation;
		this.scale = scale
		this.color = color;
    }
}
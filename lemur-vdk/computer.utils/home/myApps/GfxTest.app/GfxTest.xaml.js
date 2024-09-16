class GfxTest {
	constructor(pid) {
		this.resolution = 128;
		this.g = new GraphicsContext(pid, 'renderSurface', this.resolution, this.resolution);
		App.eventHandler('this', 'render', Event.Rendering);
		this.startTime = Date.now();
	}

	render() {
		let t = (Date.now() - this.startTime) / 1000;
		for (let y = 0; y < this.resolution; ++y) {
			for (let x = 0; x < this.resolution; ++x) {
				let dx = x - this.resolution / 2;
				let dy = y - this.resolution / 2;
				let distance = Math.sqrt(dx * dx + dy * dy);
				let angle = Math.atan2(dy, dx);
				let color = (Math.floor((Math.cos(angle + t) + Math.sin(distance / 10 - t)) * 16) % 16 + 16) % 16;
				this.g.writePixelIndexed(x, y, color);
			}
		}
		this.g.flush();
	}
}

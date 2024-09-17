class Mandelbrot {
	constructor(pid) {
		this.pid = pid;
		this.resolution = {
			x: 256,
			y: 256
		};
		this.scale = 3;
		this.pos = {
			x: -0.75,
			y: 0
		};
		this.mousePos = {
			x: 0,
			y: 0
		};
		this.dirty = true;
		this.g = new GraphicsContext(pid, 'RenderTarget', this.resolution.x, this.resolution.y);
		App.eventHandler('this', 'render', Event.Rendering);
		App.eventHandler('RenderTarget', 'onMouseDown', Event.MouseDown);
		App.eventHandler('RenderTarget', 'onMouseMove', Event.MouseMove);
	}
	onMouseMove(x, y) {
		const width = App.getProperty('RenderTarget', 'ActualWidth');
		const height = App.getProperty('RenderTarget', 'ActualHeight');
		this.mousePos = {
			x: x / width,
			y: y / height
		};
	}
	onMouseDown(left, right) {
		if (left || right) {
			this.pos = {
				x: (this.mousePos.x - 0.5) * this.scale + this.pos.x,
				y: (this.mousePos.y - 0.5) * this.scale + this.pos.y
			};
			this.dirty = true;
			if (left) {
				this.scale /= 1.5;
			} else {
				this.scale *= 1.5;
			}
		}
	}
	render() {
		if (!this.dirty) {
			return;
		}
		this.dirty = false;
		const tau = Math.PI * 2;
		for (let x = 0; x < this.resolution.x; x++) {
		    	for (let y = 0; y < this.resolution.y; y++) {
		    		const _x = (x / this.resolution.x - 0.5) * this.scale + this.pos.x;
		    		const _y = (y / this.resolution.y - 0.5) * this.scale + this.pos.y;
		    		const maxIters = 100 / this.scale;
		    		const iters = mandelbrot_itrs({r: _x, i: _y} ,maxIters);
		    		const t = iters / maxIters;
		    		const base = t * tau;
		    		const r = Math.floor((Math.sin(base) / 2 + 0.5) * 255 * t);
		    		const g = Math.floor((Math.sin(base + tau / 3) / 2 + 0.5) * 255 * t);
		    		const b = Math.floor((Math.sin(base + tau * 2 / 3) / 2 + 0.5) * 255 * t);
		    		try {
					this.g.writePixel(x, y, r, g, b, 255);
				} catch (e) {
					print({t: t, iters: iters}); 
				}
			}
		}
		this.g.flush();
	}
}
function mandelbrot_itrs(c, maxIters) {
	let z = {
		r: 0,
		i: 0
	}
	for (let iters = 1; iters < maxIters; iters++) {
		const r = z.r * z.r - z.i * z.i + c.r;
		const i = 2 * z.r * z.i + c.i;
		if (r * r + i * i >= 4) {
			return iters;
		}
		z.r = r;
		z.i = i;
	}
	return 0;
}
class Pong {
	constructor(pid) {
    	this.resolution = 64;
    	let g = new GraphicsContext(pid, 'RenderTarget', this.resolution, this.resolution);
    	this.g = g;
    	this.state = {
    		speed: 0.025,
    		ball: {
    			x:this.resolution/2,
    			y:this.resolution/2,
    			w:this.resolution/8,
    			h:this.resolution/8,
				v:{x:0,y:0}
    		},
    		p0: {
    			x:0,
    			y:0,
    			w:this.resolution/16,
    			h:this.resolution/12,
    		},
    		p1: {
    			x:this.resolution - this.resolution/16,
    			y:0,
    			w:this.resolution/16,
    			h:this.resolution/12,
    		}
    	}
    	this.resetBall(this.state.ball, -1);
    	App.eventHandler('this', 'render', Event.Rendering);
	}
	move() {
		this.state.ball.x += this.state.ball.v.x;
		this.state.ball.y += this.state.ball.v.y;
    	// second player
    	if (Key.isDown('I'))
    		this.state.p1.y -= this.state.speed;
    	if (Key.isDown('K'))
    		this.state.p1.y += this.state.speed;
    	// first player
    	if (Key.isDown('W'))
    		this.state.p0.y -= this.state.speed;
		if (Key.isDown('S'))
			this.state.p0.y += this.state.speed;

		if (Key.isDown('Space')) {
			this.state.ball.v = {x: this.state.speed, y: this.state.speed};
		}
	}
	draw() {
		let ball = this.state.ball;
    	this.g.drawRect(ball.x, ball.y, ball.w, ball.h, 0, 12);
    	let p0 = this.state.p0;
    	this.g.drawRect(p0.x, p0.y, p0.h, p0.w, 0, 13);
    	let p1 = this.state.p1;
    	this.g.drawRect(p1.x, p1.y, p1.h, p1.w, 0, 13);
    	this.g.flush();
	}
	collide() {
		let ball = this.state.ball;
		let paddles = [this.state.p0, this.state.p1];
		for (let paddle of paddles) {
			if (ball.x < paddle.x + paddle.w && ball.x + ball.w > paddle.x && ball.y < paddle.y + paddle.h && ball.y + ball.h > paddle.y) {
				let hitLocation = ((ball.x - paddle.x) / paddle.w) * 2 - 1;
				ball.v.x = -ball.v.x;
				ball.v.y += hitLocation * 0.01;
			}
		}
		
		ball.v.x = clamp(ball.v.x, -this.state.speed * 2, this.state.speed * 2);
		ball.v.y = clamp(ball.v.y, -this.state.speed * 2, this.state.speed * 2);

		// bounce off ceiling or floor.
		if (ball.y > this.resolution || ball.y <= 0)
			ball.v.y = -ball.v.y;

		// player 1 score.
		if (ball.x > this.resolution)
			this.resetBall(ball, 0);

		// player 2 score.
		if (ball.x <= 0)
			this.resetBall(ball, 1);
	}


	resetBall(ball, player_id) {
		ball.x = this.resolution / 2;
		ball.y = this.resolution / 2;

		if (player_id == -1)
			return;

		let content = App.getProperty(`p${player_id}Score`, 'Content');
		let score = parseInt(content.split(':')[1].trim()) + 1;
		App.setProperty(`p${player_id}Score`, 'Content', `player ${player_id} : ${score}`);
	}
    render() {
    	this.g.clearColor(16);
		this.move();
    	this.collide();
    	this.draw();
    }
}
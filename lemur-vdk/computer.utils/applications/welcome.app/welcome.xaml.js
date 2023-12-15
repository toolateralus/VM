class progressEntry {
	constructor (tut_id) {
		this.tutorial_id = tut_id;
		this.complete = false;
		this.begun = false;
		this.attempts = 0;
		this.special = [];
	}
}

class welcome {
    constructor() {
    	
    	// don't do it. don't edit the file D:
    	this.dataPath ='appdata/achievements/progress.json';
    	
		if (!File.read(this.dataPath)) {
			this.progress = new progressEntry();
			File.write(this.dataPath, JSON.stringify(this.progress, true));
		} 
		else {
			this.progress = JSON.parse(File.read(this.dataPath));
		}
		
    
    	this.doRendering = true;
    	App.eventHandler('tut01', 'tut01_click', Event.MouseDown);
    	App.eventHandler('tut02', 'tut02_click', Event.MouseDown);
    	App.setProperty('textBox', 'FontSize', 18);
    	App.eventHandler('this', 'on_close', Event.WindowClose);
	}
	
	on_close () {
		File.write(this.dataPath, JSON.stringify(this.progress, true));
	}
	tut01_click() {
		let pid = App.start('texed.app', 'hello-world.md', this.doRendering);
		App.close(pid);
	}
	tut02_click () {
		let pid = App.start('texed.app', 'game.md', this.doRendering);
		App.close(pid);
	}
}
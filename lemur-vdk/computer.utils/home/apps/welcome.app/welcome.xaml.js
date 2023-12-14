class welcome {
    constructor() {
    	
    	// don't do it. don't edit the file D:
    	const dataPath ='appdata/achievements/progress.json';
		if (!File.read(dataPath)) {
			this.progress = {
				t0_complete : false,
				t1_complete : false,
			}
			File.write(dataPath, JSON.stringify(this.progress, true));
		} else {
			this.progress = JSON.parse(File.read(dataPath));
		}
		
    
    	this.doRendering = true;
    	App.eventHandler('tut01', 'tut01_click', Event.MouseDown);
    	App.eventHandler('tut02', 'tut02_click', Event.MouseDown);
    	App.setProperty('textBox', 'FontSize', 18);
	}
	tut01_click() {
		let pid = App.start('texed', 'applications.md', this.doRendering);
		App.close(pid);
	}
	tut02_click () {
		let pid = App.start('texed', 'game.md', this.doRendering);
		
		App.close(pid);
		App.close(pid1);
	}
}
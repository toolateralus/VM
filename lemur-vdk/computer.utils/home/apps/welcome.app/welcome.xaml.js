class welcome {
    constructor() {
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
class welcome {
    constructor() {
    	this.doRender = true;
    	App.eventHandler('tut01', 'tut01_click', XAML_EVENTS.MOUSE_DOWN);
    	App.eventHandler('tut02', 'tut02_click', XAML_EVENTS.MOUSE_DOWN);
    	
    	App.setProperty('textBox', 'FontSize', 18);
    	
	}
	tut01_click() {
		let pid = App.start('texed', 'applications.md', this.doRender);
		App.close(pid);
	}
	tut02_click () {
		let pid = App.start('texed', 'game.md', this.doRender);
		let pid1 = App.start('texed', 'gamelib.md', this.doRender);
		
		App.close(pid);
		App.close(pid1);
	}
}
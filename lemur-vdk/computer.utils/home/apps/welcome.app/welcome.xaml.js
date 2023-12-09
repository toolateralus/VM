class welcome {
    constructor() {
    	this.doRender = true;
    	app.eventHandler('tut01', 'tut01_click', XAML_EVENTS.MOUSE_DOWN);
    	app.eventHandler('tut02', 'tut02_click', XAML_EVENTS.MOUSE_DOWN);
    	
    	app.setProperty('textBox', 'FontSize', 18);
    	
	}
	tut01_click() {
		let pid = app.start('texed', 'applications.md', this.doRender);
		app.close(pid);
	}
	tut02_click () {
		let pid = app.start('texed', 'game.md', this.doRender);
		let pid1 = app.start('texed', 'gamelib.md', this.doRender);
		
		app.close(pid);
		app.close(pid1);
	}
}
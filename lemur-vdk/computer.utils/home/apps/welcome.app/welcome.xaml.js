class welcome {
    constructor() {
    	this.child_processes = [];
    	this.doRender = true;
    	
    	app.eventHandler('tut01', 'tut01_click', XAML_EVENTS.MOUSE_DOWN);
    	app.eventHandler('tut02', 'tut02_click', XAML_EVENTS.MOUSE_DOWN);
    	app.eventHandler('dj', 'destroy_junk', XAML_EVENTS.MOUSE_DOWN);
	}
	destroy_junk() {
		this.child_processes.forEach(i => {
			app.close(i);
		});
	}
	tut01_click() {
		this.child_processes.push(app.start('TextEditor.app', 'applications.md', this.doRender));
	}
	tut02_click () {
		this.child_processes.push(app.start('TextEditor.app', 'game.md', this.doRender));
		this.child_processes.push(app.start('TextEditor.app', 'gamelib.md', this.doRender));
	}
}
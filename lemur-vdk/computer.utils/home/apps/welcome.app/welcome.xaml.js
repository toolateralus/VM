class welcome {
    constructor() {
    	app.eventHandler('tut01', 'tut01_click', XAML_EVENTS.MOUSE_DOWN);
	}
	tut01_click() {
		app.start('TextEditor.app', 'getting_started.md');
		sleep(2000);
		call("--kill-all 'TextEditor.app'");
	}
}
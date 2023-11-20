class terminal {
	constructor(id) {
		this.__ID = id;
		app.eventHandler(this.__ID, 'this', 'onKey', XAML_EVENTS.KEY_DOWN);
		
	}
	
	onKey(key) {
		print(key)
	}

	OS_MSG(message) {
		print(message);
		if (message.type === 'stdout') {
			 let body = app.getProperty(this.__ID, 'terminal_output', 'Content');
			 app.setProperty(this.__ID, 'terminal_output', 'Content', body + '\n' + message.body);
		}
	}
}


class calculator {
	constructor(id, ...args) {
		// id == this processes' process id
		// useful for graphics & system stuff.
		// ...args is a placeholder. all window constructors are varargs and can
		// be passed any arguments from App.start('appName', arg1, arg2, ...);
		// there is yet to be a cli tool to do this.
    	this.id = id;
    	this.expression = ''
    	
    	this.setup_events();	
    	
    }
    setup_events() {
    	for (let i = 0; i < 17; ++i) {
    		const name = `btn${i}`;
    		const fn_sig = `btn${i}_on_press`
    		const symbol = App.getProperty(name, 'Content');
			this[fn_sig] = () => { this.send_symbol(symbol); };
    		App.eventHandler(name, fn_sig, Event.MouseDown);
    	}
    	App.eventHandler('btnClear', 'clear', Event.MouseDown);
    	App.eventHandler('btnEq', 'solve', Event.MouseDown);
    }
    clear() {
    	this.dirty = true;
    	this.send_symbol('');
    }
    solve() {
    	this.expression = `${eval(this.expression)}`
		App.setProperty('output_tb', 'Text', this.expression);
		this.dirty = true;
    }
    send_symbol(s) {
    	if (this.dirty) {
    		this.expression = '';
    		this.dirty = false;
    	}
    	
    	if (this.isDigit(s) || s === '.' || s === '(' || s === ')') {
    		this.expression += s
    	} else {
    		this.expression += ` ${s} `
    	}
    	
    	
    	App.setProperty('output_tb', 'Text', `${this.expression}`);
    }
    isDigit(char) {
    	const code = char.charCodeAt(0);
    	return code >= 48 && code <= 57;
	}
}
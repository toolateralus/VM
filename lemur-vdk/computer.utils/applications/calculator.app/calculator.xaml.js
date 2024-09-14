class calculator {
	constructor() {
    	this.expression = ''
    	this.setup_events();
    	this.result = undefined
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
    	App.eventHandler('btnBackspace', 'backspace', Event.MouseDown);
    }
    clear() {
    	this.dirty = true;
    	this.send_symbol('');
    	this.result = undefined;
    }
    solve() {
    	this.result = eval(this.expression)
    	this.expression = `${this.result}`
		App.setProperty('output_tb', 'Text', this.expression);
		this.dirty = true;
    }
    backspace() {
    	this.expression = this.expression.slice(0, -1);
    	this.redraw();
    }
    send_symbol(s) {
    
    	if (this.dirty) {
    		if (this.result !== undefined) {
    			let fmt = s === '.' ? `${s}` : ` ${s} `;
	    		switch (s) {
	    			case '.':
	    			case '+': 
	    			case '-':
	    			case '*':
	    			case '/': {
	    				this.dirty = false;
		    			this.expression += ` ${s} `
		    			this.redraw();
	    			 	return;
	    			 }
	    			default:
	    				break;
	    		}
	    	}
    		this.expression = '';
    		this.dirty = false;
    	}
    	
    	if (this.isDigit(s) || s === '.' || s === '(' || s === ')') {
    		this.expression += s
    	} else {
    		this.expression += ` ${s} `
    	}
    	
    	this.redraw();
    	
    }
    redraw() {
    	App.setProperty('output_tb', 'Text', `${this.expression}`);
    }
    isDigit(char) {
    	return char >= '0' && char <= '9';
	}
}
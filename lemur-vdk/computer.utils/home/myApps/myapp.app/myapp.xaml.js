class myapp {
	constructor(id, ...args) {
		// id == this processes' process id
		// useful for graphics & system stuff.
		// ...args is a placeholder. all window constructors are varargs and can
		// be passed any arguments from App.start('appName', arg1, arg2, ...);
		// there is yet to be a cli tool to do this.
    	this.id = id;
    }
}
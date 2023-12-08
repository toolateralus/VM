class test_defer {
    constructor(id, args) {
        this.id = id;
        for (let i = 0; i < 2500; ++i) {
	        app.defer('printer', 2500, 100);
        }
    }
    printer(arg0) {
    	print(arg0);
    }
}
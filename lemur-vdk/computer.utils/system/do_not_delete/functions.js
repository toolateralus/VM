

// terminal -------------------------
function print(obj) {
    Terminal.print(obj);
}
function alias(cmd, path) {
    Terminal.alias(cmd, path)
}
function call(command) {
    Terminal.call(command);
}
function sleep(ms) {
    return Interop.sleep(ms);
}
function read() {
    return Terminal.read();
}

function clamp(min, max, value) {
    return Math.min(max, Math.max(min, value))
}
function to_color(color) {
    var packedColor = (color[0] << 24) | (color[1] << 16) | (color[2] << 8) | color[3];
    return packedColor;
}
function create_square() {
    const v1 = new Vec2(-0.5, -0.5, Color.WHITE)
    const v2 = new Vec2(-0.5, 0.5, Color.WHITE)
    const v3 = new Vec2(0.5, 0.5, Color.WHITE)
    const v4 = new Vec2(0.5, -0.5, Color.WHITE)
    const verts = [v1, v2, v3, v4];
    return verts;
}

// ------------------------- functions added to intrinsics

function get_set (controlName, propName, func) {
    if (typeof func !== 'function') {
        throw new Error('invalid get_set : you must pass in a function');
    }
    const value = App.getProperty(controlName, propName);
    const result = func(value);
    App.setProperty(controlName, propName, result);
}

JSON.tryParse = (msg) => {
	try {
		return  {
			hasValue:true,
			value:JSON.parse(msg),
		}
	}
	catch {
		return {
			hasValue:false,
			value:null,
		}
	}
};



// general -------------------------
function random(max = 1) {
    return Interop.random(max);
}
function describe(obj) {

    if (obj === undefined) {
        print('describe : cant print undefined')
        return;
    }

    if (obj === null) {
        print('describe : cant print null')
        return;
    }

    var string = "";

    for (const property in obj) {
        string += property + ": " + obj[property] + "\n";
        
        if (typeof property === 'object') {
        	print(`member ${property}`);
        	describe(obj);
    	}
        
    }

    print(string);
}
//-------------------------



// require -------------------------

function require(path) {
    const fn = new Function(File.read(path));
    const result = fn();
    return result;
}
// -------------------------
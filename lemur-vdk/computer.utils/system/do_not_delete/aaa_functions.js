// terminal -------------------------
function print(args) {
    Terminal.print(args);
}
function notify(obj) {
    Terminal.notify(obj);
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


// api -------------------------
function require(path) {
	if (File.exists(path) !== true) {
		throw new Error(' :: failed to include file ' + path);
	}
    const fn = new Function(File.read(path));
    const result = fn();
    return result;
}

// app / ui -------------------------
function get_set (controlName, propName, func) {
    if (typeof func !== 'function') {
        throw new Error('invalid get_set : you must pass in a function');
    }
    const value = App.getProperty(controlName, propName);
    const result = func(value);
    App.setProperty(controlName, propName, result);
}

// functions added to intrinsics -------------------------
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
function range(start, end, step = 1) {
    if (start === undefined || end === undefined) {
      throw new Error('Both start and end values must be provided.');
    }
  
    if (step === 0) {
      throw new Error('Step value cannot be zero.');
    }
  
    const result = [];
    
    if (step > 0) {
      for (let i = start; i < end; i += step) {
        result.push(i);
      }
    } else {
      for (let i = start; i > end; i += step) {
        result.push(i);
      }
    }
  
    return result;
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
function random_color() {
	let index = Math.floor(random() * palette.length);
	return index;
}
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
let __deferedFuncs = {
	index : 0
};
function __executeDeferredFunc(index) {
    let func = __deferedFuncs[index].func;
    let args = __deferedFuncs[index].args;
    func(...args);
    delete __deferedFuncs[index];
}
function defer(func, delay, ...args)
{
	if (typeof func != 'function') {
		throw new Error('defer: first arg must be function');
	}
	if (typeof delay != 'number') {
		throw new Error('defer: second arg must be number');
	}
	let index = __deferedFuncs.index++;
	__deferedFuncs[index] = {
		'func' : func,
		'args' : args
	};
	deferCached(Math.floor(delay), index);
}
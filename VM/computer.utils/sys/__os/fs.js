
// exposes basic file system functionality to the js env
class FS {
    read(path)
    {
        return interop.read_file(path);
    }
    write(path, data)
    {
        interop.write_file(path, data);
    }
    exists(path)
    {
        return interop.file_exists(path);
    }
    getentries(path) {
        return interop.getentries(path)
    }
}

class Image {
    // this returns a base64 encoded representation of file at path.
    // you can load non-image files with this, but for ease of use
    // we just have it as a file reader, since
    // our file events take Base64 encoded string images anyway. prevents confusion and unneccesary complication to the average user.
    fromFile(path){
        return interop.fromFile(path);
    }

    // These don't really seem to function right now. todo : fix these two functions.'
    toBytes(string) {
        return interop.toBytes(string);
    }

    toBase64(byteArray) {
        return interop.toBase64(byteArray);
    }

}

let IS_EXPORTING;
let __INCLUDES = [];
// runs the code in the env
function require(path){
    IS_EXPORTING = true;

    if (__INCLUDES.includes(path)) {
        return __INCLUDES[path];
    } else {
        const fn = new Function(file.read(path));
        const result = fn();
        return result;
    }
}
  
let image = new Image();
let file = new FS();
// exposes basic file system functionality to the js env
class FS {
    read(path)
    {
        return interop.read_file(path);
    }
    write(path, data)
    {
        interop.write_file(path);
    }
    exists(path)
    {
        return interop.file_exists(path);
    }
}

class Image {
    // this returns a base64 encoded representation of file at path.
    // you can load non-image files with this, but for ease of use
    // we just have it as a file reader, since
    // our file events take Base64 encoded string images anyway. prevents confusion and unneccesary complication to the average user.
    fromFile(path){
        return interop.imageFromFile(path);
    }
}

function _export(obj){
    interop.export(obj);
}

function require(path){
    return interop.require(path);
}
  
let image = new Image();
let file = new FS();
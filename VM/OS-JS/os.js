
function print(obj){
    interop.print(obj);
}

function _export(obj){
    interop.export(obj);
}

class os {
    id = 0;

    exit(code) {
        interop.exit(code);
    }

    computerID() {
        return this.id;
    }

}

let OS = new os();

print(OS.computerID);

_export(os);
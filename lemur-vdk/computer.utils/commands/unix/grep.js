
/* 
    this file was created as 'commands/grep.js'
        by the 'edit' command 
            at 1/11/2024 5:15:01 PM 
                by ??????
*/

const args = [/***/];
const flags = 'g';
const file_path = args[0];
const pattern = args[1];
const regexp = new RegExp(pattern, flags);
var file_contents;

try {
	file_contents = File.read(file_path)
} catch(err) {
	notify('grep failed : ' + err)
}

const lines = file_contents.split('\n');

lines.forEach (line => {
	const matches = line.match(regexp);
	
	if (matches == undefined || matches == null || matches.length == 0) {
		return;
	}
	
	print(line);
});

// todo: allow command line apps to return / pipe data.
// return matches;

/* 
    this file was created as 'commands/cat.js'
        by the 'edit' command 
            at 1/12/2024 4:52:41 PM 
                by ??????
*/

const args = [/***/];
var output; // string 

args.forEach(file_name => {
	if (!File.exists(file_name)) {
		throw new Error(`unable to find file ${file_name}`)
	}
	
	let contents = File.read(file_name);
	output += contents;
});

print(output)

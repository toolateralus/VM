
/* 
    this file was created as 'commands/check.js'
        by the 'edit' command 
            at 1/5/2024 6:00:19 PM 
                by ??????
*/
{

	const args = [/***/]
	
	if (args.length != 2) {
		print("you must provide two arguments to the check tool : file path, and tutorial index.")
	}
	
	let filepath = args[0]
	let tutorial_index = args[1]
	let file_contents = File.read(filepath);
	
	
	
}
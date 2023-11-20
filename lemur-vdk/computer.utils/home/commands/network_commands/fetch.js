{
	let args = [/***/];
	
	if (args.length === 0)
	{
		call('clear');
		network.check_for_downloadable_content();
	}
	else 
	{
		args.forEach(e => {
			const result = network.request('FILE_OR_DIR_EXISTS', 16);
		});
	}
	

}
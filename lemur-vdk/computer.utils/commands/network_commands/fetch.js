{
	let args = [/***/];
	
	if (args.length === 0)
	{
		call('clear');
		Network.check_for_downloadable_content();
	}
	else 
	{
		args.forEach(e => {
			const result = Network.request('FILE_OR_DIR_EXISTS', 16);
		});
	}
	

}
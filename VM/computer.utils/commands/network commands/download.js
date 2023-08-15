{

	let args = [/***/]
	let fileName = args[0]
	let install = args[1];
	if (typeof fileName === 'string'){
		
		if (!network.IsConnected){
			// try connect to last known ip.
			network.connect(null);
			
			if (!network.IsConnected){
				print('failed to connect, use the connect command to establish a connection to the server before you upload.');
			};
		}
		network.download(fileName);

		if (install === true){
			call(`install ${fileName}`)
		}
	} 
	else
	{
		print('invalid/non-string file name provided for download.')	
	}

}
{

	let args = [/***/]
	let fileName = args[0]
	
	if (typeof fileName === 'string'){
		
		print(`uploading file ${fileName}`);
		
		if (!network.IsConnected){
			// try connect to last known ip.
			network.connect(null);
			
			if (!network.IsConnected){
				print('failed to connect, use the connect/host command(s) to establish a connection to the server/host before you upload.');
			};
		}
		network.upload(fileName);
	} 
	else
	{
		print('invalid/non-string file name provided at arg 0')	
	}
}
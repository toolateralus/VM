{

	let args = [/***/]
	let fileName = args[0]
	
	if (typeof fileName === 'string'){
		
		print(`uploading file ${fileName}`);
		
		if (!Network.IsConnected){
			// try connect to last known ip.
			Network.connect(null);
			
			if (!Network.IsConnected){
				print('failed to connect, use the connect/host command(s) to establish a connection to the server/host before you upload.');
			};
		}
		Network.upload(fileName);
	} 
	else
	{
		print('invalid/non-string file name provided at arg 0')	
	}
}
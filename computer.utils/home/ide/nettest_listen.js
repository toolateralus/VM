{

	print('receiving on ch 0');

	sleep(1000);
	
	const response = network.listen(0);

	const packet = JSON.parse(response);
	
	let nested = conv.utf8FromBase64(packet.data);
	
	print(nested);
	
	let actualMsg = JSON.parse(nested);
	
	print(actualMsg);


}
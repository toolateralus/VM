
{
	print(`connected to server : ${network.IsConnected}`);
	if (network.IsConnected === false) {
		call('host');
		call('connect');
	}

	let message = "this is a message";
	
	print('sending on ch 0\nsignal to reply on 1 ch');

	network.send(0, 1, JSON.stringify(message));
	
	sleep(1000);
	print('sent');
	
	
}
{
	function get(toFile, channel) {
		if (typeof toFile !== 'string') {
			print('first arg must be string');
			return
		}
		if (typeof channel !== 'number' ||
			!Number.isInteger(channel)) {
			print('second arg must be integer');
			return
		}
		print('waiting for file...');
		file.write(toFile, network.recieve(channel));
		print(`file written to ${toFile}`);
	}
	let args = [/***/];
	get(args[0], args[1]);
}
{
    let args = [/***/]

    const ip = args[0]
	const data = file.read(ip)
	const ch = args[1]
	const reply = args[2]
	const replyLogPath = 'C:\Users\Josh\AppData\Roaming\VM\computer0\commands\log.js'
	network.connect(null)
	
	if (typeof data === 'string')
	{
		network.send(ch,reply,data);
		const replyMessage = network.recieve(reply);
		file.write(replyLogPath, replyMessage);
	}
}
function guess(max) {
	return Math.floor(Math.random() * max);
}

const number = guess(10000);

let guesses = 1


while (true) {
	const input = Terminal.read()
	
	let valid = true;
	for (let char of input) {
		if (char < '0' || char > '9') {
			print('bad input. try again.')
			valid = false;
			break;
		}
	}
	
	if (!valid) continue;
	
	const userGuess = parseInt(input)
	
	if (userGuess === number) { break; }
	let delta = number > userGuess ? 'low' : 'high';
	print(`too ${delta} try again`)
	guesses++
}

print(`correct. got it in ${guesses} tries.`)


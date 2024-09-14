function guess(max) {
	return Math.ceil(Math.random() * max);
}

class Bot {
    constructor(max) {
        this.min = 1;
        this.max = max;
        this.lastGuess = -1;
    }

    getGuess() {
        // Binary search approach
        this.lastGuess = Math.floor((this.min + this.max) / 2);
        return this.lastGuess;
    }

    informGuess(tooLow, opponentGuess, opponentWasTooLow) {
        if (tooLow) {
            this.min = this.lastGuess + 1; // Adjust the lower bound
        } else {
            this.max = this.lastGuess - 1; // Adjust the upper bound
        }
        
        if (opponentWasTooLow) {
            this.min = Math.max(this.min, opponentGuess + 1);
        } else {
            this.max = Math.min(this.max, opponentGuess - 1);
        }
    }
}




function main() {
	let userWon = false;
	
	function runGame(tries) {
	
		
		const max = 10000;
		const number = guess(max);	
		let guesses = 1	
		let bot = new Bot(max);
		
		Terminal.print(`against the bot... guess a number between 1 and ${max}.. prepare for battle!`)
		
		while (true) {
			const botGuess = bot.getGuess()
			const userGuess = parseInt(Terminal.read());
			
			if (botGuess === userGuess && botGuess === number) {
				print('draw')
				break;
			}
			
			if (botGuess === number) { 
				break; 
			}
			
			if (userGuess === number) { userWon = true; break; }
			
			let botDelta = number > botGuess;
			let delta = number > userGuess;
			
			print(`bot guessed: ${botGuess}. this was too ${botDelta ? 'low' : 'high'}`);
			print(`${userGuess} is too ${delta ? 'low' : 'high'}. try again`);
			
			bot.informGuess(botDelta, userGuess, delta);
			guesses++
		}
		return [number, guesses];
	
	}
	
	let [winningNumber, guesses] = runGame(0);
	
	print(`${userWon ? 'user won' : 'bot won'} with ${guesses} guesses. the number was ${winningNumber}`);
}

main();

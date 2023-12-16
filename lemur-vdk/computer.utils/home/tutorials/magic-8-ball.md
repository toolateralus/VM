### magic 8 ball :

first, create a terminal app template & open the JavaScript source.

then, before we implement this, let's consider some requirements:

*we need to read user input*

*we want to answer any question with a yes or no*

we might add some extras later but that's about as simple as it gets.

to do this, we only really need a few things :

our global `read()` function.

`print()` to prompt for user input.

and for the 'magic 8 ball' aspect, we can just use our `random()` global function to get a number from 0 to 1. 
we can use this to get a yes or no based on it's return value.

```JavaScript
//magic8ball.js
{
	print('ask a question:');
	const input = read();
	const randVal = random();
	const message = randVal > 0.5 ? "yes" : "no";
	// beginners : above is a ternary expression, equivalent to the following code.
	//	if (randValue > 0.5)
	//		return "yes"
	//	return "no";
	print(message);
}
```

now we can answer one question, but we want to take several questions until the user exits explictly, so we'll create a while loop to do this.
We'll also make a mechanism to break out of the infinite loop.

```JavaScript
//magic8ball.js
{
	while (true) {
		print('ask a question:');
		const input = read();
		
		if (input === 'exit') {
			break;
		}
		
		const randVal = random();
		const message = randVal > 0.5 ? "yes" : "no";
		// beginners : above is a ternary expression, equivalent to the following code.
		//	if (randValue > 0.5)
		//		return "yes"
		//	return "no";
		print(message);
	}
}
```

we want to answer questions in a somewhat relevant manner, and in an attempt to do this, we can return more than just a yes or no answer, based on some of the words used in the question.

```JavaScript
{
	// keys : buzzword list, category
	// values : possible answers for this category
	let bw = [
		'where':'nowhere|anywhere|there|here|that way|not this way',
		'will|can|should|could|wont' : 'yes|no',
		'which' : 'none|all of them|a variety|only a few',
	]
	
	while (true) {
		print('ask a question:');
		const input = read();
		
		let words = input.split(' ');
		
		for (let i = 0; i < words.length; ++i) {
			const word = words[i];
			
			for (let j = 0; j < bw.length; ++j) {
				let matches = bw[]
			}
			
		}
		
		if (input === 'exit') {
			break;
		}
		
		const randVal = random();
		const message = randVal > 0.5 ? "yes" : "no";
		// beginners : above is a ternary expression, equivalent to the following code.
		//	if (randValue > 0.5)
		//		return "yes"
		//	return "no";
		print(message);
	}
}
```






### Command Prompt Aliases and Scripts

---

Writing console commands can be helpful to make a non-gui task into a single command Line call


To define a console command, first you must make a .js command file, write a script to be executed when the command is called.
in your startup.js file, include an alias call that declares the identifier for the alias (command name) 
and the relative/absoluet path to the file, which is always valid as long as it's underneath the `computer[id]` directory.(id is the number preceding the computer in the file strucutre)
<details>
<summary> Basics/Tutorial </summary>

---

#### Hello world script

let's make a short example script to explore a few of the basic
ideas surrounding writing console commands for the OS in JavaScript.

```javascript 
//'helloworldcmd.js'

// our js engine doesn't manage the scope of variables when you run a script, and this means that variables defined with 'let' and 'const' will also be treated as global if this step isn't taken, so as to not create memory leaks/lack of garbage collection in the OS's main js engine, it's advised to just wrap the script in a body. this is due to the way we are using the engine, not the engine itself.

// this applies to the command Line and the interactive environment, which are both always running through the cmd prompts.

{
	print('hello world command:');
	print(`server connected : ${network.IsConnected}`)
}

```
Then, to have the command loaded up every time we start our computer, we can add it as an alias in `computer[id]/startup.js`

```javascript 
//'startup.js'
alias('helloWorld', 'helloWorld')
```


</details>

<details>
<summary>Getting Command Line Arguments</summary>

---

#### Getting Command Line Arguments
You can declare an array in js to recieve any command Line arguments for your script. so, 
if you declare an appropriate array as such : 

``` javascript
//Fetching command Line arguments 
	let args = [/***/]
```

You must include `/***/` in the array you want filled with arguments.

and the command is called with arguments like so 

``` javascript
-command arg1 0 'my Value'
```

your JS array will look like this

```javascript
 print(args[0]) // result : arg1
 print(args[1]) // result : 0
 print(args[2]) // result : "my Value"
```

note that the behavior surrounding strings vs identifiers into
the command Line is sketchy and somewhat unknown.

also, be aware that the commands that are written natively into
the os (the commands that have descriptions and help infos)
must follow C style data formats, such as double quoted strings `"mystring"` etc.

</details>

<details>
<summary>Improving your setup </summary>

---
### Improving the Setup
To enhance your console command system, consider dynamically
loading your commands.
``` javascript
interop.setAliasDirectory($path)
```
calling this function allows you to load all command files within a specified directory to avoid modifying the startup script for every new command.
```javascript startup.js
// in 'startup.js'
{
	const commandDirectory = "/path/to/commands";
	// note : this reloads the currently loaded 
	// aliases and only loads under this dir.

	interop.setAliasDirectory(commandDirectory);
	// By default, the method will just get the file name
	// as in ::
	// C//Users//MyPath.js
	// would result in
	// -> MyPath
	
	// optionally, you can provide regex to control the 
	// file naming convention. you MUST use C# Regex Format!!
	
	if (/*overriding the naming convention*/)
	{
		const regex = '^(.*?)_(.*?)_(.*?)\.js$';
		interop.setAliasDirectory(commandDirectory, regex)
	}
}
```
However, this just assumes your command file is named exactly what you want the alias to be represented by, and that's not always desirable, so if you prefer, you can load and name
your commands dynamically just using the function:
``` javascript
alias(cmdName, path/to/cmd.js)
```
---
</details>
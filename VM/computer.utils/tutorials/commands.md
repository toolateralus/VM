### Command Prompt Aliases and Scripts

---
To definemake a .js command file, write a script to be executed when the command is called.
in your startup.js file, include an alias call that declares the identifier for the alias (command name) 
and the relative path to the file, which is always 

---



#### Getting Command Line Arguments

You can declare an array in js to recieve any command line arguments for your script. so, 
if you declare an appropriate array as such : 

``` .js
//Fetching command line arguments 
	let args = [/***/]
```

You must include `/***/` in the array you want filled with arguments.

and the command is called with arguments like so 

```
-command arg1 0 "my Value"
```

your JS array will look like this

```
 print(args[0]) // result : 'arg1'
 print(args[1]) // result : 0
 print(args[2]) // result : "my Value"
```
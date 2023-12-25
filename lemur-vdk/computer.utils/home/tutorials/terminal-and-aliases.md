### terminal commands and aliases

### about commands.

you can install a 'program' which is referred to as a command, in which is accessible by an identifier in the terminal. for example, 'help', 'clear' etc.

we can create these programs by simply defining a `<command_name>.js` file

so far the only distinct feature command aliases get is command line arguments.
by declaring a field as such,
`foo = [/***/]` regardless of keyword, so

`let foo = [/***/]` or

`var foo = [/***/]` or 

`const foo = [/***/]`

are all valid.

then, when a command is invoked, the input is broken up into chunks at the spaces, and each chunk is considered one argument. 

arguments are always a string data type when coming into javascript, to prevent hacky args from executing any arbitrary code.


### creating a command

in this tutorial, we're going to create a command called `echo`, and it will just print any arguments to the terminal.


#### the javascript : 

create & open a new file called `cmd-echo.js`.

> (temporarily, just use vscode or the file explorer in windows to create a new single file. you can otherwise go into the lemur file explorer, right click, press new file, then use the copy command to rename it, then delete the old one.)

to open a text file, you can just double click in the file explorer.

now, our command is very simple. we will declare command line args, iterate over them and print each of them on a new line.

```{JavaScript
const args = [/***/];
for (let i = 0; i < args.length; ++i) {
	print(args[i]);
}
```

#### registering the new command

there are two easy ways to add a command to the runtime:

- including it in the `commands` directory, and it will be present next start-up.

- manually add the single alias. we can call `Terminal.alias(string identifier, string path)` to provide the identifer the command will be invoked by, and the path the command file is loaded from on invocation.

the first option is probably prefered, and you could even make a `commands/user/ directory to place yours in.



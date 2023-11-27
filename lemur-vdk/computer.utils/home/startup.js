// install OS commands.
// by default, the command will be the file name w/o the ext.
// you can pass in a CSharp format Regex (in the form of a string)
// with a matching pattern, and the result of that will be used as 
// the name & call id of the command
// such as
// myCommandFile.js
term.setAliasDirectory('commands');

/*leave this empty string arg to search whole pc and install any located apps.
     otherwise specify a target directory to recursively search for your apps*/

app.loadApps('');

if (config['welcome_read']) {
    app.uninstall('welcome.app');
} else {
    app.start('welcome.app');
}

// load commands from dir.
term.setAliasDirectory('commands');

// this needs the empty string. search from root
app.loadApps('');

// remove to stop welcome page.
app.start('welcome.app');

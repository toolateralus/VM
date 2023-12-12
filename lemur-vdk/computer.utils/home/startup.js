// load commands from dir.
Terminal.setAliasDirectory('commands');

// this needs the empty string. search from root
App.loadApps('');

// remove to stop welcome page.
App.start('welcome.app');

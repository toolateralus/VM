// load commands from dir.
Terminal.setAliasDirectory('commands');

App.loadApps('home/apps');

// remove to stop welcome page.
App.start('welcome.app');

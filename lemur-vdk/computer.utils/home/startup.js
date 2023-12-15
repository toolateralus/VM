// load commands from dir -> /commands.
Terminal.setAliasDirectory('commands');

App.loadApps('applications');
/*
:: install all applications 			:: 
:: *recursively* underneath 			:: 
:: the directory -> '/applications' 	::
*/

/*

:: feel free to change anything in this file :: 
:: this is the user startup file. 			::
*/

// :: startup the welcome app for new users. 	::
App.start('welcome.app');

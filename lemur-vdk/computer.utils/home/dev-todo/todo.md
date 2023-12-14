### see github issues for more info.
### this is not very well-maintained, and probably contains things that are already done.

#### app api

- make some api for creating & removing child buttons, labels, checkboxes.
	these few elements will cover a lot of ground and are very easy to add / remove

- maybe we could create a bunch of objects that mirror the xaml objects
so you can directly use identifiers and natural methods & properties instead of calling
app function with string arguments for control & property names.
however, this poses many threading issues and that's the only reason it's not in yet, maybe some of these new C# features like interceptors could help.

#### graphics api
- add some mechanisms for reading pixels in various ways, per pixel, reading radius, 

- getting regions of specific colors, etc. we want C# to do a lot of the heavy lifting for graphics. 

- fix the quartered performance of the graphics api, figure out why 'shapes.app'
	runs at 60fps where it ran at 3-500fps before. :: Done

#### windowing / os
	
- add a way to launch sub-process command prompts for running java script code in an
attached mode like way, so for example, the user can run commands that exist only in my app, 
or use the exposed api's to do complex things like write live scripts for a physics simulator etc.

#### paint.app

- record actions, undo
- store actual pixel data in [] not just drawn pixel data :: saving and loading can be unpredictable. :: DONE
- add some brushes, fill tool that checks edges, etc. :: STARTED ~ (BROKEN)
- save & load real image format, like bmp for desktop icons. :: STARTED ~ (BROKEN)

##### engine stuff : 

- fix the key binding issues, ctrl +c in paint should collapse the tooltray. ::DONE
- fix the fact that keybindings and eventHandler(Event.KeyDown) are ALWAYS global.
- fix the global keybindings not working when a window hasn't been the last clicked item, or a desktop button ie. needs to work always.

#### texed : 
- add more features to the preferences menu
- improve the style & clarity of UI, make it easier to use.
- fix the ctrl + s save feature being unreliable, you have to press it 1- 5 times for it to work.
- fix the sloppy attempt at making a file load / save popup using our file explorer, and stop using the windows file explorer for saving. improvements in the explorer should allow us to do this easily.

#### file explorer : 
- add more info about files, more columns of (sortable?) data.
- add searching / finding.
- improve the controls, context menus, and clarity of the UI.
- improve styling / appearance.
- improve the file system functionality : right now there's some unreliable and inconsistent parts.
- improve the security of the file system.

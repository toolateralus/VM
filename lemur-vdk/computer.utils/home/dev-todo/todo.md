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
	runs at 60fps where it ran at 3-500fps before.
	

#### windowing / os
	
- add a way to launch sub-process command prompts for running java script code in an
attached mode like way, so for example, the user can run commands that exist only in my app, 
or use the exposed api's to do complex things like write live scripts for a physics simulator etc.

#### paint.app

- record actions, undo
- store actual pixel data in [] not just drawn pixel data :: saving and loading can be unpredictable.
- add some brushes, fill tool that checks edges, etc.
- save & load real image format, like bmp for desktop icons.

##### engine stuff : 

- fix the key binding issues, ctrl +c in paint should collapse the tooltray.
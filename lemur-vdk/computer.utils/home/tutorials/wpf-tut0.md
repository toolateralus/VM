## getting started (gui)

*this tutorial assume you are at least somewhat familiar with XAML / WPF framework*

---

#### creating the project :

- Open up the `Wizard` app (green hat icon) and enter a name : we'll use `myGuiApp`.
- Select `GUI` as the `app type` & press `Create App`. You may close the `Wizard`.
- A new desktop icon should appear, double-clicking the icon should open a new window which just says 'hello' in the top left.


#### modifying the ui :

if you right click on the dekstop icon, you will find several options. one of them being `source -> xaml` : this is the file that declares the appearance of the gui application. 

open that up, and if you look down to the `Grid` and within it the `Label`, you'll see it's contents are just written in this document. we can instead control what this label says, or any property for that matter, from JavaScript. to do this though, we must have a `x:Name` property defined for any element we want to expose.
we can simply add a name like this:

```XAML
<Label x:Name="myLabel"/>
```

make sure you save by pressing the button, and then we can exit that file.


#### setting a property from JavaScript.

now that we've got a name to our element we want to modify, we can hop over to our JavaScript file, using the same context menu that we opened the XAML with.

you'll see a class that the wizard has generated that has the same name as your app. the constructor gets called when your app gets opened, it's much like a program.main() in c#, or a main function in other languages, except when control flow leaves it, the app does not close. we use this to hook into events & loops that will instead run our program over time.

the `App` object is provided to make calls to the ui for modifying, creating, destroying, and accessing elements.

we can use this, and the function within it called `App.setProperty(string controlName, string propertyName, object Value);` to set our label's contents, as such : 

```JavaScript
/* myGuiApp.xaml.js
... the class definition */
constructor(id) {
	this.id = id;
	App.setProperty('myLabel', 'Content', 'Goodbye'); // setting the value.
}
```

save that file, then double click your app's desktop icon. you should be able to see the changes you made to the ui.

#### getting a property from JavaScript.

in a similar fashion, we can get any value from a property in the XAML just by making calls to functions in the `App` intrinsic.

```JavaScript
const content = App.getProperty('myLabel', 'Content'); // getting our value.
notify(content); // to throw a gui notification in the bottom right of the desktop.
```

then when you run your app from the icon, you'll see a little notification in the bottom right that says 'Goodbye'.

#### events
right now these are the supported events for any given type of control from the JavaScript.

```
const Event = 
{
	MouseDown : 0,
	MouseUp : 1,
	MouseMove: 2,
	KeyDown : 3,
	KeyUp : 4,
	Loaded : 5,
	WindowClose : 6,
	Rendering : 7,
	MouseLeave: 9,
	SelectionChanged: 10,
}
```

in our case, we'll just create a new Button and get it's MouseDown, aka onClick event.

in the xaml: (to easily display these items we will hackily change the grid to a StackPanel just so they auto arrange. otherwise these elements would be on top of each other.)
```
<!-- ... the rest of the xaml -->
<StackPanel Orientation="Horizontal">
	<Label x:Name="myLabel"/>
	<Button x:Name="myButton" Content="Click Me."/> <!-- it must have some content or width to be visible. -->
<StackPanel/>
```

save & then head back over to the JavaScript : we can hook into the MouseDown event of that new control. However, we will need a callback function to handle when our event occurs. in _lemur_, currently all events must be member methods of the app's class. so in our case, we can just add a function called `myButtonClicked() {}` right underneath our `constructor()` method.

```JavaScript
construtor(id)
{
	// .. your constructor code
	
	// to hook into the event, call 
	App.eventHandler('myButton', 'myButtonClicked', Event.MouseDown)
}
myButtonClicked() {
	App.setProperty('myLabel', 'Content', 'You pressed it.'); // setting the value.
}

```
after saving everything, we can close those windows and double click the app icon.
when you click the button, you should see the label change.

*for fun*

you can add a number variable to your class that we'll use to tell which message to show, and flip it
on each click.

```JavaScript
construtor(id)
{
	this.clicked = false;
	App.eventHandler('myButton', 'myButtonClicked', Event.MouseDown)
}
myButtonClicked() {
	this.clicked = !this.clicked;
	App.setProperty('myLabel', 'Content', this.clicked ? 'hi' : 'bye'); 
}
```


#### adding elements dynamically from JavaScript.

if we wanted to make our button for example add a new label each time it's pressed, we can modify our code to do so.

first : we must have a container type that's capable of having 'children' elements. in WPF, there's many types that satify this condition, such as `Grid`, `StackPanel`, etc. for now, we'll just use our already existing stack panel at the root of our UI.

however to use this we need to name it, like the other elements.

in our xaml ::

``` XAML
<!-- the UserControl -->
<StackPanel x:Name="MainPanel">
	<!--  the rest of our code -->
</StackPanel>
```

save this, and hop over to the JavaScript file. we can use `App.addChild(string parentName, string childTypeString, string childName)` to add elements where we please, and `App.removeChild(string parent, string childName)` to remove them.

we'll use this in our MouseDown `myButtonClicked` method to just add a label each time the button's pressed.


```JavaScript
constructor (id) {
// your init code
this.clicked = false; 
this.pressedCount = 0;
}
myButtonClicked() {
	this.clicked = !this.clicked;
	App.setProperty('myLabel', 'Content', this.clicked ? 'hi' : 'bye'); 
	let name = 'label' + this.pressedCount++;
	// adding to main panel
				// adding type of Label (searches namespace System.Windows.Control.)
										// the name of the new control.
	App.addChild('MainPanel', 'Label', name);
	App.setProperty(name, 'Content', 'You pressed the button ' + this.pressedCount + ' times.');
}
```

if you save & run your app you should see the elements stacking downward each time you press the button.

note if you wanted to remove these lated you'd have to store the name to access during a `App.removeChild()` call, or programmatically calculate it.


*for fun*

we can use the `App.defer` function to queue a task with a specified timeout for asynchronous execution on a background thread. in other words, a fire and forget with an optional delay.

to use this we'll call 'defer' which requires a member function defined to callback, so let's define that in our class :

```
onRemove(elementName) {
	App.removeChild('MainPanel', elementName);
}
```

then in our onClick event callback, we can defer the removal of a child, the one we've just added, for a set amount of time.

```
onButtonClicked() {
	// your code...
	let name = 'label' + this.pressedCount++;
	// the rest of the code..
			// the method to call
				// the delay until execution
					// varargs any arguments to pass when calling function
	App.defer('onRemove', 1000, name);
}
```
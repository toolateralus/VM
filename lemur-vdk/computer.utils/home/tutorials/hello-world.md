## Hello World Application : 
> in this tutorial, we'll be creating a very simple application in _lemur_ which opens a terminal and prints the phrase `'hello world'`
    
### first, 
- Open _lemur vdk_, which, presumably you are viewing 
this from.

- Find the icon on the desktop labeled 'wizard', the one that has a picture of a wizard hat. this app helps us generate boiler-plate code that's otherwise slightly tedious to get going.

- Upon double clicking the 'wizard' app, you'll be greeted with a window containing a text box, an okay button, and a drop-down.

- In the text box, enter the name of your app, such as 'helloWorld'. 

> you do not need to include the '.app' extension.

### next,
- Select the combo box / 'drop down', and choose 'terminal' from the list. we'll cover GUI apps in the next tutorial.
- Press the 'Create App' button and you'll notice your app pops up on the desktop with it's own icon, albeit transparent. 

- You're done creating your application, but now we want to edit it's code. to find your newly created application, you may navigate to `home/myApps/<your app's name>`, or, for easier usage, you may right click your desktop icon for various ways to access the code. 
- select 'open containing folder' and then double click the .js file, or just directly select 'view source (js)' to open the main code file.

- Now, we're ready to move on to the next step: printing `hello world`.
  
### finally
- once your `<app name>.js` file is open, we can simply just write any JavaScript we want to execute. for more advanced usage, like command line arguments, we'll cover in another tutorial. for now, we'll just be placing one line in here.
- `print('hello world');`

- to save, you may press (ctrl + s) , which can be unreliable currently so it may need a few presses, or you can just press the save button at the top right.

- after saving, you may press F5, the run button, or just double click your desktop icon to run your app. you should see a terminal open and print 'hello world'!

### for achievement points / scoring
- to submit your progress to the tracker, you can run the terminal command
- `check <app name>.js 0` to check your validate `<app name>.js` as passing or failing tutorial 0. this will be a common theme, at least in the early development of the achievement system.

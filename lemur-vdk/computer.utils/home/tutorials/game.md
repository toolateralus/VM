
# making a game

let's cover how we can setup a very simple software rendering setup.

## Application boiler plate

first, we will use the app wizard to create the boiler plate for opening a window etc.

1. double click the desktop icon which says 'wizard'
1. type a name into the text bar. we'll use 'game'
1. click the create button.

You'll see an icon appear on the desktop. You can right click and select
'view source (.js)' to open the newly generated code.
> You need the 'id' field in the constructor to use graphics contexts. it is included by default.

## Including some helpful tools

we have a (somewhat) node-like include/require system, and we will use that to heavily simplify the process of setting up our scene & renderer

> note that, in this tutorial, we will be using as much native rendering as possible for the best performance, meaning, we won't be controlling each and every pixel in javascript. however, there are many ways to render pixels, images, or shapes in lemur, and it's rather flexible.

here's the include section of our document, 'game.xaml.js'

for our tutorial, these are mandatory :

```javascript
const {
    Point,
    GameObject,
    Scene,
} = require('game.js');
```

and optionally, we can include the profiler to get some rough latency stats

```javascript
const { Profiler } = require('profiler.js');
```

you may require any .js file underneath computer/home, see (nyi) tutorial, or check out the system/include files to see how exports are handled from source files.

for now, we don't need to be super concerned with those types. instead, we can now focus on actually setting up our graphics context, then we can begin creating a scene and rendering it.

## Graphics Module & Context

the `graphics module` is a permanently embedded type in all the JavaScript environments, accessible through the identifier `gfx`. this type has various methods to aid in creating `contexts`, drawing to them, clearing, etc. it's basically a tiny graphics library.

a `graphics context` is a drawing surface provided by the `graphics module`. it is not accessible directly to the javascript environment, instead, we call to the `graphics module` to create one. it returns an ID, referred to often as `this.gfx_ctx`, which is neccesary for performing most graphics related functions, if not all.

> `ctx` is often used as an abbreviation for context. it's to keep the scripts simple since I often write them in the Text Editor in Lemur.

so our first step is to call to the `gfx` module, and create a context.
we can simply call `gfx.createCtx()`, but to provide valid parameters, we will have to have an existing `Image` in our WPF layout. we'll call our's `renderTarget`.

```XAML
<!-- in your 'game.xaml' file, within the control grid... -->
<Grid>
    <Image
            Margin="5"
            x:Name="renderTarget"
            HorizontalAlignment="Stretch"
            VerticalAlignment="Stretch"
            Stretch="Fill"
            RenderOptions.BitmapScalingMode="NearestNeighbor"
    />
</Grid>
```

then, back in the JavaScript, we can actually register our context.
for the purpose of this simplified tutorial, we will just used a fixed resolution.

Also, while we're at it, we're going to add a `app.eventHandler` for our render loop. this is covered in another tutorial.

so, in our constructor  we will add :
---
1. `this.resolution = new Point(256, 256);`
1. `this.gfx_ctx = gfx.createCtx(id, 'renderTarget', this.resolution.x, this.resolution.y);`
3. `app.eventHandler('this', 'm_render', XAML_EVENTS.RENDER);`
 ---
    this.resolution = new Point(256, 256);
this will be the pixel width & height of our drawing context.
the image, as shown above, will automatically scale to the size of the window.

    this.gfx_ctx = gfx.createCtx(id, 'renderTarget', this.resolution.x, this.resolution.y);
this call creates our context, attaches our drawing surface to the control at specified size, and returns us a handle we can use for many graphics functions.
 `id` is our process id
 `renderTarget` is the wpf image we wil draw to
 and we pass our resolution in as `width, height`.

    app.eventHandler('this', 'm_render', XAML_EVENTS.RENDER);
`this` is passed in as a special keyword, but it would otherwise be a wpf control. `this`, when passed as a control, references the window itself.
 `m_render` is the name I've given to our render loop callback function, but this could be anything.
 this function will get called as frequently as the environment allows, any framerate limiting or sleeping can be done in javascript.

> this is not usually necessary though as we're typically struggling for performance, not limiting framerate. this is not surprising, though, since we're software rendering in two very high level languages.
> we are working on an OpenGL hardware accelerated renderer, which will boost performance and possibilities _very much_. it's really low priority though.


```Javascript
constructor(id){
    
    this.resolution = new Point(256, 256);

    this.gfx_ctx = gfx.createCtx(id, 'renderTarget', 
                        this.resolution.x, this.resolution.y);

    // create a render loop function that will get called 
    // as frequently as possible, in a loop. this persists until the app closes.
    // make sure the m_render function is defined.
    app.eventHandler('this', 'm_render', XAML_EVENTS.RENDER);
}

// the render loop function.
m_render() {

}

```

Now that we have our context, it's time to start drawing. But before we do that, we need to decide what we're going to draw. This involves creating a scene and designing some objects to populate it.

This is the reason we `require()`'d GameObject, Point (a 2D vector), and Scene, which serves as a container.

---
#### Very reccomended : 
take a look at the types in the `gamelib.md` markdown tutorial, which can be found in the same directory as this one. it shows each type and all of it's members and functions, as well as descriptions of parameters and usage.

---

So, in our constructor, we're going to add a few things.
```Javascript
constructor (id) {
    //.. init gfx context, etc.

    // start new code : 

    const gos = [];
    // let's make a couple gameObjects to just statically draw
    for (let i = 0; i < 5; ++i) {
        const scale = new Point(5,5)
        const pos = new Point(i, this.resolution.y - scale.y);
        // we can just pass in an empty array for vertices
        // won't be using them.
        // for a long time, it was not optional.
        const gO = new GameObject([], scale, pos);

        // we are going to add some extra fields
        // since we arent using vertex based rendering.

        // an index between 0 and 24
        // you can iterate over the 'palette' object
        // that's auto included in every context.
        // there is also a Color enum, with members like
        // Color.WHITE;
        gO.colorIndex = i;

        // this is a 0-3 index, check the enum.
        gO.primitiveIndex = Primitive.Triangle;

        // we'll use this as a quick way to validate 
        // we're working on our special game object.
        gO.isMesh = true;

        // add it to our scene
        gos.push(gO);
    }
    this.scene = new Scene(gos);
}
```

then in our `m_render` member function, we can add the neccesary code to render & draw our scene.. which is not much.
```Javascript

m_render () {

// clear the screen to black every frame, so we don't get trails and smudge.
gfx.clearColor(this.gfx_ctx, Color.BLACK);

// get our game objects.
const gOs = this.scene.GameObjects();

gOs.forEach(gO => {

    // if we found our special object
    if (gO.isMesh === true) {
        const x = gO.pos.x;
        const y = gO.pos.y;

        const width = gO.scale.x;
        const height = gO.scale.y;

        const rotation = gO.rotation;

        const primitive = gO.primitiveIndex;
        const color = gO.colorIndex;

        // these arguments are pretty self explanatory
        // I've attempted to make the code as expressive as possible.

        // note this doesn't actually show up on screen when we just call draw, it just organizes the data.
        gfx.drawFilledShape(this.gfx_ctx, 
                            x, y,
                            width, height,
                            rotation,
                            color, primitive);
    }
});

// this is the call that will take the drawn data and flush the 'frame buffer'
// in other words, it copies the image that you've drawn to the wpf control.
gfx.flushCtx(this.gfx_ctx);

}

```

now, if you've followed correctly, you should be able to save up your xaml & .xaml.js file, and run your app. You should see 5 pretty tiny triangles or rectangles (whatever primitive you used) in the bottom left. Epic!

Now, you can just add whatever you want during your render loop to create your game logic.

### Profiling

as for attaching the profiler we `require()`'d initially, it's pretty simple to setup and use.

there is quite a bit of overhead in xaml, so I will just provide the xaml that 
was used while it was originally designed

> Note: this replaces the entire \<Grid> in the UserControl, at least that's how it's intended.

```XAML
<Grid>
            <Grid.RowDefinitions>
                  <RowDefinition Height="*" />
                  <RowDefinition Height="35" />
            </Grid.RowDefinitions>

            <StackPanel Style="{StaticResource StackPanelStyle}" Grid.Row="1" x:Name="ProfilerPanel" Orientation="Horizontal">      
                  <Label Style="{StaticResource LabelStyle}" x:Name="framerateLabel" FontFamily="Consolas MS Bold" FontSize="20"
                        Foreground="Cyan" Background="#474354" Content="fps"/>
                        
                  <Label Style="{StaticResource LabelStyle}" x:Name="rendering" FontFamily="Consolas MS Bold" FontSize="20"
                        Foreground="Black" Background="DarkCyan" Content="rendering"/>

                  <Label Style="{StaticResource LabelStyle}" x:Name="uploading" FontFamily="Consolas MS Bold" FontSize="20"
                        Foreground="Cyan" Background="Green" Content="upload"/>

                  <Label Style="{StaticResource LabelStyle}" x:Name="collision" FontFamily="Consolas MS Bold" FontSize="20"
                        Foreground="Cyan" Background="Violet" Content="collision"/>

                  <Label Style="{StaticResource LabelStyle}" x:Name="other" FontFamily="Consolas MS Bold" FontSize="20"
                        Foreground="Cyan" Background="Black" Content="other"/>
            </StackPanel>

            <Image Grid.Row="0"
                  Margin="5"
                  x:Name="renderTarget"
                  HorizontalAlignment="Stretch"
                  VerticalAlignment="Stretch"
                  Stretch="Fill"
                  RenderOptions.BitmapScalingMode="NearestNeighbor"
            />
      </Grid>
```

This just creates a `renderTarget` on the main grid, and has a sub-section below for showing latency stats using colored & named regions that change size and report latency.

in the javascript, we also need to take a few steps to support this, mainly adjusting the texts / sizes of elements, and actually recording the latency stats.
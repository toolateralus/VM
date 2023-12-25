
#### anything in this directory is automatically loaded into / run in every new JavaScript engine / context.

- So, even if you modify this / add things, just simply opening a new app will have the changes in it.

- these files are loaded in alphabetical order, hence the strange naming modifications.
- the 'aaa_functions' file MUST load first in 90% of cases, as it contains a lot of system level code that's probably being used in anything else

> if you're looking to add a function/class library you can 'require' into your other apps, use the 'include' directory.
> this is the source of all requires. note: using a relatively unique name is important, my example of game.js is eagerly welcoming naming conflicts, as all files are loaded by name.

## Intro to programming in Lemur : 
#### _In this tutorial we're going to :_

- learn about the file structure of a _Lemur application_ 

- cover the types of executables aka `jittables` allowed in _Lemur_

### Setting up external development tools : 
###### See the intro to lemur tutorial to learn more about this process.

---
## about the Javascript Environment
> note that working in _Lemur_ is much like working in a free-standing environment, for kernel or embedded development. Meaning, you likely do not have access to many common, seemingly base-line language features. such as typical imports and exports, meaning if you need third party software, it's likely you'll need to either copy paste it into a document and 'require' it (our node-like module system), or just write it yourself.

---
---

# Apps in Lemur
- There are three kinds of user-space application you can create in _Lemur_. You should be aware of the options even though there is a HEAVILY suggested option.
---
### <u>The reccomended option</u>
#### *JS / WPF Lemur Native app.*
- This is the type of app the project was created to run !
- It features a free-standing javascript environment that we've implemented a bunch of WPF hooks and functionality into, so you can create WPF apps with java script as your backend 100% of the way.
> This has its limitations and resistances, but it's our main goal to make this development process super intuitive, performant, and fun to use. for the beginner, intermediate, or expert.
---
#### *JS / HTML Web app run in a browser container*
- I don't know too much about web dev- so I don't have much to say here. the browser container is a modern browser, Microsoft Edge, and should be fully featured. There are some sample apps chat gpt wrote
for this setup. 
- libraries, connectivity and security are things I know nothing about here.
- I added this originally for wider support but it's essentially a standalone browser and maintains it's own javascript context.
- This may very well be removed.

####  *C# / WPF app compiled into the _Lemur_ source code*

- Please avoid using this app at all costs if you intend to make a featured app for the repository.
It may be the most powerful option, but that's just because it bypasses the whole point of the project.
It's just untethered WPF at that point.
- This app has the most flexibility as in it can link with many
external libraries and code bases since it's not limited to a free standing environment. 
- This app performs generally better than the other kinds due to it being compiled in the native language of the project.

###### I plan to set up some kind of either PInvoke/LibaryImport or C++ interpreter interface to enable applications being written in C and C++ as well. It will use all the same bindings as javascript.
---
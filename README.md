# KIKI THE NANO BOT
Rewritten in Unity Dots.

## Thanks and Credits
"kiki the nanobot" was originally developed by monsterkodi. You can read about and download the original game here: https://kiki.sourceforge.net/
This version uses levels and assets from the original game. You can read the full list of thanks here: https://kiki.sourceforge.net/misc/thanks.html

This rewrite was written entirely by ITR, but with help and advice from the Unity Discord (especially Tertle).

## About the Project
This was written over the span of 50 hours for the [TurboMakesGames' DOTS JAM 1](https://itch.io/jam/tmg-dots-1/rate/2833431). Most the development was streamed on youtube and [can be watched here](https://www.youtube.com/playlist?list=PL7iZ5hVutYl0Vd7BLwD2Zsp2psnRzH_hA).  
The primary goal was to test my skills as well as showcase some techniques so other people can learn from it. Since I had limited time there's some parts of the original game I didn't implement, which I will list below:

- Menu, UI, etc.
- Bombs and infected bots.
- Slippery Blocks
- Most levels
- Wires automatically drawing connection wires on their sides
- Wire Cubes connecting diagonally
- Wires only connecting to wire cubes that don't occupy the same space if the wire cube is there when the game starts.
- However the graphics works- it was difficult enough converting OpenGL calls to meshes, I have no idea how to make Unity render the colors and transparency the same.
- A level loader- The original game uses python to make it easy for anyone to create and edit levels. I instead had each level be set up as a SubScene, which had some advantages, but is less accessible for the end user.
- Switches being allowed to freely hook up to any functionality instead of just activating the goal when all are active. (None of the levels I saw ever used this extended functionality for any gameplay)

## How to run
The game can be downloaded from the [itch.io page](https://itr.itch.io/kiki), which also has instructions on how to play the game.
You can also download the project and open it with Unity 6000.0.9f1 then run it from the "GameScene".

## Technology and Techniques
The project is written almost entirely with entities, the two exceptions being the camera that's spawned from an entity, and the UI that isn't spawned from an entity. The project barely uses any jobs due to having a low amount of entities, and instead uses DOTS and ECS as a way to structure the project and make it more modular. Almost all code is burst compiled.
Normally I would have split the scripts into multiple assemblies, but due to the small scope I decided against dealing with that here.  

### Rendering, Input, and Update
Recently I've been experiencing a lot with [OnDemandRendering](https://docs.unity3d.com/ScriptReference/Rendering.OnDemandRendering.html). This essentially allows the game to have 3 separate "updates":
- FixedUpdate: Runs at a fixed rate 
- Update: Runs as fast as possible (kinda)
- RenderUpdate: Runs whenever the screen needs to render (vsync)

I use this to run the physics of the game at 200FPS without the need of rendering at 200FPS. It's possible I could push the FPS even higher if I did some changes to how I calculate physics (read more about this in Knots & Keyframes), but for now I'm satisfied with 200.  
For other games it can be used to let people have a higher amount of control even at lower framerates. People are good at learning feel, so even if the visual feedback isn't there immediately it might be beneficial. Especially rhythm games might draw use of this.

There are a few issues however: 
- OnDemandRendering forces you to pick an integer ratio of how many "update frames" are skipped before rendering. Ideally it'd be nice if update could run uncapped, and have RenderUpdate only happen in accordance with vsync.
- The Entities Graphics system is doing something funky with how it allocates and deallocates memory. Most likely it's deallocating it the frame after it allocates it, but if OnDemandRendering is set to 4 that frame will happen 4 frames later. This causes the allocation checker to go crazy since it thinks you're having a massive amount of memory leaks.
- Not all gameplay draws use of more precise input checking unless the physics also are set to a higher frequency.  

I should also note that I technically also used VariableRateGroup for some cleanup checks that don't need to be immediate.


### Knots & Keyframes
Everything that moves works on keyframes through the DynamicBuffers ClimbKnot, RotateKnot, WheelKnot, and MultiPosition. All of them contain a value and a time, so that the different Render systems can interpolate between them when needed.
This allows me to add a sort of Coyote time to inputs, where if you press a key early or late I can either Queue the action to happen at the first available time it's possible or pretend you pressed the key earlier than you did if you're just a little late.
The current implementation has some complexities though:
- The player can start falling before the coyote time is over, forcing me to undo all the changes from the falling before I can queue new ones.
- There is a small visible jump if you press the coyote time late, though this might be due to me choosing a large coyote time.
- Some actions are dependent on the exact rotation of the player when the input is called, and therefore need to calculate the interpolation themself.
- The updating on what the "active" knot is can't happen in FixedUpdate since it also needs to be updated before rendering. So it needs to either run every frame or specifically when either a fixed update _or_ a render update will happen.

ClimbKnot ended up as a mix of visual interpolation and physical position/rotation interpolation. The way I did it in the project was by queuing multiple keyframes if I needed a special interpolation, but it might have been better to make a custom interpolator for it instead.
This made climbknot run on floating point values, even though integer values could've been better for the correctness of the program. Unfortunately I couldn't find an "integer" equivalent of Quaternions, despite them basically always having a limited set of values when they're always rotated a multiple of 90 degrees.

Having worked with this I got an idea of a more generalized system that only runs when something needs to update. This way we could only calculate stuff when either the player does a new input or when something actually happens, which could avoid the problem of frames skipping specific timestamps. Not that it's been a problem for anyone before.
Alternatively it'd also be cool to try to write a more generic character controller for the player, as currently most of the player controls are in a massive script called PlayerControllerSystem

### Wheel Rotation
I spent a few hours trying to find out how I can calculate the exact rotation a wheel would have at a specific position&player rotation. Since the radius of the wheel is exactly 0.5 every rotation + position combination should in theory have a unique wheel rotation needed to get there, but I failed to figure it out. It also wouldn't really work if you ever needed to push the player without rotating the wheels.
In the end I just added a new type of knot for wheel rotation.


### Rendering
Before setting up graphics with meshes and stuff I mostly used Debug.DrawLine and Graphics.RenderMeshInstanced with a bunch of NativeArrays as input. This let me quickly just draw whatever I needed to see without having to make sure LocalToWorlds were at those exact positions. 
It would be nice with something like Gizmos that's ecs compatible, since it's a bother to find a mesh & shader for basic wireframe shapes when I just want something that I can draw over everything. Probably could just set up a package with it myself though, so not something that I think is urgent for the Unity team to fix.

Afterwards I swapped over to entity graphics with the Keyframe system mentioned above. There's also some systems like BulletRenderSystem that instead has a start and forward position to decide the resulting position.
The original game had everything hard-coded as OpenGL calls, so I reverse-engineered how it worked and wrote a python script to convert them to obj files. I also remade some of them by hand.

### Gears, Wires, Switches, and Power
Gears and Wires are primarly controlled by InitializeCellsSystem and GearAndWireSystem, which do a series of Depth First Searches to find out what's getting powered by what. After that there's a few different systems reading the affected cells:
- ActivateGoalSystem checks if the goal itself is powered (if needed) and if all switches are active
- EnabledIfPoweredSystem checks if EnabledIfPowered is on anything that can be powered or active in some way and enables and disables the children based on that.
- AudioIfPoweredSystem does the same as EnabledIfPowered, but plays some audio clip when the object is powered or loses power instead.

There's also RegenerateCellsOnMovedObject that only runs in editor and lets me test the cells initialization and gear and wire system without having to enter play mode.
CleanupCellsSystem is the first ever cleanup system I've written and just makes sure there's no memory leaks with the cells.

### Audio
I couldn't find away to deal with audio in unmanaged code, so I handled it in two different ways:
- Creating a list of audio to play then having a managed system that reads the list later and plays the audio clips.
- Spawning entities with AudioSources that have PlayOnAwake on them.
For the latter I also had a system that delete the entities if they're done playing.

All the audio files are taken from the original game.

### Misc & Others
**The Camera** worked as an entity in editor, but once I made a build it was just black, so I had to spawn it from an entity instead (Or just have it in the scene). The UI also worked in editor but not in build.
**Shader.PropertyToID** needs to be run at runtime (can't be baked), but also didn't work properly as static variables, so I initialized shared pointers with the values in a RuntimeIntializeOnLoadMethod instead.
**Shader.SetGlobalColor** worked well to make a palette and not need a reference to all the materials, so I have InitializeColorsSystem for that.
**Persistent Variables** is a really nice way to communicate between Entities and UI without a ton of coupling, but for some reason the baked reference wasn't the same as the reference in the scene, so I had to make a monobehaviour spawn it instead. :(
**Some math stuff I keep needing to write myself** that would be really nice to have in math is an floating point ray that travels through integer positons, and also MoveTowards and SmoothDamp.
**Input** had a dumb issue in the build due to the different framerates. That one is on me, though it felt like the weirdest bug ever. 
**I didn't really talk about** bullets and Gear rotation, but other than that I think I've covered the entire project.
**Unity Recorder** for some reason breaks the project and permanently moves the camera to a strange offset. This made it difficult to record good gifs for the itch.io page.
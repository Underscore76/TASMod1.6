-- TO USE THIS FILE: copy into `StardewTAS1.6/Scripts/init.lua` file (Scripts folder may not exist, make it)
-- when you load the lua interface (type `lua`) in the in game console, it will do a bunch
-- of environment setup and will check to see if in the `StardewTAS1.6/Scripts/` folder there is
-- a file called `init.lua`. if there is, it will load the file and run it. this allows you
-- to define a custom boot script for your TAS that can do things like pull in additional libraries,
-- rename some convenience functions, load your input scripts, etc.
-- everything is programmatic so in theory your `init.lua` could define the entire TAS input sequence
-- as a series of advance calls... that sounds miserable, don't do that. but you *could*!

-- run some configuration how you want on boot
exec("loadengine default") -- can swap to whatever engine state you like
exec("overlay off Layers") -- run standard commands for toggling on/off certain features

-- import some libraries we want to have available globally in the console
keybinds = require("core.keybinds")
Queue = require("core.collections.queue")
Stack = require("core.collections.stack")

-- I hate typing so I'm going to make a functions that prints something I want to see
-- I make these aliases pretty regularly for things I use often. even better
function rt()
    print(real_time())
end

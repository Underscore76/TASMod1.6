--- adds a triggerable coroutine where if you press the J key, it will walk to the tile you are moused over
local nav = require('core.coroutines.nav') -- see this file for details on the generation/walk logic
local keybinds = require("core.keybinds")
local input = require("core.input")

-- add a new keybind
local name = "walk_to_mouse_tile"
local key = Keys.J

-- clean up existing keybind if it exists
keybinds.remove(key)
keybinds.add(
    key,
    function()
        -- convert current mouse coordinate to tile position
        local m = Mouse.GetState()
        local t = input.get_tile_from_local(Vector2(m.X, m.Y))
        -- push a coroutine for generating a path and walking to the tile
        KeyboardMouseInputQueue.PushFunction(nav.walk_to_tile(t), name)
    end,
    name
)

return nav

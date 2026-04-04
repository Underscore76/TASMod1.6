---flip back and forth of a tile boundary by staging coroutines
local KeyboardAndMouse = require("core.input.kbm")

-- lua requires forward declaring if we want to reference a variable before defining it
local walk_up, walk_down

walk_down = function()
    -- register another function to run after this one, could also be a named function
    KeyboardMouseInputQueue.PushFunction(walk_up, "walk_up")

    -- walk until we cross the tile boundary
    local kbm = KeyboardAndMouse.new()
    local tile = Game1.player.Tile
    while Game1.player.Tile.Y == tile.Y do
        kbm:press_key(Keys.S)
        kbm:push()
        coroutine.yield()
    end
end
walk_up = function()
    -- walk until we cross the tile boundary
    local kbm = KeyboardAndMouse.new()
    local tile = Game1.player.Tile
    while Game1.player.Tile.Y == tile.Y do
        kbm:press_key(Keys.W)
        kbm:push()
        coroutine.yield()
    end
    -- as long as this happens before we exit out of this function, it'll register and apply
    KeyboardMouseInputQueue.PushFunction(walk_down, "walk_down")
end

-- kicking off the process by assigning a coroutine
KeyboardMouseInputQueue.PushFunction(walk_down, "walk_down")

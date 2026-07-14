---methods for working with mouse input in coroutines

local input = require('core.input')
local mouse_utils = {}

---mouseover a tile
---@param kbm KeyboardAndMouse
---@param tile Vec2|table location to move mouse to
---@return boolean true if mouse was moved, false if already there
function mouse_utils.handle_mouseover(kbm, tile)
    local m = input.get_prev_mouse_tile()
    local same_tile = m.X == tile.X and m.Y == tile.Y
    local next_mouse = input.get_local_from_tile(tile.X, tile.Y)
    kbm:mouse_position(next_mouse.X, next_mouse.Y)
    return not same_tile
end

---swing weapon for specified number of frames
---@param kbm KeyboardAndMouse
---@param nframes number number of frames to swing for (default: 1)
function mouse_utils.swing_weapon(kbm, nframes)
    if nframes == nil or nframes < 1 then
        nframes = 1
    end
    if Controller.LastFrameMouse().LeftMouseClicked then
        kbm:press_key(Keys.C)
    else
        kbm:mouse_left_down()
    end
    kbm:push()
    coroutine.yield()
    for _ = 1, nframes - 1 do
        kbm:push()
        coroutine.yield()
    end
    kbm:press_keys({ Keys.RightShift, Keys.R, Keys.Delete })
end

---swings/animation cancels assuming that the mouse is in correct position and desired tool is equipped
---@param kbm KeyboardAndMouse
---@param cancel boolean whether to cancel the swing after
function mouse_utils.swing(kbm, cancel)
    -- toggle left click or keyboard in case a state is captured from prior frame
    if Controller.LastFrameMouse().LeftMouseClicked then
        kbm:press_key(Keys.C)
    else
        kbm:mouse_left_down()
    end
    kbm:push()
    coroutine.yield()

    --
    if cancel then
        -- stage cancel inputs so they are sent on the next set
        -- allows for possibility of overlapping cancel with other inputs
        kbm:press_keys({ Keys.RightShift, Keys.R, Keys.Delete })
    else
        -- need to advance through the full swing
        while Game1.player.UsingTool do
            kbm:push()
            coroutine.yield()
        end
    end
end

return mouse_utils

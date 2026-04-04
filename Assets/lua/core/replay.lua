local replay = {}
local keybinds = require('core.keybinds')

--- enter replay mode
function replay.enter()
    Controller.GameMode = TASMode.Replay
    Controller.IsPaused = true
    Controller.PauseFrame = -1
end

--- exit replay mode
function replay.exit()
    Controller.GameMode = TASMode.Edit
    Controller.IsPaused = false
    Controller.PauseFrame = -1
end

--- pause the game (on a specific frame if desired)
--- @param frame number|nil @frame to pause on (default: nil)
function replay.pause(frame)
    replay.enter()
    if frame == nil then
        Controller.PauseFrame = -1
    else
        Controller.PauseFrame = frame
    end
end

--- run the game until the specified frame
--- @param frame number @frame to run to
function replay.runto(frame)
    if frame == nil then
        print("ERROR: runto requires a frame number")
        return
    end
    replay.enter()
    Controller.IsPaused = false
    Controller.PauseFrame = frame
end

--- advance the game in replay mode
--- @param frame number|nil @number of frames to advance (default: 1)
function replay.step(frame)
    if frame == nil then
        frame = 1
    end
    Controller.IsPaused = false
    Controller.PauseFrame = TASDateTime.CurrentFrame + frame
end

--- unpause the game
function replay.toggle()
    Controller.IsPaused = not Controller.IsPaused
    Controller.PauseFrame = -1
end

--- register keybinds for replay mode
--- @param step_key Microsoft.Xna.Framework.Input.Keys @key to advance one frame
--- @param toggle_key Microsoft.Xna.Framework.Input.Keys @key to toggle pause
function replay.register_keybinds(step_key, toggle_key)
    replay.enter()
    if step_key == nil or toggle_key == nil then
        print("ERROR: replay.register_keybinds requires two key arguments")
        return
    end

    keybinds.add(step_key, function()
        replay.step(1)
    end, "Advance one frame in replay mode")

    keybinds.add(toggle_key, function()
        replay.toggle()
    end, "Toggle pause in replay mode")
end

return replay

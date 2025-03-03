--defines the core engine interface for dealing with frame
local engine = {}

---attempt to wait until all logic has halted
---@param max_frames number|nil maximum number of frames to wait
function engine.halt(max_frames)
    if max_frames == nil then
        max_frames = 100
    end
    local i = 0
    while interface.HasStep and i < max_frames do
        i = i + 1
        interface:WaitPrefix()
        interface:StepLogic()
        interface:WaitPostfix()
    end
end

---advance the game by one frame
---@param input table|nil input to advance with (defaults to no input)
function engine.advance(input)
    interface:AdvanceFrame(input)
    engine.halt()
end

---reset the game to the specified frame
---@param frame number|nil frame to reset to (defaults current frame)
--- negative frames are relative to the current frame (-1 == current frame)
function engine.reset(frame)
    if frame == nil then
        frame = -1
    end
    interface:ResetGame(frame)
end

---fast reset the game to the specified frame
---@param frame number|nil frame to reset to (defaults current frame)
--- negative frames are relative to the current frame (-1 == current frame)
function engine.fast_reset(frame)
    if frame == nil then
        frame = -1
    end
    interface:FastResetGame(frame)
end

---blocking reset the game to the specified frame
---@param frame number|nil frame to reset to (defaults current frame)
--- negative frames are relative to the current frame (-1 == current frame)
function engine.blocking_reset(frame)
    if frame == nil then
        frame = -1
    end
    interface:BlockResetGame(frame)
end

---blocking fast reset the game to the specified frame
---@param frame number|nil frame to reset to (defaults current frame)
--- negative frames are relative to the current frame (-1 == current frame)
function engine.blocking_fast_reset(frame)
    if frame == nil then
        frame = -1
    end
    interface:BlockFastResetGame(frame)
end

---blocking load of a file
---@param file string file to load
function engine.blocking_load(file)
    interface:BlockLoad(file)
end

---blocking load of a file
---@param file string file to load
function engine.blocking_fast_load(file)
    interface:BlockFastLoad(file)
end

return engine

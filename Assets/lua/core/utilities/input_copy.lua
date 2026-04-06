---clone frame inputs and play them back at a later time
---useful for things like replaying non-rng dependent inputs

local input_copy = {
    frames = nil,
}

local function state_to_table(frame_state)
    -- copy the keyboardState
    local keyboard = {}
    for k, v in list_items(frame_state.keyboardState) do
        table.insert(keyboard, v)
    end
    -- copy the mouseState
    local mouse = {
        left = frame_state.mouseState.LeftMouseClicked,
        right = frame_state.mouseState.RightMouseClicked,
        X = frame_state.mouseState.MouseX,
        Y = frame_state.mouseState.MouseY,
    }

    -- copy the text
    local text = frame_state.InjectText
    return {
        keyboard = keyboard,
        mouse = mouse,
        text = text,
    }
end

---copy inputs from the current save state
---@param f_start number the starting frame to copy (inclusive)
---@param f_end number the ending frame to copy (exclusive)
function input_copy.copy(f_start, f_end)
    if f_start == nil then
        error("Need to specify frames to clone")
    end
    if f_end == nil then
        f_end = f_start
        f_start = 0
    end
    if f_end <= f_start then
        error("End frame must be greater than start frame")
    end
    if f_end > Controller.State.Count then
        f_end = Controller.State.Count
    end
    input_copy.frames = {}
    for i = f_start, f_end - 1 do
        local frame = Controller.State.FrameStates[i]
        table.insert(input_copy.frames, state_to_table(frame))
    end
end

---playback the copied frames from right now
function input_copy.playback()
    if input_copy.frames == nil then
        error("No frames to playback")
        return
    end

    for i, frame in ipairs(input_copy.frames) do
        advance(frame)
    end
end

return input_copy

local game_utils = {}

---register and run a coroutine to completion
---@param func fun(...) the function to run, should return true when complete
---@param name string|nil optional name for the coroutine (for debugging purposes)
function game_utils.run(func, name)
    if name == nil then name = "run" end
    KeyboardMouseInputQueue.PushFunction(func, name)
    while KeyboardMouseInputQueue.HasCoroutine() do
        halt(100) -- let the background engine execute
    end
end

return game_utils

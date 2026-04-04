--- example of registering and an associated button to execute this script
local KeyboardAndMouse = require("core.input.kbm")

---@section Example of registering a named function and button in the imgui overlay
-- register a named function that we can set by name later
LuaFunctionRegistry.RegisterFunction(
    "walk_right", -- name of the function we want to register
    -- this is an inline function that defines our behavior
    function()
        local tile = Game1.player.Tile
        local kbm = KeyboardAndMouse.new()
        while Game1.player.Tile == tile do
            kbm:press_key(Keys.D) -- register the D key as being pressed
            kbm:push()            -- sends the game input
            --[[
                WARNING WARNING WARNING
                if you don't include a coroutine.yield in your loop, you WILL freeze the game
                as the game will never get control back to process the frame
            --]]
            coroutine.yield() -- yield control back to the game
        end
    end,
    "walk right until we reach the next tile" -- this is the description for this function
)

-- register a button to set this named function
LuaOverlay.AddButton(
    "Walk Right 1 Tile", -- name of the button
    -- inline function to run when we click the button in the imgui overlay
    function()
        -- adding our current coroutine to the input queue based on the registered function name
        KeyboardMouseInputQueue.PushNamedFunction("walk_right")
    end
)
-- Because we named the function, we can also push it whenever in other parts of our code
-- KeyboardMouseInputQueue.PushNamedFunction("walk_right")


---@section Example of registering an inline function and button in the imgui overlay
LuaOverlay.AddButton(
    "Walk Left 1 Tile", -- name of the button
    -- inline function to run when we click the button in the imgui overlay
    function()
        -- adding our current coroutine to the input queue based on the registered function name
        KeyboardMouseInputQueue.PushFunction(
            function() -- inline function
                local tile = Game1.player.Tile
                local kbm = KeyboardAndMouse.new()
                while Game1.player.Tile == tile do
                    kbm:press_key(Keys.A) -- register the A key as being pressed
                    kbm:push()            -- sends the game input
                    coroutine.yield()     -- yield control back to the game
                end
            end,
            "walk_left"
        )
    end
)

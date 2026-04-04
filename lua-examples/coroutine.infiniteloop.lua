--- Breaking out of an Infinite Frame Advance Loop
-- its possible to generate a coroutine function that loops indefinitely if not careful
-- in order to break out, press both the '-' and '+' keys at the same time while the console is closed

local KeyboardAndMouse = require("core.input.kbm")

-- this function will run forever because we never escape the loop
local function BAD_IDEA()
    local kbm = KeyboardAndMouse.new()
    while true do
        kbm:push() -- send 1 frame of input to the game
        --[[
            WARNING WARNING WARNING
            if you don't include a coroutine.yield in your loop, you WILL freeze the game
            as the game will never get control back to process the frame
        --]]
        coroutine.yield()
    end
end

LuaOverlay.AddButton(
    "Freeze The Game", -- name of the button
    -- inline function to run when we click the button in the imgui overlay
    function()
        -- registering a function that will start executing
        KeyboardMouseInputQueue.PushFunction(
            BAD_IDEA,                         -- function to run
            "freeze_game",                    -- name of the function for debugging purposes
            "will absolutely freeze the game" -- description of the function for debugging purposes
        )
    end
)

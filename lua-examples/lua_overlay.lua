--- This file is for testing the LuaOverlay functionality. It adds some watch variables and a button to the overlay.

-- Add a watch variable to the LuaOverlay for the player's name
LuaOverlay.AddData(
    "Player Name",
    function()
        if Game1.player == nil then
            return "nil"
        end
        return Game1.player.Name
    end
)

-- Add a watch variable to the LuaOverlay for experience points
LuaOverlay.AddData(
    "Experience",
    function()
        if Game1.player == nil then
            return "[]"
        end
        return Game1.player.experiencePoints:ToString()
    end
)

-- Add a button to the LuaOverlay that you can trigger via the UI
LuaOverlay.AddButton(
    "Test Button",
    function()
        print("Button Pressed")
    end
)

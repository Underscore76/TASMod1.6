--- Register a coroutine that watches for behavior and reacts to it

-- example of a coroutine that watches for when the player is not holding a pickaxe and then swaps back to it
local inventory = require('core.inventory')
local KeyboardAndMouse = require("core.input.kbm")

local function swap_back()
    local kbm = KeyboardAndMouse.new()
    while true do
        -- if the player is holding a pickaxe, just yield and check again next frame
        local current = Game1.player.CurrentTool
        if current ~= nil and current.Name ~= "Pickaxe" then
            -- figure out the keys to swap to the pickaxe and fire them off
            local keys = inventory.get_inventory_key("Pickaxe")
            if keys == nil then
                return
            end
            kbm:press_keys(keys)
            kbm:push()
        end
        coroutine.yield()
    end
end

LuaOverlay.AddButton(
    "Swap to Pickaxe When Equipped",
    function()
        KeyboardMouseInputQueue.PushFunction(
            swap_back,
            "swap_back",
            "will swap back to the previous tool"
        )
    end
)

--- Inventory helper functions
local inventory = {}

local InventoryKeys = {
    [0] = { Keys.D1 },
    [1] = { Keys.D2 },
    [2] = { Keys.D3 },
    [3] = { Keys.D4 },
    [4] = { Keys.D5 },
    [5] = { Keys.D6 },
    [6] = { Keys.D7 },
    [7] = { Keys.D8 },
    [8] = { Keys.D9 },
    [9] = { Keys.D0 },
    [10] = { Keys.OemMinus },
    [11] = { Keys.OemPlus },
    [12] = { Keys.Tab },
    [13] = { Keys.Tab },
    [14] = { Keys.Tab },
    [15] = { Keys.Tab },
    [16] = { Keys.Tab },
    [17] = { Keys.Tab },
    [18] = { Keys.Tab },
    [19] = { Keys.Tab },
    [20] = { Keys.Tab },
    [21] = { Keys.Tab },
    [22] = { Keys.Tab },
    [23] = { Keys.Tab },
    [24] = { Keys.Tab },
    [25] = { Keys.Tab },
    [26] = { Keys.Tab },
    [27] = { Keys.Tab },
    [28] = { Keys.Tab },
    [29] = { Keys.Tab },
    [30] = { Keys.Tab },
    [31] = { Keys.Tab },
    [32] = { Keys.Tab },
    [33] = { Keys.Tab },
    [34] = { Keys.Tab },
    [35] = { Keys.Tab },
}

---get the necessary keys to swap to an item
---returns {} if we are already holding the item with the necessary stack size
---returns nil if the item is not found
---otherwise returns the keys to press to swap to the item
---@param name string the name of the item to swap to
---@param minStack number|nil the minimum stack size of the item to swap to, defaults to 1
---@return table<Keys|number>|nil keys to press
function inventory.get_inventory_key(name, minStack)
    if minStack == nil then
        minStack = 1
    end
    if Game1.player.CurrentItem ~= nil and Game1.player.CurrentItem.Name == name and Game1.player.CurrentItem.Stack >= minStack then
        return {}
    end
    for i = 0, Game1.player.MaxItems - 1 do
        if Game1.player.Items[i] ~= nil and Game1.player.Items[i].Name == name then
            if Game1.player.Items[i].Stack >= minStack then
                return InventoryKeys[i]
            end
        end
    end
    return nil
end

---get the necessary keys to swap to a tool type
---returns {} if we are already holding the tool
---returns nil if the tool is not found
---otherwise returns the keys to press to swap to the tool
---@param name string the name of the tool type to swap to (e.g. "Axe", "Pickaxe", "Hoe", "Watering Can", "Scythe")
---@return table<Keys|number>|nil keys to press
function inventory.get_tool_key(name)
    if Game1.player.CurrentTool ~= nil and Game1.player.CurrentTool:GetType().Name == name then
        return {}
    end
    for i = 0, Game1.player.MaxItems - 1 do
        if Game1.player.Items[i] ~= nil and Game1.player.Items[i]:GetType().Name == name then
            return InventoryKeys[i]
        end
    end
    return nil
end

---check if the player has the specified item with the minimum stack size
---@param item string the name of the item to check
---@param minStackSize number|nil the minimum stack size of the item, defaults to 1
---@return boolean true if the player has the item with the required stack size, false otherwise
function inventory.have_items(item, minStackSize)
    local inv = inventory.get_inventory_key(item, minStackSize)
    return inv ~= nil
end

return inventory

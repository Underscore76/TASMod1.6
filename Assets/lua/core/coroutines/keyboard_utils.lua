---methods for working with keyboard input in coroutines

local inventory = require('core.inventory')
local keyboard_utils = {}

---press keys to swap to a specified tool
---@param kbm KeyboardAndMouse
---@param tool string name of the tool or tool type to swap to
---@return boolean return true if an update is needed due to staged inputs
function keyboard_utils.handle_inventory_swap(kbm, tool)
    local keys
    if tool == "Weapon" then
        keys = inventory.get_inventory_key("Scythe")
    else
        keys = inventory.get_tool_key(tool)
    end
    if keys == nil then
        error("Could not find suitable tool: " .. tool)
    end
    if #keys ~= 0 then
        kbm:press_keys(keys)
        return true
    end
    return false
end

return keyboard_utils

--- functions that can be injected as part of navigation or as standalone input handlers
--- WARNING: the output signature of some of these functions are subject to change as I futz with overlapping inputs better
--- ideally the function returning true should mean that there are some staged inputs that need to be addressed
local keyboard_utils = require('core.coroutines.keyboard_utils')
local mouse_utils = require('core.coroutines.mouse_utils')
local inventory = require('core.inventory')
local frame_funcs = {}

--
-- helper functions
--

---swap to the desired tool and move the mouse over the desired tile
---@param kbm KeyboardAndMouse
---@param tool string
---@param tile Vec2|table
local function _swap_to_tool(kbm, tool, tile)
    local swapped_to_tool = keyboard_utils.handle_tool_swap(kbm, tool)
    local moved_mouse = mouse_utils.handle_mouseover(kbm, tile)
    if swapped_to_tool or moved_mouse then
        return true
    end
    return false
end

---swap to the desired item and move over a tile
---@param kbm KeyboardAndMouse
---@param item string
---@param tile Vec2|table
local function _swap_to_item(kbm, item, tile)
    local swapped_to_item = keyboard_utils.handle_item_swap(kbm, item)
    local moved_mouse = mouse_utils.handle_mouseover(kbm, tile)
    if swapped_to_item or moved_mouse then
        return true
    end
    return false
end


---swing the desired tool at the specified tile
---@param kbm KeyboardAndMouse
---@param tool string
---@param tile Vec2|table
local function _swing_tool(kbm, tool, tile)
    -- TODO: just hiding the weapon3 check deeper so we can start stubbing above
    if tool == "Weapon3" then
        tool = "Weapon"
    end
    while _swap_to_tool(kbm, tool, tile) do
        kbm:push()
        coroutine.yield()
    end

    if tool == "Weapon" or tool == "Weapon3" then
        mouse_utils.swing_weapon(kbm, 12)
    else
        mouse_utils.swing(kbm, true)
    end
end

---use item at tile
---@param kbm KeyboardAndMouse
---@param item string
---@param tile Vec2|table
local function _place_item(kbm, item, tile)
    if not inventory.have_item(item) then
        error("Tried to place item that is not in inventory: " .. item)
    end
    while _swap_to_item(kbm, item, tile) do
        kbm:push()
        coroutine.yield()
    end
    if Controller.LastFrameMouse().LeftMouseClicked then
        kbm:press_key(Keys.C)
    else
        kbm:mouse_left_down()
    end
end


---calc a basic offset tile to put the mouse too based
---@param p Vec2|table player tile
---@param t Vec2|table target tile
---@param dx number x offset from player to target
---@param dy number y offset from player to target
---@return Vec2 tile to move mouse to
local function _offset_to_tile(p, t, dx, dy)
    if dy ~= 0 then
        return Vector2(p.X, t.Y)
    end
    if dx ~= 0 then
        return Vector2(t.X, p.Y)
    end
    return t
end

---check if a tile is tillable
---@param tile Vec2|table
---@return boolean
local function _tillable(tile)
    local rect = Rectangle(tile.X * 64, tile.Y * 64, 64, 64)
    if not Game1.currentLocation:isTilePassable(tile) then
        return false
    end
    if Game1.currentLocation.Objects:ContainsKey(tile) then
        return false
    end
    if Game1.currentLocation.terrainFeatures:ContainsKey(tile) then
        return false
    end
    for i, ltf in list_items(Game1.currentLocation.largeTerrainFeatures) do
        if ltf:getBoundingBox():Intersects(rect) then
            return false
        end
    end
    for i, clump in list_items(Game1.currentLocation.resourceClumps) do
        if clump:getBoundingBox():Intersects(rect) then
            return false
        end
    end
    for i, b in list_items(Game1.currentLocation.buildings) do
        if b:occupiesTile(tile) then
            return false
        end
    end
    return Game1.currentLocation:doesTileHaveProperty(tile.X, tile.Y, "Diggable", "Back") ~= nil
end


--
-- coroutines
--

---move the mouse over the desired tile
---@param tile Vec2|table
---@return fun(kbm: KeyboardAndMouse):boolean
function frame_funcs.mouse_over_tile(tile)
    return function(kbm)
        mouse_utils.handle_mouseover(kbm, tile)
        return false
    end
end

---coroutine to swap to specified tool
---@param tool string the class of tool to swap to (e.g. "Hoe", "Pickaxe", "Axe", "WateringCan", "Weapon")
---@param tile Vec2|table the tile to move the mouse over after swapping
---@return fun(kbm: KeyboardAndMouse):boolean coroutine
function frame_funcs.swap_to_tool(tool, tile)
    return function(kbm)
        _swap_to_tool(kbm, tool, tile)
        return false
    end
end

---coroutine to swing a specific tool at a tile
---@param tool string the class of tool to swing (e.g. "Hoe", "Pickaxe", "Axe", "WateringCan", "Weapon")
---@param tile Vec2|table the tile to swing the tool at
---@return fun(kbm: KeyboardAndMouse):boolean coroutine
function frame_funcs.swing_tool(tool, tile)
    return function(kbm)
        _swing_tool(kbm, tool, tile)
        return true -- animation cancel keys are staged
    end
end

---coroutine to use a specific item at a tile
---@param item string the name of the item to use
---@param tile Vec2|table the tile to use the item at
---@return fun(kbm: KeyboardAndMouse):boolean coroutine
function frame_funcs.place_item(item, tile)
    return function(kbm)
        _place_item(kbm, item, tile)
        return true -- click action is staged
    end
end

---coroutine to hoe the ground around the player
---@return fun(kbm: KeyboardAndMouse):boolean coroutine
function frame_funcs.hoe_ground()
    return function(kbm)
        local p = Game1.player.Tile
        local tile, rect
        for x = -1, 1 do
            for y = -1, 1 do
                if x == 0 and y == 0 then
                    goto continue
                end
                tile = Vector2(p.X + x, p.Y + y)
                if _tillable(tile) then
                    _swing_tool(kbm, "Hoe", tile)
                    return true -- animation cancel keys are staged
                end
                ::continue::
            end
        end
        return false
    end
end

---coroutine to break stones around the player
---@return fun(kbm: KeyboardAndMouse):boolean coroutine
function frame_funcs.break_stones()
    return function(kbm)
        local p = Game1.player.Tile
        local tile, obj
        for x = -1, 1 do
            for y = -1, 1 do
                if x == 0 and y == 0 then
                    goto continue
                end
                tile = Vector2(p.X + x, p.Y + y)
                if Game1.currentLocation.Objects:ContainsKey(tile) then
                    obj = Game1.currentLocation.Objects[tile]
                    if obj.Name ~= "Stone" then
                        goto continue
                    end
                    _swing_tool(kbm, "Pickaxe", tile)
                    return true -- animation cancel keys are staged
                end
                ::continue::
            end
        end
        return false
    end
end

---coroutine to break twigs around the player
---@return fun(kbm: KeyboardAndMouse):boolean coroutine
function frame_funcs.break_twigs()
    return function(kbm)
        local p = Game1.player.Tile
        local tile, obj
        for x = -1, 1 do
            for y = -1, 1 do
                if x == 0 and y == 0 then
                    goto continue
                end
                tile = Vector2(p.X + x, p.Y + y)
                if Game1.currentLocation.Objects:ContainsKey(tile) then
                    obj = Game1.currentLocation.Objects[tile]
                    if obj.Name ~= "Twig" then
                        goto continue
                    end
                    _swing_tool(kbm, "Axe", tile)
                    return true -- animation cancel keys are staged
                end
                ::continue::
            end
        end
        return false
    end
end

---coroutine to water HoeDirt around the player
---@return fun(kbm: KeyboardAndMouse):boolean coroutine
function frame_funcs.water_dirt()
    return function(kbm)
        local p = Game1.player.Tile
        local tile, tf
        for x = -1, 1 do
            for y = -1, 1 do
                tile = Vector2(p.X + x, p.Y + y)
                if Game1.currentLocation.terrainFeatures:ContainsKey(tile) then
                    tf = Game1.currentLocation.terrainFeatures[tile]
                    if tf:GetType().Name ~= "HoeDirt" then
                        goto continue
                    end
                    if tf.state.Value ~= 0 then
                        goto continue
                    end
                    _swing_tool(kbm, "WateringCan", tile)
                    return true -- animation cancel keys are staged
                end
                ::continue::
            end
        end
        return false
    end
end

---coroutine to break weeds around the player
---@return fun(kbm: KeyboardAndMouse):boolean coroutine
function frame_funcs.break_weeds()
    return function(kbm)
        local p = Game1.player.Tile
        local tile, obj
        for y = -1, 1 do
            for x = -1, 1 do
                -- the 0,0 case is weird and would be easier to just move off the tile and swing/catch this from another tile
                if x == 0 and y == 0 then
                    goto continue
                end
                tile = Vector2(p.X + x, p.Y + y)
                if Game1.currentLocation.Objects:ContainsKey(tile) then
                    obj = Game1.currentLocation.Objects[tile]
                    if obj.Name ~= "Weeds" then
                        goto continue
                    end
                    local swingTile = _offset_to_tile(p, tile, x, y)
                    _swing_tool(kbm, "Weapon", swingTile)
                    return true -- animation cancel keys are staged
                end
                ::continue::
            end
        end
        return false
    end
end

---coroutine to break grass around the player
---todo: this really tries to reach for grass that's far away so it looks a little whack
---@return fun(kbm: KeyboardAndMouse):boolean coroutine
function frame_funcs.scythe_grass()
    return function(kbm)
        local p = Game1.player.Tile
        local tile, tf
        for y = -1, 1 do
            for x = -1, 1 do
                -- the 0,0 case is weird and would be easier to just move off the tile and swing/catch this from another tile
                if x == 0 and y == 0 then
                    goto continue
                end
                tile = Vector2(p.X + x, p.Y + y)
                if Game1.currentLocation.terrainFeatures:ContainsKey(tile) then
                    tf = Game1.currentLocation.terrainFeatures[tile]
                    if tf:GetType().Name ~= "Grass" then
                        goto continue
                    end
                    local swingTile = _offset_to_tile(p, tile, x, y)
                    -- scythe or sword needed to break grass
                    _swing_tool(kbm, "Weapon3", swingTile)
                    return true -- animation cancel keys are staged
                end
                ::continue::
            end
        end
        return false
    end
end

---coroutine to plant seeds around the player
---todo: right now this will lose 1 frame per plant since it doesn't overlap plant and mouse movement and it blocks on player movement
---@param seed string the name of the seed to plant
---@return fun(kbm: KeyboardAndMouse):boolean coroutine
function frame_funcs.plant_seeds(seed)
    return function(kbm)
        if seed == nil or not inventory.have_item(seed) then
            return false
        end
        if not Game1.currentLocation.IsFarm then
            return false
        end
        local p = Game1.player.Tile
        local tile, tf
        for y = -1, 1 do
            for x = -1, 1 do
                if x == 0 and y == 0 then
                    goto continue
                end
                tile = Vector2(p.X + x, p.Y + y)
                if not Game1.currentLocation.terrainFeatures:ContainsKey(tile) then
                    goto continue
                end
                tf = Game1.currentLocation.terrainFeatures[tile]
                if tf:GetType().Name ~= "HoeDirt" then
                    goto continue
                end
                if tf.crop == nil then
                    _place_item(kbm, seed, tile)
                    return true -- click action is staged
                end
                ::continue::
            end
        end
        return false
    end
end

return frame_funcs

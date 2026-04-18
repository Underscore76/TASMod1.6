---methods for walking to a specified tile

local tile_funcs = require("core.utilities.tile_funcs")
local mouse_utils = require("core.coroutines.mouse_utils")
local keyboard_utils = require("core.coroutines.keyboard_utils")
local KeyboardAndMouse = require("core.input.kbm")

local nav = {}
local LOOKAHEAD_CHECK_DIRS = {
    VERTICAL = Vector2(0, 0.707),
    HORIZONTAL = Vector2(0.707, 0)
}
local function step(vec, dir, speed)
    return vec + dir * speed
end

local function stage_for_next_tile(kbm, nextTool, nextTile, position, xfunc, yfunc, xdir, ydir, moveTile)
    local speed = Game1.player:getMovementSpeed()
    -- pre-swap to tool/mouseover
    if nextTool ~= "" then
        keyboard_utils.handle_inventory_swap(kbm, nextTool)
        mouse_utils.handle_mouseover(kbm, nextTile)
    end
    -- we want to thin in the direction of the next tile
    -- pathing is 1 tile at a time so no need to cut outside current bounds
    -- just walk until we are on the edge of our current tile in the direction of the next
    local pn, check
    if xdir == 0 then
        local nxdir = xfunc(position, nextTile)
        pn = step(position, nxdir * LOOKAHEAD_CHECK_DIRS.HORIZONTAL, speed)
        check = xfunc(pn, moveTile) == 0
        if check then
            if nxdir > 0 then
                kbm:press_key(Keys.D)
            elseif nxdir < 0 then
                kbm:press_key(Keys.A)
            end
        end
    elseif ydir == 0 then
        local nydir = yfunc(position, nextTile)
        pn = step(position, nydir * LOOKAHEAD_CHECK_DIRS.VERTICAL, speed)
        check = yfunc(pn, moveTile) == 0
        if check then
            if nydir > 0 then
                kbm:press_key(Keys.S)
            elseif nydir < 0 then
                kbm:press_key(Keys.W)
            end
        end
    end
end

---generates a path to the given tile, optionally using tools if needed
---@param tile Vec2|nil tile to path to
---@param use_tool boolean|nil whether to use tools to clear a path if needed, defaults to true
function nav.generate_path(tile, use_tool)
    if tile == nil then
        return
    end
    if use_tool == nil or type(use_tool) ~= "boolean" then
        use_tool = true
    end
    Controller.PathFinder:Reset()
    Controller.PathFinder:Update(0, tile.X, tile.Y, use_tool)
    if not Controller.PathFinder.hasPath then
        local err = string.format("failed to generate path to tile %d,%d", tile.X, tile.Y)
        error(err)
        print(err)
    end
end

---returns a coroutine function that generates and walks a path to the selected tile
---@param x Vec2
---@return function coroutine function to walk to the tile
function nav.walk_to_tile(x)
    return function()
        local kbm = KeyboardAndMouse.new()
        nav.generate_path(x)
        if not Controller.PathFinder.hasPath then
            error(string.format("failed to generate path to tile %d,%d", x.X, x.Y))
        end
        -- functions designed to move the players bounding box fully into each tile of the path

        while Controller.PathFinder.hasPath do
            local loc = Controller.PathFinder:PeekFront()
            if loc == nil then
                return
            end
            local moveTile = loc:toVector2()

            -- a little bit of lookahead to try and overlap inputs/speedup pathing
            local nextTile = nil
            if Controller.PathFinder.path.Count > 1 then
                nextTile = Controller.PathFinder.path[1]:toVector2()
            end
            local nextTool = Controller.PathFinder:GetToolString(nextTile)

            while not tile_funcs.ContainedRect(Game1.player:GetBoundingBox(), moveTile) do
                -- there's something on the current planned tile we need to clear
                local p = tile_funcs.PlayerPosition()
                local xdir = tile_funcs.FullyWithinTileWidth(p, moveTile)
                local ydir = tile_funcs.FullyWithinTileHeight(p, moveTile)
                local tool = Controller.PathFinder:GetToolString(moveTile)

                if tool ~= "" then
                    local swapped_inventory = keyboard_utils.handle_inventory_swap(kbm, tool)
                    local moved_mouse = mouse_utils.handle_mouseover(kbm, moveTile)
                    if swapped_inventory or moved_mouse then
                        goto setup_tools
                    end
                    -- would only happen if animation cancel inputs are staged from last swing
                    if kbm:has_pending() then
                        kbm:push()
                        coroutine.yield()
                    end
                    mouse_utils.swing(kbm, tool ~= "Weapon")
                    goto swing
                end

                -- want to walk toward our current planned tile
                if ydir == 0 and xdir == 0 then
                    break
                end
                if xdir > 0 then
                    kbm:press_key(Keys.D)
                elseif xdir < 0 then
                    kbm:press_key(Keys.A)
                end

                if ydir > 0 then
                    kbm:press_key(Keys.S)
                elseif ydir < 0 then
                    kbm:press_key(Keys.W)
                end

                -- want to stage inputs for the next step of the path
                if nextTile ~= nil then
                    stage_for_next_tile(kbm, nextTool, nextTile, p, tile_funcs.FullyWithinTileWidth,
                        tile_funcs.FullyWithinTileHeight, xdir, ydir, moveTile)
                end

                ::setup_tools::
                kbm:push()
                coroutine.yield()
                ::swing::
            end
            Controller.PathFinder:PopFront()
        end
        -- ensure any last staged inputs are sent
        if kbm:has_pending() then
            kbm:push()
            coroutine.yield()
        end
    end
end

return nav

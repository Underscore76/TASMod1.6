--- `input`: defines some basic click functions for interacting with the game

local clickables = require("core.clickables")

local input = {}

---click a point on the screen
---@param point Vec2 a table with X and Y fields
---@param left boolean true if you want to left click
---@param right boolean true if you want to right click
function input.click_point(point, left, right)
    local mouse = {
        X = point.X,
        Y = point.Y,
        left = false,
        right = false
    }
    local last_mouse = Controller.LastFrameMouse()
    if last_mouse.MouseX ~= mouse.X or last_mouse.MouseY ~= mouse.Y or (last_mouse.LeftMouseClicked and left) or (last_mouse.RightMouseClicked and right) then
        advance({ mouse = mouse })
    end
    mouse.left = left
    mouse.right = right
    advance({ mouse = mouse })
end

---click the center of a rectangle
---@param rect Rect a table with Center field or a Rectangle object
function input.click_rect(rect)
    input.click_point(rect.Center, true, false)
end

---click a component by name
---@param name string the name of the component to click
function input.click_component(name)
    local component = clickables.get_object_by_name(name)
    if component == nil then
        print("Component not found: " .. name)
    else
        input.click_rect(component.Rect)
    end
end

---click a slider component by name
---@param name string the name of the component to click
---@param value number the value to set the slider to (0-100)
function input.click_slider_component(name, value)
    local obj = clickables.get_object_by_name(name)
    if obj ~= nil then
        input.click_horizontal_slider(obj.Rect, value)
    end
end

---click a horizontal slider
---@param rect table|Rect a table with X, Y, Width, and Center fields or a Rectangle object
---@param value number the value to set the slider to (0-100)
function input.click_horizontal_slider(rect, value)
    local point = {
        X = rect.X + rect.Width * value / 100,
        Y = rect.Center.Y
    }
    input.click_point(point, true, false)
end

---get local coordinates from tile coordinates
---@param tileX number the x coordinate of the tile
---@param tileY number the y coordinate of the tile
---@return Vec2 local coordinates of the center of the tile
function input.get_local_from_tile(tileX, tileY)
    local tileSize = Game1.tileSize
    local viewport = RunCS("Game1.viewport")
    local zoomLevel = RunCS("Game1.options.zoomLevel")
    local tile = { X = (tileX + 0.5) * tileSize, Y = (tileY + 0.5) * tileSize }
    local localX = (tile.X - viewport.X) * zoomLevel
    local localY = (tile.Y - viewport.Y) * zoomLevel
    return Vector2(localX, localY)
end

---get tile coordinates from local coordinates
---@param vec Vec2 the vector to get the tile of
---@return Vec2 tile coordinates of the vector
function input.get_tile_from_local(vec)
    local coords = Vector2(vec.X / Game1.options.zoomLevel + Game1.viewport.X,
        vec.Y / Game1.options.zoomLevel + Game1.viewport.Y)
    local mTile = Vector2(coords.X // Game1.tileSize, coords.Y // Game1.tileSize)
    return mTile
end

---get previous mouse tile
---@return Vec2 tile coordinates of the previous mouse position
function input.get_prev_mouse_tile()
    local mouse = Controller.LastFrameMouse()
    return input.get_tile_from_local(Vector2(mouse.MouseX, mouse.MouseY))
end

return input

---methods for working with positioning inside of a tile
local tileFuncs = {}

--[[
    varying methods for checking if our rect is overlapping or contained with a tile
--]]

---is the rect fully inside the tile
---@param rect Rect
---@param tile Vec2|table
---@return boolean true if the rect is fully inside the tile, false otherwise
function tileFuncs.ContainedRect(rect, tile)
    local x = rect.X
    local y = rect.Y
    local w = rect.Width
    local h = rect.Height
    local tx = tile.X * 64
    local ty = tile.Y * 64
    return x > tx and x + w < tx + 64 and y > ty and y + h < ty + 64
end

---is the center inside the tile
---@param rect Rect
---@param tile Vec2|table
---@return boolean true if the center is inside the tile, false otherwise
function tileFuncs.ContainedCenter(rect, tile)
    local x = rect.Center.X
    local y = rect.Center.Y
    local tx = tile.X * 64
    local ty = tile.Y * 64
    return x > tx and x < tx + 63 and y > ty and y < ty + 63
end

---is the rect centered in the tile
---@param rect Rect
---@param tile Vec2|table
---@return boolean true if the rect is centered in the tile, false otherwise
function tileFuncs.ContainedRectCentered(rect, tile)
    local x = rect.X
    local y = rect.Y
    local w = rect.Width
    local h = rect.Height
    local tx = tile.X * 64
    local ty = tile.Y * 64
    return x + 4 > tx and x + w + 12 < tx + 64 and y - 12 > ty and y + h + 12 < ty + 64
end

--[[
    varying methods for figuring out the offset from a vec to the tile
--]]

---offset direction to center the player box horizontally in the tile
---@param vec Vec2
---@param tile Vec2|table
---@return integer 1 if the x position is to the left of the center, -1 if to the right of the center, 0 if centered
function tileFuncs.CenteredWithinTileWidth(vec, tile)
    local tleft = tile.X * 64
    local tright = (tile.X + 1) * 64
    local vleft = vec.X + 8 - 4
    local vright = vec.X + 48 + 8 + 4
    if vleft < tleft then
        return 1
    end
    if vright > tright then
        return -1
    end
    return 0
end

---offset direction to center the player box vertically in the tile
---@param vec Vec2
---@param tile Vec2|table
---@return integer 1 if the y position is above the center, -1 if below the center, 0 if centered
function tileFuncs.CenteredWithinTileHeight(vec, tile)
    local ttop = tile.Y * 64
    local tbottom = (tile.Y + 1) * 64
    local vtop = vec.Y - 12
    local vbottom = vec.Y + 32 + 12
    if vtop < ttop then
        return 1
    end
    if vbottom > tbottom then
        return -1
    end
    return 0
end

---offset direction to have the player box fully inside the tile horizontally
---@param vec Vec2
---@param tile Vec2|table
---@return integer 1 if the player box is to the left of the center, -1 if to the right of the center, 0 if centered
function tileFuncs.FullyWithinTileWidth(vec, tile)
    -- |----| vl, vr
    --   |==============| tl, tr
    -- if vl < tl we are below the lower bound and need to go right
    -- if vr > tr we are above the upper bound and need to go left
    local tleft = tile.X * 64
    local tright = (tile.X + 1) * 64
    local vleft = vec.X + 8
    local vright = vec.X + 48 + 8
    if vleft < tleft then
        return 1
    end
    if vright > tright then
        return -1
    end
    return 0
end

---offset direction to have the player box fully inside the tile vertically
---@param vec Vec2
---@param tile Vec2|table
---@return integer 1 if the player box is above the center, -1 if below the center, 0 if centered
function tileFuncs.FullyWithinTileHeight(vec, tile)
    local ttop = tile.Y * 64
    local tbottom = (tile.Y + 1) * 64
    local vtop = vec.Y
    local vbottom = vec.Y + 32
    if vtop < ttop then
        return 1
    end
    if vbottom > tbottom then
        return -1
    end
    return 0
end

---check if the x coordinate of the vector is within the tile bound
---@param vec Vec2
---@param tile Vec2|table
---@return integer 1 if the x position is to the left of the tile, -1 if to the right of the tile, 0 if within the tile
function tileFuncs.VecWithinTileWidth(vec, tile)
    -- |----| vl, vr
    --   |==============| tl, tr
    -- if vl < tl we are below the lower bound and need to go right
    -- if vr > tr we are above the upper bound and need to go left
    local tleft = tile.X * 64
    local tright = (tile.X + 1) * 64 - 1
    if vec.X < tleft then
        return 1
    end
    if vec.X > tright then
        return -1
    end
    return 0
end

---check if the y coordinate of the vector within the tile bound
---@param vec Vec2
---@param tile Vec2|table
---@return integer 1 if the y position is above the tile, -1 if below the tile, 0 if within the tile
function tileFuncs.VecWithinTileHeight(vec, tile)
    -- |----| vl, vr
    --   |==============| tl, tr
    -- if vl < tl we are below the lower bound and need to go right
    -- if vr > tr we are above the upper bound and need to go left
    local ttop = tile.Y * 64
    local tbottom = (tile.Y + 1) * 64 - 1
    if vec.Y < ttop then
        return 1
    end
    if vec.Y > tbottom then
        return -1
    end
    return 0
end

---returns the center of the players bounding box
---@return Vec2 the center of the players bounding box
function tileFuncs.PlayerBBoxCenter()
    local c = Game1.player:GetBoundingBox().Center
    return Vector2(c.X, c.Y)
end

---returns the position of the player
---@return Vec2 the position of the player
function tileFuncs.PlayerPosition()
    return Game1.player.Position
end

return tileFuncs

---@meta

---@class Vec2
---@field X number
---@field Y number
local Vec2 = {}

---@param X number
---@param Y number
---@return Vec2
function Vector2(X, Y) end

---@class Rect
---@field X number
---@field Y number
---@field Width number
---@field Height number
---@field Center Vec2
local Rect = {}

---@param X number
---@param Y number
---@param Width number
---@param Height number
---@return Rect
function Rectangle(X, Y, Width, Height) end

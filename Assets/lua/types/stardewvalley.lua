---@meta

---@class Item
---@field ParentSheetIndex number @the item ID
---@field Stack number @the number of items in the stack
---@field Name string @the name of the item
local Item = {}

---@class NetString
---@field Value string @the value of the NetString
local NetString = {}

---@class NetInt
---@field Value number @the value of the NetInt
local NetInt = {}

---@class Farmer
---@field CurrentItem Item|nil
---@field CurrentTool Item|nil
---@field FacingDirection number
---@field skin NetInt
---@field hair NetInt
---@field shirt NetString
---@field pants NetString
---@field accessory NetInt
---@field Tile Vec2
---@field Stamina number
local Farmer = {}

---@class GameLocation
---@field Name string @the name of the location
---@field terrainFeatures any
local GameLocation = {}

---@class Game1
---@field player Farmer @the current player object
---@field currentLocation GameLocation @the current location object
---@field tileSize number @the size of a tile in pixels
---@field locations GameLocation[] @list of all locations in the game
Game1 = {}

--- Wraps writing to the GamePadInputQueue

local KeyboardAndMouse = {}
KeyboardAndMouse.__index = KeyboardAndMouse

---creates a new keyboard and mouse input object
---@return KeyboardAndMouse @new keyboard and mouse input object
function KeyboardAndMouse.new()
    local self = setmetatable({ keys = {}, mouse = {} }, KeyboardAndMouse) --[[@as KeyboardAndMouse]]
    return self
end

---clears current keys and mouse buttons
function KeyboardAndMouse:clear()
    self.keys = {}
    self.mouse = {}
end

---press a key
---@param key Keys|number the key to press
function KeyboardAndMouse:press_key(key)
    self.keys[key] = true
end

---release a key
---@param key Keys|number the key to release
function KeyboardAndMouse:release_key(key)
    self.keys[key] = false
end

---press multiple keys
---@param keys (Keys|number)[] the keys to press
function KeyboardAndMouse:press_keys(keys)
    for _, key in ipairs(keys) do
        self:press_key(key)
    end
end

---press the left mouse button
function KeyboardAndMouse:mouse_left_down()
    self.mouse.left = true
end

---release the left mouse button
function KeyboardAndMouse:mouse_left_up()
    self.mouse.left = false
end

---press the right mouse button
function KeyboardAndMouse:mouse_right_down()
    self.mouse.right = true
end

---release the right mouse button
function KeyboardAndMouse:mouse_right_up()
    self.mouse.right = false
end

---set the mouse position
---@param x number the x position of the mouse
---@param y number the y position of the mouse
function KeyboardAndMouse:mouse_position(x, y)
    self.mouse.x = x
    self.mouse.y = y
end

---pushes current keys and mouse buttons to the input queue and clears them
function KeyboardAndMouse:push()
    local keyboard = {}
    for k, v in pairs(self.keys) do
        if v then
            table.insert(keyboard, k)
        end
    end
    interface:AddKeyboardMouseInput({ keyboard = keyboard, mouse = self.mouse })
    self:clear()
end

return KeyboardAndMouse

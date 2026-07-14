--- Wraps writing to the GamePadInputQueue

local Gamepad = {}
Gamepad.__index = Gamepad

---creates a new gamepad for player index (0 indexed so p1 = 0, p2 = 1, etc)
---@param index number the player index for this gamepad (0 indexed so p1 = 0, p2 = 1, etc)
---@return Gamepad new gamepad object
function Gamepad.new(index)
    local self = setmetatable({}, Gamepad) --[[@as Gamepad]]
    self.buttons = {}
    self.index = index or 1
    return self
end

---gets the number of inputs currently staged in the queue for this gamepad
---@return number the number of inputs currently staged
function Gamepad:num_staged()
    return GamePadInputQueue.Queues[self.index].Count
end

---clears current buttons
function Gamepad:clear()
    self.buttons = {}
end

---press up
function Gamepad:up()
    self.buttons.up = true
end

---press down
function Gamepad:down()
    self.buttons.down = true
end

---press left
function Gamepad:left()
    self.buttons.left = true
end

---press right
function Gamepad:right()
    self.buttons.right = true
end

---press 'a' button
function Gamepad:a()
    self.buttons.a = true
end

---press 'b' button
function Gamepad:b()
    self.buttons.b = true
end

---press 'x' button
function Gamepad:x()
    self.buttons.x = true
end

---press 'y' button
function Gamepad:y()
    self.buttons.y = true
end

---press start button
function Gamepad:start()
    self.buttons.start = true
end

---press select button
function Gamepad:select()
    self.buttons.select = true
end

---press left trigger
function Gamepad:lt()
    self.buttons.lt = true
end

---press right trigger
function Gamepad:rt()
    self.buttons.rt = true
end

---press left bumper
function Gamepad:zl()
    self.buttons.zl = true
end

---press right bumper
function Gamepad:zr()
    self.buttons.zr = true
end

---set the analog stick values, x and y should be between -1 and 1
---@param x number the x value for the right stick, between -1 and 1
---@param y number the y value for the right stick, between -1 and 1
function Gamepad:analog(x, y)
    self.buttons.rx = x
    self.buttons.ry = y
end

---push the current buttons to the input queue and clears state
function Gamepad:push()
    interface:AddGamePadInput(self.index, self.buttons)
    self:clear()
end

return Gamepad

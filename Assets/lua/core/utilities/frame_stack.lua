---Desc: frame stack utility
---defines a utility stack class for working with frames
---allows pushing and popping frames from the stack
---and fast resetting the game to the frame on top of the stack
local Stack = require('core.collections.stack')
local fs = {
    stack = Stack.new()
}

---get the last frame on the stack
---@return number last frame number
function fs.last()
    return fs.stack:peek()
end

---print the frame stack
function fs.print()
    fs.stack:print()
end

---push a frame to the stack
---@param f number|nil frame to push (defaults current frame)
---@return number pushed frame number added to the stack
function fs.push(f)
    if f == nil then
        f = interface:GetCurrentFrame()
    end
    if fs.last() ~= f then
        fs.stack:push(f)
    end
    return f ---type:ignore
end

---pop a frame from the stack
---@return number|nil popped frame number removed from the stack
function fs.pop()
    if fs.last() == nil then
        return nil
    end
    return fs.stack:pop()
end

---clear the frame stack
function fs.clear()
    fs.stack = Stack.new()
end

---fast reset the game to the last frame on the stack (NON-BLOCKING)
function fs.rw()
    if fs.last() ~= nil then
        freset(fs.last())
    end
end

---fast reset the game to the last frame on the stack (BLOCKING)
function fs.brw()
    if fs.last() ~= nil then
        bfreset(fs.last())
    end
end

return fs

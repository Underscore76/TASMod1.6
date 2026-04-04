--- A simple stack implementation in Lua

local Stack = {}
Stack.__index = Stack

---initialize a new stack
---@return table new stack object
function Stack.new()
    return setmetatable({ data = {} }, Stack)
end

---push a value onto the stack
---@param value any
function Stack:push(value)
    table.insert(self.data, value)
end

---pull the top value from the stack without removing it
function Stack:peek()
    return self.data[#self.data]
end

---pop the top value off the stack
function Stack:pop()
    return table.remove(self.data)
end

---check if the stack is empty
---@return boolean true if the stack is empty, false otherwise
function Stack:empty()
    return #self.data == 0
end

---check the size of the stack
---@return number the number of items in the stack
function Stack:size()
    return #self.data
end

---prints the stack contents to the console
function Stack:print()
    for i, v in ipairs(self.data) do
        printf("%d:\t%s", i, tostring(v))
    end
end

return Stack

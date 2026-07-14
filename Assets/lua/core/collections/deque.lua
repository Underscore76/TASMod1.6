--- A simple double-ended queue implementation in Lua

local Deque = {}
Deque.__index = Deque

---initialize a new queue
---@return table new queue object
function Deque.new()
    return setmetatable({ first = 0, last = -1 }, Deque)
end

---push a value to the left end of the queue
---@param value any
function Deque:pushleft(value)
    local first = self.first - 1
    self.first = first
    self[first] = value
end

---push a value to the right end of the queue
---@param value any
function Deque:pushright(value)
    local last = self.last + 1
    self.last = last
    self[last] = value
end

---pop a value from the left end of the queue
---@return any value popped from the left end of the queue
function Deque:popleft()
    local first = self.first
    if first > self.last then error("queue is empty") end
    local value = self[first]
    self[first] = nil -- to allow garbage collection
    self.first = first + 1
    return value
end

---pop a value from the right end of the queue
---@return any value popped from the right end of the queue
function Deque:popright()
    local last = self.last
    if self.first > last then error("queue is empty") end
    local value = self[last]
    self[last] = nil -- to allow garbage collection
    self.last = last - 1
    return value
end

---check if the queue is empty
---@return boolean true if the queue is empty, false otherwise
function Deque:empty()
    return self.first > self.last
end

---check the size of the queue
---@return number the number of items in the queue
function Deque:size()
    return self.last - self.first + 1
end

return Deque

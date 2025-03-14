﻿--- NOTE: this file pollutes the global namespace with common named elements

local engine = require('core.engine')
local frame_stack = require('core.utilities.frame_stack')

--
-- functions for working with the game engine interface
--

---advance the game by one frame
---@param input any
function advance(input)
    engine.advance(input)
end

---wait for the game to halt logic
---@param max_frames number|nil
function halt(max_frames)
    engine.halt(max_frames)
end

---advance the game by one frame
---@param frame number
function reset(frame)
    engine.reset(frame)
end

---fast reset back to the specified frame
---@param frame number
function freset(frame)
    engine.fast_reset(frame)
end

---blocking reset back to the specified frame
---@param frame number
function breset(frame)
    engine.blocking_reset(frame)
end

---blocking fast reset back to the specified frame
---@param frame number
function bfreset(frame)
    engine.blocking_fast_reset(frame)
end

---blocking load of a file
---@param file string
function load(file)
    engine.blocking_load(file)
end

---blocking fast load of a file
---@param file string
function bfload(file)
    engine.blocking_fast_load(file)
end

---returns the current game frame
---@return number current frame number
function current_frame()
    return interface:GetCurrentFrame()
end

---returns the current game1 random
---@return Random @current game1 random
function game1_random()
    return interface:GetGame1Random()
end

---copies the random object
---@param random Random
---@return Random @copied random object
function copy_random(random)
    return interface:CopyRandom(random)
end

--
-- functions for manipulating/working with the global framestack
--

---push the current frame to the global framestack
---@return number current frame number
function gcf()
    return frame_stack.push(nil)
end

---print the global frame stack
function pgcf()
    frame_stack.print()
end

---push a specific frame to the global frame stack
function push(f)
    frame_stack.push(f)
end

---pop a frame from the global frame stack
---@return number|nil popped frame number
function pop()
    return frame_stack.pop()
end

---fast reset back to the top of the global frame stack (NON-BLOCKING).
---for general resets this will work significantly faster than brw()
function rw()
    frame_stack.rw()
end

---fast reset back to the top of the global frame stack (BLOCKING).
---this allows accurate syncing in a lua script to continue operating post-reset
function brw()
    frame_stack.brw()
end

---clear the global frame stack
function frame_stack_clear()
    frame_stack._X = {}
end

---gets the current real time
---@return System.DateTime @current real time
function real_time()
    return DateTimeOffset.FromUnixTimeSeconds(os.time()):ToLocalTime()
end

-- stashing a raw mechanism to check if a file exists
-- function file_exists(name)
--     local f = io.open(name, "r")
--     return f ~= nil and io.close(f)
-- end

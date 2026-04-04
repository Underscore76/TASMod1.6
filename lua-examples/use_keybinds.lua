local keybinds = require("core.keybinds")

-- example for adding a custom keybind,
-- here we bind the key 'j' to an inlined function definition that animation cancels 10 times
keybinds.add(Keys.J,
    function()
        for i = 1, 10 do
            advance({ keyboard = { Keys.C, Keys.RightShift, Keys.R, Keys.Delete } })
        end
    end
)

-- we can also take an existing function and bind it to a key, and we can also add a description for the keybind
local function z()
    for i = 1, 10 do
        advance({ keyboard = { Keys.C, Keys.RightShift, Keys.R, Keys.Delete } })
    end
end
keybinds.add(Keys.Z, z, "press z")

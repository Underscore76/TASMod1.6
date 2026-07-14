--- basic usage of the startup script for creating a character
local startup = require("core.scripts.startup")

local function launch()
    -- character setup
    -- NOTE: change to modify name/farm name/favorite thing for new game creation
    startup.set_name('Underscore')
    startup.set_farm_name('Casual')
    startup.set_favorite('Going Fast')

    -- farm startup params
    startup.set_skip(true)
    startup.set_sex('female')
    startup.set_farm('Forest')
    -- NOTE: these numbers can be wonky due to issues with how SDV actually numbers them
    -- there may be gaps in the numbers, some numbers might be negative indexed
    -- its confusing... Generally it's the number shown in the menu - 1.
    -- if you try and number that doesn't work, it will just attempt to spin for a while before giving up and moving on
    startup.set_hair(8)                   -- there are missing hair styles
    startup.set_shirt("1105")
    startup.set_acc(0)                    -- 0 is the beard in this case, -1 is no accessory...
    startup.set_skin(2)                   -- 0 indexed and as expected indexing
    startup.set_pant("0")                 -- 0 indexed and as expected indexing

    startup.set_eye_color(nil, nil, 89)   -- these are the hsv sliders, 0-100% on each
    startup.set_hair_color(nil, nil, 24)  -- these are the hsv sliders, 0-100% on each
    startup.set_pants_color(nil, nil, 21) -- these are the hsv sliders, 0-100% on each

    -- this is an example startup script that will launch you into the start of day 1
    startup.run()
    startup.click_ok()

    advance() -- get past the initial launch
    halt()    -- waits until past the save (generally shouldn't need to manually halt outside this case of overnight load)

    advance() -- push into the day1 load
    -- the game will be paused at this point, so you can do whatever you want
end

launch()

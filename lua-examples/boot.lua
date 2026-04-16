-- boot.lua: this maybe should be called post_startup.lua, this runs after the game has fully initialized and the lua engine is setup
-- the idea/my primary use cases have been around automating the game post launch to get a certain state

-- This example runs a boot loop that finds a set of initial conditions that have really good first day spring onions.
-- The random state at game launch influences some pieces of the first day rng state/updates outside of the selection of
-- the actual game seed, so we both have the capability to manipulate that without impacting our selection of game seed.
-- Because this process requires a TON of resets, small memory leaks in the core game can lead it to an eventual crash and
-- so we run this in a loop where we capture the best seed/offset combination we find and then kill the instance and restart it to clear memory

local fileio = require('core.fileio')

local file = Constants.ScriptsPath .. '/' .. 'FrameRNG.dat'

-- write a default search setup if one doesn't exist
if not fileio.exists(file) then
    fileio.WriteFileTable(file, {
        iterations = 100, -- how many seeds to test before killing the instance
        curr_seed = 0,    -- the starting seed to test
        best_count = -1,  -- best number of spring onions found so far
        best_seed = -1,   -- the seed that resulted in the best count
    })
end

local search_setup = fileio.ReadFileTable(file)

function forest_spring_onions()
    local c = 0
    for _, v in dict_items(Game1.locations[21].terrainFeatures) do
        if v:GetType().Name == "HoeDirt" then
            c = c + 1
        end
    end
    return c
end

bfload('after_day1_new_day') -- blocking fast load a file that is just past the point of day 1 update/where spring onions are generated.
fs_push()                    -- store the current frame

for i = 1, search_setup['iterations'] do
    Controller.State.Frame0RandomSeed = search_setup['curr_seed']
    brw() -- reset to the stored frame, but with the new startup seed
    local c = forest_spring_onions()
    if c > search_setup['best_count'] then
        search_setup['best_count'] = c
        search_setup['best_seed'] = search_setup['curr_seed']
    end
    printf("Seed: %d => %d onions\tBest: %d onions on seed %d",
        search_setup['curr_seed'], c,
        search_setup['best_count'], search_setup['best_seed'])
    search_setup['curr_seed'] = search_setup['curr_seed'] + 1
    fileio.WriteFileTable(file, search_setup)
end
interface:Kill() -- kill the instance to clear memory and allow us to restart fresh with the next set of seeds

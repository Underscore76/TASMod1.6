---script to create a new save state file
---wrapper around the newgame console command
local newgame = {
    seed = 0,
    language = "en",
    file_prefix = "tmp",
}

---set the seed
---@param seed number the seed to use for the new game (0-2^31-1)
function newgame.set_seed(seed)
    newgame.seed = seed
end

---set the language
---@param language string the language to use for the new game (en, fr, es, de, pt, ru, ja, zh)
function newgame.set_language(language)
    -- validate the language choice
    local options = { "en", "ja", "ru", "zh", "pt", "es", "de", "th", "fr", "ko", "it", "tr", "hu" }
    for _, option in ipairs(options) do
        if language == option then
            newgame.language = language
            return
        end
    end
    error(string.format("invalid language choice %s, must be one of %s", language, table.concat(options, ", ")))
end

---set the file prefix
---@param file_prefix string the filename prefix to use for the new game
function newgame.set_file_prefix(file_prefix)
    newgame.file_prefix = file_prefix
end

---run the new game script
function newgame.run()
    exec(string.format("newgame %d %s %s", newgame.seed, newgame.language, newgame.file_prefix))
end

return newgame

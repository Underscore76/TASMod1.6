-- shows an example of how to generate a new game file
local newgame = require("core.scripts.newgame")

local function gen_file(seed, language, filePrefix)
    newgame.set_seed(seed)
    newgame.set_language(language)
    newgame.set_file_prefix(filePrefix)
    newgame.run()
end

gen_file(1234, "en", "gaming")

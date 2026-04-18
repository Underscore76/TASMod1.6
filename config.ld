-- ldoc configuration file
file = "Assets/lua"
file = {
    "Assets/lua",
    exclude = {
        "Assets/lua/types",
        "Assets/lua/core/data/click_maps.lua"
    }
}
project = "TASMod 1.6"
dir = "docs/lua"
format="markdown"
description = [[
    TAS Mod 1.6 Lua Documentation
]]
title = "TAS Mod Lua Docs"
examples = {
    "lua-examples"
}
readme = "README.md"
style="!new"
kind_names = {
   topic = "Readme",
}
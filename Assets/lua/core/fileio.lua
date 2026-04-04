--- fileio - utility functions for reading/writing files
local fileio = {}

---checks if file exists
---@param name string the name of the file
---@return boolean whether the file exists or not
function fileio.Exists(name)
    local f = io.open(name, "r")
    if f ~= nil then
        io.close(f)
        return true
    else
        return false
    end
end

---reads lines from a passed file into a table array
---@param fname string the filename to read from
---@return table a table that is an array of the lines in the file
function fileio.ReadFile(fname)
    local lines = {}
    -- read the lines in table 'lines'
    if not fileio.Exists(fname) then
        return lines
    end
    for line in io.lines(fname) do
        table.insert(lines, line)
    end
    return lines
end

---writes lines from a table array to a passed file
---@param fname string the filename to write to
---@param lines table a table that is an array of the lines to write
function fileio.WriteFile(fname, lines)
    io.output(fname)
    for i, line in ipairs(lines) do
        io.write(line .. "\n")
    end
    io.flush()
    io.close()
end

---reads a file into a table of key-value pairs (simple number/string pairs, not nested)
---strings that represent numbers will be coerced to number type
---@param fname string the filename to read from
---@return table a table of key-value pairs read from the file
function fileio.ReadFileTable(fname)
    local lines = fileio.ReadFile(fname)
    local tab = {}
    for i, line in ipairs(lines) do
        local tokens = string.split(line, ':')
        local key = tokens[1]
        local value = tonumber(tokens[2])
        if value ~= nil then
            tab[key] = value
        else
            tab[key] = tokens[2]
        end
    end
    return tab
end

---writes a table of key-value pairs to a file (simple number/string pairs, not nested)
---@param fname string the filename to write to
---@param tab table a table of key-value pairs to write
function fileio.WriteFileTable(fname, tab)
    local lines = {}
    for k, v in pairs(tab) do
        table.insert(lines, string.format("%s:%s", tostring(k), tostring(v)))
    end
    fileio.WriteFile(fname, lines)
end

return fileio

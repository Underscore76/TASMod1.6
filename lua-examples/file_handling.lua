local fileio = require('core.fileio')

-- Constants.ScriptsPath is mapped to StardewTAS1.6/Scripts directory
local testfile = Constants.ScriptsPath .. '/' .. 'test.dat'

-- read some random data from a file, if it doesn't exist it will just return an empty table
local setup = fileio.read_file_table(testfile)
print(setup)

-- modify the data
setup['test'] = 'hello world'
setup['number'] = math.random()

-- write the modified data back to the file
fileio.write_file_table(testfile, setup)

-- read the data back from the file to verify it was written correctly
local newsetup = fileio.read_file_table(testfile)
print(newsetup)

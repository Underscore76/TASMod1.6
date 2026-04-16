---@meta

---@class FrameState
---@field keyboardState any @keyboard state for the frame
---@field mouseState any @mouse state for the frame
local FrameState = {}

---@class StateList:{ [integer]: FrameState }
---@field Count number @number of frames in the list
local StateList = {}

---@class SaveState
---@field Prefix string @prefix for the save state
---@field Seed number @seed for the save state
---@field Count number @number of frames in the save state
---@field ReRecords number @number of re-records for the save state
---@field FrameStates StateList @list of frame states
---@field Frame0RandomSeed number @the random seed at the start of the save state
---@field Frame0RandomIndex number @the random index at the start of the save state
local SaveState = {}

---@class Console
---@field Open fun(self: Console): nil @opens the console
---@field Close fun(self: Console): nil @closes the console
---@field RunCommand fun(self: Console, command: string): nil @runs a command
---@field WriteToRandomFile fun(self: Console): nil @writes content to a random file
---@field RunOnNextUpdate fun(self: Console, code: string): nil @runs command on the next frame
---@field Commands table @table of console commands
---@field openHeightTarget number @target height for the console (0 to 1)
---@field ShowWarnings boolean @whether to show warnings in the console
---@field ShowErrors boolean @whether to show errors in the console
---@field DebugMode boolean @whether the console is in debug mode
local Console = {}

---@class TASMouseState
---@field X number @mouse x position
---@field Y number @mouse y position
---@field LeftMouseClicked boolean @whether the left mouse button was clicked
---@field RightMouseClicked boolean @whether the right mouse button was clicked
local TASMouseState = {}

---@class Controller
---@field State SaveState
---@field Console Console
---@field LastFrameMouse fun(): TASMouseState @returns the mouse state from the last frame
Controller = {}

---@class TASDateTime
---@field CurrentFrame number @current frame of the run
---@field CurrentGameTime any @current game time
---@field UtcNow any @current UTC time
TASDateTime = {}

---@enum TASView
TASView = {
    Base = 0,
    Map = 1,
}

---@class interface
---@field HasStep boolean @whether there is an update ready
---@field WaitPrefix fun(self: interface): nil @sleep until the next update
---@field StepLogic fun(self: interface): nil @step the game logic forward one update
---@field WaitPostfix fun(self: interface): nil @clear the update timer
---@field AdvanceFrame fun(self: interface, input: table|nil): nil @advance the game by one frame with the specified input
---@field ResetGame fun(self: interface, frame: number): nil @reset the game to the specified frame
---@field FastResetGame fun(self: interface, frame: number): nil @fast reset the game to the specified frame
---@field BlockResetGame fun(self: interface, frame: number): nil @blocking reset the game to the specified frame
---@field BlockFastResetGame fun(self: interface, frame: number): nil @blocking fast reset the game to the specified frame
---@field BlockLoad fun(self: interface, file: string): nil @blocking load of a file
---@field BlockFastLoad fun(self: interface, file: string): nil @blocking fast load of a file
---@field SpawnMineShaft fun(self: interface, level: number): any @spawn a mine shaft floor at specified level
---@field TrySpawnChestFloorItem fun(self: interface, menuFrames: number, unpausedRandomOffset: number, tryCursor: boolean): any @try to estimate the upcoming chest floor item
---@field AddGamePadInput fun(self: interface, index: number, buttons: table): nil @add gamepad input for the specified player
---@field AddKeyboardMouseInput fun(self: interface, input: table): nil @add keyboard and mouse input
---@field GetClay fun(self: interface): Vec2[] @get the current clay for the player
---@field GetGame1Random fun(self: interface): Random @get the current game1 random
---@field GetCurrentFrame fun(self: interface): number @get the current game frame
---@field CopyRandom fun(self: interface, random: Random): Random @copy a random object
---@field GetClickableObjects fun(self: interface): any @get the current clickable objects on screen
---@field AddKeyBind fun(self: interface, key: Keys|number, funcName: string, func: function): nil @add a key bind for the console
---@field RemoveKeyBind fun(self: interface, key: Keys|number): nil @remove a key bind for the console
---@field PrintKeyBinds fun(self: interface): nil @print the current key binds to the console
---@field ClearKeyBinds fun(self: interface): nil @clear all key binds
---@field NextView fun(self: interface): any @swap to the next view type
---@field SetView fun(self: interface, view: TASView): nil @set the current view
---@field ResetView fun(self: interface): nil @reset the view to the base
---@field ViewLocation fun(self: interface, location: GameLocation): nil @set the view location to the specified coordinates
---@field Print fun(self: interface, message: string): nil @print a message to the console
---@field Kill fun(self: interface): nil @kill the game instance
interface = {}

---@enum TASMode
TASMode = {
    Edit = 0,
    Replay = 1,
}

---@param obj any @object to get the value from
---@param field string @field name to get the value of
---@return any
function GetValue(obj, field) end

---@param code string @C# code to run
---@return any
function RunCS(code) end

---@class GamepadButtons
---@field up? boolean
---@field down? boolean
---@field left? boolean
---@field right? boolean
---@field a? boolean
---@field b? boolean
---@field x? boolean
---@field y? boolean
---@field start? boolean
---@field select? boolean
---@field lt? boolean
---@field rt? boolean
---@field zl? boolean
---@field zr? boolean
---@field rx? number
---@field ry? number

---@class Gamepad
---@field buttons GamepadButtons
---@field index integer
---@field num_staged fun(self: Gamepad): integer @number of staged button presses
---@field clear fun(self: Gamepad): nil @clear staged button presses
---@field up fun(self: Gamepad): nil @press up button
---@field down fun(self: Gamepad): nil @press down button
---@field left fun(self: Gamepad): nil @press left button
---@field right fun(self: Gamepad): nil @press right button
---@field a fun(self: Gamepad): nil @press a button
---@field b fun(self: Gamepad): nil @press b button
---@field x fun(self: Gamepad): nil @press x button
---@field y fun(self: Gamepad): nil @press y button
---@field start fun(self: Gamepad): nil @press start button
---@field select fun(self: Gamepad): nil @press select button
---@field lt fun(self: Gamepad): nil @press left trigger
---@field rt fun(self: Gamepad): nil @press right trigger
---@field zl fun(self: Gamepad): nil @press left bumper
---@field zr fun(self: Gamepad): nil @press right bumper
---@field analog fun(self: Gamepad, x: number, y: number): nil @set the analog stick values, x and y should be between -1 and 1
---@field push fun(self: Gamepad): nil @push the current buttons to the input queue and clear state
local Gamepad = {}

---@class GamepadModule
---@field new fun(): Gamepad

---@class KeyboardAndMouse
---@field keys table<Keys|number, boolean>
---@field mouse table<string, number|boolean>
---@field clear fun(self: KeyboardAndMouse) @clears current keys and mouse buttons
---@field press_key fun(self: KeyboardAndMouse, key: Keys|number) @press a key
---@field release_key fun(self: KeyboardAndMouse, key: Keys|number) @release a key
---@field press_keys fun(self: KeyboardAndMouse, keys: (Keys|number)[]) @press multiple keys
---@field mouse_left_down fun(self: KeyboardAndMouse) @press the left mouse button
---@field mouse_left_up fun(self: KeyboardAndMouse) @release the left mouse button
---@field mouse_right_down fun(self: KeyboardAndMouse) @press the right mouse button
---@field mouse_right_up fun(self: KeyboardAndMouse) @release the right mouse button
---@field mouse_position fun(self: KeyboardAndMouse, x: number, y: number) @set the mouse position
---@field push fun(self: KeyboardAndMouse) @pushes current keys and mouse buttons to the input queue and clears state
local KeyboardAndMouse = {}

---@class KeyboardAndMouseModule
---@field new fun(): KeyboardAndMouse

---@class csQueue
---@field Count number @number of items in the queue

---@class GamePadInputQueue
---@field Queues csQueue[]
---@field SetManualFrameFunction fun(index: number, func: function, name: string, description: string|nil): nil
GamePadInputQueue = {}

---@class KeyboardMouseInputQueue
---@field Queue csQueue
---@field PushNamedFunction fun(name: string): nil
---@field PushFunction fun(func: function, name: string, description: string|nil): nil
KeyboardMouseInputQueue = {}

---@class LuaOverlay
---@field AddButton fun(name: string, onClick: function): nil
---@field AddData fun(name: string, func: function): nil
LuaOverlay = {}

---@class LuaFunctionRegistry
---@field RegisterFunction fun(name: string, func: function, description: string|nil): nil
---@field Clear fun(): nil
LuaFunctionRegistry = {}

---@class Constants
---@field ScriptsPath string @path to the scripts folder
---@field SavesPath string @path to the saves folder
---@field SaveStatePath string @path to the save states folder
---@field ExportsPath string @path to exports folder
---@field ScreenshotPath string @path to screenshots folder
Constants = {}

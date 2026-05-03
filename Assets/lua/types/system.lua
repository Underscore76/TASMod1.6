---@meta

---@class Random
local Random = {}

---@overload fun(): number @returns a random integer between 0 (inclusive)
---@overload fun(max: number): number @returns a random integer between 0 (inclusive) and max (exclusive)
---@param min number @minimum value (inclusive)
---@param max number @maximum value (exclusive)
---@return number @returns a random integer between min (inclusive) and max (exclusive)
function Random:Next(min, max) end

---@return number @returns a random integer between 0 and 1
function Random:NextDouble() end

---@class DateTimeOffset
---@field FromUnixTimeSeconds fun(seconds: number): DateTimeOffset @creates a DateTimeOffset from the given Unix time in seconds
---@field ToLocalTime fun(self: DateTimeOffset): DateTimeOffset @converts the DateTimeOffset to local time
DateTimeOffset = {}

---@class DateTime
---@field UtcNow any @current TAS UTC time
DateTime = {}

---@enum GCCollectionMode
GCCollectionMode = {
    Default = 0,
    Forced = 1,
    Optimized = 2,
    Background = 3,
}

---@class GC
---@field Collect fun(generation: number, mode: GCCollectionMode, blocking: boolean, compacting: boolean): nil @forces garbage collection
GC = {}

---@class SystemType
---@field Name string @the name of the type
SystemType = {}

---@class Enumerator<T>
---@field Current any @the current item in the enumeration
---@field MoveNext fun(self: Enumerator): boolean @moves to the next item in the enumeration
Enumerator = {}

---@class List<T>
---@field Count number @the number of items in the list
---@field Add fun(self: List, item: T): nil @adds an item to the list
---@field Clear fun(self: List): nil @clears the list
---@field Get fun(self: List, index: number): T @gets the item at the specified index
---@field GetEnumerator fun(self: List): Enumerator<T> @gets an enumerator for the list
List = {}

---@class Pair<T1, T2>
---@field Item1 T1 @the first item in the tuple
---@field Item2 T2 @the second item in the tuple
Pair = {}

---@class Dictionary<TKey, TValue>
---@field Get fun(self: Dictionary, key: TKey): TValue @gets the value associated with a key
---@field ContainsKey fun(self: Dictionary, key: TKey): boolean @checks if the dictionary contains a key
---@field Remove fun(self: Dictionary, key: TKey): nil @removes a key from the dictionary
Dictionary = {}

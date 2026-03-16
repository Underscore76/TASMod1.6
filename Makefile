# Purpose: Makefile for building and running TASMod

.phony: run nettrace

# build and install the mod
build:
	dotnet build

release:
	dotnet build --property WarningLevel=0 --nologo -v q /clp:ErrorsOnly -c Release

# run smapi with the mod installed
run: build
	smapi

nettrace:
	dotnet-trace collect --name StardewModdingAPI --format speedscope
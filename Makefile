# Purpose: Makefile for building and running TASMod

.phony: run

# build and install the mod
build:
	dotnet build

# run smapi with the mod installed
run: build
	smapi
# Lights Out Cube App

This repository contains the source for the Lights Out Cube App — a 3D emulator and solver for the
physical Lights Out Cube toy (Tiger, late 1990s). The objective is to press the cube's buttons to 
turn all lights off.

## History
This code was started in 2005, thought lost on an old PC, but recently November 2025 found on a backup of the old PC.

### Why - more history
In around 2000 during, it may have even been in during the Y2K project, someone brought into work a Lights Out Cube toy. Eventually if you play with this cube you will end up with one light on in the centre (level 49 in this puzzle). The challenge was to work out a way to solve the puzzle for this state. A linear search through the solution space takes too long. But by using the symmetries of the puzzle I was able to create an almost instantaneous solver, see the source in LightsOutCubeSolver.cs to learn more. Having crafted a solver, one is forced to write an emulator to display the solution. Thus this app.

### XAML 3D - Why
Imagine the planning meeting at Redmond when someone suggested the next generation of Windows forms needs a 3D rendering engine built in. Engineer "If we don't do it someone else will and we will lose the desktop!", Manager "Good point well made, I'll raise a JIRA for it".
Unfortunatly the rest of the world largly ignored the possibility of 3D XAML.

### Finishing the APP in 2025
XAML is not my day job. Fortunatly Mr Copilot writes it as if he has been doing it for 20 years, or did he look at 20 years worth of XAML examples? He worked out things like animations and getting trackballs to work correctly and all the bits I was too lazy to do.

## Features
- 3D XAML viewport with interactive controls and animations
- Fast solver for cube puzzles
- Visual solution highlighting and press animation
- Unit tests for the solver

## Getting started

Prerequisites
- Visual Studio 2019+ with .NET Framework 4.8 workload installed
- Optional: NuGet restore enabled

Build and run
1. Clone the repo and open the solution in Visual Studio.
2. Restore NuGet packages (if any) and build the solution.
3. Run from Visual Studio (F5) or Start Without Debugging (Ctrl+F5).

## Project notes
- Targets: .NET Framework 4.8, C# 7.3
- UI/view-model separation is used: visual state lives in `CubeViewModel`.

## Contributing
- Fork => branch => PR. Include tests for bug fixes or new logic.
- Run tests locally before submitting a PR.

## Troubleshooting / Known issues
- Some puzzle entries (e.g. puzzles 29 and 46) have previously failed solver verification — 
these may be transcription issues in the puzzle XML. If you own the original toy, please verify these puzzles.

## People who should have had better things to do
[Turning the Lights Out in Three Dimensions.](https://scholar.rose-hulman.edu/cgi/viewcontent.cgi?article=1323&context=rhumj)
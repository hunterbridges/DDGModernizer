# DDGModernizer

Binary patcher for a modern Densha de GO!! experience

## Overview

This utility aims to provide some simple quality-of-life upgrades for PC versions of Densha de GO!!

It adds support for:
- Changing aspect ratio for 3D projections in the game (Does not affect UI)
- Modifying the draw distance

Versions supported:
- Densha de GO!! Professional 2
- Densha de GO!! Sanyo Shinkansen
- Densha de GO!! Final

## How it works

This utility makes a copy of the game EXE in a temporary folder, and patches certain bytes in the EXE to achieve
the 
## Usage

The application presents a simple dashboard UI:

![image](https://user-images.githubusercontent.com/775593/119245170-fb5b8900-bb2b-11eb-9590-d8b88a3e42ac.png)

First, select the game you wish to play from the dropdown. The program will attempt to locate the game EXE and game folder in the default locations. If it does not work, point the "EXE Path" to your game EXE, and the "Game Folder" to the game data folder (generally the folder which contains the EXE).

Next, you can choose which patches to enable. For the aspect ratio patch, the tool will attempt to read the configured screen settings for the game from the registry, and calculate the aspect parameters automatically.

## Acknowledgements

Thanks to the Unofficial Densha de GO!! discord for various tips and resources.

Thanks to @autotraintas for creating the [Auto Train TAS tool](http://autotraintas.hariko.com/)

Thanks to zarala for creating [dengo_tools](http://zarala.g2.xrea.com/soft/dengo_tools.zip), from which I derived the patch format

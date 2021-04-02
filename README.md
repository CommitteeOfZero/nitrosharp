# NitroSharp (codename Project Hoppy)

**NitroSharp** is an open-source reimplementation of **N2System**, a visual novel engine used in a number of games made by Nitroplus. The primary goal of the project is to make [Chaos;Head Noah](https://vndb.org/v22505), a console-exclusive game, fully playable on PC (Windows, Linux) and possibly other platforms (listed below). Support for other N2System-based games may come later.

## Status
NitroSharp is close to being feature-complete. You should be able to reach at least some of the endings in Chaos;Head Noah without crashes, if you're lucky. **Please do not expect NitroSharp to be a viable drop-in replacement for N2System just yet.**

* Do expect all sorts of bugs.
* Do not expect your saves to work after upgrading to a newer build of the engine or modifying the scripts.
* Do not use NitroSharp if you simply want to enjoy the game.

### Missing features:
* Support for the archive formats
  * AFS
  * NPA
* Decent, reliable save system
* Graphical effects
  * Motion blur
  * Lens (partially implemented)
  * Box blur (currently disabled)
* Fullscreen mode
* Auto mode
* Backlog voice replay
* Controller support (partially implemented)

### Supported games
* [x] Chaos;Head Noah
* [ ] Chaos;Head

### Supported platforms
The initial release will likely only support two major platforms:
* [x] Windows x64
  * Direct3D 11
  * Vulkan
* [ ] Linux x64
  * Vulkan

There is currently no way to produce a working Linux build.

It should be theoretically possible to add support for the following platforms:
* Windows x86
* Windows ARM64
* Linux ARM64
* macOS
* Android
* Nintendo Switch

## Building
### Required Software
- [.NET 5 SDK](https://dotnet.microsoft.com/download/dotnet/5.0)
- [PowerShell 7](https://github.com/PowerShell/PowerShell) (for AOT-compiled builds)
- Windows SDK might be required for producing Windows builds

Run
```
dotnet run --no-launch-profile --project ./src/NitroSharp.ShaderCompiler/NitroSharp.ShaderCompiler.csproj ./src/NitroSharp/Graphics/Shaders ./bin/obj/NitroSharp/Shaders.Generated
dotnet build NitroSharp.sln [-c Release]
```
OR

Run ``aot-build.ps1`` in PowerShell 7 to produce an [AOT-compiled](https://en.wikipedia.org/wiki/Ahead-of-time_compilation) build. Do not expect the build script to work in the old Windows PowerShell.

## How do I play the game?
There is currently no support for AFS and NPA archives, which means you will have to extract the game files first.
Before you start, make sure your game directory looks this way:

![image](https://user-images.githubusercontent.com/6377116/113422461-6e227080-93d5-11eb-86fa-56cee4e6e434.png)

1. Get the tools for extracting the archives [here](https://1drv.ms/u/s!Aryvcp_pUUGhjZ4gECiHRIxDYHD0yQ?e=y5SEwm).
2. Place them in the same directory as the game assets and run ``./extract.ps1`` in PowerShell.
The extracted files will be placed inside a folder named ``content``.
3. Edit ``Game.json`` so that ``dev.contentRoot`` points to the ``content`` directory. Avoid unescaped backslashes in the path.
4. Run ``Game.exe``.

## License
NitroSharp is licensed under the [MIT](https://github.com/CommitteeOfZero/nitrosharp/blob/meowster/LICENSE.TXT) license.
This project uses a number of third-party components. See [THIRDPARTY.md](https://github.com/CommitteeOfZero/nitrosharp/blob/meowster/THIRDPARTY.md) for more details.

## Legal disclaimers
* This project is non-commercial and is not affiliated with Nitroplus or MAGES. in any way.
* This is a black-box reimplementation: the code in this project was written based on observing the runtime behavior of the original closed-source implementation.
* No assets from any Nitroplus games are included in this repo.

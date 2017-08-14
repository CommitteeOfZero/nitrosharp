# NitroSharp (codename Project Hoppy)

Committee of Zero's effort to reimplement n2system, a visual novel engine used in a number of games made by Nitroplus. The effort is primarily focused on making the entirety of Chaos;Head Noah, a console-exclusive game, fully playable on PC (and potentially other platforms).

## Building
You can use the command line, Visual Studio 2017 15.3 (currently in preview) or JetBrains Rider 2017.1 to build NitroSharp.
Having Visual Studio installed on your machine is *optional*.
### Required Software
- [.NET Framework 4.6.1 SDK](https://www.microsoft.com/en-us/download/details.aspx?id=49978)
- [.NET Core 2.0 SDK (currently in preview)](https://www.microsoft.com/net/core/preview#windowscmd)

### Building From the Command Line
Get the latest FFmpeg binaries [here](https://ffmpeg.zeranoe.com/builds/). If you're running a 64-bit version of Windows, you're going to need x64 binaries. Make sure to select the "Shared" linking option.
Extract the archive and put the binaries (just the .dll files) into `src/NitroSharp.Foundation/FFmpeg/%arch%`, where `%arch%` is your CPU architecture (either `x64` or `x86`).

Run
```
(optional) dotnet restore
dotnet build [-c Release]
```

This produces both .NET Framework 4.6.1 and .NET Core 2.0 builds.

### Building From Visual Studio/JetBrains Rider
Follow the same instructions. After everything's in place, you should be able use your IDE to build the solution.


**Note**:
Even though some prerelease software is currently required to build the solution, NitroSharp does not rely on any unfinished specifications or any prerelease/unstable NuGet packages.
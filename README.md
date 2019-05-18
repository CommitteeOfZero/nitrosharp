# NitroSharp (codename Project Hoppy)

Committee of Zero's effort to reimplement n2system, a visual novel engine used in a number of games made by Nitroplus. The effort is primarily focused on making the entirety of Chaos;Head Noah, a console-exclusive game, fully playable on PC (and potentially other platforms).

## Building
### Required Software
- [.NET Core 3.0 SDK (preview)](https://dotnet.microsoft.com/download/dotnet-core/3.0)

Run
```
dotnet run --project src/NitroSharp.ShaderCompiler/NitroSharp.ShaderCompiler.csproj
dotnet build [-c Release]
```

You can also use Visual Studio 2019, VS Code or JetBrains Rider to build the solution.

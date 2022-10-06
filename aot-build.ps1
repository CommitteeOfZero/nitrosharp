param(
    [ValidateSet("win-x64", "linux-x64", "osx-x64")][string]$Runtime,
    [string]$CppCompiler = "clang",
    [string]$PublishDir
)

$Framework = "net7.0"
$ExeName = "Game"

if ($Runtime -eq "") {
    if ($IsWindows -or $null -eq $IsWindows) {
        $Runtime = "win-x64"
        $msvc = $true
    }
    elseif ($IsMacOS) {
        $Runtime = "osx-x64"
    }
    else {
        $Runtime = "linux-x64"
        $linuxBuild = $true
    }
}

if ($PublishDir -eq "") {
    $PublishDir = "publish/$Runtime"
}

if (!$msvc) {
    $env:CppCompilerAndLinker = $CppCompiler
}

$dotnetArgs = @(
    "run", "--no-launch-profile",
    "--project", "./src/NitroSharp.ShaderCompiler/NitroSharp.ShaderCompiler.csproj",
    "./src/NitroSharp/Graphics/Shaders", "./bin/obj/NitroSharp/Shaders.Generated"
)
dotnet($dotnetArgs)

$dotnetArgs = @(
    "publish", "src/Game/Game.csproj",
    "-r", "$Runtime",
    "-c", "Release"
)
dotnet($dotnetArgs)

Remove-Item -Path $PublishDir -Recurse -ErrorAction SilentlyContinue
Copy-Item -Path bin/Release/Game/$Framework/$Runtime/publish -Destination $PublishDir `
    -Recurse -Container -Force -Exclude *.pdb,*.deps.json,*.runtimeconfig.json

if ($linuxBuild) {
    $stripArgs = @("$PublishDir/$ExeName", "--strip-all")
    if (Get-Command wsl -ErrorAction SilentlyContinue) {
        & wsl strip $stripArgs
    }
    else {
        & strip $stripArgs
    }
}

function dotnet($dotnetArgs) {
    if ($IsWindows -and $linuxBuild) {
        & bash --login -c "dotnet $dotnetArgs"
    }
    else {
        & dotnet $dotnetArgs
    }
}

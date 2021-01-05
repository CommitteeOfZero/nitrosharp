param(
    [ValidateSet("win-x64", "linux-x64", "osx-x64")][string]$Runtime,
    [string]$CppCompiler = "clang"
)

$Framework = "net5.0"

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

if (!$msvc) {
    $env:CppCompilerAndLinker = $CppCompiler
}

$args = @(
    "run", "--no-launch-profile",
    "--project", "./src/NitroSharp.ShaderCompiler/NitroSharp.ShaderCompiler.csproj",
    "./src/NitroSharp/Graphics/Shaders", "./bin/obj/NitroSharp/Shaders.Generated"
)
dotnet($args)

$args = @(
    "publish", "src/Games/CowsHead/CowsHead.csproj",
    "-r", "$Runtime",
    "-c", "Release",
    "/p:Native=true"
)
dotnet($args)

$dst = "publish/$Runtime"
Remove-Item -Path $dst -Recurse -ErrorAction SilentlyContinue
Copy-Item -Path bin/Release/Games/CowsHead/$Framework/$Runtime/publish -Destination $dst `
    -Recurse -Container -Force -Exclude *.pdb,*.deps.json,*.runtimeconfig.json

if ($linuxBuild) {
    $args = @("$dst/CowsHead", "--strip-all")
    if (Get-Command wsl -ErrorAction SilentlyContinue) {
        & wsl strip $args
    }
    else {
        & strip $args
    }
}

function dotnet($args) {
    if ($IsWindows -and $linuxBuild) {
        & bash --login -c "dotnet $args"
    }
    else {
        & dotnet $args
    }
}

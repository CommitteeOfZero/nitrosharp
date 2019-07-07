param(
    [ValidateSet("win-x64", "linux-x64", "osx-x64")][string]$Runtime
)

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
    }
}

if (!$msvc) {
    $env:CppCompilerAndLinker = "clang-6.0"
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
Copy-Item -Path bin/Release/Games/CowsHead/netcoreapp3.0/$Runtime/publish -Destination $dst `
    -Recurse -Container -Force -Exclude *.pdb,*.deps.json,*.runtimeconfig.json

if (!$msvc) {
    $args = @("$dst/CowsHead", "--strip-all")
    if (Get-Command wsl -ErrorAction SilentlyContinue) {
        & wsl strip $args
    }
    else {
        & strip $args
    }
}

function dotnet($args) {
    if ($IsWindows -and $Runtime -eq "linux-x64") {
        & bash --login -c "dotnet $args"
    }
    else {
        & dotnet $args
    }
}

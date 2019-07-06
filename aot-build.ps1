param(
    [ValidateSet("win-x64", "linux-x64")][string]$Runtime
)

if ($Runtime -eq "") {
    if ($IsWindows -or $null -eq $IsWindows) {
        $Runtime = "win-x64"
        $msvc = true
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
    "publish", "src/Games/CowsHead/CowsHead.csproj",
    "-r", "$Runtime",
    "-c", "Release",
    "/p:Native=true"
)
if ($IsWindows -and $Runtime -eq "linux-x64") {
    & bash.exe --login -c "dotnet $args"
}
else {
    & dotnet $args
}

$dst = "publish/$Runtime"
Remove-Item -Path $dst -Recurse -ErrorAction SilentlyContinue
Copy-Item -Path bin/Release/Games/CowsHead/netcoreapp3.0/$Runtime/publish -Destination $dst `
    -Recurse -Container -Force -Exclude *.pdb,*.deps.json,*.runtimeconfig.json

if ($Runtime -ne "win-x64") {
    $args = @("$dst/CowsHead", "--strip-all")
    if (Get-Command wsl -ErrorAction SilentlyContinue) {
        & wsl strip $args
    }
    else {
        & strip $args
    }
}

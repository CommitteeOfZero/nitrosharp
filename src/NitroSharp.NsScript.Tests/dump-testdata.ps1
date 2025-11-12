param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string] $TestFileName,

    [Parameter(Mandatory = $true, Position = 1)]
    [ValidateSet('Syntax', 'Diagnostics')]
    [string] $DataKind
)

$compilerProject =  "$PSScriptRoot/../NitroSharp.ScriptCompiler/NitroSharp.ScriptCompiler.csproj"
$testDataDir = "$PSScriptRoot/Data"

switch ($DataKind) {
    'Syntax' {
        $outputName = "$TestFileName.ast.temp"
        & dotnet run --project $compilerProject -- dump-ast $testDataDir $TestFileName --output $outputName
    }
    'Diagnostics' {
        $outputName = "$TestFileName.diag.temp"
        & dotnet run --project $compilerProject -- check $testDataDir --files $TestFileName --output $outputName
    }
    default {
        throw "Unknown test data kind '$Kind'. Expected 'Syntax' or 'Diagnostics'."
    }
}

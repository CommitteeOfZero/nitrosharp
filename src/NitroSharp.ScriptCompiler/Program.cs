using System.CommandLine;
using System.CommandLine.Parsing;
using System.Text;
using NitroSharp.NsScript;
using NitroSharp.NsScript.Compiler;
using NitroSharp.NsScript.Syntax;

Console.OutputEncoding = Encoding.UTF8;

var rootCommand = new RootCommand("NitroSharp NSS Script Compiler");

var sourceDirArg = new Argument<DirectoryInfo>("src_dir")
{
    Description = "The root directory containing the script source files.",
}.AcceptExistingOnly();

var rootScriptsOption = new Option<FileInfo[]>("--roots")
{
    Description = "The list of script file names to treat as roots.",
    AllowMultipleArgumentsPerToken = true,
    Required = false,
    CustomParser = x => parseRelativePaths(x, mustExist: true)
};

var filesOption = new Option<FileInfo[]>("--files")
{
    Description = "The list of script file names to inspect.",
    AllowMultipleArgumentsPerToken = true,
    Required = false,
    CustomParser = x => parseRelativePaths(x, mustExist: true),
};

var outputOption = new Option<FileInfo>("--output")
{
    Description = "The file name to direct output to.",
    Required = false,
    CustomParser = x => parseSingleRelativePath(x, mustExist: false)
};

var minSeverityOption = new Option<DiagnosticSeverity>("--min-severity")
{
    Description = "The minimum severity level to report.",
    Required = false,
    DefaultValueFactory = _ => DiagnosticSeverity.Info
};

var checkCommand = new Command("check", "Check script files for errors.");
checkCommand.Arguments.Add(sourceDirArg);
checkCommand.Options.Add(rootScriptsOption);
checkCommand.Options.Add(filesOption);
checkCommand.Options.Add(outputOption);
checkCommand.Options.Add(minSeverityOption);

checkCommand.SetAction(result =>
{
    RunCheck(
        result.GetRequiredValue(sourceDirArg),
        result.GetValue(rootScriptsOption)!,
        result.GetValue(filesOption) ?? [],
        result.GetValue(outputOption),
        result.GetRequiredValue(minSeverityOption)
    );
});

var dumpAstInputArg = new Argument<FileInfo>("input")
{
    Description = "Relative path to a single script file to parse.",
    CustomParser = x => parseSingleRelativePath(x, mustExist: true),
};

var dumpAstFormatArg = new Option<SyntaxDumpFormat>("--format")
{
    Description = "Output format: 'debug' or 'roundtrip'",
    Required = false,
    DefaultValueFactory = _ => SyntaxDumpFormat.Debug,
    CustomParser = parse =>
    {
        return parse.Tokens.Single().Value switch
        {
            "debug" => SyntaxDumpFormat.Debug,
            "roundtrip" => SyntaxDumpFormat.RoundtripText,
            _ => error()
        };

        SyntaxDumpFormat error()
        {
            parse.AddError("Unrecognized value for '--format'.");
            return default;
        }
    }
};

var dumpAstCommand = new Command("dump-ast", "Parse a single script file and dump its AST.");
dumpAstCommand.Arguments.Add(sourceDirArg);
dumpAstCommand.Arguments.Add(dumpAstInputArg);
dumpAstCommand.Options.Add(dumpAstFormatArg);
dumpAstCommand.Options.Add(outputOption);

dumpAstCommand.SetAction(result =>
{
    RunDumpAst(
        result.GetRequiredValue(sourceDirArg),
        result.GetRequiredValue(dumpAstInputArg),
        result.GetRequiredValue(dumpAstFormatArg),
        result.GetValue(outputOption)
    );
});

rootCommand.Subcommands.Add(checkCommand);
rootCommand.Subcommands.Add(dumpAstCommand);

return rootCommand.Parse(args).Invoke();

FileInfo parseSingleRelativePath(ArgumentResult parse, bool mustExist)
    => parseRelativePath(parse, parse.Tokens.Single(), mustExist);

FileInfo[] parseRelativePaths(ArgumentResult parse, bool mustExist)
    => parse.Tokens.Select(tk => parseRelativePath(parse, tk, mustExist)).ToArray();

FileInfo parseRelativePath(ArgumentResult parse, Token token, bool mustExist)
{
    DirectoryInfo root = parse.GetRequiredValue(sourceDirArg);
    string relativePath = token.Value;
    string fullPath = Path.Combine(root.FullName, relativePath);
    var fileInfo = new FileInfo(fullPath);
    if (mustExist && !fileInfo.Exists)
    {
        parse.AddError($"Input file does not exist: '{relativePath}'");
    }

    return fileInfo;
}

static void RunCheck(
    DirectoryInfo sourceDir,
    FileInfo[] rootFilePaths,
    FileInfo[] filesToInspect,
    FileInfo? outputFile,
    DiagnosticSeverity minSeverity)
{
    string dumpPath = Path.Combine(sourceDir.Name, outputFile?.FullName ?? "out.txt");
    using Stream outputStream = outputFile is null
        ? Console.OpenStandardOutput()
        : File.Create(dumpPath);
    using TextWriter output = new StreamWriter(outputStream);

    var compilation = new Compilation(sourceDir.FullName);
    rootFilePaths = DetermineRoots(rootFilePaths, filesToInspect, sourceDir);
    SourceModuleSymbol[] rootModules = rootFilePaths
        .Select(ResolvedPath.FromFileSystemInfo)
        .Select(root => compilation.GetSourceModule(root))
        .ToArray();

    compilation = compilation.EmitDiagnostics(rootModules);

    IEnumerable<Diagnostic> filteredDiagnostics = compilation.Diagnostics;
    if (filesToInspect.Length > 0)
    {
        ResolvedPath[] filePathsToInspect = filesToInspect
            .Select(ResolvedPath.FromFileSystemInfo)
            .ToArray();

        filteredDiagnostics = filteredDiagnostics
            .Where(x => filePathsToInspect.Any(y => x.Location.FilePath == y));
    }

    if (minSeverity != DiagnosticSeverity.Info)
    {
        filteredDiagnostics = filteredDiagnostics.Where(x => x.Severity >= minSeverity);
    }

    SquiggleStyle squiggleStyle = outputFile is null
        ? SquiggleStyle.Underline
        : SquiggleStyle.VerticalBar;

    foreach (Diagnostic diagnostic in filteredDiagnostics
                 .OrderBy(d => d.Location.FilePath.Value)
                 .ThenBy(d => d.Location.Span))
    {
        diagnostic.Dump(output, squiggleStyle);
        output.WriteLine();
    }
}

static void RunDumpAst(
    DirectoryInfo sourceDir,
    FileInfo inputFile,
    SyntaxDumpFormat format,
    FileInfo? outputFile)
{
    string dumpPath = Path.Combine(sourceDir.FullName, outputFile?.FullName ?? "out.txt");
    using Stream outputStream = outputFile is null
        ? Console.OpenStandardOutput()
        : File.Create(dumpPath);
    using TextWriter output = new StreamWriter(outputStream);

    using FileStream inputStream = inputFile.OpenRead();
    var sourceText = SourceText.From(inputStream, ResolvedPath.FromFileSystemInfo(inputFile));
    var tree = SyntaxTree.ParseText(sourceText);
    tree.Root.Dump(output, format);
}

static FileInfo[] DetermineRoots(FileInfo[] providedRoots, FileInfo[] filesToInspect, DirectoryInfo sourceDir)
{
    if (providedRoots.Length > 0) { return providedRoots; }

    var bootScript = new FileInfo(Path.Combine(sourceDir.FullName, "boot.nss"));
    return bootScript.Exists ? [bootScript] : filesToInspect;
}

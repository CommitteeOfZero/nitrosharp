using System.CommandLine;
using System.CommandLine.Parsing;
using System.Text;
using NitroSharp.NsScript;
using NitroSharp.NsScript.Compiler;
using NitroSharp.NsScript.Syntax;

namespace NitroSharp.ScriptCompiler;

internal static class Program
{
    private static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        var rootCommand = new RootCommand("NitroSharp NSS Script Compiler");

        var sourceDirArg = new Argument<DirectoryInfo>("source_directory")
        {
            Description = "The root directory containing the script source files.",
        }.AcceptExistingOnly();

        var rootScriptsOption = new Option<string[]>("--roots")
        {
            Description = "The list of script names to treat as roots.",
            AllowMultipleArgumentsPerToken = true,
            Required = false,
            DefaultValueFactory = _ => ["boot.nss"]
        };

        var outputOption = new Option<FileInfo>("--output")
        {
            Description = "The file name to direct output to.",
            Required = false,
            CustomParser = x => parseRelativePath(x, mustExist: false)
        };

        var checkCommand = new Command("check", "Check script files for errors.");
        checkCommand.Arguments.Add(sourceDirArg);
        checkCommand.Options.Add(rootScriptsOption);
        checkCommand.Options.Add(outputOption);

        checkCommand.SetAction(result =>
        {
            RunCheck(
                result.GetRequiredValue(sourceDirArg),
                result.GetRequiredValue(rootScriptsOption),
                result.GetValue(outputOption)
            );
        });

        var dumpAstInputArg = new Argument<FileInfo>("input")
        {
            Description = "Relative path to a single script file to parse.",
            CustomParser = x => parseRelativePath(x, mustExist: true),
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

        FileInfo parseRelativePath(ArgumentResult parse, bool mustExist)
        {
            DirectoryInfo root = parse.GetRequiredValue(sourceDirArg);
            string relativePath = parse.Tokens.Single().Value;
            string fullPath = Path.Combine(root.FullName, relativePath);
            var fileInfo = new FileInfo(fullPath);
            if (mustExist && !fileInfo.Exists)
            {
                parse.AddError($"Input file does not exist: '{relativePath}'");
            }

            return fileInfo;
        }
    }

    private static void RunCheck(DirectoryInfo sourceDir, string[] rootScriptNames, FileInfo? outputFile)
    {
        string dumpPath = Path.Combine(sourceDir.Name, outputFile?.FullName ?? "out.txt");
        using Stream outputStream = outputFile is null
            ? Console.OpenStandardOutput()
            : File.Create(dumpPath);
        using TextWriter output = new StreamWriter(outputStream);

        var compilation = new Compilation(sourceDir.FullName);

        SourceModuleSymbol?[] rootModules = rootScriptNames
            .Select(rootName => compilation.TryGetSourceModule(rootName))
            .ToArray();

        string[] unresolvedRoots = rootScriptNames
            .Zip(rootModules)
            .Where(tuple => tuple.Second is null)
            .Select(tuple => $"'{tuple.First}'")
            .ToArray();

        if (unresolvedRoots.Length > 0)
        {
            output.WriteLine(
                $"The following root scripts were not found: " +
                $"[{string.Join(", ", unresolvedRoots)}]"
            );
            return;
        }

        compilation = compilation.EmitDiagnostics(rootModules!);

        foreach (Diagnostic diagnostic in compilation.Diagnostics.All
                     .OrderBy(d => d.Location.SourceText.FilePath.Value)
                     .ThenBy(d => d.Location.Span))
        {
            diagnostic.Dump(output, SquiggleStyle.Underline);
            output.WriteLine();
        }
    }

    private static void RunDumpAst(
        DirectoryInfo sourceDir,
        FileInfo inputFile,
        SyntaxDumpFormat format,
        FileInfo? outputFile)
    {
        string dumpPath = Path.Combine(sourceDir.Name, outputFile?.FullName ?? "out.txt");
        using Stream outputStream = outputFile is null
            ? Console.OpenStandardOutput()
            : File.Create(dumpPath);
        using TextWriter output = new StreamWriter(outputStream);

        using FileStream fs = inputFile.OpenRead();
        var sourceText = SourceText.From(fs, new ResolvedPath(inputFile.FullName));
        var tree = SyntaxTree.ParseText(sourceText);
        tree.Root.Dump(output, format);
    }
}

using System.CommandLine;
using System.Text;
using NitroSharp.NsScript;
using NitroSharp.NsScript.Compiler;
using Console = System.Console;

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

        var rootScriptsOption = new Option<FileInfo[]>("--roots")
        {
            Description = "The list of script names to treat as roots.",
            Required = false,
            CustomParser = result =>
            {
                DirectoryInfo srcDir = result.GetRequiredValue(sourceDirArg);
                return result.Tokens
                    .Select(t => new FileInfo(Path.Combine(srcDir.FullName, t.Value)))
                    .ToArray();
            },
        }.AcceptLegalFilePathsOnly();

        var checkCommand = new Command("check", "Check script files for errors.");
        checkCommand.Arguments.Add(sourceDirArg);
        checkCommand.Options.Add(rootScriptsOption);

        checkCommand.SetAction(result =>
        {
            RunCheck(result.GetRequiredValue(sourceDirArg), result.GetRequiredValue(rootScriptsOption));
        });

        rootCommand.Subcommands.Add(checkCommand);

        return rootCommand.Parse(args).Invoke();
    }

    private static void RunCheck(DirectoryInfo sourceDir, FileInfo[] rootScripts)
    {
        var compilation = new Compilation(sourceDir.FullName);

        SourceModuleSymbol[] rootModules = rootScripts
            .Select(fileInfo => Path.GetRelativePath(sourceDir.FullName, fileInfo.FullName))
            .Select(relativePath => compilation.GetSourceModule(relativePath)).ToArray();

        compilation = compilation.EmitDiagnostics(rootModules);

        foreach (Diagnostic diagnostic in compilation.Diagnostics.All
                     .OrderBy(d => d.Location.SourceText.FilePath.Value)
                     .ThenBy(d => d.Span.Start))
        {
            PrintDiagnostic(diagnostic, sourceDir);
            Console.WriteLine();
        }
    }

    private static string RelativePath(DirectoryInfo sourceDir, ResolvedPath scriptPath)
    {
        return Path.GetRelativePath(sourceDir.FullName, scriptPath.Value);
    }

    private static void PrintDiagnostic(Diagnostic diagnostic, DirectoryInfo sourceDir)
    {
        SourceText sourceText = diagnostic.Location.SourceText;
        LinePositionSpan lineSpan = diagnostic.Location.GetLineSpan();

        (LinePosition startLine, LinePosition endLine) = lineSpan;

        string scriptPath = RelativePath(sourceDir, sourceText.FilePath);

        string severity = diagnostic.Severity.ToString();
        string message = diagnostic.Message;

        Console.WriteLine($"{severity}: {message}");
        Console.WriteLine($"  --> {scriptPath}:{startLine.Line + 1}:{startLine.Column + 1}");
        Console.WriteLine("     |");

        const int tabSize = 4;
        const string underlineSeqStart = "\e[4;31m";
        const string underlineSeqEnd = "\e[0m";

        if (startLine.Line > 0)
        {
            string before = sourceText.GetLineText(startLine.Line - 1);
            Console.WriteLine($"{startLine.Line,4} | {ExpandTabs(before, tabSize)}");
        }

        var sb = new StringBuilder();
        for (int line = startLine.Line; line <= endLine.Line; line++)
        {
            string lineText = sourceText.GetLineText(line);

            (int underlineStart, int underlineEnd) = (0, lineText.Length);
            if (line == startLine.Line)
            {
                underlineStart = startLine.Column;
                underlineEnd = startLine.Line == endLine.Line ? endLine.Column : lineText.Length;
            }
            else if (line == endLine.Line)
            {
                (underlineStart, underlineEnd) = (0, endLine.Column);
            }

            bool zeroLength = (startLine == endLine) && (underlineStart == underlineEnd);
            if (zeroLength)
            {
                underlineEnd = Math.Min(underlineStart + 1, lineText.Length);
            }

            sb.Append(ExpandTabs(lineText[..underlineStart], tabSize));
            sb.Append(underlineSeqStart);
            sb.Append(ExpandTabs(lineText[underlineStart..underlineEnd], tabSize));
            sb.Append(underlineSeqEnd);
            sb.Append(ExpandTabs(lineText[underlineEnd..], tabSize));

            Console.WriteLine($"{line + 1,4} | {sb}");
        }

        if (endLine.Line < sourceText.LineCount - 1)
        {
            string after = sourceText.GetLineText(endLine.Line + 1);
            Console.WriteLine($"{endLine.Line + 2,4} | {ExpandTabs(after, tabSize)}");
        }

        Console.WriteLine("     |");
    }

    private static string ExpandTabs(string text, int tabSize)
    {
        var result = new StringBuilder();
        int column = 0;

        foreach (char ch in text)
        {
            if (ch == '\t')
            {
                int spacesToAdd = tabSize - (column % tabSize);
                result.Append(' ', spacesToAdd);
                column += spacesToAdd;
            }
            else
            {
                result.Append(ch);
                column++;
            }
        }
        return result.ToString();
    }
}

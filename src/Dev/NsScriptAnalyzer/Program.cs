using NitroSharp.NsScript;
using NitroSharp.NsScript.Text;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Text;

namespace NsScriptAnalyzer
{
    class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length == 1)
            {
                Analyze(args[0]);
            }
        }

        private static void Analyze(string input)
        {
            string folder = Path.GetDirectoryName(input);
            string nameOrMask = Path.GetFileName(input);
            foreach (var path in Directory.EnumerateFiles(folder, nameOrMask))
            {
                try
                {
                    using (var stream = File.OpenRead(path))
                    {
                        Analyze(stream, path);
                        Console.WriteLine();
                    }
                }
                catch (FileNotFoundException e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        private static void Analyze(Stream stream, string path)
        {
            void ReportMany(ImmutableArray<Diagnostic> collection, SourceText sourceText)
            {
                foreach (var diag in collection)
                {
                    Report(diag, sourceText);
                    Console.WriteLine();
                }
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Analyzing '{path}'");
            Console.ResetColor();

            var source = SourceText.From(stream, path);
            var syntaxTree = Parsing.ParseText(source);

            var diagnostics = syntaxTree.Diagnostics;
            if (diagnostics.IsEmpty)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("0 issues found.");
                Console.ResetColor();
            }

            ReportMany(diagnostics.Errors, source);
            ReportMany(diagnostics.Warnings, source);
            ReportMany(diagnostics.Information, source);
        }

        private static void Report(Diagnostic diagnostic, SourceText sourceText)
        {
            int lineNumber = sourceText.GetLineNumberFromPosition(diagnostic.Span.Start);
            var line = sourceText.Lines[lineNumber];

            string message = diagnostic.Message;
            var fullMessage = new StringBuilder(message.Substring(0, Math.Max(message.Length - 1, 0)));
            fullMessage.Append(" at line ");
            fullMessage.Append(lineNumber);
            fullMessage.Append(".");

            ConsoleColor color = default;
            switch (diagnostic.Severity)
            {
                case DiagnosticSeverity.Error:
                    color = ConsoleColor.Red;
                    break;

                case DiagnosticSeverity.Warning:
                    color = ConsoleColor.Yellow;
                    break;

                case DiagnosticSeverity.Info:
                    color = ConsoleColor.DarkCyan;
                    break;
            }

            Console.BackgroundColor = color;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.Write(diagnostic.Severity.ToString() + ":");
            Console.ResetColor();
            Console.WriteLine(" " + fullMessage.ToString());

            Console.BackgroundColor = ConsoleColor.Gray;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine(line.GetText().Trim());
            Console.ResetColor();
        }
    }
}

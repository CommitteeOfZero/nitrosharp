using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NitroSharp.Common;
using NitroSharp.NsScript.Compiler;
using NitroSharp.NsScript.Syntax;
using Xunit;
using Xunit.Abstractions;

namespace NitroSharp.NsScript.Tests;

public sealed class GoldenTests
{
    public enum TestKind
    {
        Syntax,
        Diagnostics
    }

    public record struct Test(
        ResolvedRelativePath SourcePath,
        ResolvedPath GoldPath,
        TestKind Kind) : IXunitSerializable
    {
        public void Deserialize(IXunitSerializationInfo info)
        {
            SourcePath = info.GetValue<ResolvedRelativePath>(nameof(SourcePath));
            GoldPath = info.GetValue<ResolvedPath>(nameof(GoldPath));
            Kind = info.GetValue<TestKind>(nameof(Kind));
        }

        public void Serialize(IXunitSerializationInfo info)
        {
            info.AddValue(nameof(SourcePath), SourcePath);
            info.AddValue(nameof(GoldPath), GoldPath);
            info.AddValue(nameof(Kind), Kind);
        }

        public override string ToString() => $"(\"{SourcePath.Value}\", {Kind})";
    }

    private readonly record struct TestContext(
        SourceModuleSymbol TestModule,
        Compilation Compilation,
        TestKind TestKind);

    [Theory]
    [MemberData(nameof(DiscoverTests))]
    public void RunTest(Test test)
    {
        string rootDir = AppContext.BaseDirectory;
        string testDataDir = Path.Combine(rootDir, "Data");

        var compilation = new Compilation(testDataDir);
        SourceModuleSymbol module = compilation.GetSourceModule(test.SourcePath.Value);
        compilation = compilation.EmitDiagnostics(new[] { module });

        using var outputWriter = new StringWriter();
        var context = new TestContext(module, compilation, test.Kind);
        Dump(context, outputWriter);
        string actualDump = outputWriter.ToString().Replace("\r\n", "\n");

        string expectedDump = File.ReadAllText(test.GoldPath.Value)
            .Replace("\r\n", "\n");
        Assert.Equal(expectedDump, actualDump);
    }

    public static TheoryData<Test> DiscoverTests()
    {
        var testDataDir = new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "Data"));
        var testDataPath = ResolvedPath.FromFileSystemInfo(testDataDir);

        var testFiles = testDataDir.EnumerateFiles("*.nss", SearchOption.AllDirectories);
        var testsWithGold = testFiles
            .Select(x => (source: relativePath(x), golds: goldFor(x).ToArray()));

        var tests = testsWithGold
            .SelectMany(x => x.golds.Select(gold => new Test(x.source, gold, getTestKind(gold))))
            .ToArray();

        return new TheoryData<Test>(tests);

        IEnumerable<ResolvedPath> goldFor(FileInfo testFile)
        {
            var nonCursedOptions = new EnumerationOptions { MatchType = MatchType.Simple };
            return testFile.Directory.NotNull()
                .EnumerateFiles($"{testFile.Name}.*", nonCursedOptions)
                .Where(x => x.Extension != ".temp")
                .Select(ResolvedPath.FromFileSystemInfo);
        }

        ResolvedRelativePath relativePath(FileInfo fileInfo)
            => ResolvedPath.FromFileSystemInfo(fileInfo).RelativeTo(testDataPath);

        TestKind getTestKind(ResolvedPath goldPath)
        {
            return Path.GetExtension(goldPath.Value) switch
            {
                ".ast" => TestKind.Syntax,
                ".diag" => TestKind.Diagnostics,
                _ => throw new Exception($"Gold file has unknown extension: '{goldPath.Value}'")
            };
        }
    }

    private static void Dump(TestContext context, StringWriter outputWriter)
    {
        switch (context.TestKind)
        {
            case TestKind.Syntax:
            {
                SourceFileSymbol rootSourceFile = context.TestModule.RootSourceFile;
                rootSourceFile.Syntax.Dump(outputWriter, SyntaxDumpFormat.Debug);
                break;
            }
            case TestKind.Diagnostics:
            {
                foreach (Diagnostic diagnostic in context.Compilation.Diagnostics.All)
                {
                    diagnostic.Dump(outputWriter, SquiggleStyle.VerticalBar);
                    outputWriter.WriteLine();
                }
                break;
            }
        }
    }
}

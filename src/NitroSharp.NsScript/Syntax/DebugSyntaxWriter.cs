using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NitroSharp.NsScript.Syntax;

internal sealed class DebugSyntaxWriter(TextWriter textWriter)
    : SyntaxWriter(textWriter)
{
    private readonly List<bool> _lastChildFlags = [];

    protected override void DefaultVisitNode(SyntaxNode node)
    {
        Visit(node, isLastChild: true);
    }

    private void Visit(SyntaxNode node, bool isLastChild)
    {
        WriteNode(node, isLastChild);

        SyntaxNode[] children = node.GetChildren().ToArray();
        if (children.Length == 0) { return; }

        _lastChildFlags.Add(isLastChild);
        for (int i = 0; i < children.Length; i++)
        {
            SyntaxNode child = children[i];
            Visit(child, isLastChild: i == children.Length - 1);
        }
        _lastChildFlags.RemoveAt(_lastChildFlags.Count - 1);
    }

    private void WriteNode(SyntaxNode node, bool isLastChild)
    {
        foreach (bool isAncestorLast in _lastChildFlags)
        {
            Write(isAncestorLast ? "   " : "│  ");
        }
        Write(isLastChild ? "└─ " : "├─ ");

        Write(node.Kind.ToString());

        string nodeText = FormatNode(node);
        if (nodeText.Length > 0)
        {
            Write(" ");
            Write(nodeText);
        }

        Write(" ");
        Write(node.Span.ToString());

        WriteLine();
    }

    private static string FormatNode(SyntaxNode node)
    {
        return node switch
        {
            SourceFileRoot root => $"subroutines: C={root.ChapterCount}, S={root.SceneCount}," +
                $"F={root.FunctionCount}, includes={root.FileReferences.Length}",
            ChapterDeclaration chapter => $"name=\"{chapter.Name.Value}\"",
            SceneDeclaration scene => $"name=\"{scene.Name.Value}\"",
            FunctionDeclaration fn => $"name=\"{fn.Name.Value}\" params={fn.Parameters.Length}",
            Parameter parameter => $"name=\"{parameter.Name}\"",
            Block block => $"statements={block.Statements.Length}",
            SelectSection section => $"label={section.Label.Value}",
            CallChapterStatement callChapter => $"targetModule=\"{callChapter.TargetModule.Value}\"",
            CallSceneStatement callScene => callScene.TargetModule is { } target
                ? $"targetModule=\"{target.Value}\" targetScene=\"{callScene.TargetScene.Value}\""
                : $"targetScene=\"{callScene.TargetScene.Value}\"",
            DialogueBlock dlg => $"name=\"{dlg.Name}\" box=\"{dlg.AssociatedBox}\" parts={dlg.Parts.Length}",
            DialogueBlockPart.CodeBlock codeBlock => $"statements={codeBlock.Statements.Length}",
            DialogueBlockPart.Markup markup => $"text={QuoteAndEscape(markup.Text)}",
            LiteralExpression literal => $"value={QuoteAndEscape(literal.Value.ToString())}",
            NameExpression name => name.Sigil switch
            {
                SigilKind.Dollar => $"name=\"${name.Name}\"",
                SigilKind.Hash => $"name=\"#{name.Name}\"",
                _ => $"name=\"{name.Name}\""
            },
            UnaryExpression unary => $"op=\"{OperatorInfo.GetText(unary.OperatorKind.Value)}\"",
            BinaryExpression bin => $"op=\"{OperatorInfo.GetText(bin.OperatorKind.Value)}\"",
            AssignmentExpression assignment => $"op=\"{OperatorInfo.GetText(assignment.OperatorKind.Value)}\"",
            FunctionCallExpression call => $"target=\"{call.TargetName.Value}\" args={call.Arguments.Length}",
            BezierExpression bezier => $"points={bezier.ControlPoints.Length}",
            ErrorExpression error => $"test: {QuoteAndEscape(error.Text.ToString())}",
            ErrorStatement error => $"text: {QuoteAndEscape(error.Text.ToString())}",
            _ => string.Empty
        };
    }

    private static string QuoteAndEscape(string value)
    {
        var sb = new StringBuilder(value.Length);
        sb.Append('"');
        foreach (char c in value)
        {
            string escape = c switch
            {
                '\\' => @"\\",
                '"' => @"\""",
                '\n' => @"\n",
                '\r' => @"\r",
                '\t' => @"\t",
                _ => ""
            };

            if (escape.Length > 0)
            {
                sb.Append(escape);
            }
            else if (char.IsControl(c))
            {
                sb.Append("\\u");
                sb.Append(((int)c).ToString("x4"));
            }
            else
            {
                sb.Append(c);
            }
        }
        sb.Append('"');
        return sb.ToString();
    }
}

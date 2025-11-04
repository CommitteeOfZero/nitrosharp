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
            SourceFileRoot root =>
                $"subroutines: C={root.ChapterCount}, S={root.SceneCount}, F={root.FunctionCount}, files={root.FileReferences.Length}",
            ChapterDeclaration ch => $"name=\"{ch.Name.Value}\"",
            SceneDeclaration sc => $"name=\"{sc.Name.Value}\"",
            FunctionDeclaration fn => $"name=\"{fn.Name.Value}\" params={fn.Parameters.Length}",
            Parameter p => $"name=\"{p.Name}\"",
            Block b => $"statements={b.Statements.Length}",
            SelectSection sec => $"label={sec.Label.Value}",
            CallChapterStatement cc => $"targetModule=\"{cc.TargetModule.Value}\"",
            CallSceneStatement cs => cs.TargetModule is { } target
                ? $"targetModule=\"{target.Value}\" targetScene=\"{cs.TargetScene.Value}\""
                : $"targetScene=\"{cs.TargetScene.Value}\"",
            DialogueBlock db => $"name=\"{db.Name}\" box=\"{db.AssociatedBox}\" parts={db.Parts.Length}",
            DialogueBlockPart.CodeBlock cb => $"statements={cb.Statements.Length}",
            DialogueBlockPart.Markup mk => $"text={QuoteAndEscape(mk.Text)}",
            LiteralExpression lit => $"value={QuoteAndEscape(lit.Value.ToString())}",
            NameExpression name => name.Sigil switch
            {
                SigilKind.Dollar => $"name=\"${name.Name}\"",
                SigilKind.Hash => $"name=\"#{name.Name}\"",
                _ => $"name=\"{name.Name}\""
            },
            UnaryExpression un => $"op=\"{OperatorInfo.GetText(un.OperatorKind.Value)}\"",
            BinaryExpression bin => $"op=\"{OperatorInfo.GetText(bin.OperatorKind.Value)}\"",
            AssignmentExpression ass => $"op=\"{OperatorInfo.GetText(ass.OperatorKind.Value)}\"",
            FunctionCallExpression call => $"target=\"{call.TargetName.Value}\" args={call.Arguments.Length}",
            BezierExpression bez => $"points={bez.ControlPoints.Length}",
            ErrorExpression => "<error expr>",
            ErrorStatement => "<error stmt>",
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

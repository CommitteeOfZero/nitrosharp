using System.Collections.Immutable;
using System.IO;

namespace NitroSharp.NsScript.Syntax;

internal sealed class RoundtripSyntaxWriter(TextWriter textWriter)
    : SyntaxWriter(textWriter)
{
    protected override void DefaultVisitNode(SyntaxNode node)
    {
        VisitChildren(node);
    }

    public override void VisitSourceFileRoot(SourceFileRoot root)
    {
        foreach (Spanned<string> import in root.FileReferences)
        {
            WriteLine($"#include \"{import.Value}\"");
        }

        if (root.FileReferences.Length > 0 && root.SubroutineDeclarations.Length > 0)
        {
            WriteLine();
        }

        for (int i = 0; i < root.SubroutineDeclarations.Length; i++)
        {
            Visit(root.SubroutineDeclarations[i]);
            if (i < root.SubroutineDeclarations.Length - 1)
            {
                WriteLine();
            }
        }
    }

    public override void VisitChapter(ChapterDeclaration chapter)
    {
        Write(SyntaxFacts.GetText(SyntaxTokenKind.ChapterKeyword));
        WriteSpace();
        Write(chapter.Name);
        Visit(chapter.Body);

        WriteDialogueBlocks(chapter.DialogueBlocks);
    }

    public override void VisitScene(SceneDeclaration scene)
    {
        Write(SyntaxFacts.GetText(SyntaxTokenKind.SceneKeyword));
        WriteSpace();
        Write(scene.Name);
        VisitBlock(scene.Body);
        WriteDialogueBlocks(scene.DialogueBlocks);
    }

    public override void VisitFunction(FunctionDeclaration function)
    {
        Write(SyntaxFacts.GetText(SyntaxTokenKind.FunctionKeyword));
        Write(function.Name);
        Write("(");
        for (int i = 0; i < function.Parameters.Length; i++)
        {
            if (i > 0) { Write(", "); }
            Visit(function.Parameters[i]);
        }
        Write(")");
        WriteSpace();
        VisitBlock(function.Body);
        WriteDialogueBlocks(function.DialogueBlocks);
    }

    private void WriteDialogueBlocks(ImmutableArray<DialogueBlock> blocks)
    {
        foreach (DialogueBlock block in blocks)
        {
            WriteLine();
            Visit(block);
        }
    }

    public override void VisitBlock(Block block)
    {
        WriteLine();
        Write("{");
        WriteLine();
        Indent();

        VisitArray(block.Statements);

        Outdent();
        Write("}");
        WriteLine();
    }

    public override void VisitExpressionStatement(ExpressionStatement expressionStatement)
    {
        WriteExpression(expressionStatement.Expression, Precedence.Expression);
        Write(";");
    }

    public override void VisitIfStatement(IfStatement ifStatement)
    {
        Write(SyntaxFacts.GetText(SyntaxTokenKind.IfKeyword));
        WriteSpace();
        Write("(");
        WriteExpression(ifStatement.Condition, Precedence.Expression);
        Write(")");

        if (ifStatement.IfTrueStatement.Kind == SyntaxNodeKind.Block)
        {
            Visit(ifStatement.IfTrueStatement);
        }
        else
        {
            WriteLine();
            Indent();
            Visit(ifStatement.IfTrueStatement);
            Outdent();
            WriteLine();
        }

        if (ifStatement.IfFalseStatement is { } elseBranch)
        {
            Write(SyntaxFacts.GetText(SyntaxTokenKind.ElseKeyword));
            bool block = elseBranch.Kind == SyntaxNodeKind.Block;
            bool elif = elseBranch.Kind == SyntaxNodeKind.IfStatement;
            if (!block && !elif)
            {
                WriteLine();
                Indent();
                Visit(elseBranch);
                Outdent();
                WriteLine();
            }
            else if (elif)
            {
                WriteLine();
            }

            Visit(elseBranch);
        }
    }

    public override void VisitBreakStatement(BreakStatement breakStatement)
    {
        Write(SyntaxFacts.GetText(SyntaxTokenKind.BreakKeyword));
        Write(";");
    }

    public override void VisitWhileStatement(WhileStatement whileStatement)
    {
        Write(SyntaxFacts.GetText(SyntaxTokenKind.WhileKeyword));
        Write(" (");
        WriteExpression(whileStatement.Condition, Precedence.Expression);
        Write(") ");
        Visit(whileStatement.Body);
    }

    public override void VisitReturnStatement(ReturnStatement returnStatement)
    {
        Write(SyntaxFacts.GetText(SyntaxTokenKind.ReturnKeyword));
        Write(";");
    }

    public override void VisitSelectStatement(SelectStatement selectStatement)
    {
        Write(SyntaxFacts.GetText(SyntaxTokenKind.SelectKeyword));
        VisitBlock(selectStatement.Body);
    }

    public override void VisitSelectSection(SelectSection selectSection)
    {
        Write(SyntaxFacts.GetText(SyntaxTokenKind.CaseKeyword));
        WriteSpace();
        Write(selectSection.Label);
        WriteSpace();
        VisitBlock(selectSection.Body);
    }

    public override void VisitFunctionCall(FunctionCallExpression functionCall)
    {
        Write(functionCall.TargetName);
        Write("(");
        for (int i = 0; i < functionCall.Arguments.Length; i++)
        {
            if (i > 0) Write(", ");
            WriteExpression(functionCall.Arguments[i], Precedence.Expression);
        }
        Write(")");
    }

    public override void VisitCallChapterStatement(CallChapterStatement callChapterStatement)
    {
        Write(SyntaxFacts.GetText(SyntaxTokenKind.CallChapterKeyword));
        WriteSpace();
        Write(callChapterStatement.TargetModule);
        Write(";");
        WriteLine();
    }

    public override void VisitCallSceneStatement(CallSceneStatement callSceneStatement)
    {
        Write(SyntaxFacts.GetText(SyntaxTokenKind.CallSceneKeyword));
        WriteSpace();
        if (callSceneStatement.TargetModule is null)
        {
            Write("@->");
        }
        else
        {
            Write(callSceneStatement.TargetModule.Value);
            Write("->");
        }

        Write(callSceneStatement.TargetScene);
        Write(";");
        WriteLine();
    }

    public override void VisitLiteral(LiteralExpression literal)
    {
        Write(literal.Value.ToString());
    }

    public override void VisitNameExpression(NameExpression name)
    {
        string sigil = name.Sigil switch
        {
            SigilKind.None => "",
            SigilKind.Dollar => "$",
            SigilKind.Hash => "$",
            _ => ThrowHelper.Unreachable<string>()
        };
        Write(sigil);
        Write(name.Name);
    }

    public override void VisitUnaryExpression(UnaryExpression unaryExpression)
    {
        Write(OperatorInfo.GetText(unaryExpression.OperatorKind.Value));
        WriteExpression(unaryExpression.Operand, Precedence.Unary);
    }

    public override void VisitBinaryExpression(BinaryExpression binaryExpression)
    {
        WriteBinary(binaryExpression, Precedence.Expression);
    }

    public override void VisitAssignmentExpression(AssignmentExpression assignmentExpression)
    {
        WriteAssignment(assignmentExpression, Precedence.Expression);
    }

    public override void VisitDialogueBlock(DialogueBlock dialogueBlock)
    {
        Write($"<PRE {dialogueBlock.AssociatedBox}>");
        WriteLine();
        Write($"[{dialogueBlock.Name}]");
        WriteLine();

        foreach (DialogueBlockPart part in dialogueBlock.Parts)
        {
            Visit(part);
        }

        WriteLine("</PRE>");
    }

    public override void VisitDialogueCodeBlock(DialogueBlockPart.CodeBlock codeBlock)
    {
        Write("{");
        WriteLine();
        Indent();
        VisitArray(codeBlock.Statements);
        Outdent();
        Write("}");
        WriteLine();
    }

    public override void VisitDialogueMarkup(DialogueBlockPart.Markup markup)
    {
        WriteLine(markup.Text);
    }

    public override void VisitBezierExpression(BezierExpression bezierExpression)
    {
        if (bezierExpression.ControlPoints.Length == 0)
        {
            return;
        }

        BezierControlPoint first = bezierExpression.ControlPoints[0];
        Write("(");
        WriteExpression(first.X, Precedence.Expression);
        Write(", ");
        WriteExpression(first.Y, Precedence.Expression);
        Write(")");

        for (int i = 1; i < bezierExpression.ControlPoints.Length; i++)
        {
            BezierControlPoint point = bezierExpression.ControlPoints[i];
            bool paren = point.IsStartingPoint;
            Write(paren ? "(" : "{");
            WriteExpression(point.X, Precedence.Expression);
            Write(", ");
            WriteExpression(point.Y, Precedence.Expression);
            Write(paren ? ")" : "}");
        }
    }

    private void WriteExpression(Expression expression, Precedence parentPrecedence)
    {
        switch (expression)
        {
            case UnaryExpression unary:
            {
                bool needsParens = parentPrecedence < Precedence.Unary;
                if (needsParens) { Write("("); }
                VisitUnaryExpression(unary);
                if (needsParens) { Write(")"); }
                break;
            }
            case BinaryExpression binary:
            {
                WriteBinary(binary, parentPrecedence);
                break;
            }
            case AssignmentExpression assignment:
            {
                WriteAssignment(assignment, parentPrecedence);
                break;
            }
            default:
            {
                Visit(expression);
                break;
            }
        }
    }

    private void WriteBinary(BinaryExpression bin, Precedence parentPrecedence)
    {
        Precedence ownPrecedence = Parser.GetPrecedence(bin.OperatorKind.Value);
        bool needsParens = ownPrecedence < parentPrecedence;
        if (needsParens) { Write("("); }
        WriteExpression(bin.Left, ownPrecedence);
        WriteSpace();
        Write(OperatorInfo.GetText(bin.OperatorKind.Value));
        WriteSpace();
        WriteExpression(bin.Right, ownPrecedence);
        if (needsParens) { Write(")"); }
    }

    private void WriteAssignment(AssignmentExpression assign, Precedence parentPrecedence)
    {
        const Precedence ownPrecedence = Precedence.Assignment;
        bool needsParens = ownPrecedence < parentPrecedence;
        if (needsParens) Write("(");
        WriteExpression(assign.Target, ownPrecedence);
        WriteSpace();
        Write(OperatorInfo.GetText(assign.OperatorKind.Value));
        if (assign.OperatorKind.Value is not (AssignmentOperatorKind.Increment or AssignmentOperatorKind.Decrement))
        {
            WriteSpace();
            WriteExpression(assign.Value, ownPrecedence);
        }

        if (needsParens) Write(")");
    }
}

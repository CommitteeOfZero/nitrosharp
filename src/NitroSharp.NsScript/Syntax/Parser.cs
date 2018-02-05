using NitroSharp.NsScript.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace NitroSharp.NsScript.Syntax
{
    internal sealed class Parser
    {
        private readonly Lexer _lexer;
        private readonly SyntaxToken[] _tokens;
        private int _tokenOffset;

        // Used to differentiate between string literals and quoted identifiers (aka string parameter references).
        // Normally, a parser should not keep track of something like that, but this is NSS.
        private readonly Dictionary<string, Parameter> _currentParameterList;

        public Parser(Lexer lexer)
        {
            _lexer = lexer;
            _tokens = PreLex().ToArray();
            _currentParameterList = new Dictionary<string, Parameter>();
        }

        private SyntaxToken CurrentToken => _tokens[_tokenOffset];
        private SourceText SourceText => _lexer.SourceText;

        public SourceFile ParseScript()
        {
            var fileReferences = ImmutableArray.CreateBuilder<SourceFileReference>();
            SyntaxTokenKind tk;
            while ((tk = CurrentToken.Kind) != SyntaxTokenKind.EndOfFileToken && !CanStartDeclaration(tk))
            {
                switch (CurrentToken.Kind)
                {
                    case SyntaxTokenKind.IncludeDirective:
                        EatToken();
                        var filePath = EatToken(SyntaxTokenKind.StringLiteralToken);
                        fileReferences.Add(new SourceFileReference((string)filePath.Value));
                        break;

                    case SyntaxTokenKind.SemicolonToken:
                    default:
                        EatToken();
                        break;
                }
            }

            var members = ImmutableArray.CreateBuilder<MemberDeclaration>();
            while (CurrentToken.Kind != SyntaxTokenKind.EndOfFileToken)
            {
                switch (CurrentToken.Kind)
                {
                    case SyntaxTokenKind.ChapterKeyword:
                    case SyntaxTokenKind.SceneKeyword:
                    case SyntaxTokenKind.FunctionKeyword:
                        members.Add(ParseMemberDeclaration());
                        break;

                    default:
                        SkipToNextLine();
                        break;
                }
            }

            return new SourceFile(SourceText.FilePath, members.ToImmutable(), fileReferences.ToImmutable());
        }

        private static bool CanStartDeclaration(SyntaxTokenKind tk)
        {
            switch (tk)
            {
                case SyntaxTokenKind.ChapterKeyword:
                case SyntaxTokenKind.SceneKeyword:
                case SyntaxTokenKind.FunctionKeyword:
                    return true;

                default:
                    return false;
            }
        }

        public MemberDeclaration ParseMemberDeclaration()
        {
            switch (CurrentToken.Kind)
            {
                case SyntaxTokenKind.ChapterKeyword:
                    return ParseChapter();

                case SyntaxTokenKind.SceneKeyword:
                    return ParseScene();

                case SyntaxTokenKind.FunctionKeyword:
                    _currentParameterList.Clear();
                    return ParseFunction();

                default:
                    throw new InvalidOperationException($"{CurrentToken.Kind} cannot start a declaration.");
            }
        }

        private void SkipToNextLine()
        {
            int currentLine = GetLineNumber();
            //Debug.WriteLine($"{SourceText.FileName}: Skipping line #{currentLine}");
            //Debug.WriteLine(SourceText.GetText(SourceText.GetLineFromPosition(currentLine).Span));
            do
            {
                EatToken();
            } while (GetLineNumber() == currentLine);

            int GetLineNumber() => SourceText.GetLineNumberFromPosition(CurrentToken.TextSpan.Start);
        }

        private IEnumerable<SyntaxToken> PreLex()
        {
            while (true)
            {
                SyntaxToken token = _lexer.Lex();
                yield return token;

                if (token.Kind == SyntaxTokenKind.EndOfFileToken)
                {
                    break;
                }
            }
        }

        private SyntaxToken PeekToken(int n) => _tokens[_tokenOffset + n];
        private SyntaxToken EatToken() => _tokens[_tokenOffset++];
        private SyntaxToken EatToken(SyntaxTokenKind expectedKind)
        {
            if (CurrentToken.Kind != expectedKind)
            {
                throw UnexpectedToken(SourceText.FilePath, CurrentToken.Text);
            }

            return _tokens[_tokenOffset++];
        }

        // Statement terminator characters used in NSS: ';', ':'.
        // There may be more than one terminator character in a row.
        private void EatStatementTerminator()
        {
            while (IsStatementTerminator(CurrentToken.Kind))
            {
                EatToken();
            }
        }

        private static bool IsStatementTerminator(SyntaxTokenKind tokenKind)
        {
            return tokenKind == SyntaxTokenKind.SemicolonToken || tokenKind == SyntaxTokenKind.ColonToken;
        }

        private Chapter ParseChapter()
        {
            EatToken(SyntaxTokenKind.ChapterKeyword);
            var name = ParseIdentifier();
            var body = ParseBlock();

            return new Chapter(name, body);
        }

        private Scene ParseScene()
        {
            EatToken(SyntaxTokenKind.SceneKeyword);
            var name = ParseIdentifier();
            var body = ParseBlock();

            return new Scene(name, body);
        }

        private Function ParseFunction()
        {
            EatToken(SyntaxTokenKind.FunctionKeyword);
            var name = ParseIdentifier();
            var parameters = ParseParameterList();

            foreach (var param in parameters)
            {
                _currentParameterList[param.Identifier.Name] = param;
            }

            var body = ParseBlock();
            return new Function(name, parameters, body);
        }

        private ImmutableArray<Parameter> ParseParameterList()
        {
            EatToken(SyntaxTokenKind.OpenParenToken);

            var parameters = ImmutableArray.CreateBuilder<Parameter>();
            while (CurrentToken.Kind != SyntaxTokenKind.CloseParenToken && CurrentToken.Kind != SyntaxTokenKind.EndOfFileToken)
            {
                switch (CurrentToken.Kind)
                {
                    case SyntaxTokenKind.IdentifierToken:
                    case SyntaxTokenKind.StringLiteralToken:
                        var p = new Parameter(ParseIdentifier());
                        parameters.Add(p);
                        break;

                    case SyntaxTokenKind.CommaToken:
                        EatToken();
                        break;

                    default:
                        throw UnexpectedToken(SourceText.FilePath, CurrentToken.Text);
                }
            }

            EatToken(SyntaxTokenKind.CloseParenToken);
            return parameters.ToImmutable();
        }

        private Block ParseBlock()
        {
            EatToken(SyntaxTokenKind.OpenBraceToken);
            var statements = ParseStatements();
            EatToken(SyntaxTokenKind.CloseBraceToken);

            return new Block(statements);
        }

        private ImmutableArray<Statement> ParseStatements()
        {
            var statements = ImmutableArray.CreateBuilder<Statement>();
            while (CurrentToken.Kind != SyntaxTokenKind.CloseBraceToken)
            {
                var statement = ParseStatement();
                if (statement != null)
                {
                    statements.Add(statement);
                }
            }

            return statements.ToImmutable();
        }

        internal Statement ParseStatement()
        {
            switch (CurrentToken.Kind)
            {
                case SyntaxTokenKind.OpenBraceToken:
                    return ParseBlock();

                case SyntaxTokenKind.IdentifierToken:
                case SyntaxTokenKind.StringLiteralToken:
                    if (PeekToken(1).Kind == SyntaxTokenKind.ColonToken)
                    {
                        EatToken(SyntaxTokenKind.IdentifierToken);
                        EatToken(SyntaxTokenKind.ColonToken);
                        return null;
                    }

                    return ParseExpressionStatement();

                case SyntaxTokenKind.IfKeyword:
                    return ParseIfStatement();

                case SyntaxTokenKind.BreakKeyword:
                    return ParseBreakStatement();

                case SyntaxTokenKind.WhileKeyword:
                    return ParseWhileStatement();

                case SyntaxTokenKind.ReturnKeyword:
                    return ParseReturnStatement();

                case SyntaxTokenKind.SelectKeyword:
                    return ParseSelectStatement();

                case SyntaxTokenKind.CaseKeyword:
                    return ParseSelectSection();

                case SyntaxTokenKind.CallChapterKeyword:
                    return ParseCallChapterStatement();

                case SyntaxTokenKind.CallSceneKeyword:
                    return ParseCallSceneStatement();

                case SyntaxTokenKind.DialogueBlockStartTag:
                    return ParseDialogueBlock();

                case SyntaxTokenKind.PXmlString:
                    return new PXmlString(EatToken().Text);

                case SyntaxTokenKind.PXmlLineSeparator:
                    EatToken();
                    return new PXmlLineSeparator();

                default:
                    SkipToNextLine();
                    return null;
            }
        }

        private ExpressionStatement ParseExpressionStatement()
        {
            var expr = ParseExpression();
            EatStatementTerminator();
            return new ExpressionStatement(expr);
        }

        internal Expression ParseExpression()
        {
            return ParseSubExpression(Precedence.Expression);
        }

        private enum Precedence
        {
            Expression = 0,
            Assignment,
            Logical,
            Equality,
            Relational,
            Additive,
            Multiplicative,
            Unary
        }

        private static Precedence GetPrecedence(BinaryOperatorKind operatorKind)
        {
            switch (operatorKind)
            {
                case BinaryOperatorKind.Multiply:
                case BinaryOperatorKind.Divide:
                case BinaryOperatorKind.Remainder:
                    return Precedence.Multiplicative;

                case BinaryOperatorKind.Add:
                case BinaryOperatorKind.Subtract:
                    return Precedence.Additive;

                case BinaryOperatorKind.GreaterThan:
                case BinaryOperatorKind.GreaterThanOrEqual:
                case BinaryOperatorKind.LessThan:
                case BinaryOperatorKind.LessThanOrEqual:
                    return Precedence.Relational;

                case BinaryOperatorKind.Equals:
                case BinaryOperatorKind.NotEquals:
                    return Precedence.Equality;

                case BinaryOperatorKind.And:
                case BinaryOperatorKind.Or:
                    return Precedence.Logical;

                default:
                    throw ExceptionUtils.UnexpectedValue(nameof(operatorKind));
            }
        }

        private Expression ParseSubExpression(Precedence minPrecedence)
        {
            Expression leftOperand;
            Precedence newPrecedence;

            var tk = CurrentToken.Kind;
            if (SyntaxFacts.TryGetUnaryOperatorKind(tk, out var unaryOperator))
            {
                EatToken();
                newPrecedence = Precedence.Unary;
                var operand = ParseSubExpression(newPrecedence);
                leftOperand = new UnaryExpression(operand, unaryOperator);
            }
            else
            {
                leftOperand = ParseTerm(minPrecedence);
            }

            while (true)
            {
                tk = CurrentToken.Kind;
                bool binary;
                BinaryOperatorKind binOpKind = default(BinaryOperatorKind);
                AssignmentOperatorKind assignOpKind = default(AssignmentOperatorKind);

                if (SyntaxFacts.TryGetBinaryOperatorKind(tk, out binOpKind))
                {
                    binary = true;
                }
                else if (SyntaxFacts.TryGetAssignmentOperatorKind(tk, out assignOpKind))
                {
                    binary = false;
                }
                else
                {
                    break;
                }

                newPrecedence = binary ? GetPrecedence(binOpKind) : Precedence.Assignment;
                if (newPrecedence < minPrecedence)
                {
                    break;
                }

                EatToken();

                bool hasRightOperand = assignOpKind != AssignmentOperatorKind.Increment && assignOpKind != AssignmentOperatorKind.Decrement;
                Expression rightOperand = hasRightOperand ? ParseSubExpression(newPrecedence) : leftOperand;
                
                leftOperand = binary ? (Expression)new BinaryExpression(leftOperand, binOpKind, rightOperand) :
                    new AssignmentExpression((Identifier)leftOperand, assignOpKind, rightOperand);
            }

            return leftOperand;
        }

        private Expression ParseTerm(Precedence precedence)
        {
            switch (CurrentToken.Kind)
            {
                case SyntaxTokenKind.IdentifierToken:
                    return IsFunctionCall() ? (Expression)ParseFunctionCall() : ParseIdentifier();

                case SyntaxTokenKind.StringLiteralToken:
                    return IsParameter() ? (Expression)ParseIdentifier() : ParseLiteral();

                case SyntaxTokenKind.NumericLiteralToken:
                case SyntaxTokenKind.NullKeyword:
                case SyntaxTokenKind.TrueKeyword:
                case SyntaxTokenKind.FalseKeyword:
                    return ParseLiteral();

                case SyntaxTokenKind.OpenParenToken:
                    EatToken(SyntaxTokenKind.OpenParenToken);
                    var expr = ParseSubExpression(Precedence.Expression);
                    EatToken(SyntaxTokenKind.CloseParenToken);
                    return expr;

                case SyntaxTokenKind.AtToken:
                    return ParseDeltaExpression(precedence);

                default:
                    throw UnexpectedToken(SourceText.FilePath, CurrentToken.Text);
            }
        }

        private DeltaExpression ParseDeltaExpression(Precedence precedence)
        {
            EatToken(SyntaxTokenKind.AtToken);
            var expr = ParseSubExpression(precedence);
            return new DeltaExpression(expr);
        }

        private Literal ParseLiteral()
        {
            switch (CurrentToken.Kind)
            {
                case SyntaxTokenKind.NumericLiteralToken:
                    var token = (NumericLiteralToken)EatToken();
                    return new Literal(token.Text, ConstantValue.Create(token.DoubleValue));

                case SyntaxTokenKind.StringLiteralToken:
                    var tk = (StringLiteralToken)EatToken();
                    return new Literal(tk.StringValue, ConstantValue.Create(tk.StringValue));

                case SyntaxTokenKind.NullKeyword:
                    EatToken();
                    return Literal.Null;

                case SyntaxTokenKind.TrueKeyword:
                    EatToken();
                    return Literal.True;

                case SyntaxTokenKind.FalseKeyword:
                    EatToken();
                    return Literal.False;

                default:
                    throw UnexpectedToken(SourceText.FilePath, CurrentToken.Text);
            }
        }

        private Identifier ParseIdentifier()
        {
            Debug.Assert(CurrentToken.Kind == SyntaxTokenKind.IdentifierToken || CurrentToken.Kind == SyntaxTokenKind.StringLiteralToken);

            var token = EatToken();
            if (token.Kind == SyntaxTokenKind.IdentifierToken)
            {
                var idToken = (IdentifierToken)token;
                return new Identifier(idToken.StringValue, idToken.Sigil, idToken.IsQuoted);
            }

            var literal = (StringLiteralToken)token;
            return new Identifier(literal.StringValue, SigilKind.None, isQuoted: true);
        }

        private bool IsFunctionCall()
        {
            Debug.Assert(CurrentToken.Kind == SyntaxTokenKind.IdentifierToken || CurrentToken.Kind == SyntaxTokenKind.StringLiteralToken);

            var next = PeekToken(1);
            return next.Kind == SyntaxTokenKind.OpenParenToken;
        }

        private bool IsParameter()
        {
            switch (CurrentToken.Kind)
            {
                case SyntaxTokenKind.IdentifierToken:
                case SyntaxTokenKind.StringLiteralToken:
                    return _currentParameterList.ContainsKey((string)CurrentToken.Value);

                default:
                    return false;
            }
        }

        private FunctionCall ParseFunctionCall()
        {
            var targetName = ParseIdentifier();

            var args = ImmutableArray<Expression>.Empty;
            if (CurrentToken.Kind == SyntaxTokenKind.OpenParenToken)
            {
                args = ParseArgumentList();
            }

            return new FunctionCall(targetName, args);
        }

        private ImmutableArray<Expression> ParseArgumentList()
        {
            EatToken(SyntaxTokenKind.OpenParenToken);

            var args = ImmutableArray.CreateBuilder<Expression>();
            while (CurrentToken.Kind != SyntaxTokenKind.CloseParenToken && CurrentToken.Kind != SyntaxTokenKind.EndOfFileToken)
            {
                switch (CurrentToken.Kind)
                {
                    case SyntaxTokenKind.NumericLiteralToken:
                    case SyntaxTokenKind.StringLiteralToken:
                    case SyntaxTokenKind.IdentifierToken:
                    case SyntaxTokenKind.NullKeyword:
                    case SyntaxTokenKind.TrueKeyword:
                    case SyntaxTokenKind.FalseKeyword:
                        args.Add(ParseExpression());
                        break;

                    case SyntaxTokenKind.CommaToken:
                    case SyntaxTokenKind.DotToken:
                    // Ampersand? Why?
                    case SyntaxTokenKind.AmpersandToken:
                        EatToken();
                        break;

                    default:
                        var expr = ParseExpression();
                        args.Add(expr);
                        break;
                }
            }

            EatToken(SyntaxTokenKind.CloseParenToken);
            return args.ToImmutable();
        }

        private IfStatement ParseIfStatement()
        {
            EatToken(SyntaxTokenKind.IfKeyword);
            EatToken(SyntaxTokenKind.OpenParenToken);
            var condition = ParseExpression();
            EatToken(SyntaxTokenKind.CloseParenToken);

            Statement ifTrue = ParseStatement();
            Statement ifFalse = null;
            if (CurrentToken.Kind == SyntaxTokenKind.ElseKeyword)
            {
                EatToken();
                ifFalse = ParseStatement();
            }

            return new IfStatement(condition, ifTrue, ifFalse);
        }

        private BreakStatement ParseBreakStatement()
        {
            EatToken(SyntaxTokenKind.BreakKeyword);
            EatStatementTerminator();
            return new BreakStatement();
        }

        private WhileStatement ParseWhileStatement()
        {
            EatToken(SyntaxTokenKind.WhileKeyword);
            EatToken(SyntaxTokenKind.OpenParenToken);
            var condition = ParseExpression();
            EatToken(SyntaxTokenKind.CloseParenToken);
            var body = ParseStatement();

            return new WhileStatement(condition, body);
        }

        private ReturnStatement ParseReturnStatement()
        {
            EatToken(SyntaxTokenKind.ReturnKeyword);
            EatStatementTerminator();

            return new ReturnStatement();
        }

        private SelectStatement ParseSelectStatement()
        {
            EatToken(SyntaxTokenKind.SelectKeyword);
            var body = ParseBlock();
            return new SelectStatement(body);
        }

        private SelectSection ParseSelectSection()
        {
            EatToken(SyntaxTokenKind.CaseKeyword);
            string labelName = ConsumeTextUntil(tk => tk == SyntaxTokenKind.OpenBraceToken || tk == SyntaxTokenKind.ColonToken);
            if (CurrentToken.Kind == SyntaxTokenKind.ColonToken)
            {
                EatToken();
            }

            var body = ParseBlock();
            return new SelectSection(new Identifier(labelName), body);
        }

        private CallChapterStatement ParseCallChapterStatement()
        {
            EatToken(SyntaxTokenKind.CallChapterKeyword);
            string filePath = ConsumeTextUntil(tk => tk == SyntaxTokenKind.SemicolonToken);
            EatStatementTerminator();
            return new CallChapterStatement(filePath);
        }

        private CallSceneStatement ParseCallSceneStatement()
        {
            EatToken(SyntaxTokenKind.CallSceneKeyword);
            (SourceFileReference file, string scene) = ParseSymbolPath();
            EatStatementTerminator();
            return new CallSceneStatement(file, scene);
        }

        // Parses call_scene specific symbol path syntax.
        // call_scene can be followed by either '@->{localSymbolName}' (e.g. '@->SelectStoryModeA')
        // or '{path}->{symbolName}' (e.g. 'nss/extra_gallery.nss->extra_gallery_main').
        private (string filePath, string symbolName) ParseSymbolPath()
        {
            if (CurrentToken.Kind == SyntaxTokenKind.AtArrowToken)
            {
                EatToken();
            }
            
            string filePath = null, symbolName = null;
            string part = ConsumeTextUntil(tk => tk == SyntaxTokenKind.SemicolonToken || tk == SyntaxTokenKind.ArrowToken);
            if (CurrentToken.Kind == SyntaxTokenKind.ArrowToken)
            {
                EatToken();
                filePath = part;
                symbolName = ConsumeTextUntil(tk => tk == SyntaxTokenKind.SemicolonToken);
            }
            else
            {
                symbolName = part;
            }

            filePath = filePath ?? SourceText.FilePath;
            return (filePath, symbolName);
        }

        // Consumes tokens until the specified condition is met. 
        // Returns the concatenation of their .Text values.
        private string ConsumeTextUntil(Func<SyntaxTokenKind, bool> condition)
        {
            string s = string.Empty;
            SyntaxTokenKind tk;
            while ((tk = CurrentToken.Kind) != SyntaxTokenKind.EndOfFileToken && !condition(tk))
            {
                s += EatToken().Text;
            }

            return s;
        }

        private DialogueBlock ParseDialogueBlock()
        {
            var startTag = EatToken(SyntaxTokenKind.DialogueBlockStartTag);
            string associatedBox = (string)startTag.Value;

            var boxName = (string)EatToken(SyntaxTokenKind.DialogueBlockIdentifier).Value;
            var statements = ImmutableArray.CreateBuilder<Statement>();
            while (CurrentToken.Kind != SyntaxTokenKind.DialogueBlockEndTag)
            {
                var statement = ParseStatement();
                if (statement != null)
                {
                    statements.Add(statement);
                }
            }

            EatToken(SyntaxTokenKind.DialogueBlockEndTag);
            var id = new Identifier(boxName);
            return new DialogueBlock(id, associatedBox, new Block(statements.ToImmutable()));
        }

        private static NsParseException UnexpectedToken(string scriptName, string token)
        {
            return new NsParseException($"Parsing '{scriptName}' failed: unexpected token '{token}'");
        }
    }
}

using NitroSharp.NsScript.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace NitroSharp.NsScript.Syntax
{
    internal sealed class Parser
    {
        private readonly Lexer _lexer;
        private IReadOnlyList<SyntaxToken> _tokens;
        private int _tokenOffset;

        // Used to differentiate between string literals and quoted identifiers (aka string parameter references).
        // Normally, a parser should not keep track of something like that, but this is NSS.
        private readonly Dictionary<string, Parameter> _currentParameterList;

        private readonly DiagnosticBuilder _diagnostics;

        public Parser(Lexer lexer)
        {
            _lexer = lexer;
            _diagnostics = new DiagnosticBuilder();
            _tokens = Lex();
            _currentParameterList = new Dictionary<string, Parameter>();
        }

        private SyntaxToken PeekToken(int n) => _tokens[_tokenOffset + n];
        private SyntaxToken CurrentToken => _tokens[_tokenOffset];
        private SyntaxToken PreviousToken => PeekToken(-1);
        private SourceText SourceText => _lexer.SourceText;

        internal DiagnosticBuilder DiagnosticBuilder => _diagnostics;

        private IReadOnlyList<SyntaxToken> Lex()
        {
            int capacity = Math.Min(4096, Math.Max(32, SourceText.Source.Length / 2));
            var tokens = new List<SyntaxToken>(capacity);
            SyntaxToken token = null;
            do
            {
                token = _lexer.Lex();
                tokens.Add(token);

                if (token.HasDiagnostics)
                {
                    _diagnostics.Add(token.Diagnostic);
                }

                if (token.Kind == SyntaxTokenKind.EndOfFileToken)
                {
                    break;
                }

            } while (token.Kind != SyntaxTokenKind.EndOfFileToken);

            return tokens;
        }

        private SyntaxToken EatToken()
        {
            var ct = CurrentToken;
            _tokenOffset++;
            return ct;
        }

        private SyntaxToken EatToken(SyntaxTokenKind expectedKind)
        {
            var ct = CurrentToken;
            if (ct.Kind != expectedKind)
            {
                return CreateMissingToken(expectedKind, ct.Kind);
            }

            _tokenOffset++;
            return ct;
        }

        private SyntaxToken CreateMissingToken(SyntaxTokenKind expected, SyntaxTokenKind actual)
        {
            TokenExpected(expected, actual);

            var span = GetSpanForMissingToken();
            return SyntaxToken.Missing(expected, span);
        }

        private void EatTokens(int count)
        {
            _tokenOffset += count;
        }

        private void EatStrayToken()
        {
            var token = PeekToken(0);
            Report(DiagnosticId.StrayToken, token.Text);
            EatToken();
        }

        // Statement terminator characters used in NSS: ';', ':'.
        // There may be more than one terminator character in a row.
        private void EatStatementTerminator()
        {
            int tokensConsumed = 0;
            while (SyntaxFacts.IsStatementTerminator(CurrentToken.Kind))
            {
                EatToken();
                tokensConsumed++;
            }

            if (tokensConsumed == 0)
            {
                _diagnostics.Report(DiagnosticId.MissingStatementTerminator, GetSpanForMissingToken());
            }
        }

        public SourceFile ParseSourceFile()
        {
            var fileReferences = ImmutableArray.CreateBuilder<SourceFileReference>();
            SyntaxTokenKind tk;
            while ((tk = CurrentToken.Kind) != SyntaxTokenKind.EndOfFileToken && !SyntaxFacts.CanStartDeclaration(tk))
            {
                switch (CurrentToken.Kind)
                {
                    case SyntaxTokenKind.IncludeDirective:
                        EatToken();
                        var filePath = EatToken(SyntaxTokenKind.StringLiteralToken);
                        fileReferences.Add(new SourceFileReference((string)filePath.Value));
                        break;

                    case SyntaxTokenKind.SemicolonToken:
                        Report(DiagnosticId.MisplacedSemicolon);
                        EatToken();
                        break;

                    default:
                        EatStrayToken();
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

                    // Lines starting with a '.' are treated as comments.
                    case SyntaxTokenKind.DotToken:
                        SkipToNextLine();
                        break;

                    default:
                        Report(DiagnosticId.ExpectedMemberDeclaration, CurrentToken.Text);
                        SkipToNextLine();
                        break;
                }
            }

            return new SourceFile(members.ToImmutable(), fileReferences.ToImmutable());
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
                        EatStrayToken();
                        break;
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
            Statement statement = null;
            while (statement == null)
            {
                try
                {
                    statement = ParseStatementCore();
                }
                catch (ParseError)
                {
                    var tk = CurrentToken.Kind;
                    if (tk == SyntaxTokenKind.EndOfFileToken || tk == SyntaxTokenKind.CloseBraceToken)
                    {
                        return null;
                    }
                }
            }

            return statement;
        }

        private Statement ParseStatementCore()
        {
            switch (CurrentToken.Kind)
            {
                case SyntaxTokenKind.OpenBraceToken:
                    return ParseBlock();

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

                case SyntaxTokenKind.LessThanToken:
                    HandleStrayPXmlElement();
                    goto default;

                case SyntaxTokenKind.DotToken:
                    SkipToNextLine();
                    throw new ParseError();

                case SyntaxTokenKind.IdentifierToken:
                case SyntaxTokenKind.StringLiteralToken:
                    if (IsArgumentListOrSemicolon())
                    {
                        return ParseFunctionCallWithOmittedParentheses();
                    }
                    goto default;

                default:
                    return ParseExpressionStatement();
            }
        }

        // Checks if the current line is a stray PXml element.
        // In case it is one, reports an error, skips the current line and throws a ParseError.
        private void HandleStrayPXmlElement()
        {
            Debug.Assert(CurrentToken.Kind == SyntaxTokenKind.LessThanToken);
            int currentLine = GetLineNumber();

            int n = 0;
            SyntaxToken token = null;
            // Look for the closing '>'
            while ((token = PeekToken(n)).Kind != SyntaxTokenKind.GreaterThanToken)
            {
                if (token.Kind == SyntaxTokenKind.EndOfFileToken)
                {
                    return;
                }

                n++;
            }

            // Check if the current line ends with the '>' character that we found
            if (GetLineNumber(PeekToken(n + 1)) != currentLine)
            {
                Report(DiagnosticId.StrayPXmlElement, SourceText.Lines[currentLine].Span);
                EatTokens(n + 1); // skip to the next line
                throw new ParseError();
            }
        }

        private ExpressionStatement ParseExpressionStatement()
        {
            var start = CurrentToken.Span.Start;
            var expr = ParseExpression();
            var end = PreviousToken.Span.End;

            if (!SyntaxFacts.IsStatementExpression(expr))
            {
                var diagnosticSpan = new TextSpan(start, end - start);
                Report(DiagnosticId.InvalidExpressionStatement, diagnosticSpan);
                EatStatementTerminator();
                throw new ParseError();
            }

            EatStatementTerminator();
            return new ExpressionStatement(expr);
        }

        private ExpressionStatement ParseFunctionCallWithOmittedParentheses()
        {
            var call = ParseFunctionCall();
            EatStatementTerminator();
            return new ExpressionStatement(call);
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

                leftOperand = binary
                    ? (Expression)new BinaryExpression(leftOperand, binOpKind, rightOperand)
                    : new AssignmentExpression(leftOperand, assignOpKind, rightOperand);
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
                    Report(DiagnosticId.InvalidExpressionTerm, CurrentToken.Text);
                    EatToken();
                    throw new ParseError();
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
                    // Should never happen.
                    throw new InvalidOperationException("Expected a literal.");
            }
        }

        private Identifier ParseIdentifier(bool isFunctionName = false)
        {
            Debug.Assert(CurrentToken.Kind == SyntaxTokenKind.IdentifierToken || CurrentToken.Kind == SyntaxTokenKind.StringLiteralToken);

            var token = EatToken();
            if (token.Kind == SyntaxTokenKind.IdentifierToken)
            {
                var idToken = (IdentifierToken)token;
                return new Identifier(idToken.StringValue, idToken.Sigil, idToken.IsQuoted && !isFunctionName);
            }

            var literal = (StringLiteralToken)token;
            return new Identifier(literal.StringValue, SigilKind.None, isQuoted: !isFunctionName);
        }

        private bool IsFunctionCall()
        {
            return PeekToken(1).Kind == SyntaxTokenKind.OpenParenToken;
        }

        private bool IsArgumentListOrSemicolon()
        {
            SyntaxTokenKind peek;
            int n = 0;
            while ((peek = PeekToken(n).Kind) != SyntaxTokenKind.EndOfFileToken)
            {
                switch (peek)
                {
                    case SyntaxTokenKind.NullKeyword:
                    case SyntaxTokenKind.TrueKeyword:
                    case SyntaxTokenKind.FalseKeyword:
                    case SyntaxTokenKind.IdentifierToken:
                    case SyntaxTokenKind.StringLiteralToken:
                    case SyntaxTokenKind.NumericLiteralToken:
                    case SyntaxTokenKind.CommaToken:
                    case SyntaxTokenKind.DotToken:
                        n++;
                        break;

                    case SyntaxTokenKind.SemicolonToken:
                    case SyntaxTokenKind.CloseBraceToken:
                        return true;

                    default:
                        return false;
                }
            }

            return false;
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
            var targetName = ParseIdentifier(isFunctionName: true);
            var args = ParseArgumentList();
            return new FunctionCall(targetName, args);
        }

        private ImmutableArray<Expression> ParseArgumentList()
        {
            if (SyntaxFacts.IsStatementTerminator(CurrentToken.Kind))
            {
                return ImmutableArray<Expression>.Empty;
            }

            EatToken(SyntaxTokenKind.OpenParenToken);

            var args = ImmutableArray.CreateBuilder<Expression>();
            SyntaxTokenKind tk;
            while ((tk = CurrentToken.Kind) != SyntaxTokenKind.CloseParenToken && tk != SyntaxTokenKind.SemicolonToken
                && tk != SyntaxTokenKind.EndOfFileToken)
            {
                switch (tk)
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
        // or '{filepath}->{symbolName}' (e.g. 'nss/extra_gallery.nss->extra_gallery_main').
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

        private int GetLineNumber() => SourceText.GetLineNumberFromPosition(CurrentToken.Span.Start);
        private int GetLineNumber(SyntaxToken token) => SourceText.GetLineNumberFromPosition(token.Span.Start);

        private void SkipToNextLine()
        {
            int currentLine = GetLineNumber();
            int lineCount = SourceText.Lines.Length;
            do
            {
                var tk = EatToken();
                if (tk.Kind == SyntaxTokenKind.EndOfFileToken)
                {
                    break;
                }

            } while (currentLine <= lineCount && GetLineNumber() == currentLine);
        }

        private void Report(DiagnosticId diagnosticId)
        {
            _diagnostics.Report(diagnosticId, CurrentToken.Span);
        }

        private void Report(DiagnosticId diagnosticId, TextSpan span)
        {
            _diagnostics.Report(diagnosticId, span);
        }

        private void Report(DiagnosticId diagnosticId, params object[] arguments)
        {
            _diagnostics.Report(diagnosticId, CurrentToken.Span, arguments);
        }

        private TextSpan GetSpanForMissingToken()
        {
            if (_tokenOffset > 0)
            {
                var prevToken = PeekToken(-1);
                var prevTokenLine = SourceText.GetLineFromPosition(prevToken.Span.End);
                var currentTokenLine = SourceText.GetLineFromPosition(CurrentToken.Span.Start);
                if (currentTokenLine != prevTokenLine)
                {
                    int newLineSequenceLength = currentTokenLine.Span.Start - prevTokenLine.Span.End;
                    return new TextSpan(prevToken.Span.End, newLineSequenceLength);
                }
            }

            return CurrentToken.Span;
        }

        private void TokenExpected(SyntaxTokenKind expected, SyntaxTokenKind actual)
        {
            string expectedText = SyntaxFacts.GetText(expected);
            string actualText = SyntaxFacts.GetText(actual);

            Report(DiagnosticId.TokenExpected, expectedText, actualText);
        }

        // Yeah... I'm using exceptions for control flow.
        private sealed class ParseError : Exception
        {
        }
    }
}

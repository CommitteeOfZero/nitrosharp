using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using NitroSharp.Common;
using NitroSharp.NsScript.Utilities;

namespace NitroSharp.NsScript.Syntax
{
    internal sealed class Parser
    {
        private readonly Lexer _lexer;
        private readonly ArrayBuilder<SyntaxToken> _tokens;
        private SyntaxToken _currentToken;
        private int _tokenIndex;

        // It's not always possible for the lexer to tell whether something is a string literal
        // or an identifier, since some identifiers (more specifically, parameter names and
        // parameter references) in NSS can also be enclosed in quotes. In such cases, the lexer
        // outputs a StringLiteralOrQuotedIdentifier token and lets the parser decide whether
        // it's a string literal or an identifier. In order to do that, the parser needs to
        // keep track of the parameters that can be referenced in the current scope.
        //
        // Example:
        // function foo("stringParameter1", "stringParameter2") {
        //                     ↑                   ↑
        //     $bar = "stringParameter1" + "stringParameter2";
        // }             <identifier>         <identifier>
        private readonly Dictionary<string, Parameter> _parameterMap;

        private readonly StringInternTable _internTable;

        private readonly ImmutableArray<Parameter>.Builder _parameters;
        private readonly ImmutableArray<DialogueBlock>.Builder _dialogueBlocks;
        private readonly DiagnosticBuilder _diagnosticBuilder;

        public Parser(Lexer lexer)
        {
            _lexer = lexer;
            _diagnosticBuilder = lexer.Diagnostics;
            _internTable = new StringInternTable();
            _tokens = Lex();
            _parameterMap = [];
            _parameters = ImmutableArray.CreateBuilder<Parameter>();
            _dialogueBlocks = ImmutableArray.CreateBuilder<DialogueBlock>();
            if (Tokens is [var firstToken, ..])
            {
                _currentToken = firstToken;
            }
        }

        private SourceText SourceText => _lexer.SourceText;
        private Span<SyntaxToken> Tokens => _tokens.AsSpan();
        private int LexerPosition => _currentToken.TextSpan.Start;

        private SyntaxToken PeekToken(int n) => Tokens[_tokenIndex + n];

        private bool IsAtEnd() => _currentToken.Kind == SyntaxTokenKind.EndOfFile;

        private TextLine GetCurrentLine()
            => SourceText.GetLine(GetLineNumber());

        private int GetLineNumber()
            => SourceText.GetLineNumberFromPosition(LexerPosition);

        private int GetLineNumber(SyntaxToken token)
            => SourceText.GetLineNumberFromPosition(token.TextSpan.Start);

        private ArrayBuilder<SyntaxToken> Lex()
        {
            int capacity = Math.Max(32, SourceText.Source.Length / 6);
            var tokens = new ArrayBuilder<SyntaxToken>(capacity);
            ref SyntaxToken token = ref tokens.Add();
            do
            {
                _lexer.Lex(ref token);
                if (token.Kind == SyntaxTokenKind.EndOfFile)
                {
                    break;
                }

                token = ref tokens.Add();
            } while (token.Kind != SyntaxTokenKind.EndOfFile);

            return tokens;
        }

        private SyntaxToken EatToken()
        {
            SyntaxToken tk = _currentToken;
            if (tk.Kind != SyntaxTokenKind.EndOfFile)
            {
                _tokenIndex++;
            }
            _currentToken = Tokens[_tokenIndex];
            return tk;
        }

        private SyntaxToken EatToken(SyntaxTokenKind expectedKind)
        {
            SyntaxToken tk = _currentToken;
            if (tk.Kind == expectedKind)
            {
                _tokenIndex++;
            }
            _currentToken = Tokens[_tokenIndex];
            return tk.Kind == expectedKind ? tk : CreateMissingToken(expectedKind);
        }

        private bool TryEatToken(SyntaxTokenKind expectedKind)
        {
            if (_currentToken.Kind != expectedKind) { return false; }

            _tokenIndex++;
            _currentToken = Tokens[_tokenIndex];
            return true;
        }

        private string GetText(in SyntaxToken token)
            => SourceText.GetText(token.TextSpan);

        private string GetValueText(in SyntaxToken token)
            => SourceText.GetText(token.GetValueSpan());

        private string InternValueText(in SyntaxToken token)
            => _internTable.Add(SourceText.GetCharacterSpan(token.GetValueSpan()));

        [MethodImpl(MethodImplOptions.NoInlining)]
        private SyntaxToken CreateMissingToken(SyntaxTokenKind expected)
        {
            ReportTokenExpected(expected);
            TextSpan span = GetSpanForMissingToken();
            return new SyntaxToken(expected, span, SyntaxTokenFlags.Empty);
        }

        private void EatTokens(int count)
        {
            _tokenIndex += count;
            _currentToken = Tokens[_tokenIndex];
        }

        private TextSpan SpanFrom(SyntaxNode firstNode)
            => TextSpan.FromBounds(firstNode.Span.Start, LexerPosition);

        private TextSpan SpanFrom(in SyntaxToken firstToken)
            => TextSpan.FromBounds(firstToken.TextSpan.Start, LexerPosition);

        public SourceFileRoot ParseSourceFile()
        {
            var fileReferences = ImmutableArray.CreateBuilder<Spanned<string>>();
            (uint chapterCount, uint sceneCount, uint functionCount) subroutineCounts = default;
            while (!SyntaxFacts.CanStartDeclaration(_currentToken.Kind) && !IsAtEnd())
            {
                SkipOnCurrentLineUntil(tk => tk == SyntaxTokenKind.IncludeDirective
                    || SyntaxFacts.CanStartDeclaration(tk), report: true);

                if (TryEatToken(SyntaxTokenKind.IncludeDirective))
                {
                    SyntaxToken filePath = EatToken(SyntaxTokenKind.StringLiteralOrQuotedIdentifier);
                    fileReferences.Add(new Spanned<string>(GetValueText(filePath), filePath.TextSpan));
                    if (SyntaxFacts.IsStatementTerminator(_currentToken.Kind))
                    {
                        EatToken();
                    }
                }
            }

            var subroutines = ImmutableArray.CreateBuilder<SubroutineDeclaration>();
            while (!IsAtEnd())
            {
                _dialogueBlocks.Clear();
                switch (_currentToken.Kind)
                {
                    case SyntaxTokenKind.ChapterKeyword:
                        subroutines.Add(ParseChapterDeclaration());
                        subroutineCounts.chapterCount++;
                        break;
                    case SyntaxTokenKind.SceneKeyword:
                        subroutines.Add(ParseSceneDeclaration());
                        subroutineCounts.sceneCount++;
                        break;
                    case SyntaxTokenKind.FunctionKeyword:
                        subroutines.Add(ParseFunctionDeclaration());
                        subroutineCounts.functionCount++;
                        break;
                    // Lines starting with a '.' are treated as comments.
                    case SyntaxTokenKind.Dot:
                        SkipToNextLine(out _);
                        break;
                    default:
                        Report(DiagnosticId.ExpectedSubroutineDeclaration, GetText(_currentToken));
                        SkipToNextLine(out _);
                        break;
                }
            }

            var span = new TextSpan(0, SourceText.Length);
            return new SourceFileRoot(
                subroutines.ToImmutable(),
                fileReferences.ToImmutable(),
                subroutineCounts,
                span
            );
        }

        public SubroutineDeclaration ParseSubroutineDeclaration()
        {
            _dialogueBlocks.Clear();
            switch (_currentToken.Kind)
            {
                case SyntaxTokenKind.ChapterKeyword:
                    return ParseChapterDeclaration();
                case SyntaxTokenKind.SceneKeyword:
                    return ParseSceneDeclaration();
                case SyntaxTokenKind.FunctionKeyword:
                    _parameterMap.Clear();
                    return ParseFunctionDeclaration();
                default:
                    throw new InvalidOperationException($"{_currentToken.Kind} cannot start a declaration.");
            }
        }

        private ChapterDeclaration ParseChapterDeclaration()
        {
            SyntaxToken keyword = EatToken(SyntaxTokenKind.ChapterKeyword);
            Spanned<string> name = ParseIdentifier();
            Block body = ParseBlock();
            ImmutableArray<DialogueBlock> dialogueBlocks = _dialogueBlocks.ToImmutable();
            return new ChapterDeclaration(name, body, dialogueBlocks, SpanFrom(keyword));
        }

        private SceneDeclaration ParseSceneDeclaration()
        {
            SyntaxToken keyword = EatToken(SyntaxTokenKind.SceneKeyword);
            Spanned<string> name = ParseIdentifier();
            Block body = ParseBlock();
            ImmutableArray<DialogueBlock> dialogueBlocks = _dialogueBlocks.ToImmutable();
            return new SceneDeclaration(name, body, dialogueBlocks, SpanFrom(keyword));
        }

        private FunctionDeclaration ParseFunctionDeclaration()
        {
            SyntaxToken keyword = EatToken(SyntaxTokenKind.FunctionKeyword);
            Spanned<string> name = ParseIdentifier();
            ImmutableArray<Parameter> parameters = ParseParameterList();

            Block body = ParseBlock();
            ImmutableArray<DialogueBlock> dialogueBlocks = _dialogueBlocks.ToImmutable();
            return new FunctionDeclaration(name, parameters, body, dialogueBlocks, SpanFrom(keyword));
        }

        private ImmutableArray<Parameter> ParseParameterList()
        {
            SkipOnCurrentLineUntil(SyntaxTokenKind.OpenParen, report: true);
            EatToken(SyntaxTokenKind.OpenParen);

            _parameters.Clear();
            while (_currentToken.Kind is not (SyntaxTokenKind.CloseParen or SyntaxTokenKind.EndOfFile))
            {
                switch (_currentToken.Kind)
                {
                    case SyntaxTokenKind.Identifier:
                    case SyntaxTokenKind.StringLiteralOrQuotedIdentifier:
                        Spanned<string> identifier = ParseIdentifier();
                        var parameter = new Parameter(identifier.Value, identifier.Span);
                        _parameters.Add(parameter);
                        _parameterMap[parameter.Name] = parameter;
                        break;
                    case SyntaxTokenKind.Comma:
                        EatToken();
                        break;
                    default:
                        EatStrayToken();
                        break;
                }
            }

            EatToken(SyntaxTokenKind.CloseParen);
            return _parameters.ToImmutable();
        }

        private Block ParseBlock()
        {
            SyntaxToken openBrace = EatToken(SyntaxTokenKind.OpenBrace);
            ImmutableArray<Statement> statements = ParseStatements();
            EatToken(SyntaxTokenKind.CloseBrace);
            return new Block(statements, SpanFrom(openBrace));
        }

        private ImmutableArray<Statement> ParseStatements()
        {
            var statements = ImmutableArray.CreateBuilder<Statement>();
            while (_currentToken.Kind is not (SyntaxTokenKind.CloseBrace or SyntaxTokenKind.EndOfFile))
            {
                Statement statement = ParseStatement(skipErrorStatements: false);
                if (statement.Kind == SyntaxNodeKind.DialogueBlock)
                {
                    _dialogueBlocks.Add((DialogueBlock)statement);
                }

                statements.Add(statement);
            }

            return statements.ToImmutable();
        }

        internal Statement ParseStatement(bool skipErrorStatements = true)
        {
            int startOffset = LexerPosition;
            Statement? statement;
            do
            {
                int prevDiagnosticCount = _diagnosticBuilder.Count;
                statement = ParseStatementCore();
                bool producedDiagnostics = _diagnosticBuilder.Count > prevDiagnosticCount;

                if (statement.Kind == SyntaxNodeKind.ErrorStatement)
                {
                    statement = reportError(producedDiagnostics);
                }
                if (statement.Kind != SyntaxNodeKind.ErrorStatement || !skipErrorStatements)
                {
                    break;
                }

                SyntaxTokenKind tk = _currentToken.Kind;
                if (tk is SyntaxTokenKind.EndOfFile or SyntaxTokenKind.CloseBrace)
                {
                    return CreateErrorStatement(startOffset);
                }
            } while (!IsAtEnd());

            return statement;

            [MethodImpl(MethodImplOptions.NoInlining)]
            Statement reportError(bool producedDiagnostics)
            {
                int errorStart = statement.Span.Start;
                SkipToNextStatementOrLine(out int tokensSkipped);
                statement = new ErrorStatement(TextSpan.FromBounds(errorStart, LexerPosition));
                if (!producedDiagnostics)
                {
                    ReportSkippedBadSyntax(statement.Span, tokensSkipped);
                }

                return statement;
            }
        }

        private Statement ParseStatementCore()
        {
            switch (_currentToken.Kind)
            {
                case SyntaxTokenKind.OpenBrace:
                    return ParseBlock();
                case SyntaxTokenKind.IfKeyword:
                    return ParseIfStatement();
                // case SyntaxTokenKind.ElseKeyword:
                //     return ParseMisplacedElse();
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
                case SyntaxTokenKind.LessThan:
                    if (TryCreateStrayMarkupNode() is { } strayMarkupNode)
                    {
                        return strayMarkupNode;
                    }
                    goto default;
                case SyntaxTokenKind.Dot:
                    return CreateErrorStatement(LexerPosition);

                case SyntaxTokenKind.Identifier when PeekToken(1).Kind == SyntaxTokenKind.Colon:
                    return ParseLabeledStatement();

                case SyntaxTokenKind.Identifier:
                case SyntaxTokenKind.StringLiteralOrQuotedIdentifier:
                    if (IsArgumentListOrSemicolon())
                    {
                        return ParseFunctionCallWithOmittedParentheses();
                    }
                    goto default;

                default:
                    return SyntaxFacts.CanStartExpressionTerm(_currentToken.Kind)
                        ? ParseExpressionStatement()
                        : CreateErrorStatement(LexerPosition);
            }
        }

        private Statement ParseLabeledStatement()
        {
            Spanned<string> _ = ParseIdentifier();
            EatToken(SyntaxTokenKind.Colon);
            return ParseStatement();
        }

        private Statement ParseExpressionStatement()
        {
            Expression expr = ParseExpression();

            bool isValid = SyntaxFacts.IsStatementExpression(expr)
                && expr.Kind != SyntaxNodeKind.ErrorExpression;
            if (isValid)
            {
                EatStatementTerminator();
            }

            var span = TextSpan.FromBounds(expr.Span.Start, LexerPosition);
            return isValid ? new ExpressionStatement(expr, span) : new ErrorStatement(span);
        }

        // Statement terminator characters used in NSS: ';', ':'.
        // There may be more than one terminator character in a row.
        private void EatStatementTerminator()
        {
            int tokensConsumed = 0;
            while (SyntaxFacts.IsStatementTerminator(_currentToken.Kind))
            {
                EatToken();
                tokensConsumed++;
            }

            if (tokensConsumed == 0)
            {
                doReportMissing();
            }

            return;

            [MethodImpl(MethodImplOptions.NoInlining)]
            void doReportMissing()
            {
                TextSpan span = GetSpanForMissingToken();
                if (_tokenIndex > 0)
                {
                    SyntaxToken prevToken = PeekToken(-1);
                    int prevLine = GetLineNumber(prevToken);
                    int currentLine = GetLineNumber();
                    if (prevLine != currentLine)
                    {
                        span = new TextSpan(prevToken.TextSpan.End, length: 0);
                    }
                }

                Report(DiagnosticId.MissingStatementTerminator, span);
            }
        }

        private ExpressionStatement ParseFunctionCallWithOmittedParentheses()
        {
            FunctionCallExpression call = ParseFunctionCall();
            EatStatementTerminator();
            return new ExpressionStatement(call, SpanFrom(call));
        }

        internal Expression ParseExpression()
        {
            return ParseSubExpression(Precedence.Expression);
        }

        internal static Precedence GetPrecedence(BinaryOperatorKind operatorKind)
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
                    throw ThrowHelper.UnexpectedValueOf<BinaryOperatorKind>();
            }
        }

        private Expression ParseSubExpression(Precedence minPrecedence)
        {
            Expression? leftOperand;
            Precedence newPrecedence;

            SyntaxTokenKind tk = _currentToken.Kind;
            TextSpan tkSpan = _currentToken.TextSpan;
            if (SyntaxFacts.TryGetUnaryOperatorKind(tk) is { } unaryOperator)
            {
                EatToken();
                newPrecedence = Precedence.Unary;
                Expression operand = ParseSubExpression(newPrecedence);
                var fullSpan = TextSpan.FromBounds(tkSpan.Start, operand.Span.End);
                leftOperand = new UnaryExpression(
                    operand,
                    new Spanned<UnaryOperatorKind>(unaryOperator, tkSpan),
                    fullSpan
                );
            }
            else
            {
                leftOperand = ParseTerm();
            }

            while (true)
            {
                tk = _currentToken.Kind;
                tkSpan = _currentToken.TextSpan;
                BinaryOperatorKind? binOpKind = SyntaxFacts.TryGetBinaryOperatorKind(tk);
                AssignmentOperatorKind? assignOpKind = null;
                if (!binOpKind.HasValue)
                {
                    assignOpKind = SyntaxFacts.TryGetAssignmentOperatorKind(tk);
                }
                if (!(assignOpKind.HasValue || binOpKind.HasValue))
                {
                    break;
                }

                newPrecedence = binOpKind.HasValue ? GetPrecedence(binOpKind.Value) : Precedence.Assignment;
                if (newPrecedence < minPrecedence)
                {
                    break;
                }

                EatToken();

                bool hasRightOperand = assignOpKind != AssignmentOperatorKind.Increment
                    && assignOpKind != AssignmentOperatorKind.Decrement;
                Expression rightOperand = hasRightOperand
                    ? ParseSubExpression(newPrecedence)
                    : leftOperand;

                var span = TextSpan.FromBounds(leftOperand.Span.Start, rightOperand.Span.End);
                leftOperand = binOpKind.HasValue
                    ? new BinaryExpression(
                        leftOperand,
                        new Spanned<BinaryOperatorKind>(binOpKind.Value, tkSpan),
                        rightOperand,
                        span
                    )
                    : new AssignmentExpression(
                        leftOperand,
                        new Spanned<AssignmentOperatorKind>(assignOpKind!.Value, tkSpan),
                        rightOperand,
                        span
                    );
            }

            return leftOperand;
        }

        private Expression ParseTerm()
        {
            switch (_currentToken.Kind)
            {
                case SyntaxTokenKind.Identifier:
                    return IsFunctionCall() ? ParseFunctionCall() : ParseNameExpression();

                case SyntaxTokenKind.StringLiteralOrQuotedIdentifier:
                    return IsParameter() || (_currentToken.Flags & SyntaxTokenFlags.HasDollarPrefix) == SyntaxTokenFlags.HasDollarPrefix
                        ? ParseNameExpression()
                        : ParseLiteral();

                case SyntaxTokenKind.NumericLiteral:
                case SyntaxTokenKind.NullKeyword:
                case SyntaxTokenKind.TrueKeyword:
                case SyntaxTokenKind.FalseKeyword:
                    return ParseLiteral();

                case SyntaxTokenKind.OpenParen:
                    SyntaxToken openParen = EatToken(SyntaxTokenKind.OpenParen);
                    Expression expr = ParseSubExpression(Precedence.Expression);
                    if (_currentToken.Kind == SyntaxTokenKind.Comma)
                    {
                        return ParseBezierExpression(openParen, expr);
                    }
                    EatToken(SyntaxTokenKind.CloseParen);
                    return expr;

                case SyntaxTokenKind.EndOfFile:
                    return new ErrorExpression(SpanFrom(_currentToken));
                default:
                    Report(DiagnosticId.InvalidExpressionTerm, GetText(_currentToken));
                    var result = new ErrorExpression(SpanFrom(_currentToken));
                    switch (_currentToken.Kind)
                    {
                        // Avoid eating tokens that are likely to be handled elsewhere
                        case SyntaxTokenKind.CloseParen:
                        case SyntaxTokenKind.CloseBrace:
                        case SyntaxTokenKind.Comma:
                            return result;
                        default:
                            EatToken();
                            return result;
                    }
            }
        }

        private BezierExpression ParseBezierExpression(in SyntaxToken openParen, Expression x0)
        {
            var controlPoints = ImmutableArray.CreateBuilder<BezierControlPoint>();
            EatToken(SyntaxTokenKind.Comma);
            Expression y0 = ParseSubExpression(Precedence.Expression);
            controlPoints.Add(new BezierControlPoint(x0, y0, starting: true));
            EatToken(SyntaxTokenKind.CloseParen);
            while (!IsAtEnd())
            {
                bool? paren = _currentToken.Kind switch
                {
                    SyntaxTokenKind.OpenParen => true,
                    SyntaxTokenKind.OpenBrace => false,
                    _ => null
                };
                if (paren is null) { break; }
                BezierControlPoint cp = parseControlPoint(paren.Value);
                controlPoints.Add(cp);
            }

            return new BezierExpression(controlPoints.ToImmutable(), SpanFrom(openParen));

            BezierControlPoint parseControlPoint(bool starting)
            {
                (SyntaxTokenKind startTk, SyntaxTokenKind endTk) = starting
                    ? (SyntaxTokenKind.OpenParen, SyntaxTokenKind.CloseParen)
                    : (SyntaxTokenKind.OpenBrace, SyntaxTokenKind.CloseBrace);
                EatToken(startTk);
                Expression x = ParseSubExpression(Precedence.Expression);
                EatToken(SyntaxTokenKind.Comma);
                Expression y = ParseSubExpression(Precedence.Expression);
                EatToken(endTk);
                return new BezierControlPoint(x, y, starting);
            }
        }

        private Expression ParseLiteral()
        {
            SyntaxToken token = EatToken();
            ConstantValue value;
            switch (token.Kind)
            {
                case SyntaxTokenKind.NumericLiteral:
                    ReadOnlySpan<char> valueText = SourceText.GetCharacterSpan(token.GetValueSpan());
                    var numberStyle = token.IsHexTriplet ? NumberStyles.HexNumber : NumberStyles.None;
                    value = token.IsFloatingPointLiteral
                        ? ConstantValue.Number(float.Parse(valueText, CultureInfo.InvariantCulture))
                        : ConstantValue.Number(int.Parse(valueText, numberStyle));
                    break;
                case SyntaxTokenKind.StringLiteralOrQuotedIdentifier:
                    string str = InternValueText(token);
                    value = ConstantValue.String(str);
                    break;
                case SyntaxTokenKind.NullKeyword:
                    value = ConstantValue.Null;
                    break;
                case SyntaxTokenKind.TrueKeyword:
                    value = ConstantValue.True;
                    break;
                case SyntaxTokenKind.FalseKeyword:
                    value = ConstantValue.False;
                    break;
                default:
                    ThrowHelper.ThrowUnreachable();
                    return null!;
            }

            return !token.HasDiagnostics
                ? new LiteralExpression(value, token.TextSpan)
                : new ErrorExpression(token.TextSpan);
        }

        private Spanned<string> ParseIdentifier()
        {
            SyntaxToken token = _currentToken.Kind switch
            {
                SyntaxTokenKind.Identifier or SyntaxTokenKind.StringLiteralOrQuotedIdentifier => EatToken(),
                _ => CreateMissingToken(SyntaxTokenKind.Identifier)
            };
            return new Spanned<string>(InternValueText(token), token.TextSpan);
        }

        private NameExpression ParseNameExpression()
        {
            Debug.Assert(_currentToken.Kind is SyntaxTokenKind.Identifier
                or SyntaxTokenKind.StringLiteralOrQuotedIdentifier);

            SyntaxToken token = EatToken();
            var identifier = new Spanned<string>(InternValueText(token), token.TextSpan);
            return new NameExpression(identifier.Value, token.GetSigil(), identifier.Span);
        }

        private bool IsFunctionCall()
        {
            return PeekToken(1).Kind == SyntaxTokenKind.OpenParen;
        }

        private bool IsArgumentListOrSemicolon()
        {
            SyntaxTokenKind peek;
            int n = 0;
            while ((peek = PeekToken(n).Kind) != SyntaxTokenKind.EndOfFile)
            {
                switch (peek)
                {
                    case SyntaxTokenKind.NullKeyword:
                    case SyntaxTokenKind.TrueKeyword:
                    case SyntaxTokenKind.FalseKeyword:
                    case SyntaxTokenKind.Identifier:
                    case SyntaxTokenKind.StringLiteralOrQuotedIdentifier:
                    case SyntaxTokenKind.NumericLiteral:
                    case SyntaxTokenKind.Comma:
                    case SyntaxTokenKind.Dot:
                        n++;
                        break;

                    case SyntaxTokenKind.Semicolon:
                    case SyntaxTokenKind.CloseBrace:
                        return true;

                    default:
                        return false;
                }
            }

            return false;
        }

        private bool IsParameter()
        {
            switch (_currentToken.Kind)
            {
                case SyntaxTokenKind.Identifier:
                case SyntaxTokenKind.StringLiteralOrQuotedIdentifier:
                    return _parameterMap.ContainsKey(InternValueText(_currentToken));
                default:
                    return false;
            }
        }

        private FunctionCallExpression ParseFunctionCall()
        {
            Spanned<string> targetName = ParseIdentifier();
            ImmutableArray<Expression> args = ParseArgumentList();
            var span = TextSpan.FromBounds(targetName.Span.Start, LexerPosition);
            return new FunctionCallExpression(targetName, args, span);
        }

        private ImmutableArray<Expression> ParseArgumentList()
        {
            if (SyntaxFacts.IsStatementTerminator(_currentToken.Kind))
            {
                return ImmutableArray<Expression>.Empty;
            }

            EatToken(SyntaxTokenKind.OpenParen);
            if (TryEatToken(SyntaxTokenKind.CloseParen))
            {
                return ImmutableArray<Expression>.Empty;
            }

            var arguments = ImmutableArray.CreateBuilder<Expression>();
            SyntaxTokenKind tk;
            while ((tk = _currentToken.Kind) is not (SyntaxTokenKind.CloseParen
                   or SyntaxTokenKind.Semicolon
                   or SyntaxTokenKind.EndOfFile))
            {
                if (SyntaxFacts.CanStartExpressionTerm(tk))
                {
                    Expression arg = ParseExpression();
                    arguments.Add(arg);
                }
                else if (tk is SyntaxTokenKind.Comma or SyntaxTokenKind.Dot
                         or SyntaxTokenKind.Ampersand)
                {
                    EatToken();
                }
                else
                {
                    EatStrayToken();
                }

                // Bail if the current line ends with an error node - assume it ends the whole statement
                if (arguments is [.., { Kind: SyntaxNodeKind.ErrorExpression } errorArg]
                    && GetCurrentLine().Start >= errorArg.Span.End)
                {
                    return arguments.ToImmutable();
                }
            }

            EatToken(SyntaxTokenKind.CloseParen);
            return arguments.ToImmutable();
        }

        private IfStatement ParseIfStatement()
        {
            SyntaxToken ifKeyword = EatToken(SyntaxTokenKind.IfKeyword);
            EatToken(SyntaxTokenKind.OpenParen);
            Expression condition = ParseExpression();
            EatToken(SyntaxTokenKind.CloseParen);

            Statement ifTrue = ParseStatement();
            Statement? ifFalse = null;
            if (TryEatToken(SyntaxTokenKind.ElseKeyword))
            {
                ifFalse = ParseStatement();
            }

            return new IfStatement(condition, ifTrue, ifFalse, SpanFrom(ifKeyword));
        }

        private ErrorStatement ParseMisplacedElse()
        {
            SyntaxToken keyword = EatToken(SyntaxTokenKind.ElseKeyword);
            _ = ParseStatement();
            return new ErrorStatement(SpanFrom(keyword));
        }

        private BreakStatement ParseBreakStatement()
        {
            SyntaxToken keyword = EatToken(SyntaxTokenKind.BreakKeyword);
            EatStatementTerminator();
            return new BreakStatement(SpanFrom(keyword));
        }

        private WhileStatement ParseWhileStatement()
        {
            SyntaxToken keyword = EatToken(SyntaxTokenKind.WhileKeyword);
            EatToken(SyntaxTokenKind.OpenParen);
            Expression condition = ParseExpression();
            EatToken(SyntaxTokenKind.CloseParen);
            Statement body = ParseStatement();
            return new WhileStatement(condition, body, SpanFrom(keyword));
        }

        private ReturnStatement ParseReturnStatement()
        {
            SyntaxToken keyword = EatToken(SyntaxTokenKind.ReturnKeyword);
            EatStatementTerminator();
            return new ReturnStatement(SpanFrom(keyword));
        }

        private SelectStatement ParseSelectStatement()
        {
            SyntaxToken keyword = EatToken(SyntaxTokenKind.SelectKeyword);
            Block body = ParseBlock();
            return new SelectStatement(body, SpanFrom(keyword));
        }

        private SelectSection ParseSelectSection()
        {
            SyntaxToken keyword = EatToken(SyntaxTokenKind.CaseKeyword);
            Spanned<string> labelName = ConsumeTextUntil(
                tk => tk is SyntaxTokenKind.OpenBrace or SyntaxTokenKind.Colon
            );
            if (_currentToken.Kind == SyntaxTokenKind.Colon)
            {
                EatToken();
            }

            Block body = ParseBlock();
            return new SelectSection(labelName, body, SpanFrom(keyword));
        }

        private CallChapterStatement ParseCallChapterStatement()
        {
            SyntaxToken keyword = EatToken(SyntaxTokenKind.CallChapterKeyword);
            Spanned<string> filePath = ConsumeTextUntil(tk => tk == SyntaxTokenKind.Semicolon);
            EatStatementTerminator();
            return new CallChapterStatement(filePath, SpanFrom(keyword));
        }

        private CallSceneStatement ParseCallSceneStatement()
        {
            SyntaxToken keyword = EatToken(SyntaxTokenKind.CallSceneKeyword);
            (Spanned<string>? file, Spanned<string> scene) = ParseSymbolPath();
            EatStatementTerminator();
            return new CallSceneStatement(file, scene, SpanFrom(keyword));
        }

        // Parses call_scene specific symbol path syntax.
        // call_scene can be followed by either '@->{localSymbolName}' (e.g. '@->SelectStoryModeA')
        // or '{filepath}->{symbolName}' (e.g. 'nss/extra_gallery.nss->extra_gallery_main').
        private (Spanned<string>? filePath, Spanned<string> symbolName) ParseSymbolPath()
        {
            TryEatToken(SyntaxTokenKind.AtArrow);

            Spanned<string>? filePath = null;
            Spanned<string> symbolName;
            Spanned<string> part = ConsumeTextUntil(
                tk => tk is SyntaxTokenKind.Semicolon or SyntaxTokenKind.Arrow
            );
            if (TryEatToken(SyntaxTokenKind.Arrow))
            {
                filePath = part;
                symbolName = ConsumeTextUntil(tk => tk == SyntaxTokenKind.Semicolon);
            }
            else
            {
                symbolName = part;
            }

            return (filePath, symbolName);
        }

        // Consumes tokens until the specified condition is met.
        private Spanned<string> ConsumeTextUntil(Func<SyntaxTokenKind, bool> condition)
        {
            SyntaxTokenKind tk;
            int start = LexerPosition;
            int end = 0;
            while ((tk = _currentToken.Kind) != SyntaxTokenKind.EndOfFile && !condition(tk))
            {
                end = EatToken().TextSpan.End;
            }

            var span = TextSpan.FromBounds(start, end);
            return new Spanned<string>(SourceText.GetText(span), span);
        }

        private DialogueBlock ParseDialogueBlock()
        {
            SyntaxToken startTag = EatToken(SyntaxTokenKind.DialogueBlockStartTag);
            string associatedBox = extractBoxName(startTag);
            SyntaxToken blockIdentifier = EatToken(SyntaxTokenKind.DialogueBlockIdentifier);
            string name = extractBlockName(blockIdentifier);

            var parts = ImmutableArray.CreateBuilder<DialogueBlockPart>();
            while (_currentToken.Kind is not (SyntaxTokenKind.DialogueBlockEndTag or SyntaxTokenKind.EndOfFile))
            {
                DialogueBlockPart? part = ParseDialogueBlockPart();
                if (part is not null)
                {
                    parts.Add(part);
                }
            }

            EatToken(SyntaxTokenKind.DialogueBlockEndTag);
            return new DialogueBlock(name, associatedBox, parts.ToImmutable(), SpanFrom(startTag));

            string extractBoxName(in SyntaxToken tag)
            {
                ReadOnlySpan<char> span = SourceText.GetCharacterSpan(tag.TextSpan);
                span = span["<pre ".Length..^1].Trim();
                Debug.Assert(span.Length > 0);
                return span.ToString();
            }

            string extractBlockName(in SyntaxToken identifierToken)
            {
                ReadOnlySpan<char> span = SourceText.GetCharacterSpan(identifierToken.TextSpan);
                return span.Length > 2 ? span[1..^1].ToString() : "";
            }
        }

        private DialogueBlockPart? ParseDialogueBlockPart()
        {
            DialogueBlockPart? dialogueBlockPart;
            do
            {
                dialogueBlockPart = ParseDialogueBlockPartCore();
                if (dialogueBlockPart is not null) { break; }
                SyntaxTokenKind tk = _currentToken.Kind;
                if (tk is SyntaxTokenKind.EndOfFile)
                {
                    return null;
                }
            } while (true);

            return dialogueBlockPart;
        }

        private DialogueBlockPart? ParseDialogueBlockPartCore()
        {
            switch (_currentToken.Kind)
            {
                case SyntaxTokenKind.Markup:
                {
                    return new DialogueBlockPart.Markup(EatToken().TextSpan);
                }
                case SyntaxTokenKind.MarkupBlankLine:
                {
                    return new DialogueBlockPart.BlankLine(EatToken().TextSpan);
                }
                case SyntaxTokenKind.OpenBrace:
                {
                    SyntaxToken openBrace = EatToken(SyntaxTokenKind.OpenBrace);
                    ImmutableArray<Statement> statements = ParseStatements();
                    EatToken(SyntaxTokenKind.CloseBrace);
                    return new DialogueBlockPart.CodeBlock(statements, SpanFrom(openBrace));
                }
                case SyntaxTokenKind.EndOfFile:
                {
                    return null;
                }
                default:
                {
                    Report(DiagnosticId.SkippedStrayToken, GetText(_currentToken));
                    EatToken();
                    return null;
                }
            }
        }

        private ErrorStatement CreateErrorStatement(int startOffset)
        {
            var span = TextSpan.FromBounds(startOffset, LexerPosition);
            return new ErrorStatement(span);
        }

        private ErrorStatement? TryCreateStrayMarkupNode()
        {
            Debug.Assert(_currentToken.Kind == SyntaxTokenKind.LessThan);
            int startOffset = LexerPosition;
            TextLine currentLine = GetCurrentLine();

            int tokenOffset = 0;
            SyntaxToken token;
            // Look for the closing '>'
            while ((token = PeekToken(tokenOffset)).Kind != SyntaxTokenKind.GreaterThan)
            {
                if (token.Kind == SyntaxTokenKind.EndOfFile)
                {
                    return null;
                }

                tokenOffset++;
            }

            // Check if the current line ends with the '>' character that we found
            if (PeekToken(tokenOffset + 1).TextSpan.Start >= currentLine.End)
            {
                Report(DiagnosticId.StrayMarkupBlock, TextSpan.FromBounds(startOffset, currentLine.End));
                EatTokens(tokenOffset);
                EatToken(SyntaxTokenKind.GreaterThan);
                return CreateErrorStatement(startOffset);
            }

            return null;
        }

        private void SkipOnCurrentLineUntil(Func<SyntaxTokenKind, bool> condition, bool report)
        {
            int start = LexerPosition, end = LexerPosition;
            TextLine startLine = GetCurrentLine();
            int tokensSkipped = 0;
            while (!condition(_currentToken.Kind) && LexerPosition < startLine.End)
            {
                end = EatToken().TextSpan.End;
                tokensSkipped++;
            }

            if (report && start != end)
            {
                ReportSkippedBadSyntax(TextSpan.FromBounds(start, end), tokensSkipped);
            }
        }

        private void SkipOnCurrentLineUntil(SyntaxTokenKind tokenKind, bool report)
        {
            int start = LexerPosition, end = LexerPosition;
            TextLine startLine = GetCurrentLine();
            int tokensSkipped = 0;
            while (_currentToken.Kind != tokenKind && LexerPosition < startLine.End)
            {
                EatToken();
                tokensSkipped++;
            }

            if (report && start != end)
            {
                ReportSkippedBadSyntax(TextSpan.FromBounds(start, end), tokensSkipped);
            }
        }

        private void SkipToNextLine(out int tokensSkipped)
        {
            TextLine startLine = GetCurrentLine();
            tokensSkipped = 0;
            while (LexerPosition < startLine.End)
            {
                EatToken();
                tokensSkipped++;
            }
        }

        private void SkipToNextStatementOrLine(out int tokensSkipped)
        {
            TextLine startLine = GetCurrentLine();
            tokensSkipped = 0;
            while (LexerPosition < startLine.End)
            {
                if (IsLikelyStatementStart()) { return; }
                EatToken();
                tokensSkipped++;
            }
        }

        private bool IsLikelyStatementStart()
        {
            SyntaxTokenKind tk = _currentToken.Kind;
            if (SyntaxFacts.IsDefiniteStatementStart(tk)) { return true; }

            if (tk is SyntaxTokenKind.Identifier or SyntaxTokenKind.StringLiteralOrQuotedIdentifier)
            {
                SyntaxToken lookahead = PeekToken(1);
                // function call
                if (lookahead.Kind is SyntaxTokenKind.OpenParen or SyntaxTokenKind.Semicolon)
                {
                    return true;
                }

                // assignment
                if (SyntaxFacts.TryGetAssignmentOperatorKind(lookahead.Kind) is not null)
                {
                    return true;
                }
            }

            return false;
        }

        private void EatStrayToken()
        {
            Report(DiagnosticId.SkippedStrayToken, GetText(_currentToken));
            EatToken();
        }

        private void ReportSkippedBadSyntax(TextSpan span, int tokensSkipped)
        {
            if (tokensSkipped == 1)
            {
                SyntaxToken prevToken = Tokens[_tokenIndex - 1];
                DiagnosticId diagnosticId = prevToken.Kind switch
                {
                    SyntaxTokenKind.CloseParen or SyntaxTokenKind.CloseBrace => DiagnosticId.MismatchedBrace,
                    var staticTk when SyntaxFacts.GetText(staticTk).Length == 1 => DiagnosticId.SkippedStrayCharacter,
                    _ => DiagnosticId.SkippedStrayToken
                };

                string text = SyntaxFacts.GetText(prevToken.Kind) is { Length: > 0 } staticText
                    ? staticText
                    : GetText(prevToken);
                Report(diagnosticId, span, text);
            }
            else
            {
                Report(DiagnosticId.SkippedBadSyntax, span);
            }
        }

        private void Report(DiagnosticId diagnosticId, TextSpan span)
        {
            var location = new SourceLocation(SourceText, span);
            _diagnosticBuilder.Add(Diagnostic.Create(location, diagnosticId));
        }

        private void Report(DiagnosticId diagnosticId, params object[] arguments)
        {
            Report(diagnosticId, _currentToken.TextSpan, arguments);
        }

        private void Report(DiagnosticId diagnosticId, TextSpan span, params object[] arguments)
        {
            var location = new SourceLocation(SourceText, span);
            _diagnosticBuilder.Add(Diagnostic.Create(location, diagnosticId, arguments));
        }

        private TextSpan GetSpanForMissingToken()
        {
            int start = Math.Min(LexerPosition, SourceText.Length - 1);
            return new TextSpan(start, 0);
        }

        private void ReportTokenExpected(SyntaxTokenKind expected)
        {
            string actualText = SyntaxFacts.GetText(_currentToken.Kind) is { Length: > 0 } staticText
                ? staticText
                : GetText(_currentToken);
            if (expected == SyntaxTokenKind.Identifier)
            {
                Report(DiagnosticId.IdentifierExpected, actualText);
            }
            else
            {
                string expectedText = SyntaxFacts.GetText(expected);
                Report(DiagnosticId.TokenExpected, expectedText, actualText);
            }
        }
    }

    internal enum Precedence
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
}

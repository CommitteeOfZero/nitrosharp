using NitroSharp.NsScriptNew.Text;
using NitroSharp.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace NitroSharp.NsScriptNew.Syntax
{
    internal sealed class Parser
    {
        private readonly Lexer _lexer;
        private readonly SyntaxToken[] _tokens;
        private int _tokenOffset;

        // Used to differentiate between string literals and quoted identifiers (aka string parameter references).
        // Normally, a parser should not keep track of something like that, but this is NSS.
        private readonly Dictionary<string, Parameter> _currentParameterList;

        private readonly StringInternTable _internTable;
        private readonly DiagnosticBuilder _diagnostics;

        public Parser(Lexer lexer)
        {
            _lexer = lexer;
            _diagnostics = new DiagnosticBuilder();
            _internTable = new StringInternTable();
            _tokens = Lex();
            _currentParameterList = new Dictionary<string, Parameter>();
        }

        private SyntaxToken PeekToken(int n) => _tokens[_tokenOffset + n];
        private SyntaxToken CurrentToken => _tokens[_tokenOffset];
        private SyntaxToken PreviousToken => PeekToken(-1);
        private SourceText SourceText => _lexer.SourceText;

        internal DiagnosticBuilder DiagnosticBuilder => _diagnostics;

        private SyntaxToken[] Lex()
        {
            int capacity = Math.Max(32, SourceText.Source.Length / 6);
            var tokens = new ArrayBuilder<SyntaxToken>(capacity);
            ref SyntaxToken token = ref tokens.Add();
            do
            {
                _lexer.Lex(ref token);
                if (token.Kind == SyntaxTokenKind.EndOfFileToken)
                {
                    break;
                }

                token = ref tokens.Add();

            } while (token.Kind != SyntaxTokenKind.EndOfFileToken);

            return tokens.UnderlyingArray;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private SyntaxToken EatToken()
        {
            SyntaxToken ct = CurrentToken;
            _tokenOffset++;
            return ct;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private SyntaxToken EatToken(SyntaxTokenKind expectedKind)
        {
            SyntaxToken ct = CurrentToken;
            if (ct.Kind != expectedKind)
            {
                return CreateMissingToken(expectedKind, ct.Kind);
            }

            _tokenOffset++;
            return ct;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ReadOnlySpan<char> GetCharSpan(in SyntaxToken token) => SourceText.GetSlice(token.TextSpan);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ReadOnlySpan<char> GetValueCharSpan(in SyntaxToken token) => SourceText.GetSlice(token.GetValueSpan());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string GetText(in SyntaxToken token) => SourceText.GetText(token.TextSpan);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string GetValueText(in SyntaxToken token) => SourceText.GetText(token.GetValueSpan());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string InternText(in SyntaxToken token) => _internTable.Add(SourceText.GetSlice(token.TextSpan));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string InternValueText(in SyntaxToken token) => _internTable.Add(SourceText.GetSlice(token.GetValueSpan()));

        [MethodImpl(MethodImplOptions.NoInlining)]
        private SyntaxToken CreateMissingToken(SyntaxTokenKind expected, SyntaxTokenKind actual)
        {
            TokenExpected(expected, actual);
            TextSpan span = GetSpanForMissingToken();
            return new SyntaxToken(SyntaxTokenKind.MissingToken, span, SyntaxTokenFlags.Empty);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void EatTokens(int count)
        {
            _tokenOffset += count;
        }

        private void EatStrayToken()
        {
            SyntaxToken token = PeekToken(0);
            Report(DiagnosticId.StrayToken, GetText(token));
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
            while ((tk = CurrentToken.Kind) != SyntaxTokenKind.EndOfFileToken
                   && !SyntaxFacts.CanStartDeclaration(tk))
            {
                switch (CurrentToken.Kind)
                {
                    case SyntaxTokenKind.IncludeDirective:
                        EatToken();
                        SyntaxToken filePath = EatToken(SyntaxTokenKind.StringLiteral);
                        fileReferences.Add(new SourceFileReference(GetValueText(filePath)));
                        break;

                    case SyntaxTokenKind.Semicolon:
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
                    case SyntaxTokenKind.Dot:
                        SkipToNextLine();
                        break;

                    default:
                        Report(DiagnosticId.ExpectedMemberDeclaration, GetText(CurrentToken));
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
            string name = ParseSimpleName();
            Block body = ParseBlock();
            return new Chapter(name, body);
        }

        private Scene ParseScene()
        {
            EatToken(SyntaxTokenKind.SceneKeyword);
            string name = ParseSimpleName();
            Block body = ParseBlock();
            return new Scene(name, body);
        }

        private Function ParseFunction()
        {
            EatToken(SyntaxTokenKind.FunctionKeyword);
            string name = ParseSimpleName();
            ImmutableArray<Parameter> parameters = ParseParameterList();

            foreach (Parameter param in parameters)
            {
                _currentParameterList[param.Name] = param;
            }

            Block body = ParseBlock();
            return new Function(name, parameters, body);
        }

        private ImmutableArray<Parameter> ParseParameterList()
        {
            EatToken(SyntaxTokenKind.OpenParen);

            var parameters = ImmutableArray.CreateBuilder<Parameter>();
            while (CurrentToken.Kind != SyntaxTokenKind.CloseParen
                && CurrentToken.Kind != SyntaxTokenKind.EndOfFileToken)
            {
                switch (CurrentToken.Kind)
                {
                    case SyntaxTokenKind.Identifier:
                    case SyntaxTokenKind.StringLiteral:
                        var p = new Parameter(ParseSimpleName());
                        parameters.Add(p);
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
            return parameters.ToImmutable();
        }

        private Block ParseBlock()
        {
            EatToken(SyntaxTokenKind.OpenBrace);
            ImmutableArray<Statement> statements = ParseStatements();
            EatToken(SyntaxTokenKind.CloseBrace);
            return new Block(statements);
        }

        private ImmutableArray<Statement> ParseStatements()
        {
            var statements = ImmutableArray.CreateBuilder<Statement>();
            while (CurrentToken.Kind != SyntaxTokenKind.CloseBrace)
            {
                Statement statement = ParseStatement();
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
                    SyntaxTokenKind tk = CurrentToken.Kind;
                    if (tk == SyntaxTokenKind.EndOfFileToken || tk == SyntaxTokenKind.CloseBrace)
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
                case SyntaxTokenKind.OpenBrace:
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
                    return new PXmlString(GetText(EatToken()));
                case SyntaxTokenKind.PXmlLineSeparator:
                    EatToken();
                    return new PXmlLineSeparator();
                case SyntaxTokenKind.LessThan:
                    HandleStrayPXmlElement();
                    goto default;
                case SyntaxTokenKind.Dot:
                    SkipToNextLine();
                    throw new ParseError();

                case SyntaxTokenKind.Identifier:
                case SyntaxTokenKind.StringLiteral:
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
            Debug.Assert(CurrentToken.Kind == SyntaxTokenKind.LessThan);
            int currentLine = GetLineNumber();

            int n = 0;
            SyntaxToken token = default;
            // Look for the closing '>'
            while ((token = PeekToken(n)).Kind != SyntaxTokenKind.GreaterThan)
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
                Report(DiagnosticId.StrayPXmlElement, SourceText.Lines[currentLine]);
                EatTokens(n + 1); // skip to the next line
                throw new ParseError();
            }
        }

        private ExpressionStatement ParseExpressionStatement()
        {
            int start = CurrentToken.TextSpan.Start;
            Expression expr = ParseExpression();
            int end = PreviousToken.TextSpan.End;

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
            FunctionCall call = ParseFunctionCall();
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

            SyntaxTokenKind tk = CurrentToken.Kind;
            if (SyntaxFacts.TryGetUnaryOperatorKind(tk, out UnaryOperatorKind unaryOperator))
            {
                EatToken();
                newPrecedence = Precedence.Unary;
                Expression operand = ParseSubExpression(newPrecedence);
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
                BinaryOperatorKind binOpKind = default;
                AssignmentOperatorKind assignOpKind = default;

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

                bool hasRightOperand = assignOpKind != AssignmentOperatorKind.Increment
                                       && assignOpKind != AssignmentOperatorKind.Decrement;
                Expression rightOperand = hasRightOperand
                    ? ParseSubExpression(newPrecedence)
                    : leftOperand;

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
                case SyntaxTokenKind.Identifier:
                    return IsFunctionCall() ? (Expression)ParseFunctionCall() : ParseIdentifier();

                case SyntaxTokenKind.StringLiteral:
                case SyntaxTokenKind.StringLiteralOrQuotedIdentifier:
                    return IsParameter() ? (Expression)ParseIdentifier() : ParseLiteral();

                case SyntaxTokenKind.NumericLiteral:
                case SyntaxTokenKind.NullKeyword:
                case SyntaxTokenKind.TrueKeyword:
                case SyntaxTokenKind.FalseKeyword:
                    return ParseLiteral();

                case SyntaxTokenKind.OpenParen:
                    EatToken(SyntaxTokenKind.OpenParen);
                    Expression expr = ParseSubExpression(Precedence.Expression);
                    EatToken(SyntaxTokenKind.CloseParen);
                    return expr;

                case SyntaxTokenKind.At:
                    return ParseDeltaExpression(precedence);

                default:
                    Report(DiagnosticId.InvalidExpressionTerm, GetText(CurrentToken));
                    EatToken();
                    throw new ParseError();
            }
        }

        private DeltaExpression ParseDeltaExpression(Precedence precedence)
        {
            EatToken(SyntaxTokenKind.At);
            Expression expr = ParseSubExpression(precedence);
            return new DeltaExpression(expr);
        }

        private Literal ParseLiteral()
        {
            SyntaxToken token = EatToken();
            switch (token.Kind)
            {
                case SyntaxTokenKind.NumericLiteral:
                    ReadOnlySpan<char> valueText = GetValueCharSpan(token);
                    var numberStyle = token.IsHexTriplet ? NumberStyles.HexNumber : NumberStyles.None;
                    ConstantValue value = token.IsFloatingPointLiteral
                        ? ConstantValue.Float(float.Parse(valueText))
                        : ConstantValue.Integer(int.Parse(valueText, numberStyle));
                    return new Literal(value);

                case SyntaxTokenKind.StringLiteral:
                case SyntaxTokenKind.StringLiteralOrQuotedIdentifier:
                    string str = InternValueText(token);
                    return new Literal(ConstantValue.String(str));

                case SyntaxTokenKind.NullKeyword:
                    return Literal.Null;
                case SyntaxTokenKind.TrueKeyword:
                    return Literal.True;
                case SyntaxTokenKind.FalseKeyword:
                    return Literal.False;

                default:
                    ExceptionUtils.Unreachable();
                    return null;
            }
        }

        private string ParseSimpleName()
        {
            SyntaxToken token = EatToken();
            switch (token.Kind)
            {
                case SyntaxTokenKind.Identifier:
                case SyntaxTokenKind.StringLiteralOrQuotedIdentifier:
                default:
                    return InternValueText(token);
            }
        }

        private Identifier ParseIdentifier(bool isFunctionName = false)
        {
            Debug.Assert(CurrentToken.Kind == SyntaxTokenKind.Identifier
                         || CurrentToken.Kind == SyntaxTokenKind.StringLiteral);

            return new Identifier(ParseSimpleName());
        }

        private bool IsFunctionCall()
        {
            return PeekToken(1).Kind == SyntaxTokenKind.OpenParen;
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
                    case SyntaxTokenKind.Identifier:
                    case SyntaxTokenKind.StringLiteral:
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
            switch (CurrentToken.Kind)
            {
                case SyntaxTokenKind.Identifier:
                case SyntaxTokenKind.StringLiteral:
                case SyntaxTokenKind.StringLiteralOrQuotedIdentifier:
                    return _currentParameterList.ContainsKey(InternValueText(CurrentToken));

                default:
                    return false;
            }
        }

        private FunctionCall ParseFunctionCall()
        {
            string targetName = ParseSimpleName();
            ImmutableArray<Expression> args = ParseArgumentList();
            return new FunctionCall(targetName, args);
        }

        private ImmutableArray<Expression> ParseArgumentList()
        {
            if (SyntaxFacts.IsStatementTerminator(CurrentToken.Kind))
            {
                return ImmutableArray<Expression>.Empty;
            }

            EatToken(SyntaxTokenKind.OpenParen);

            var args = ImmutableArray.CreateBuilder<Expression>();
            SyntaxTokenKind tk;
            while ((tk = CurrentToken.Kind) != SyntaxTokenKind.CloseParen
                   && tk != SyntaxTokenKind.Semicolon
                   && tk != SyntaxTokenKind.EndOfFileToken)
            {
                switch (tk)
                {
                    case SyntaxTokenKind.NumericLiteral:
                    case SyntaxTokenKind.StringLiteral:
                    case SyntaxTokenKind.StringLiteralOrQuotedIdentifier:
                    case SyntaxTokenKind.Identifier:
                    case SyntaxTokenKind.NullKeyword:
                    case SyntaxTokenKind.TrueKeyword:
                    case SyntaxTokenKind.FalseKeyword:
                        args.Add(ParseExpression());
                        break;

                    case SyntaxTokenKind.Comma:
                    case SyntaxTokenKind.Dot:
                    // Ampersand? Why?
                    case SyntaxTokenKind.Ampersand:
                        EatToken();
                        break;

                    default:
                        Expression expr = ParseExpression();
                        args.Add(expr);
                        break;
                }
            }

            EatToken(SyntaxTokenKind.CloseParen);
            return args.ToImmutable();
        }

        private IfStatement ParseIfStatement()
        {
            EatToken(SyntaxTokenKind.IfKeyword);
            EatToken(SyntaxTokenKind.OpenParen);
            Expression condition = ParseExpression();
            EatToken(SyntaxTokenKind.CloseParen);

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
            EatToken(SyntaxTokenKind.OpenParen);
            Expression condition = ParseExpression();
            EatToken(SyntaxTokenKind.CloseParen);
            Statement body = ParseStatement();

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
            Block body = ParseBlock();
            return new SelectStatement(body);
        }

        private SelectSection ParseSelectSection()
        {
            EatToken(SyntaxTokenKind.CaseKeyword);
            string labelName = ConsumeTextUntil(tk => tk == SyntaxTokenKind.OpenBrace
                                                || tk == SyntaxTokenKind.Colon);
            if (CurrentToken.Kind == SyntaxTokenKind.Colon)
            {
                EatToken();
            }

            Block body = ParseBlock();
            return new SelectSection(labelName, body);
        }

        private CallChapterStatement ParseCallChapterStatement()
        {
            EatToken(SyntaxTokenKind.CallChapterKeyword);
            string filePath = ConsumeTextUntil(tk => tk == SyntaxTokenKind.Semicolon);
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
            if (CurrentToken.Kind == SyntaxTokenKind.AtArrow)
            {
                EatToken();
            }

            string filePath = null, symbolName = null;
            string part = ConsumeTextUntil(tk => tk == SyntaxTokenKind.Semicolon
                                           || tk == SyntaxTokenKind.Arrow);
            if (CurrentToken.Kind == SyntaxTokenKind.Arrow)
            {
                EatToken();
                filePath = part;
                symbolName = ConsumeTextUntil(tk => tk == SyntaxTokenKind.Semicolon);
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
            SyntaxTokenKind tk;
            int start = CurrentToken.TextSpan.Start;
            int end = 0;
            while ((tk = CurrentToken.Kind) != SyntaxTokenKind.EndOfFileToken && !condition(tk))
            {
                end = EatToken().TextSpan.End;
            }

            return SourceText.GetText(TextSpan.FromBounds(start, end));
        }

        private DialogueBlock ParseDialogueBlock()
        {
            SyntaxToken startTag = EatToken(SyntaxTokenKind.DialogueBlockStartTag);
            string associatedBox = GetValueText(startTag);

            string boxName = GetValueText(EatToken(SyntaxTokenKind.DialogueBlockIdentifier));
            var statements = ImmutableArray.CreateBuilder<Statement>();
            while (CurrentToken.Kind != SyntaxTokenKind.DialogueBlockEndTag)
            {
                Statement statement = ParseStatement();
                if (statement != null)
                {
                    statements.Add(statement);
                }
            }

            EatToken(SyntaxTokenKind.DialogueBlockEndTag);
            return new DialogueBlock(boxName, associatedBox, new Block(statements.ToImmutable()));
        }

        private int GetLineNumber() => SourceText.GetLineNumberFromPosition(CurrentToken.TextSpan.Start);
        private int GetLineNumber(SyntaxToken token) => SourceText.GetLineNumberFromPosition(token.TextSpan.Start);

        private void SkipToNextLine()
        {
            int currentLine = GetLineNumber();
            int lineCount = SourceText.Lines.Count;
            do
            {
                SyntaxToken tk = EatToken();
                if (tk.Kind == SyntaxTokenKind.EndOfFileToken)
                {
                    break;
                }

            } while (currentLine <= lineCount && GetLineNumber() == currentLine);
        }

        private void Report(DiagnosticId diagnosticId)
        {
            _diagnostics.Report(diagnosticId, CurrentToken.TextSpan);
        }

        private void Report(DiagnosticId diagnosticId, TextSpan span)
        {
            _diagnostics.Report(diagnosticId, span);
        }

        private void Report(DiagnosticId diagnosticId, params object[] arguments)
        {
            _diagnostics.Report(diagnosticId, CurrentToken.TextSpan, arguments);
        }

        private TextSpan GetSpanForMissingToken()
        {
            if (_tokenOffset > 0)
            {
                SyntaxToken prevToken = PeekToken(-1);
                TextSpan prevTokenLineSpan = SourceText.GetLineSpanFromPosition(prevToken.TextSpan.End);
                TextSpan currentLineSpan = SourceText.GetLineSpanFromPosition(CurrentToken.TextSpan.Start);
                if (currentLineSpan != prevTokenLineSpan)
                {
                    int newLineSequenceLength = currentLineSpan.Start - prevTokenLineSpan.End;
                    return new TextSpan(prevTokenLineSpan.End, newLineSequenceLength);
                }
            }

            return CurrentToken.TextSpan;
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

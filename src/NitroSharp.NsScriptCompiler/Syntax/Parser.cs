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

        // It's not always possible for the lexer to tell whether something is a string literal or an identifier,
        // since some identifiers (more specifically, parameter names and parameter references) in NSS can also
        // be enclosed in quotes. In such cases, the lexer outputs a StringLiteralOrQuotedIdentifier token and
        // lets the parser decide whether it's a string literal or an identifier. In order to do that, the parser
        // needs to keep track of the parameters that can be referenced in the current scope.
        //
        // Example:
        // function foo("stringParameter1", "stringParameter2") {
        //                     ↑                   ↑
        //     $bar = "stringParameter1" + "stringParameter2";
        // }             <identifier>         <identifier>
        private readonly Dictionary<string, ParameterSyntax> _currentParameterList;

        private readonly StringInternTable _internTable;
        private readonly DiagnosticBuilder _diagnostics;

        public Parser(Lexer lexer)
        {
            _lexer = lexer;
            _diagnostics = new DiagnosticBuilder();
            _internTable = new StringInternTable();
            _tokens = Lex();
            _currentParameterList = new Dictionary<string, ParameterSyntax>();
            if (_tokens.Length > 0)
            {
                CurrentToken = _tokens[0];
            }
        }

        private SyntaxToken PeekToken(int n) => _tokens[_tokenOffset + n];
        private SyntaxToken CurrentToken;
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
            CurrentToken = _tokens[++_tokenOffset];
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

            CurrentToken = _tokens[++_tokenOffset];
            return ct;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string GetText(in SyntaxToken token)
            => SourceText.GetText(token.TextSpan);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string GetValueText(in SyntaxToken token)
            => SourceText.GetText(token.GetValueSpan());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string InternValueText(in SyntaxToken token)
            => _internTable.Add(SourceText.GetCharacterSpan(token.GetValueSpan()));

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
            CurrentToken = _tokens[_tokenOffset];
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

        public SourceFileRootSyntax ParseSourceFile()
        {
            var fileReferences = ImmutableArray.CreateBuilder<Spanned<string>>();
            SyntaxTokenKind tk;
            while ((tk = CurrentToken.Kind) != SyntaxTokenKind.EndOfFileToken
                   && !SyntaxFacts.CanStartDeclaration(tk))
            {
                switch (CurrentToken.Kind)
                {
                    case SyntaxTokenKind.IncludeDirective:
                        EatToken();
                        SyntaxToken filePath = EatToken(SyntaxTokenKind.StringLiteralOrQuotedIdentifier);
                        fileReferences.Add(new Spanned<string>(GetValueText(filePath), filePath.TextSpan));
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

            var members = ImmutableArray.CreateBuilder<MemberDeclarationSyntax>();
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

            return new SourceFileRootSyntax(members.ToImmutable(), fileReferences.ToImmutable());
        }

        public MemberDeclarationSyntax ParseMemberDeclaration()
        {
            switch (CurrentToken.Kind)
            {
                case SyntaxTokenKind.ChapterKeyword:
                    return ParseChapterDeclaration();
                case SyntaxTokenKind.SceneKeyword:
                    return ParseSceneDeclaration();
                case SyntaxTokenKind.FunctionKeyword:
                    _currentParameterList.Clear();
                    return ParseFunctionDeclaration();

                default:
                    throw new InvalidOperationException($"{CurrentToken.Kind} cannot start a declaration.");
            }
        }

        private ChapterDeclarationSyntax ParseChapterDeclaration()
        {
            EatToken(SyntaxTokenKind.ChapterKeyword);
            Spanned<string> name = ParseIdentifier();
            BlockSyntax body = ParseBlock();
            return new ChapterDeclarationSyntax(name, body);
        }

        private SceneDeclarationSyntax ParseSceneDeclaration()
        {
            EatToken(SyntaxTokenKind.SceneKeyword);
            Spanned<string> name = ParseIdentifier();
            BlockSyntax body = ParseBlock();
            return new SceneDeclarationSyntax(name, body);
        }

        private FunctionDeclarationSyntax ParseFunctionDeclaration()
        {
            EatToken(SyntaxTokenKind.FunctionKeyword);
            Spanned<string> name = ParseIdentifier();
            ImmutableArray<ParameterSyntax> parameters = ParseParameterList();

            foreach (ParameterSyntax param in parameters)
            {
                _currentParameterList[param.Name.Value] = param;
            }

            BlockSyntax body = ParseBlock();
            return new FunctionDeclarationSyntax(name, parameters, body);
        }

        private ImmutableArray<ParameterSyntax> ParseParameterList()
        {
            EatToken(SyntaxTokenKind.OpenParen);

            var parameters = ImmutableArray.CreateBuilder<ParameterSyntax>();
            while (CurrentToken.Kind != SyntaxTokenKind.CloseParen
                && CurrentToken.Kind != SyntaxTokenKind.EndOfFileToken)
            {
                switch (CurrentToken.Kind)
                {
                    case SyntaxTokenKind.Identifier:
                    case SyntaxTokenKind.StringLiteralOrQuotedIdentifier:
                        var p = new ParameterSyntax(ParseIdentifier());
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

        private BlockSyntax ParseBlock()
        {
            EatToken(SyntaxTokenKind.OpenBrace);
            ImmutableArray<StatementSyntax> statements = ParseStatements();
            EatToken(SyntaxTokenKind.CloseBrace);
            return new BlockSyntax(statements);
        }

        private ImmutableArray<StatementSyntax> ParseStatements()
        {
            var statements = ImmutableArray.CreateBuilder<StatementSyntax>();
            while (CurrentToken.Kind != SyntaxTokenKind.CloseBrace)
            {
                StatementSyntax statement = ParseStatement();
                if (statement != null)
                {
                    statements.Add(statement);
                }
            }

            return statements.ToImmutable();
        }

        internal StatementSyntax ParseStatement()
        {
            StatementSyntax statement = null;
            do
            {
                statement = ParseStatementCore();
                if (statement != null) { break; }
                SyntaxTokenKind tk = CurrentToken.Kind;
                if (tk == SyntaxTokenKind.EndOfFileToken || tk == SyntaxTokenKind.CloseBrace)
                {
                    return null;
                }
            } while (true);

            return statement;
        }

        private StatementSyntax ParseStatementCore()
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
                    if (SkipStrayPXmlElementIfApplicable())
                    {
                        return null;
                    }
                    goto default;
                case SyntaxTokenKind.Dot:
                    SkipToNextLine();
                    return null;

                case SyntaxTokenKind.Identifier:
                case SyntaxTokenKind.StringLiteralOrQuotedIdentifier:
                    if (IsArgumentListOrSemicolon())
                    {
                        return ParseFunctionCallWithOmittedParentheses();
                    }
                    goto default;

                default:
                    return ParseExpressionStatement();
            }
        }

        private bool SkipStrayPXmlElementIfApplicable()
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
                    return false;
                }

                n++;
            }

            // Check if the current line ends with the '>' character that we found
            if (GetLineNumber(PeekToken(n + 1)) != currentLine)
            {
                Report(DiagnosticId.StrayPXmlElement, SourceText.Lines[currentLine]);
                EatTokens(n + 1); // skip to the next line
                return true;
            }

            return false;
        }

        private ExpressionStatementSyntax ParseExpressionStatement()
        {
            int start = CurrentToken.TextSpan.Start;
            ExpressionSyntax expr = ParseExpression();
            if (expr == null) { return null; }
            int end = PreviousToken.TextSpan.End;

            if (!SyntaxFacts.IsStatementExpression(expr))
            {
                var diagnosticSpan = new TextSpan(start, end - start);
                Report(DiagnosticId.InvalidExpressionStatement, diagnosticSpan);
                EatStatementTerminator();
                return null;
            }

            EatStatementTerminator();
            return new ExpressionStatementSyntax(expr);
        }

        private ExpressionStatementSyntax ParseFunctionCallWithOmittedParentheses()
        {
            FunctionCallExpressionSyntax call = ParseFunctionCall();
            EatStatementTerminator();
            return new ExpressionStatementSyntax(call);
        }

        internal ExpressionSyntax ParseExpression()
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

        private ExpressionSyntax ParseSubExpression(Precedence minPrecedence)
        {
            ExpressionSyntax leftOperand;
            Precedence newPrecedence;

            SyntaxTokenKind tk = CurrentToken.Kind;
            TextSpan tkSpan = CurrentToken.TextSpan;
            if (SyntaxFacts.TryGetUnaryOperatorKind(tk, out UnaryOperatorKind unaryOperator))
            {
                EatToken();
                newPrecedence = Precedence.Unary;
                ExpressionSyntax operand = ParseSubExpression(newPrecedence);
                if (operand == null) { return null; }
                leftOperand = new UnaryExpressionSyntax(
                    operand, new Spanned<UnaryOperatorKind>(unaryOperator, tkSpan));
            }
            else
            {
                leftOperand = ParseTerm(minPrecedence);
                if (leftOperand == null)
                {
                    return null;
                }
            }

            while (true)
            {
                tk = CurrentToken.Kind;
                tkSpan = CurrentToken.TextSpan;
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
                ExpressionSyntax rightOperand = hasRightOperand
                    ? ParseSubExpression(newPrecedence)
                    : leftOperand;

                if (rightOperand == null)
                {
                    return null;
                }

                leftOperand = binary
                    ? (ExpressionSyntax)new BinaryExpressionSyntax(
                        leftOperand, new Spanned<BinaryOperatorKind>(binOpKind, tkSpan), rightOperand)
                    : new AssignmentExpressionSyntax(
                        leftOperand, new Spanned<AssignmentOperatorKind>(assignOpKind, tkSpan), rightOperand);
            }

            return leftOperand;
        }

        private ExpressionSyntax ParseTerm(Precedence precedence)
        {
            switch (CurrentToken.Kind)
            {
                case SyntaxTokenKind.Identifier:
                    return IsFunctionCall() ? (ExpressionSyntax)ParseFunctionCall() : ParseNameSyntax();

                case SyntaxTokenKind.StringLiteralOrQuotedIdentifier:
                    return IsParameter() ? (ExpressionSyntax)ParseNameSyntax() : ParseLiteral();

                case SyntaxTokenKind.NumericLiteral:
                case SyntaxTokenKind.NullKeyword:
                case SyntaxTokenKind.TrueKeyword:
                case SyntaxTokenKind.FalseKeyword:
                    return ParseLiteral();

                case SyntaxTokenKind.OpenParen:
                    EatToken(SyntaxTokenKind.OpenParen);
                    ExpressionSyntax expr = ParseSubExpression(Precedence.Expression);
                    if (expr == null) { return null; }
                    EatToken(SyntaxTokenKind.CloseParen);
                    return expr;

                case SyntaxTokenKind.At:
                    return ParseDeltaExpression(precedence);

                default:
                    Report(DiagnosticId.InvalidExpressionTerm, GetText(CurrentToken));
                    EatToken();
                    return null;
            }
        }

        private DeltaExpressionSyntax ParseDeltaExpression(Precedence precedence)
        {
            EatToken(SyntaxTokenKind.At);
            ExpressionSyntax expr = ParseSubExpression(precedence);
            if (expr == null) { return null; }
            return new DeltaExpressionSyntax(expr);
        }

        private LiteralExpressionSyntax ParseLiteral()
        {
            SyntaxToken token = EatToken();
            ConstantValue value = default;
            switch (token.Kind)
            {
                case SyntaxTokenKind.NumericLiteral:
#if NETCOREAPP2_1
                    ReadOnlySpan<char> valueText = GetValueCharSpan(token);
#else
                    string valueText = InternValueText(token);
#endif
                    var numberStyle = token.IsHexTriplet ? NumberStyles.HexNumber : NumberStyles.None;
                    value = token.IsFloatingPointLiteral
                        ? ConstantValue.Float(float.Parse(valueText))
                        : ConstantValue.Integer(int.Parse(valueText, numberStyle));
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
                    ExceptionUtils.Unreachable();
                    return null;
            }

            return new LiteralExpressionSyntax(
                new Spanned<ConstantValue>(value, token.TextSpan));
        }

        private Spanned<string> ParseIdentifier()
        {
            SyntaxToken token = EatToken();
            switch (token.Kind)
            {
                case SyntaxTokenKind.Identifier:
                case SyntaxTokenKind.StringLiteralOrQuotedIdentifier:
                default:
                    return new Spanned<string>(InternValueText(token), token.TextSpan);
            }
        }

        private NameExpressionSyntax ParseNameSyntax(bool isFunctionName = false)
        {
            Debug.Assert(CurrentToken.Kind == SyntaxTokenKind.Identifier
                      || CurrentToken.Kind == SyntaxTokenKind.StringLiteralOrQuotedIdentifier);

            return new NameExpressionSyntax(ParseIdentifier());
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
            switch (CurrentToken.Kind)
            {
                case SyntaxTokenKind.Identifier:
                case SyntaxTokenKind.StringLiteralOrQuotedIdentifier:
                    return _currentParameterList.ContainsKey(InternValueText(CurrentToken));

                default:
                    return false;
            }
        }

        private FunctionCallExpressionSyntax ParseFunctionCall()
        {
            Spanned<string> targetName = ParseIdentifier();
            ImmutableArray<ExpressionSyntax>? args = ParseArgumentList();
            if (!args.HasValue) { return null; }
            return new FunctionCallExpressionSyntax(targetName, args.Value);
        }

        private ImmutableArray<ExpressionSyntax>? ParseArgumentList()
        {
            if (SyntaxFacts.IsStatementTerminator(CurrentToken.Kind))
            {
                return ImmutableArray<ExpressionSyntax>.Empty;
            }

            EatToken(SyntaxTokenKind.OpenParen);

            var args = ImmutableArray.CreateBuilder<ExpressionSyntax>();
            SyntaxTokenKind tk;
            while ((tk = CurrentToken.Kind) != SyntaxTokenKind.CloseParen
                   && tk != SyntaxTokenKind.Semicolon
                   && tk != SyntaxTokenKind.EndOfFileToken)
            {
                switch (tk)
                {
                    case SyntaxTokenKind.NumericLiteral:
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
                        ExpressionSyntax expr = ParseExpression();
                        if (expr == null) { return null; }
                        args.Add(expr);
                        break;
                }
            }

            EatToken(SyntaxTokenKind.CloseParen);
            return args.ToImmutable();
        }

        private IfStatementSyntax ParseIfStatement()
        {
            EatToken(SyntaxTokenKind.IfKeyword);
            EatToken(SyntaxTokenKind.OpenParen);
            ExpressionSyntax condition = ParseExpression();
            if (condition == null) { return null; }
            EatToken(SyntaxTokenKind.CloseParen);

            StatementSyntax ifTrue = ParseStatement();
            StatementSyntax ifFalse = null;
            if (CurrentToken.Kind == SyntaxTokenKind.ElseKeyword)
            {
                EatToken();
                ifFalse = ParseStatement();
            }

            return new IfStatementSyntax(condition, ifTrue, ifFalse);
        }

        private BreakStatementSyntax ParseBreakStatement()
        {
            EatToken(SyntaxTokenKind.BreakKeyword);
            EatStatementTerminator();
            return new BreakStatementSyntax();
        }

        private WhileStatementSyntax ParseWhileStatement()
        {
            EatToken(SyntaxTokenKind.WhileKeyword);
            EatToken(SyntaxTokenKind.OpenParen);
            ExpressionSyntax condition = ParseExpression();
            if (condition == null) { return null; }
            EatToken(SyntaxTokenKind.CloseParen);
            StatementSyntax body = ParseStatement();
            return new WhileStatementSyntax(condition, body);
        }

        private ReturnStatementSyntax ParseReturnStatement()
        {
            EatToken(SyntaxTokenKind.ReturnKeyword);
            EatStatementTerminator();
            return new ReturnStatementSyntax();
        }

        private SelectStatementSyntax ParseSelectStatement()
        {
            EatToken(SyntaxTokenKind.SelectKeyword);
            BlockSyntax body = ParseBlock();
            return new SelectStatementSyntax(body);
        }

        private SelectSectionSyntax ParseSelectSection()
        {
            EatToken(SyntaxTokenKind.CaseKeyword);
            Spanned<string> labelName = ConsumeTextUntil(tk => tk == SyntaxTokenKind.OpenBrace
                                                         || tk == SyntaxTokenKind.Colon);
            if (CurrentToken.Kind == SyntaxTokenKind.Colon)
            {
                EatToken();
            }

            BlockSyntax body = ParseBlock();
            return new SelectSectionSyntax(labelName, body);
        }

        private CallChapterStatementSyntax ParseCallChapterStatement()
        {
            EatToken(SyntaxTokenKind.CallChapterKeyword);
            Spanned<string> filePath = ConsumeTextUntil(tk => tk == SyntaxTokenKind.Semicolon);
            EatStatementTerminator();
            return new CallChapterStatementSyntax(filePath);
        }

        private CallSceneStatementSyntax ParseCallSceneStatement()
        {
            EatToken(SyntaxTokenKind.CallSceneKeyword);
            (Spanned<string>? file, Spanned<string> scene) = ParseSymbolPath();
            EatStatementTerminator();
            return new CallSceneStatementSyntax(file, scene);
        }

        // Parses call_scene specific symbol path syntax.
        // call_scene can be followed by either '@->{localSymbolName}' (e.g. '@->SelectStoryModeA')
        // or '{filepath}->{symbolName}' (e.g. 'nss/extra_gallery.nss->extra_gallery_main').
        private (Spanned<string>? filePath, Spanned<string> symbolName) ParseSymbolPath()
        {
            if (CurrentToken.Kind == SyntaxTokenKind.AtArrow)
            {
                EatToken();
            }

            Spanned<string>? filePath = null;
            Spanned<string> symbolName = default;
            Spanned<string> part = ConsumeTextUntil(tk => tk == SyntaxTokenKind.Semicolon
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

            return (filePath, symbolName);
        }

        // Consumes tokens until the specified condition is met. 
        private Spanned<string> ConsumeTextUntil(Func<SyntaxTokenKind, bool> condition)
        {
            SyntaxTokenKind tk;
            int start = CurrentToken.TextSpan.Start;
            int end = 0;
            while ((tk = CurrentToken.Kind) != SyntaxTokenKind.EndOfFileToken && !condition(tk))
            {
                end = EatToken().TextSpan.End;
            }

            var span = TextSpan.FromBounds(start, end);
            return new Spanned<string>(SourceText.GetText(span), span);
        }

        private DialogueBlockSyntax ParseDialogueBlock()
        {
            string extractBoxName(in SyntaxToken tag)
            {
                ReadOnlySpan<char> span = SourceText.GetCharacterSpan(tag.TextSpan);
                span = span.Slice(5, span.Length - 6);
                Debug.Assert(span.Length > 0);
                if (span[0] == '@')
                {
                    span = span.Slice(1);
                }

                return span.ToString();
            }

            string extractBlockName(in SyntaxToken identifierToken)
            {
                ReadOnlySpan<char> span = SourceText.GetCharacterSpan(identifierToken.TextSpan);
                Debug.Assert(span.Length >= 3);
                return span.Slice(1, span.Length - 2).ToString();
            }

            SyntaxToken startTag = EatToken(SyntaxTokenKind.DialogueBlockStartTag);
            string associatedBox = extractBoxName(startTag);
            SyntaxToken blockIdentifier = EatToken(SyntaxTokenKind.DialogueBlockIdentifier);
            string name = extractBlockName(blockIdentifier);

            var statements = ImmutableArray.CreateBuilder<StatementSyntax>();
            while (CurrentToken.Kind != SyntaxTokenKind.DialogueBlockEndTag)
            {
                StatementSyntax statement = ParseStatement();
                if (statement != null)
                {
                    statements.Add(statement);
                }
            }

            EatToken(SyntaxTokenKind.DialogueBlockEndTag);
            return new DialogueBlockSyntax(
                name, associatedBox, statements.ToImmutable());
        }

        private int GetLineNumber()
            => SourceText.GetLineNumberFromPosition(CurrentToken.TextSpan.Start);
        
        private int GetLineNumber(SyntaxToken token)
            => SourceText.GetLineNumberFromPosition(token.TextSpan.Start);

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
    }
}

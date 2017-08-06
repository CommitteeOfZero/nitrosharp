using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace NitroSharp.NsScript
{
    internal sealed class NsScriptParser
    {
        private readonly NsScriptLexer _lexer;
        private readonly SyntaxToken[] _tokens;
        private int _tokenOffset;

        private ImmutableArray<ParameterReference> _currentParameterList;

        public NsScriptParser(NsScriptLexer lexer)
        {
            _lexer = lexer;
            _tokens = PreLex().ToArray();
            _currentParameterList = ImmutableArray<ParameterReference>.Empty;
        }

        private string FileName => _lexer.FileName;
        private SyntaxToken CurrentToken => _tokens[_tokenOffset];

        public NsSyntaxTree ParseScript()
        {
            Chapter main = null;
            var scenes = ImmutableArray.CreateBuilder<Scene>();
            var functions = ImmutableArray.CreateBuilder<Function>();
            var includes = ImmutableArray.CreateBuilder<string>();
            while (CurrentToken.Kind != SyntaxTokenKind.EndOfFileToken)
            {
                _currentParameterList = ImmutableArray<ParameterReference>.Empty;
                switch (CurrentToken.Kind)
                {
                    case SyntaxTokenKind.IncludeDirective:
                        string fileName = ParseIncludeDirective();
                        includes.Add(fileName);
                        break;

                    case SyntaxTokenKind.ChapterKeyword:
                        main = ParseChapter();
                        break;

                    case SyntaxTokenKind.SceneKeyword:
                        var scene = ParseScene();
                        scenes.Add(scene);
                        break;

                    case SyntaxTokenKind.FunctionKeyword:
                        var function = ParseFunction();
                        functions.Add(function);
                        break;

                    default:
                        EatToken();
                        break;
                }
            }

            return new NsSyntaxTree(main, functions.ToImmutable(), scenes.ToImmutable(), includes.ToImmutable());
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
                throw UnexpectedToken(FileName, CurrentToken.Text);
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

        private string ParseIncludeDirective()
        {
            var directive = EatToken(SyntaxTokenKind.IncludeDirective);
            int idxNameStart = directive.Text.IndexOf('"') + 1;
            int idxNameEnd = directive.Text.IndexOf('"', idxNameStart);

            return directive.Text.Substring(idxNameStart, idxNameEnd - idxNameStart);
        }

        private Chapter ParseChapter()
        {
            EatToken(SyntaxTokenKind.ChapterKeyword);
            var name = ParseIdentifier();
            var body = ParseBlock();

            return StatementFactory.Chapter(name, body);
        }

        private Scene ParseScene()
        {
            EatToken(SyntaxTokenKind.SceneKeyword);
            var name = ParseIdentifier();
            var body = ParseBlock();

            return StatementFactory.Scene(name, body);
        }

        private Function ParseFunction()
        {
            EatToken(SyntaxTokenKind.FunctionKeyword);
            var name = ParseIdentifier();
            var parameters = _currentParameterList = ParseParameterList();
            var body = ParseBlock();

            return StatementFactory.Function(name, parameters, body);
        }

        private ImmutableArray<ParameterReference> ParseParameterList()
        {
            EatToken(SyntaxTokenKind.OpenParenToken);

            var parameters = ImmutableArray.CreateBuilder<ParameterReference>();
            while (CurrentToken.Kind != SyntaxTokenKind.CloseParenToken && CurrentToken.Kind != SyntaxTokenKind.EndOfFileToken)
            {
                switch (CurrentToken.Kind)
                {
                    case SyntaxTokenKind.IdentifierToken:
                        var p = ExpressionFactory.ParameterReference(ParseIdentifier());
                        parameters.Add(p);
                        break;

                    case SyntaxTokenKind.CommaToken:
                        EatToken();
                        break;

                    default:
                        throw UnexpectedToken(FileName, CurrentToken.Text);
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

            return StatementFactory.Block(statements);
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
                    return ParseChapterCall();

                case SyntaxTokenKind.CallSceneKeyword:
                    return ParseSceneCall();

                case SyntaxTokenKind.ParagraphStartTag:
                    return ParseParagraph();

                case SyntaxTokenKind.PXmlString:
                    return StatementFactory.PXmlString(EatToken().Text);

                case SyntaxTokenKind.PXmlLineSeparator:
                    EatToken();
                    return StatementFactory.PXmlLineSeparator();

                default:
                    EatToken();
                    return null;
            }
        }

        private ExpressionStatement ParseExpressionStatement()
        {
            var expr = ParseExpression();
            EatStatementTerminator();
            return StatementFactory.ExpressionStatement(expr);
        }

        internal Expression ParseExpression()
        {
            return ParseSubExpression(OperationPrecedence.Expression);
        }

        private bool IsExpectedPrefixUnaryOperator() => SyntaxFacts.IsPrefixUnaryOperator(CurrentToken.Kind);
        private bool IsExpectedPostfixUnaryOperator() => SyntaxFacts.IsPostfixUnaryOperator(CurrentToken.Kind);
        private bool IsExpectedBinaryOperator() => SyntaxFacts.IsBinaryOperator(CurrentToken.Kind);
        private bool IsExpectedAssignmentOperator() => SyntaxFacts.IsAssignmentOperator(CurrentToken.Kind);

        private Expression ParseSubExpression(OperationPrecedence minPrecedence)
        {
            Expression leftOperand;
            OperationPrecedence newPrecedence;

            var tk = CurrentToken.Kind;
            if (IsExpectedPrefixUnaryOperator())
            {
                var operationKind = SyntaxFacts.GetPrefixUnaryOperationKind(tk);
                EatToken();
                newPrecedence = OperationInfo.GetPrecedence(operationKind);
                var operand = ParseSubExpression(newPrecedence);
                leftOperand = ExpressionFactory.Unary(operand, operationKind);
            }
            else
            {
                leftOperand = ParseTerm(minPrecedence);
            }

            while (true)
            {
                tk = CurrentToken.Kind;
                OperationKind operationKind = default(OperationKind);
                bool binary;
                if (IsExpectedBinaryOperator())
                {
                    binary = true;
                    operationKind = SyntaxFacts.GetBinaryOperationKind(tk);
                }
                else if (IsExpectedAssignmentOperator())
                {
                    binary = false;
                    operationKind = SyntaxFacts.GetAssignmentOperationKind(tk);
                }
                else
                {
                    break;
                }

                newPrecedence = OperationInfo.GetPrecedence(operationKind);
                if (newPrecedence < minPrecedence)
                {
                    break;
                }

                EatToken();
                var rightOperand = ParseSubExpression(newPrecedence);
                leftOperand = binary ? (Expression)ExpressionFactory.Binary(leftOperand, operationKind, rightOperand) :
                    ExpressionFactory.Assignment(leftOperand as Variable, operationKind, rightOperand);
            }

            return leftOperand;
        }

        private Expression ParseTerm(OperationPrecedence precedence)
        {
            switch (CurrentToken.Kind)
            {
                case SyntaxTokenKind.IdentifierToken:
                    if (PeekToken(1).Kind == SyntaxTokenKind.OpenParenToken)
                    {
                        return ParseFunctionCall();
                    }

                    var symbol = ParseVariableOrParameterOrConstant();
                    return ParsePostfixExpression(symbol);

                case SyntaxTokenKind.StringLiteralToken:
                    if (IsParameter())
                    {
                        return ExpressionFactory.ParameterReference(ParseIdentifier());
                    }
                    else
                    {
                        return ParseLiteral();
                    }

                case SyntaxTokenKind.NumericLiteralToken:
                case SyntaxTokenKind.NullKeyword:
                case SyntaxTokenKind.TrueKeyword:
                case SyntaxTokenKind.FalseKeyword:
                    return ParseLiteral();

                case SyntaxTokenKind.OpenParenToken:
                    EatToken(SyntaxTokenKind.OpenParenToken);
                    var expr = ParseSubExpression(OperationPrecedence.Expression);
                    EatToken(SyntaxTokenKind.CloseParenToken);
                    return expr;

                case SyntaxTokenKind.AtToken:
                    return ParseDeltaExpression(precedence);

                default:
                    throw UnexpectedToken(FileName, CurrentToken.Text);
            }
        }

        private DeltaExpression ParseDeltaExpression(OperationPrecedence precedence)
        {
            EatToken(SyntaxTokenKind.AtToken);
            var expr = ParseSubExpression(precedence);
            return ExpressionFactory.DeltaExpression(expr);
        }

        private Expression ParsePostfixExpression(Expression expr)
        {
            if (IsExpectedPostfixUnaryOperator())
            {
                var operationKind = SyntaxFacts.GetPostfixUnaryOperationKind(CurrentToken.Kind);
                EatToken();
                return ExpressionFactory.Unary(expr, operationKind);
            }

            return expr;
        }

        private Literal ParseLiteral()
        {
            switch (CurrentToken.Kind)
            {
                case SyntaxTokenKind.NumericLiteralToken:
                case SyntaxTokenKind.StringLiteralToken:
                    bool isDeltaExpr = CurrentToken.Text[0] == '@';
                    var literal = ExpressionFactory.Literal(CurrentToken.Text, ConstantValue.Create(CurrentToken.Value, isDeltaExpr));
                    EatToken();
                    return literal;

                case SyntaxTokenKind.NullKeyword:
                    EatToken();
                    return ExpressionFactory.Null;

                case SyntaxTokenKind.TrueKeyword:
                    EatToken();
                    return ExpressionFactory.True;

                case SyntaxTokenKind.FalseKeyword:
                    EatToken();
                    return ExpressionFactory.False;

                default:
                    throw UnexpectedToken(FileName, CurrentToken.Text);
            }
        }

        private Identifier ParseIdentifier()
        {
            string fullName = EatToken().Text;

            int idxSimplifiedNameStart = 0;
            bool quotes = fullName[0] == '"';
            if (quotes)
            {
                idxSimplifiedNameStart++;
            }

            SigilKind sigil;
            char sigilChar = fullName[0] != '"' ? fullName[0] : fullName[1];
            switch (sigilChar)
            {
                case '$':
                    sigil = SigilKind.Dollar;
                    idxSimplifiedNameStart++;
                    break;

                case '#':
                    sigil = SigilKind.Hash;
                    idxSimplifiedNameStart++;
                    break;

                case '@':
                    if (fullName.Length > 3 && fullName[1] == '-' && fullName[2] == '>')
                    {
                        sigil = SigilKind.Arrow;
                        idxSimplifiedNameStart += 3;
                    }
                    else
                    {
                        sigil = SigilKind.At;
                        idxSimplifiedNameStart++;
                    }
                    break;

                default:
                    sigil = SigilKind.None;
                    break;
            }

            int idxSimplifiedNameEnd = quotes ? fullName.Length - 2 : fullName.Length - 1;
            string simplifiedName = fullName.Substring(idxSimplifiedNameStart, idxSimplifiedNameEnd - idxSimplifiedNameStart + 1);

            return ExpressionFactory.Identifier(fullName, simplifiedName, sigil);
        }

        // TODO: never used.
        private bool IsFunctionCall()
        {
            Debug.Assert(CurrentToken.Kind == SyntaxTokenKind.IdentifierToken || CurrentToken.Kind == SyntaxTokenKind.StringLiteralToken);

            var next = PeekToken(1);
            return next.Kind == SyntaxTokenKind.OpenParenToken || IsStatementTerminator(next.Kind);
        }

        private bool IsParameter()
        {
            switch (CurrentToken.Kind)
            {
                case SyntaxTokenKind.IdentifierToken:
                case SyntaxTokenKind.StringLiteralToken:
                    return _currentParameterList.Any(x => x.ParameterName.FullName.Equals(CurrentToken.Text, StringComparison.OrdinalIgnoreCase));

                default:
                    return false;
            }
        }

        private bool IsVariable()
        {
            if (CurrentToken.Kind != SyntaxTokenKind.IdentifierToken)
            {
                return false;
            }

            return SyntaxFacts.IsSigil(CurrentToken.Text[0]) || (CurrentToken.Text[0] == '"' && CurrentToken.Text[1] == '$');
        }

        private Expression ParseVariableOrParameterOrConstant()
        {
            if (IsParameter())
            {
                return ExpressionFactory.ParameterReference(ParseIdentifier());
            }
            else if (IsVariable())
            {
                return ExpressionFactory.Variable(ParseIdentifier());
            }

            return ExpressionFactory.NamedConstant(ParseIdentifier());
        }

        private FunctionCall ParseFunctionCall()
        {
            var targetName = ParseIdentifier();

            var args = ImmutableArray<Expression>.Empty;
            if (CurrentToken.Kind == SyntaxTokenKind.OpenParenToken)
            {
                args = ParseArgumentList();
            }

            return ExpressionFactory.FunctionCall(targetName, args);
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
                    case SyntaxTokenKind.NullKeyword:
                    case SyntaxTokenKind.TrueKeyword:
                    case SyntaxTokenKind.FalseKeyword:
                        var literal = ParseLiteral();
                        args.Add(literal);
                        break;

                    case SyntaxTokenKind.StringLiteralToken:
                        if (IsParameter())
                        {
                            var p = ExpressionFactory.ParameterReference(ParseIdentifier());
                            args.Add(p);
                        }
                        else
                        {
                            args.Add(ParseLiteral());
                        }
                        break;

                    case SyntaxTokenKind.IdentifierToken:
                        var name = ParseVariableOrParameterOrConstant();
                        args.Add(name);
                        break;

                    case SyntaxTokenKind.CommaToken:
                    case SyntaxTokenKind.DotToken:
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

            return StatementFactory.If(condition, ifTrue, ifFalse);
        }

        private BreakStatement ParseBreakStatement()
        {
            EatToken(SyntaxTokenKind.BreakKeyword);
            EatStatementTerminator();
            return StatementFactory.Break();
        }

        private WhileStatement ParseWhileStatement()
        {
            EatToken(SyntaxTokenKind.WhileKeyword);
            EatToken(SyntaxTokenKind.OpenParenToken);
            var condition = ParseExpression();
            EatToken(SyntaxTokenKind.CloseParenToken);
            var body = ParseStatement();

            return StatementFactory.While(condition, body);
        }

        private ReturnStatement ParseReturnStatement()
        {
            EatToken(SyntaxTokenKind.ReturnKeyword);
            EatStatementTerminator();

            return StatementFactory.Return();
        }

        private SelectStatement ParseSelectStatement()
        {
            EatToken(SyntaxTokenKind.SelectKeyword);
            var body = ParseBlock();
            return StatementFactory.Select(body);
        }

        private SelectSection ParseSelectSection()
        {
            EatToken(SyntaxTokenKind.CaseKeyword);
            var label = ParseIdentifier();
            if (CurrentToken.Kind == SyntaxTokenKind.ColonToken)
            {
                EatToken();
            }
            var body = ParseBlock();
            return StatementFactory.SelectSection(label, body);
        }

        private CallChapterStatement ParseChapterCall()
        {
            EatToken(SyntaxTokenKind.CallChapterKeyword);
            var chapterName = ParseIdentifier();
            EatStatementTerminator();
            return StatementFactory.CallChapter(chapterName);
        }

        private CallSceneStatement ParseSceneCall()
        {
            EatToken(SyntaxTokenKind.CallSceneKeyword);
            var sceneName = ParseIdentifier();
            EatStatementTerminator();
            return StatementFactory.CallScene(sceneName);
        }

        private Paragraph ParseParagraph()
        {
            string ExtractBoxName(string text)
            {
                int idxStart = 5;
                int idxEnd = text.Length - 1;

                return text.Substring(idxStart, idxEnd - idxStart);
            }

            string TrimParagraphIdentifier(string s)
            {
                return s.Length > 2 && s[0] == '[' && s[s.Length - 1] == ']' ? s.Substring(1, s.Length - 2) : s;
            }

            var startTag = EatToken(SyntaxTokenKind.ParagraphStartTag);
            string associatedBox = ExtractBoxName(startTag.Text);

            var identifier = EatToken(SyntaxTokenKind.ParagraphIdentifier);
            var statements = ImmutableArray.CreateBuilder<Statement>();
            while (CurrentToken.Kind != SyntaxTokenKind.ParagraphEndTag)
            {
                var statement = ParseStatement();
                if (statement != null)
                {
                    statements.Add(statement);
                }
            }

            EatToken(SyntaxTokenKind.ParagraphEndTag);
            string identifierString = TrimParagraphIdentifier(identifier.Text);
            return StatementFactory.Paragraph(identifierString, associatedBox, statements.ToImmutable());
        }

        private static NssParseException UnexpectedToken(string scriptName, string token)
        {
            return new NssParseException($"Parsing '{scriptName}' failed: unexpected token '{token}'");
        }
    }
}

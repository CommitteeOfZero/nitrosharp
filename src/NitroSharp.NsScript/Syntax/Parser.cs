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
            while (CurrentToken.Kind == SyntaxTokenKind.HashToken && PeekToken(1).Kind == SyntaxTokenKind.IncludeKeyword)
            {
                EatToken();
                EatToken();
                var filePath = EatToken(SyntaxTokenKind.StringLiteralToken);
                fileReferences.Add(new SourceFileReference((string)filePath.Value));
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

            return new SourceFile(SourceText.FileName, members.ToImmutable(), fileReferences.ToImmutable());
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
                    throw new InvalidOperationException();
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
                throw UnexpectedToken(SourceText.FileName, CurrentToken.Text);
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
                        var p = new Parameter(ParseIdentifier());
                        parameters.Add(p);
                        break;

                    case SyntaxTokenKind.CommaToken:
                        EatToken();
                        break;

                    default:
                        throw UnexpectedToken(SourceText.FileName, CurrentToken.Text);
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
                    return ParseChapterCall();

                case SyntaxTokenKind.CallSceneKeyword:
                    return ParseSceneCall();

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
                    throw UnexpectedToken(SourceText.FileName, CurrentToken.Text);
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
                case SyntaxTokenKind.StringLiteralToken:
                    var literal = new Literal(CurrentToken.Text, ConstantValue.Create(CurrentToken.Value));
                    EatToken();
                    return literal;

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
                    throw UnexpectedToken(SourceText.FileName, CurrentToken.Text);
            }
        }

        private Identifier ParseIdentifier()
        {
            Debug.Assert(CurrentToken.Kind == SyntaxTokenKind.IdentifierToken || CurrentToken.Kind == SyntaxTokenKind.StringLiteralToken);

            var token = EatToken();
            if (token.Kind == SyntaxTokenKind.IdentifierToken)
            {
                var idToken = (IdentifierToken)token;
                return new Identifier(idToken.Text, idToken.NameWithoutSigil, idToken.SigilCharacter);
            }

            return new Identifier(token.Text, (string)token.Value, SigilKind.None);
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

        private bool IsVariable()
        {
            if (CurrentToken.Kind != SyntaxTokenKind.IdentifierToken)
            {
                return false;
            }

            return SyntaxFacts.IsSigil(CurrentToken.Text[0]) || (CurrentToken.Text[0] == '"' && CurrentToken.Text[1] == '$');
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
            var label = ParseIdentifier();
            if (CurrentToken.Kind == SyntaxTokenKind.ColonToken)
            {
                EatToken();
            }
            var body = ParseBlock();
            return new SelectSection(label, body);
        }

        private CallChapterStatement ParseChapterCall()
        {
            EatToken(SyntaxTokenKind.CallChapterKeyword);
            var chapterName = ParseIdentifier();
            EatStatementTerminator();
            return new CallChapterStatement(chapterName);
        }

        private CallSceneStatement ParseSceneCall()
        {
            EatToken(SyntaxTokenKind.CallSceneKeyword);
            var sceneName = ParseIdentifier();
            EatStatementTerminator();
            return new CallSceneStatement(sceneName);
        }

        private DialogueBlock ParseDialogueBlock()
        {
            string TrimDialogueBlockIdentifier(string s)
            {
                return s.Length > 2 && s[0] == '[' && s[s.Length - 1] == ']' ? s.Substring(1, s.Length - 2) : s;
            }

            var startTag = EatToken(SyntaxTokenKind.DialogueBlockStartTag);
            string associatedBox = (string)startTag.Value;

            var identifier = EatToken(SyntaxTokenKind.DialogueBlockIdentifier);
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
            string identifierString = TrimDialogueBlockIdentifier(identifier.Text);
            var name = new Identifier(identifierString, identifierString, SigilKind.None);
            return new DialogueBlock(name, associatedBox, new Block(statements.ToImmutable()));
        }

        private static NsParseException UnexpectedToken(string scriptName, string token)
        {
            return new NsParseException($"Parsing '{scriptName}' failed: unexpected token '{token}'");
        }
    }
}

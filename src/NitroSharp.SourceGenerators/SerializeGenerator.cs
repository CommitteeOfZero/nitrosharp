using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static NitroSharp.SourceGenerators.Common;

namespace NitroSharp.SourceGenerators
{
    public static class SerializeGenerator
    {
        public static MethodDeclarationSyntax GenerateMethod(INamedTypeSymbol type)
        {
            return MethodDeclaration(
                PredefinedType(Token(SyntaxKind.VoidKeyword)),
                Identifier("Serialize"))
                .WithModifiers(TokenList(new[]
                {
                    Token(SyntaxKind.PublicKeyword)
                }))
                .WithParameterList(ParameterList(SingletonSeparatedList(
                    Parameter(Identifier("writer"))
                        .WithModifiers(TokenList(Token(SyntaxKind.RefKeyword)))
                        .WithType(IdentifierName("MessagePackWriter")))))
                .WithBody(Block(SerializeMembers(type, ThisExpression())));
        }

        private static SyntaxList<StatementSyntax> SerializeMembers(ITypeSymbol type, ExpressionSyntax target)
        {
            Stack<ISymbol> members = CollectFieldsAndProperties(type);
            var stmts = new List<StatementSyntax>();
            stmts.Add(WriteArrayHeader(members.Count));
            foreach (ISymbol member in members)
            {
                ITypeSymbol memberType = member switch
                {
                    IFieldSymbol field => field.Type,
                    IPropertySymbol prop => prop.Type,
                    _ => throw Unreachable()
                };
                try
                {
                    stmts.AddRange(Write(MemberAccess(target, member.Name), memberType));
                }
                catch (SourceGeneratorException e)
                {
                    throw new SourceGeneratorException(
                        $"Error when generating serialization code for " +
                        $"{type.Name}.{member.Name}: {e.Message}"
                    );
                }
            }

            return new SyntaxList<StatementSyntax>(stmts);
        }

        private static SyntaxList<StatementSyntax> Write(ExpressionSyntax target, ITypeSymbol type)
        {
            return GetFieldTypeKind(type) switch
            {
                FieldTypeKind.PrimitiveType or FieldTypeKind.CommonStdType
                    => SingletonList(WritePrimitiveTypeValue(target)),
                FieldTypeKind.Serializable => SingletonList(WriteSerializable(target)),
                FieldTypeKind.Array => WriteArray(target, (IArrayTypeSymbol)type),
                FieldTypeKind.Enum => WriteEnum(target, (INamedTypeSymbol)type),
                FieldTypeKind.ValueTuple => SerializeMembers(type, target),
                FieldTypeKind.Nullable => WriteNullable(target, (INamedTypeSymbol)type),
                _ => throw UnsupportedType(type)
            };
        }

        private static SyntaxList<StatementSyntax> WriteNullable(ExpressionSyntax target, INamedTypeSymbol type)
        {
            return SingletonList<StatementSyntax>(
                IfStatement(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            target,
                            IdentifierName("HasValue")),
                        Block(
                            Write(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    target,
                                    IdentifierName("Value")),
                                type.TypeArguments[0])))
                .WithElse(
                    ElseClause(
                        Block(
                            SingletonList<StatementSyntax>(
                                ExpressionStatement(
                                    InvocationExpression(
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName("writer"),
                                            IdentifierName("WriteNil")))))))));
        }

        private static StatementSyntax WriteSerializable(ExpressionSyntax target)
        {
            return ExpressionStatement(
                InvocationExpression(MemberAccess(target, "Serialize"))
                .WithArgumentList(
                    ArgumentList(
                        SingletonSeparatedList(
                            Argument(IdentifierName("writer"))
                                .WithRefKindKeyword(Token(SyntaxKind.RefKeyword))))));
        }

        private static StatementSyntax WritePrimitiveTypeValue(ExpressionSyntax target)
        {
            return ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("writer"),
                        IdentifierName("Write")))
                .WithArgumentList(
                    ArgumentList(SingletonSeparatedList(Argument(target)))));
        }

        private static SyntaxList<StatementSyntax> WriteEnum(ExpressionSyntax target, INamedTypeSymbol type)
        {
            INamedTypeSymbol underlyingType = type.EnumUnderlyingType!;
            return Write(
                CastExpression(ParseTypeName(FullyQualifiedName(underlyingType)), target),
                underlyingType);
        }

        private static StatementSyntax WriteArrayHeader(int count)
        {
            return WriteArrayHeader(
                LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    Literal(count)));
        }

        private static StatementSyntax WriteArrayHeader(ExpressionSyntax count)
        {
            return ExpressionStatement(
                InvocationExpression(MemberAccess(IdentifierName("writer"), "WriteArrayHeader"))
                .WithArgumentList(
                    ArgumentList(SingletonSeparatedList(Argument(count)))));
        }

        private static SyntaxList<StatementSyntax> WriteArray(ExpressionSyntax target, IArrayTypeSymbol array)
        {
            var length = MemberAccess(target, "Length");
            var stmts = new SyntaxList<StatementSyntax>(WriteArrayHeader(length));

            ForEachStatementSyntax loop = ForEachStatement(
                IdentifierName("var"),
                Identifier("item"),
                target,
                Block(Write(IdentifierName("item"), array.ElementType)));

            return stmts.Add(loop);
        }
    }
}

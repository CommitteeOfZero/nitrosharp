using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static NitroSharp.SourceGenerators.Common;

namespace NitroSharp.SourceGenerators
{
    public static class DeserializeGenerator
    {
        public static MemberDeclarationSyntax GenerateConstructor(INamedTypeSymbol type)
        {
            ConstructorDeclarationSyntax ctor = ConstructorDeclaration(Identifier(type.Name))
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                .WithParameterList(
                    ParameterList(
                        SingletonSeparatedList(
                            Parameter(Identifier("reader"))
                                .WithModifiers(TokenList(Token(SyntaxKind.RefKeyword)))
                                .WithType(IdentifierName("MessagePackReader")))))
                .WithBody(DeserializeMembers(type, ThisExpression()));

            if (type.IsRecord)
            {
                ParameterListSyntax? primaryCtorParams = null;

                foreach (SyntaxReference declReference in type.DeclaringSyntaxReferences)
                {
                    if (declReference.GetSyntax() is RecordDeclarationSyntax { ParameterList: { } parameterList })
                    {
                        primaryCtorParams = parameterList;
                        break;
                    }
                }

                ArgumentListSyntax? args = null;
                if (primaryCtorParams is not null)
                {
                    int count = primaryCtorParams.Parameters.Count;
                    LiteralExpressionSyntax defaultLiteral = LiteralExpression(SyntaxKind.DefaultLiteralExpression);
                    IEnumerable<ArgumentSyntax> values = Enumerable.Repeat(Argument(defaultLiteral), count);
                    args = ArgumentList(SeparatedList(values));
                }

                ctor = ctor.WithInitializer(ConstructorInitializer(SyntaxKind.ThisConstructorInitializer, args));
            }

            return ctor;
        }

        private static BlockSyntax DeserializeMembers(INamedTypeSymbol type, ExpressionSyntax target)
        {
            Stack<ISymbol> members = CollectFieldsAndProperties(type);
            var stmts = new List<StatementSyntax>();
            stmts.Add(ExpressionStatement(ArrayHeader()));
            foreach (ISymbol member in members)
            {
                ITypeSymbol memberType = member switch
                {
                    IFieldSymbol field => field.Type,
                    IPropertySymbol prop => prop.Type,
                    _ => throw new SourceGeneratorException("Unreachable")
                };
                try
                {
                    stmts.AddRange(Read(MemberAccess(target, member.Name), memberType));
                }
                catch (SourceGeneratorException e)
                {
                    throw new SourceGeneratorException(
                        $"Error when generating deserialization code for " +
                        $"{type.Name}.{member.Name}: {e.Message}"
                    );
                }
            }

            return Block(stmts);
        }

        private static SyntaxList<StatementSyntax> Read(ExpressionSyntax target, ITypeSymbol type)
        {
            return GetFieldTypeKind(type) switch
            {
                FieldTypeKind.PrimitiveType or FieldTypeKind.CommonStdType
                    => SingletonList(ReadPrimitiveTypeValue(target, type)),
                FieldTypeKind.Serializable => SingletonList(ReadSerializable(target, type)),
                FieldTypeKind.Array => ReadArray(target, (IArrayTypeSymbol)type),
                FieldTypeKind.Enum => ReadEnum(target, (INamedTypeSymbol)type),
                FieldTypeKind.ValueTuple => ReadValueTuple(target, type),
                FieldTypeKind.Nullable => ReadNullable(target, (INamedTypeSymbol)type),
                _ => throw UnsupportedType(type)
            };
        }

        private static SyntaxList<StatementSyntax> ReadNullable(ExpressionSyntax target, INamedTypeSymbol type)
        {
            return List(new StatementSyntax[]
            {
                ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        target,
                        LiteralExpression(SyntaxKind.NullLiteralExpression))),
                IfStatement(
                PrefixUnaryExpression(
                    SyntaxKind.LogicalNotExpression,
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName("reader"),
                            IdentifierName("TryReadNil")))),
                Block(Read(target, type.TypeArguments[0])))
            });
        }

        private static StatementSyntax ReadSerializable(ExpressionSyntax target, ITypeSymbol type)
        {
            return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    target,
                    ObjectCreationExpression(
                        IdentifierName(FullyQualifiedName(type)))
                    .WithArgumentList(
                        ArgumentList(
                            SingletonSeparatedList(
                                Argument(IdentifierName("reader"))
                                .WithRefKindKeyword(Token(SyntaxKind.RefKeyword)))))));
        }

        private static SyntaxList<StatementSyntax> ReadEnum(ExpressionSyntax target, INamedTypeSymbol type)
        {
            INamedTypeSymbol underlyingType = type.EnumUnderlyingType!;
            return SingletonList<StatementSyntax>(ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    target,
                    CastExpression(
                        ParseTypeName(FullyQualifiedName(type)),
                        CallReadPrimitive(underlyingType)))));
        }

        private static StatementSyntax ReadPrimitiveTypeValue(ExpressionSyntax target, ITypeSymbol type)
        {
            return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    target,
                    CallReadPrimitive(type)));
        }

        private static SyntaxList<StatementSyntax> ReadValueTuple(ExpressionSyntax target, ITypeSymbol type)
        {
            var stmts = new SyntaxList<StatementSyntax>();

            (LocalDeclarationStatementSyntax, string) local(IFieldSymbol field)
            {
                string name = $"{GenerateIdentifier(target)}{field.Name}";
                return (LocalDeclarationStatement(
                    VariableDeclaration(ParseTypeName(FullyQualifiedName(field.Type)))
                    .WithVariables(
                        SingletonSeparatedList(
                            VariableDeclarator(Identifier(name))))), name);
            }

            IFieldSymbol[] fields = type.GetMembers().OfType<IFieldSymbol>().ToArray();
            stmts = stmts.Add(ReadArrayHeader($"{GenerateIdentifier(target)}TupleSize"));

            var locals = new List<string>(fields.Length);
            foreach (IFieldSymbol field in fields)
            {
                (LocalDeclarationStatementSyntax decl, string varName) = local(field);
                stmts = stmts.Add(decl);
                stmts = stmts.AddRange(Read(IdentifierName(varName), field.Type));
                locals.Add(varName);
            }

            TupleExpressionSyntax tuple = TupleExpression(
                SeparatedList(
                    locals.Select(x => Argument(IdentifierName(x))),
                    Enumerable.Repeat(Token(SyntaxKind.CommaToken), locals.Count - 1)));

            ExpressionStatementSyntax assignment = ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    target,
                    tuple));

            return stmts.Add(assignment);
        }

        private static SyntaxList<StatementSyntax> ReadArray(ExpressionSyntax target, IArrayTypeSymbol arrayType)
        {
            var stmts = new SyntaxList<StatementSyntax>();

            string lenField = $"{GenerateIdentifier(target)}Length";
            stmts = stmts.Add(ReadArrayHeader(lenField));

            //string src =
            //    $"for (int i = 0; i < {lenField}; i++){{" +
            //    $"obj.{field.Name}[i] = reader.{SelectReadMethod(arrayType.ElementType.SpecialType)}();}}";

            ExpressionStatementSyntax arrayInit = ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    target,
                    ArrayCreationExpression(
                        ArrayType(ParseTypeName(
                            arrayType.ElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))
                        .WithRankSpecifiers(
                            SingletonList(
                                ArrayRankSpecifier(
                                    SingletonSeparatedList<ExpressionSyntax>(
                                        IdentifierName(lenField))))))));

            stmts = stmts.Add(arrayInit);

            ForStatementSyntax loop = ForStatement(
                Block(
                    Read(ElementAccessExpression(target)
                        .WithArgumentList(
                            BracketedArgumentList(
                                SingletonSeparatedList(Argument(IdentifierName("i"))))), arrayType.ElementType)))
                .WithDeclaration(
                    VariableDeclaration(PredefinedType(Token(SyntaxKind.IntKeyword)))
                    .WithVariables(
                        SingletonSeparatedList(
                            VariableDeclarator(Identifier("i"))
                            .WithInitializer(EqualsValueClause(
                                LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0)))))))
                .WithCondition(
                    BinaryExpression(
                        SyntaxKind.LessThanExpression,
                        IdentifierName("i"),
                        IdentifierName(lenField)))
                .WithIncrementors(
                    SingletonSeparatedList<ExpressionSyntax>(
                        PostfixUnaryExpression(
                            SyntaxKind.PostIncrementExpression,
                            IdentifierName("i"))));

            return stmts.Add(loop);
        }

        private static LocalDeclarationStatementSyntax ReadArrayHeader(string variableName)
        {
            return LocalDeclarationStatement(
                VariableDeclaration(PredefinedType(Token(SyntaxKind.IntKeyword)))
                .WithVariables(
                    SingletonSeparatedList(
                        VariableDeclarator(Identifier(variableName))
                        .WithInitializer(EqualsValueClause(ArrayHeader())))));
        }

        private static InvocationExpressionSyntax ArrayHeader()
        {
            return InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName("reader"),
                    IdentifierName("ReadArrayHeader")));
        }

        private static InvocationExpressionSyntax CallReadPrimitive(ITypeSymbol type)
        {
            return InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName("reader"),
                    IdentifierName(SelectReadMethod(type))));
        }

        private static string SelectReadMethod(ITypeSymbol type)
        {
            if (GetFieldTypeKind(type) == FieldTypeKind.CommonStdType)
            {
                return $"Read{type.Name}";
            }

            Debug.Assert(GetFieldTypeKind(type) == FieldTypeKind.PrimitiveType);
            return type.SpecialType switch
            {
                SpecialType.System_Boolean => "ReadBoolean",
                SpecialType.System_Char => "ReadChar",
                SpecialType.System_SByte => "ReadSByte",
                SpecialType.System_Byte => "ReadByte",
                SpecialType.System_Int16 => "ReadInt16",
                SpecialType.System_UInt16 => "ReadUInt16",
                SpecialType.System_Int32 => "ReadInt32",
                SpecialType.System_UInt32 => "ReadUInt32",
                SpecialType.System_Int64 => "ReadInt64",
                SpecialType.System_UInt64 => "ReadUInt64",
                SpecialType.System_Single => "ReadSingle",
                SpecialType.System_Double => "ReadDouble",
                SpecialType.System_String => "ReadString",
                SpecialType.System_DateTime => "ReadDateTime",
                _ => throw Unreachable()
            };
        }

        private static string GenerateIdentifier(ExpressionSyntax expr)
            => string.Join("", expr.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>());
    }
}

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace NitroSharp.SourceGenerators
{
    internal enum FieldTypeKind
    {
        NonSerializable,
        PrimitiveType,
        Array,
        ValueTuple,
        Enum,
        CommonStdType,
        Nullable,
        Serializable
    }

    internal static class Common
    {
        public static ITypeSymbol SerializableAttribute = null!;

        public static bool IsSerializable(ITypeSymbol type)
        {
            bool partial = type.DeclaringSyntaxReferences
                .Any(x => x.GetSyntax() is TypeDeclarationSyntax decl and (StructDeclarationSyntax or ClassDeclarationSyntax)
                    && decl.Modifiers.Any(x => x.Kind() == SyntaxKind.PartialKeyword));

            if (!partial) { return false; }

            bool marked = type.GetAttributes()
                .Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, SerializableAttribute));

            return marked;
        }

        public static SyntaxKind AccessibilityKeyword(ITypeSymbol type)
        {
            return type.DeclaredAccessibility switch
            {
                Accessibility.Public => SyntaxKind.PublicKeyword,
                Accessibility.Internal => SyntaxKind.InternalKeyword,
                _ => throw new SourceGeneratorException("Serializable types must be either public or internal.")
            };
        }

        public static MemberAccessExpressionSyntax MemberAccess(ExpressionSyntax expr, string memberName)
        {
            return MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                expr,
                IdentifierName(memberName));
        }

        public static SyntaxList<StatementSyntax> CallBaseMethod(INamedTypeSymbol type, string argument, string method)
        {
            LocalDeclarationStatementSyntax varDecl = LocalDeclarationStatement(
                VariableDeclaration(IdentifierName("var"))
                .WithVariables(
                    SingletonSeparatedList(VariableDeclarator(Identifier("baseType"))
                        .WithInitializer(
                            EqualsValueClause(
                                CastExpression(
                                    IdentifierName(type.BaseType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                                    IdentifierName("obj")))))));

            ExpressionStatementSyntax call = ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("baseType"),
                        IdentifierName(method)))
                .WithArgumentList(
                    ArgumentList(
                        SingletonSeparatedList(Argument(IdentifierName(argument))
                            .WithRefKindKeyword(Token(SyntaxKind.RefKeyword))))));

            return List(new StatementSyntax[] { varDecl, call });
        }

        public static Stack<ISymbol> CollectFieldsAndProperties(ITypeSymbol type)
        {
            var symbols = new Stack<ISymbol>();
            ITypeSymbol? currentType = type;
            while (currentType is ITypeSymbol { SpecialType: not SpecialType.System_Object })
            {
                foreach (ISymbol sym in currentType.GetMembers().Reverse())
                {
                    if (sym is IFieldSymbol { IsStatic: false }
                        or IPropertySymbol { IsStatic: false })
                    {
                        if (sym is IPropertySymbol prop && !prop.IsAutoProperty()) { continue; }
                        if (currentType.IsTupleType || !sym.IsImplicitlyDeclared)
                        {
                            symbols.Push(sym);
                        }
                    }
                }
                currentType = currentType.BaseType;
            }

            return symbols;
        }

        public static bool IsAutoProperty(this IPropertySymbol property)
        {
            IEnumerable<IFieldSymbol> fields = property.ContainingType
                .GetMembers().OfType<IFieldSymbol>();
            return fields.Any(field => SymbolEqualityComparer.Default
                .Equals(field.AssociatedSymbol, property));
        }

        public static FieldTypeKind GetFieldTypeKind(ITypeSymbol type)
        {
            if (IsPrimitiveType(type))
            {
                return FieldTypeKind.PrimitiveType;
            }
            if (IsSerializable(type) || type is INamedTypeSymbol nts && IsCustomSerializable(nts))
            {
                return FieldTypeKind.Serializable;
            }
            if (type is IArrayTypeSymbol { Rank: 1 } arrayType)
            {
                if (GetFieldTypeKind(arrayType.ElementType) != FieldTypeKind.NonSerializable)
                {
                    return FieldTypeKind.Array;
                }
            }
            else if (type.TypeKind == TypeKind.Enum
                && type is INamedTypeSymbol { EnumUnderlyingType: INamedTypeSymbol underlyingType }
                && IsPrimitiveType(underlyingType))
            {
                return FieldTypeKind.Enum;
            }
            else if (type.IsTupleType)
            {
                return FieldTypeKind.ValueTuple;
            }

            if (type.MetadataName == "Nullable`1")
            {
                return FieldTypeKind.Nullable;
            }

            return FullyQualifiedName(type) switch
            {
                "global::System.Numerics.Vector2"
                    or "global::System.Numerics.Vector3"
                    or "global::System.Numerics.Vector4"
                    or "global::System.Numerics.Matrix4x4" => FieldTypeKind.CommonStdType,
                _ => FieldTypeKind.NonSerializable
            };
        }

        private static bool IsPrimitiveType(ITypeSymbol type)
        {
            switch (type.SpecialType)
            {
                case SpecialType.System_Boolean:
                case SpecialType.System_Char:
                case SpecialType.System_SByte:
                case SpecialType.System_Byte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_String:
                case SpecialType.System_DateTime:
                    return true;
                default:
                    return false;
            }
        }

        public static string GetFullName(INamespaceSymbol namespaceSymbol)
        {
            return namespaceSymbol.ContainingNamespace is { IsGlobalNamespace: false } containingNs
                ? GetFullName(containingNs) + "." + namespaceSymbol.Name
                : namespaceSymbol.Name;
        }

        private static bool IsCustomSerializable(INamedTypeSymbol type)
        {
            bool hasConstructor = type.Constructors
                .Any(x => x.Parameters.Length == 1 &&
                     x.Parameters.Any(x => x.Type.MetadataName == "MessagePackReader"));

            bool hasSerializeMethod = type.GetMembers()
                .Any(x =>
                {
                    return x is IMethodSymbol
                    {
                        Name: "Serialize",
                        IsStatic: false,
                        DeclaredAccessibility: Accessibility.Public or Accessibility.Internal,
                        Parameters: { Length: 1 } parameters
                    }
                    && parameters.Any(x => x.Type.MetadataName == "MessagePackWriter");
                });

            return hasConstructor && hasSerializeMethod;
        }

        public static SourceGeneratorException UnsupportedType(ITypeSymbol type)
        {
            return new($"Type is not serializable: {FullyQualifiedName(type)}.");
        }

        public static SourceGeneratorException Unreachable()
            => new("his program location is expected to be unreachable.");

        public static string FullyQualifiedName(ISymbol symbol)
            => symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }
}

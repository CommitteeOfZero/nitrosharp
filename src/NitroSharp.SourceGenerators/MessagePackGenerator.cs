using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace NitroSharp.SourceGenerators;

[Generator(LanguageNames.CSharp)]
public sealed class MessagePackGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        const string persistableAttribute = @"
using System;
namespace NitroSharp;
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
internal sealed class PersistableAttribute : Attribute
{
}";
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
            "PersistableAttribute.g.cs",
            SourceText.From(persistableAttribute, Encoding.UTF8)));

        IncrementalValuesProvider<INamedTypeSymbol> serializableTypes = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: static (node, _) => node is TypeDeclarationSyntax typeDecl && Common.IsSerializableCandidate(typeDecl),
            transform: Resolve
        ).Where(x => x is not null)!;

        context.RegisterSourceOutput(
            serializableTypes.Collect(),
            static (ctx, source) => Execute(ctx, source)
        );
    }

    private static INamedTypeSymbol? Resolve(GeneratorSyntaxContext ctx, CancellationToken cancellationToken)
    {
        if (ctx.SemanticModel.GetDeclaredSymbol(ctx.Node, cancellationToken) is not INamedTypeSymbol typeSymbol)
        {
            return null;
        }

        return Common.HasPersistableAttribute(typeSymbol) ? typeSymbol : null;
    }

    private static void Execute(SourceProductionContext context, ImmutableArray<INamedTypeSymbol> serializableTypes)
    {
        CompilationUnitSyntax cu = CompilationUnit()
            .WithUsings(List(new[]
            {
                UsingDirective(IdentifierName("MessagePack")), UsingDirective(IdentifierName("System"))
            }))
            .WithMembers(List(serializableTypes.Select(GenerateMembersForType)))
            .NormalizeWhitespace();

        context.AddSource("SaveData.Generated.cs", cu.GetText(Encoding.UTF8));
        GenerateExtensions(context);
    }

    private static MemberDeclarationSyntax GenerateMembersForType(INamedTypeSymbol type)
    {
        var firstDecl = (TypeDeclarationSyntax)type.DeclaringSyntaxReferences[0].GetSyntax();

        MemberDeclarationSyntax serializeMethod = SerializeGenerator.GenerateMethod(type);
        MemberDeclarationSyntax ctor = DeserializeGenerator.GenerateConstructor(type);

        TypeDeclarationSyntax newDecl = firstDecl
            .WithMembers(List(new[] { ctor, serializeMethod }))
            .WithBaseList(null)
            .WithAttributeLists(default);

        if (newDecl is RecordDeclarationSyntax recordDecl)
        {
            newDecl = recordDecl
                .WithParameterList(null)
                .WithOpenBraceToken(Token(SyntaxKind.OpenBraceToken))
                .WithCloseBraceToken(Token(SyntaxKind.CloseBraceToken));
        }

        return NamespaceDeclaration(ParseName(Common.GetFullName(type.ContainingNamespace)))
            .WithMembers(SingletonList((MemberDeclarationSyntax)newDecl));
    }

    private static void GenerateExtensions(SourceProductionContext context)
    {
        const string mpWriterExtensions = @"
using System.Numerics;
using MessagePack;

namespace NitroSharp
{
    internal static class MessagePackExtensions
    {
        public static void Write(this scoped ref MessagePackWriter writer, Vector2 v)
        {
            writer.WriteArrayHeader(2);
            writer.Write(v.X);
            writer.Write(v.Y);
        }

        public static Vector2 ReadVector2(this scoped ref MessagePackReader reader)
        {
            reader.ReadArrayHeader();
            return new Vector2(reader.ReadSingle(), reader.ReadSingle());
        }

        public static void Write(this ref MessagePackWriter writer, scoped in Vector3 v)
        {
            writer.WriteArrayHeader(3);
            writer.Write(v.X);
            writer.Write(v.Y);
            writer.Write(v.Z);
        }

        public static Vector3 ReadVector3(this scoped ref MessagePackReader reader)
        {
            reader.ReadArrayHeader();
            return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }

        public static void Write(this scoped ref MessagePackWriter writer, scoped in Vector4 v)
        {
            writer.WriteArrayHeader(4);
            writer.Write(v.X);
            writer.Write(v.Y);
            writer.Write(v.Z);
            writer.Write(v.W);
        }

        public static Vector4 ReadVector4(this scoped ref MessagePackReader reader)
        {
            reader.ReadArrayHeader();
            return new Vector4(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle()
            );
        }

        public static void Write(this scoped ref MessagePackWriter writer, in Matrix4x4 m)
        {
            writer.WriteArrayHeader(16);
            writer.Write(m.M11);
            writer.Write(m.M12);
            writer.Write(m.M13);
            writer.Write(m.M14);
            writer.Write(m.M21);
            writer.Write(m.M22);
            writer.Write(m.M23);
            writer.Write(m.M24);
            writer.Write(m.M31);
            writer.Write(m.M32);
            writer.Write(m.M33);
            writer.Write(m.M34);
            writer.Write(m.M41);
            writer.Write(m.M42);
            writer.Write(m.M43);
            writer.Write(m.M44);
        }

        public static Matrix4x4 ReadMatrix4x4(this scoped ref MessagePackReader reader)
        {
            reader.ReadArrayHeader();
            return new Matrix4x4(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle()
            );
        }
    }
}";
        context.AddSource("MessagePackExtensions.g.cs", SourceText.From(mpWriterExtensions, Encoding.UTF8));
    }
}

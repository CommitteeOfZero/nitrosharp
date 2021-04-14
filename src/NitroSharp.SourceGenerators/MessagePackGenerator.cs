using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static NitroSharp.SourceGenerators.Common;

namespace NitroSharp.SourceGenerators
{
    [Generator]
    public class MessagePackGenerator : ISourceGenerator
    {
        private const string PersistableAttributeName = "NitroSharp.PersistableAttribute";

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.Compilation is CSharpCompilation compilation)
            {
                Execute(context, compilation);
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
        }

        private void Execute(GeneratorExecutionContext context, CSharpCompilation compilation)
        {
            if (compilation.GetTypeByMetadataName(PersistableAttributeName) is not INamedTypeSymbol attrType)
            {
                throw new SourceGeneratorException($"The type {PersistableAttributeName} does not exist.");
            }

            INamespaceSymbol ns = compilation.Assembly.GlobalNamespace
                .GetNamespaceMembers()
                .First(x => x.Name == "NitroSharp");

            SerializableAttribute = attrType;
            List<INamedTypeSymbol> serializableTypes = GetSerializables(ns);

            CompilationUnitSyntax cu = CompilationUnit()
                .WithUsings(List(new[]
                {
                    UsingDirective(IdentifierName("ToolGeneratedExtensions")),
                    UsingDirective(IdentifierName("MessagePack")), UsingDirective(IdentifierName("System"))
                }))
                .WithMembers(List(serializableTypes.Select(GenerateMembersForType)))
                .NormalizeWhitespace();

            context.AddSource("SaveData.Generated.cs", cu.GetText(Encoding.UTF8));
            GenerateExtensions(context);
        }

        private static void GenerateExtensions(GeneratorExecutionContext context)
        {
            const string mpWriterExtensions = @"
using System.Numerics;
using MessagePack;

namespace ToolGeneratedExtensions
{
    internal static class MessagePackExtensions
    {
        public static void Write(this ref MessagePackWriter writer, Vector2 v)
        {
            writer.WriteArrayHeader(2);
            writer.Write(v.X);
            writer.Write(v.Y);
        }

        public static Vector2 ReadVector2(this ref MessagePackReader reader)
        {
            reader.ReadArrayHeader();
            return new Vector2(reader.ReadSingle(), reader.ReadSingle());
        }

        public static void Write(this ref MessagePackWriter writer, in Vector3 v)
        {
            writer.WriteArrayHeader(3);
            writer.Write(v.X);
            writer.Write(v.Y);
            writer.Write(v.Z);
        }

        public static Vector3 ReadVector3(this ref MessagePackReader reader)
        {
            reader.ReadArrayHeader();
            return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }

        public static void Write(this ref MessagePackWriter writer, in Vector4 v)
        {
            writer.WriteArrayHeader(4);
            writer.Write(v.X);
            writer.Write(v.Y);
            writer.Write(v.Z);
            writer.Write(v.W);
        }

        public static Vector4 ReadVector4(this ref MessagePackReader reader)
        {
            reader.ReadArrayHeader();
            return new Vector4(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle()
            );
        }

        public static void Write(this ref MessagePackWriter writer, in Matrix4x4 m)
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

        public static Matrix4x4 ReadMatrix4x4(this ref MessagePackReader reader)
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
            context.AddSource("MessagePackExtensions.cs", SourceText.From(mpWriterExtensions, Encoding.UTF8));
        }

        private static MemberDeclarationSyntax GenerateMembersForType(INamedTypeSymbol type)
        {
            MemberDeclarationSyntax serializeMethod = SerializeGenerator.GenerateMethod(type);
            MemberDeclarationSyntax ctor = DeserializeGenerator.GenerateConstructor(type);

            TypeDeclarationSyntax partialType = type.IsValueType
                ? StructDeclaration(type.Name)
                : ClassDeclaration(type.Name);
            partialType = partialType
            .WithModifiers(TokenList(new[]
            {
                Token(AccessibilityKeyword(type)),
                Token(SyntaxKind.PartialKeyword)
            }))
            .WithMembers(List(new[] { ctor, serializeMethod }));

            return NamespaceDeclaration(ParseName(GetFullName(type.ContainingNamespace)))
                .WithMembers(SingletonList((MemberDeclarationSyntax)partialType));
        }

        private static List<INamedTypeSymbol> GetSerializables(INamespaceSymbol root)
        {
            static void doGetTypes(List<INamedTypeSymbol> list, INamespaceOrTypeSymbol symbol)
            {
                if (symbol is INamedTypeSymbol type && IsSerializable(type))
                {
                    list.Add(type);
                }

                foreach (ISymbol child in symbol.GetMembers())
                {
                    if (child is INamespaceOrTypeSymbol namespaceOrType)
                    {
                        doGetTypes(list, namespaceOrType);
                    }
                }
            }

            var list = new List<INamedTypeSymbol>();
            doGetTypes(list, root);
            return list;
        }
    }
}

using System;
using System.IO;
using System.Reflection;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal static class EffectLibrary
    {
        private static Assembly s_assembly = typeof(EffectLibrary).Assembly;

        public static T LoadEffect<T>(GraphicsDevice graphicsDevice) where T : Effect
        {
            var type = typeof(T);
            var factory = graphicsDevice.ResourceFactory;
            string name = type.Name.Replace("Effect", string.Empty);
            var vertex = LoadShader(factory, name, ShaderStages.Vertex, "VS");
            var fragment = LoadShader(factory, name, ShaderStages.Fragment, "FS");
            return (T)Activator.CreateInstance(type, graphicsDevice, vertex, fragment);
        }

        public static Shader LoadShader(ResourceFactory factory, string set, ShaderStages stage, string entryPoint)
        {
            string name = "NitroSharp.Graphics.Shaders." + 
                $"{set}-{stage.ToString().ToLower()}.{GetExtension(factory.BackendType)}";

            using (var stream = s_assembly.GetManifestResourceStream(name))
            using (var reader = new BinaryReader(stream))
            {
                var bytes = reader.ReadBytes((int)stream.Length);
                return factory.CreateShader(new ShaderDescription(stage, bytes, entryPoint));
            }
        }

        private static string GetExtension(GraphicsBackend backendType)
        {
            return (backendType == GraphicsBackend.Direct3D11)
                ? "hlsl.bytes"
                : (backendType == GraphicsBackend.Vulkan)
                    ? "450.glsl.spv"
                    : (backendType == GraphicsBackend.Metal)
                        ? "metallib"
                        : "330.glsl";
        }
    }
}

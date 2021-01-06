using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Veldrid;

namespace NitroSharp.Graphics.Core
{
    internal sealed class ShaderLibrary : IDisposable
    {
        private static readonly Assembly s_assembly = typeof(ShaderLibrary).Assembly;
        private readonly List<(Shader, Shader)> _shaderSets;

        public ShaderLibrary(GraphicsDevice graphicsDevice)
        {
            GraphicsDevice = graphicsDevice;
            _shaderSets = new List<(Shader, Shader)>();
        }

        public GraphicsDevice GraphicsDevice { get; }

        public (Shader vs, Shader fs) LoadShaderSet(string name)
        {
            Shader vs = LoadShader(name, ShaderStages.Vertex, "main");
            Shader fs = LoadShader(name, ShaderStages.Fragment, "main");
            _shaderSets.Add((vs, fs));
            return (vs, fs);
        }

        private Shader LoadShader(string set, ShaderStages stage, string entryPoint)
        {
            ResourceFactory factory = GraphicsDevice.ResourceFactory;
            string name = "NitroSharp.Graphics.Shaders." + set +
                $"-{stage.ToString().ToLower()}{GetExtension(factory.BackendType)}";

            Stream? stream = s_assembly.GetManifestResourceStream(name);
            if (stream == null)
            {
                throw new InvalidOperationException(
                    $"Couldn't find shader set '{set}'. " +
                    "Did you forget to run the shader compiler?"
                );
            }
            using (var reader = new BinaryReader(stream))
            {
                byte[] bytes = reader.ReadBytes((int)stream.Length);
                return factory.CreateShader(new ShaderDescription(stage, bytes, entryPoint));
            }
        }

        private static string GetExtension(GraphicsBackend backend)
        {
            return backend switch
            {
                GraphicsBackend.Direct3D11 => ".hlsl.bytes",
                GraphicsBackend.Vulkan => ".450.glsl.spv",
                GraphicsBackend.OpenGL => ".330.glsl",
                GraphicsBackend.OpenGLES => ".300.glsles",
                GraphicsBackend.Metal => ".metallib",
                _ => ThrowIllegalValue(nameof(backend))
            };
        }

        public void Dispose()
        {
            foreach ((Shader vs, Shader fs) in _shaderSets)
            {
                vs.Dispose();
                fs.Dispose();
            }
            _shaderSets.Clear();
        }

        private static string ThrowIllegalValue(string paramName)
            => throw new ArgumentException("Illegal value.", paramName);
    }
}

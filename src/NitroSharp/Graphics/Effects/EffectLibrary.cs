using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Veldrid;

namespace NitroSharp.Graphics
{
    public sealed class EffectLibrary : IDisposable
    {
        private static readonly Assembly s_assembly = typeof(EffectLibrary).Assembly;

        private readonly GraphicsDevice _gd;
        private readonly Dictionary<Type, Effect> _effectCache = new Dictionary<Type, Effect>();

        public EffectLibrary(GraphicsDevice graphicsDevice)
        {
            _gd = graphicsDevice;
        }

        public T Get<T>(BoundResourceSet sharedProperties) where T : Effect
        {
            if (!_effectCache.TryGetValue(typeof(T), out var effect))
            {
                effect = _effectCache[typeof(T)] = LoadEffect<T>(_gd, sharedProperties);
            }

            return (T)effect;
        }

        private T LoadEffect<T>(GraphicsDevice graphicsDevice, BoundResourceSet sharedProperties) where T : Effect
        {
            var type = typeof(T);
            var factory = graphicsDevice.ResourceFactory;
            string name = type.Name.Replace("Effect", string.Empty);
            var vertex = LoadShader(factory, name, ShaderStages.Vertex, "VS");
            var fragment = LoadShader(factory, name, ShaderStages.Fragment, "FS");
            return (T)Activator.CreateInstance(type, graphicsDevice, vertex, fragment, sharedProperties);
        }

        public static Shader LoadShader(ResourceFactory factory, string set, ShaderStages stage, string entryPoint)
        {
            string name = "NitroSharp.Graphics.Shaders." + 
                $"{set}-{stage.ToString().ToLower()}{GetExtension(factory.BackendType)}";

            using (var stream = s_assembly.GetManifestResourceStream(name))
            using (var reader = new BinaryReader(stream))
            {
                var bytes = reader.ReadBytes((int)stream.Length);
                return factory.CreateShader(new ShaderDescription(stage, bytes, entryPoint));
            }
        }

        public static string GetExtension(GraphicsBackend backend)
        {
            if (backend == GraphicsBackend.Vulkan || backend == GraphicsBackend.Direct3D11)
            {
                return GetBytecodeExtension(backend);
            }

            return GetSourceExtension(backend);
        }

        private static string GetBytecodeExtension(GraphicsBackend backend)
        {
            switch (backend)
            {
                case GraphicsBackend.Direct3D11: return ".hlsl.bytes";
                case GraphicsBackend.Vulkan: return ".450.glsl.spv";
                case GraphicsBackend.OpenGL:
                    throw new InvalidOperationException("OpenGL and OpenGLES do not support shader bytecode.");

                default:
                    throw new InvalidOperationException("Invalid Graphics backend: " + backend);
            }
        }

        private static string GetSourceExtension(GraphicsBackend backend)
        {
            switch (backend)
            {
                case GraphicsBackend.Direct3D11:
                    return ".hlsl";
                case GraphicsBackend.Vulkan:
                    return ".450.glsl";
                case GraphicsBackend.OpenGL:
                    return ".330.glsl";
                case GraphicsBackend.OpenGLES:
                    return ".300.glsles";
                case GraphicsBackend.Metal:
                    return ".metallib";

                default:
                    throw new InvalidOperationException("Invalid Graphics backend: " + backend);
            }
        }

        public void Dispose()
        {
            foreach (var effect in _effectCache.Values)
            {
                effect.Dispose();
            }

            _effectCache.Clear();
        }
    }
}

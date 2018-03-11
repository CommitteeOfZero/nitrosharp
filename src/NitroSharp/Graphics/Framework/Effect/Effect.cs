using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using NitroSharp.Utilities;
using Veldrid;

namespace NitroSharp.Graphics
{
    public abstract class Effect : IDisposable
    {
        private readonly GraphicsDevice _gd;
        private readonly Shader _vs, _fs;
        private readonly DisposeCollectorResourceFactory _factory;
        private readonly Pipeline _pipeline;

        private readonly IDictionary<string, PropertyBinding> _propertyBindings;
        private readonly ResourceLayoutSetPair[] _layoutSetPairs;

        private CommandList _cl;

        private readonly Dictionary<ResourceSetDescription, ResourceSet> _resourceSetCache;

        protected Effect(GraphicsDevice graphicsDevice, Shader vertexShader, Shader fragmentShader)
        {
            _gd = graphicsDevice;
            _vs = vertexShader;
            _fs = fragmentShader;
            _factory = new DisposeCollectorResourceFactory(graphicsDevice.ResourceFactory);
            _resourceSetCache = new Dictionary<ResourceSetDescription, ResourceSet>();

            EffectPropertyBinder.Bind(effectType: GetType(), _factory, out _propertyBindings, out _layoutSetPairs);

            ShaderSetDescription shaderSet = new ShaderSetDescription(
                new[]
                {
                    Vertex2D.LayoutDescription
                },
                new Shader[]
                {
                    vertexShader,
                    fragmentShader
                });

            _pipeline = _factory.CreateGraphicsPipeline(new GraphicsPipelineDescription(
                BlendStateDescription.SingleAlphaBlend,
                DepthStencilStateDescription.Disabled,
                RasterizerStateDescription.Default,
                PrimitiveTopology.TriangleList,
                shaderSet,
                _layoutSetPairs.Select(x => x.ResourceLayout).ToArray(),
                graphicsDevice.SwapchainFramebuffer.OutputDescription));
        }

        public void Begin(CommandList cl)
        {
            _cl = cl;
        }

        public void End()
        {
            _cl.SetPipeline(_pipeline);
            for (uint i = 0; i < _layoutSetPairs.Length; i++)
            {
                ref var descriptor = ref _layoutSetPairs[i].ResourceSetDescription;
                _cl.SetGraphicsResourceSet(i, GetResourceSet(descriptor));
            }
        }

        protected void NotifyPropertyChanged(string propertyName, BindableResource newValue)
        {
            var binding = _propertyBindings[propertyName];
            ref var pair = ref _layoutSetPairs[(int)binding.Attribute.ResourceSet];
            ref var resource = ref pair.ResourceSetDescription.BoundResources[binding.PositionInResourceSet];
            resource = newValue;
        }

        protected void Set<T>(ref T property, T newValue, [CallerMemberName] string propertyName = "")
            where T : BindableResource
        {
            property = newValue;
            NotifyPropertyChanged(propertyName, newValue);
        }

        protected void Update<T>(ref T property, T newValue, [CallerMemberName] string propertyName = "")
            where T : struct
        {
            property = newValue;

            var binding = _propertyBindings[propertyName];
            ref var pair = ref _layoutSetPairs[(int)binding.Attribute.ResourceSet];
            ref var buffer = ref pair.ResourceSetDescription.BoundResources[binding.PositionInResourceSet];
            if (buffer == null)
            {
                buffer = _factory.CreateBuffer(new BufferDescription(binding.BufferSize, BufferUsage.UniformBuffer));
            }

            if (_cl != null)
            {
                _cl.UpdateBuffer((DeviceBuffer)buffer, 0, ref newValue);
            }
            else
            {
                _gd.UpdateBuffer((DeviceBuffer)buffer, 0, ref newValue);
            }
        }

        private ResourceSet GetResourceSet(in ResourceSetDescription description)
        {
            if (!_resourceSetCache.TryGetValue(description, out var resourceSet))
            {
                var copy = new ResourceSetDescription(description.Layout, (BindableResource[])description.BoundResources.Clone());
                resourceSet = _resourceSetCache[description] = _factory.CreateResourceSet(ref copy);
            }

            return resourceSet;
        }

        public virtual void Dispose()
        {
            _resourceSetCache.Clear();
            _factory.DisposeCollector.DisposeAll();
            _vs.Dispose();
            _fs.Dispose();
        }
    }
}

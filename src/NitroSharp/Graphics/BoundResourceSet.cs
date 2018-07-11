using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NitroSharp.Utilities;
using Veldrid;

namespace NitroSharp.Graphics
{
    public abstract class BoundResourceSet
    {
        private readonly GraphicsDevice _gd;
        private readonly DisposeCollectorResourceFactory _factory;
        private CommandList _cl;

        private readonly IDictionary<string, PropertyBinding> _propertyBindings;
        private (ResourceLayout layout, ResourceSetDescription setDescription) _layoutSetPair;
        private readonly Dictionary<ResourceSetDescription, ResourceSet> _resourceSetCache;

        public BoundResourceSet(GraphicsDevice graphicsDevice)
        {
            _gd = graphicsDevice;
            _factory = new DisposeCollectorResourceFactory(_gd.ResourceFactory);
            _resourceSetCache = new Dictionary<ResourceSetDescription, ResourceSet>();

            Bind(GetType(), _factory, out _propertyBindings, out _layoutSetPair);
            ResourceLayout = _layoutSetPair.layout;
        }

        public ResourceLayout ResourceLayout { get; }

        public void BeginRecording(CommandList commandList)
        {
            _cl = commandList;
        }

        public void EndRecording()
        {
            _cl = null;
        }

        public void Apply(CommandList commandList, uint slot)
        {
            ref var descriptor = ref _layoutSetPair.setDescription;
            commandList.SetGraphicsResourceSet(slot, GetResourceSet(ref descriptor));
        }

        protected void Set<T>(ref T property, T newValue, [CallerMemberName] string propertyName = "")
            where T : BindableResource
        {
            property = newValue;

            var binding = _propertyBindings[propertyName];
            ref var resourceSetDesc = ref _layoutSetPair.setDescription;
            ref var resource = ref resourceSetDesc.BoundResources[binding.PositionInResourceSet];
            resource = newValue;
        }

        protected void Update<T>(ref T property, T newValue, [CallerMemberName] string propertyName = "")
            where T : struct
        {
            property = newValue;

            var binding = _propertyBindings[propertyName];
            ref var resourceSetDesc = ref _layoutSetPair.setDescription;
            ref var buffer = ref resourceSetDesc.BoundResources[binding.PositionInResourceSet];
            if (buffer == null)
            {
                buffer = _factory.CreateBuffer(
                    new BufferDescription(binding.BufferSize, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
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

        private ResourceSet GetResourceSet(ref ResourceSetDescription description)
        {
            if (!_resourceSetCache.TryGetValue(description, out var resourceSet))
            {
                var copy = new ResourceSetDescription(description.Layout, (BindableResource[])description.BoundResources.Clone());
                resourceSet = _factory.CreateResourceSet(ref copy);
                _resourceSetCache[description] = resourceSet;
            }

            return resourceSet;
        }

        public virtual void Dispose()
        {
            _resourceSetCache.Clear();
            _factory.DisposeCollector.DisposeAll();
        }

        private static void Bind(
            Type type, ResourceFactory resourceFactory,
            out IDictionary<string, PropertyBinding> propertyBindings,
            out (ResourceLayout, ResourceSetDescription) layoutSetPair)
        {
            propertyBindings = new Dictionary<string, PropertyBinding>();
            var layoutBuilder = new ArrayBuilder<ResourceLayoutElementDescription>(4);

            var typeInfo = type.GetTypeInfo();
            uint positionInResourceSet = 0;

            foreach (var propertyInfo in typeInfo.DeclaredProperties)
            {
                var attribute = propertyInfo.GetCustomAttribute<BoundResourceAttribute>();
                if (attribute != null)
                {
                    var propertyType = propertyInfo.PropertyType;
                    uint bufferSize = propertyType.IsValueType
                        ? (uint)MathUtil.RoundUp(Marshal.SizeOf(propertyType), multiple: 16)
                        : 0;

                    propertyBindings[propertyInfo.Name] = new PropertyBinding(attribute, positionInResourceSet++, bufferSize);

                    ref var currentElement = ref layoutBuilder.Add();
                    currentElement.Name = attribute.ResourceName ?? propertyInfo.Name;
                    currentElement.Kind = attribute.ResourceKind;
                    currentElement.Stages = attribute.ShaderStages;
                }
            }

            var layoutElements = layoutBuilder.ToArray();
            var layout = resourceFactory.CreateResourceLayout(new ResourceLayoutDescription(layoutElements));
            var set = new ResourceSetDescription(layout, new BindableResource[layoutElements.Length]);
            layoutSetPair = (layout, set);
        }

        internal readonly struct PropertyBinding
        {
            public PropertyBinding(BoundResourceAttribute attribute, uint positionInResourceSet, uint bufferSize)
            {
                Attribute = attribute;
                PositionInResourceSet = positionInResourceSet;
                BufferSize = bufferSize;
            }

            public BoundResourceAttribute Attribute { get; }
            public uint PositionInResourceSet { get; }
            public uint BufferSize { get; }
        }
    }
}

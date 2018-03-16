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
        public ResourceLayout ResourceLayout { get; }

        private readonly DisposeCollectorResourceFactory _factory;

        private readonly IDictionary<string, PropertyBinding> _propertyBindings;
        private (ResourceLayout layout, ResourceSetDescription setDescription) _layoutSetPair;

        private CommandList _cl;

        private readonly Dictionary<ResourceSetDescription, ResourceSet> _resourceSetCache;

        public BoundResourceSet(GraphicsDevice graphicsDevice)
        {
            _gd = graphicsDevice;
            _factory = new DisposeCollectorResourceFactory(_gd.ResourceFactory);

            _layoutSetPair = Bind(GetType(), _factory, out _propertyBindings);

            ResourceLayout = _layoutSetPair.layout;
            _resourceSetCache = new Dictionary<ResourceSetDescription, ResourceSet>();
        }

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

        protected void NotifyPropertyChanged(string propertyName, BindableResource newValue)
        {
            var binding = _propertyBindings[propertyName];
            ref var pair = ref _layoutSetPair;
            ref var resource = ref pair.setDescription.BoundResources[binding.PositionInResourceSet];
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
            ref var pair = ref _layoutSetPair;
            ref var buffer = ref pair.setDescription.BoundResources[binding.PositionInResourceSet];
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

        private ResourceSet GetResourceSet(ref ResourceSetDescription description)
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
        }

        private static (ResourceLayout layout, ResourceSetDescription setDesc) Bind(
            Type type, ResourceFactory resourceFactory,
            out IDictionary<string, PropertyBinding> propertyBindings)
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
                        ? (uint)MathHelper.RoundUp(Marshal.SizeOf(propertyType), multiple: 16)
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

            layoutBuilder.Reset();
            return (layout, set);
        }
    }
}

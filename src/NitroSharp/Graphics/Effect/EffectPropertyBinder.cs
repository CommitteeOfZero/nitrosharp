using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using NitroSharp.Utilities;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal static class EffectPropertyBinder
    {
        public static void Bind(
            Type effectType, ResourceFactory resourceFactory,
            out IDictionary<string, PropertyBinding> propertyBindings,
            out ResourceLayoutSetPair[] layoutSetPairs)
        {
            propertyBindings = new Dictionary<string, PropertyBinding>();
            var layoutBuilder = new ArrayBuilder<ResourceLayoutElementDescription>(4);
            var layoutSetPairBuilder = new ArrayBuilder<ResourceLayoutSetPair>(4);

            var typeInfo = effectType.GetTypeInfo();
            uint lastResourceSet = 0;
            uint positionInResourceSet = 0;

            foreach (var propertyInfo in typeInfo.DeclaredProperties)
            {
                var attribute = propertyInfo.GetCustomAttribute<BoundResourceAttribute>();
                if (attribute != null)
                {
                    if (attribute.ResourceSet != lastResourceSet)
                    {
                        FinalizeResourceLayout(ref layoutBuilder, ref layoutSetPairBuilder, resourceFactory);
                        positionInResourceSet = 0;
                    }

                    var propertyType = propertyInfo.PropertyType;
                    string resourceName = attribute.ResourceName ?? propertyInfo.Name;
                    uint bufferSize = propertyType.IsValueType ? (uint)MathHelper.RoundUp(Marshal.SizeOf(propertyType), multiple: 16) : 0;
                    propertyBindings[propertyInfo.Name] = new PropertyBinding(resourceName, attribute, positionInResourceSet++, bufferSize);

                    ref var currentElement = ref layoutBuilder.Add();
                    currentElement.Name = propertyInfo.Name;
                    currentElement.Kind = attribute.ResourceKind;
                    currentElement.Stages = attribute.ShaderStages;

                    lastResourceSet = attribute.ResourceSet;
                }
            }

            FinalizeResourceLayout(ref layoutBuilder, ref layoutSetPairBuilder, resourceFactory);
            layoutSetPairs = layoutSetPairBuilder.ToArray();
        }

        private static void FinalizeResourceLayout(
                ref ArrayBuilder<ResourceLayoutElementDescription> resourceLayoutBuilder,
                ref ArrayBuilder<ResourceLayoutSetPair> layoutSetPairs, ResourceFactory resourceFactory)
        {
            var layoutElements = resourceLayoutBuilder.ToArray();
            var layout = resourceFactory.CreateResourceLayout(new ResourceLayoutDescription(layoutElements));
            var rsd = new ResourceSetDescription(layout, new BindableResource[layoutElements.Length]);

            ref var pair = ref layoutSetPairs.Add();
            pair.ResourceLayout = layout;
            pair.ResourceSetDescription = rsd;

            resourceLayoutBuilder.Reset();
        }
    }
}

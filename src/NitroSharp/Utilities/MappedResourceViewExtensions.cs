using System;
using Veldrid;

namespace NitroSharp.Utilities
{
    internal static class MappedResourceViewExtensions
    {
        public static Span<T> ToSpan<T>(this MappedResourceView<T> resourceView) where T : struct
        {
            unsafe
            {
                return new Span<T>(resourceView.MappedResource.Data.ToPointer(), (int)resourceView.SizeInBytes);
            }
        }
    }
}

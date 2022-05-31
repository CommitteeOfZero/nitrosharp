using NitroSharp.Text;

namespace NitroSharp.Graphics;

internal static class Extensions
{
    public static DesignSizeU GetMaxDesignBounds(this TextLayout textLayout, RenderContext renderCtx)
    {
        return textLayout.MaxBounds.Convert(renderCtx.DeviceToWorldScale);
    }
}

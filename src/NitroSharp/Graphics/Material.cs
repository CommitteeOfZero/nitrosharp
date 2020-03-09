namespace NitroSharp.Graphics.New
{
    internal enum BlendMode : byte
    {
        Alpha,
        Additive,
        ReverseSubtractive,
        Multiplicative
    }

    internal enum FilterMode : byte
    {
        Point,
        Linear
    }

    internal enum MaterialKind
    {
        SolidColor,
        Texture
    }

    internal struct TextureVariant
    {

    }

    internal struct Material
    {
        public MaterialKind Kind;
        public TextureVariant Texture;

        public static void Meow(Material mat)
        {
            _ = mat switch
            {
                Material { Kind: MaterialKind.SolidColor } => 0,
                Material { Kind: MaterialKind.Texture, Texture: var tex} => 1
            };
        }
    }
}

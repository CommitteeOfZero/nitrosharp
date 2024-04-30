#version 450

layout(set = 1, binding = 0) uniform texture2D Texture;
layout(set = 1, binding = 1) uniform texture2D Mask;
layout(set = 1, binding = 2) uniform sampler Sampler;

layout(set = 2, binding = 0) uniform FadeAmount
{
    float _FadeAmount;
};

layout(location = 0) in vec4 fs_Color;
layout(location = 1) in vec2 fs_TexCoord;

layout(location = 0) out vec4 OutColor;

const float feather = 0.1;

void main()
{
    float mask = texture(sampler2D(Mask, Sampler), fs_TexCoord).r
        * (1 - feather) + feather * 0.5;
    float alpha = clamp((_FadeAmount - mask) / feather + 0.5, 0.0, 1.0);
	OutColor = texture(sampler2D(Texture, Sampler), fs_TexCoord)
        * fs_Color * alpha;
}

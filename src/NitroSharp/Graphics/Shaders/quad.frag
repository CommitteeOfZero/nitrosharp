#version 450

#extension GL_EXT_samplerless_texture_functions : enable

layout(set = 1, binding = 0) uniform texture2D Texture;
layout(set = 1, binding = 1) uniform texture2D AlphaMask;
layout(set = 1, binding = 2) uniform sampler Sampler;
layout(set = 1, binding = 3) uniform AlphaMaskPos
{
    vec2 _AlphaMaskPos;
    float _ScaleFactor;
    float _Padding;
};

layout(location = 0) in vec4 fs_Color;
layout(location = 1) in vec2 fs_TexCoord;

layout(location = 0) out vec4 OutColor;

void main()
{
    vec2 tex_res = textureSize(Texture, 0);
    vec2 uv = fs_TexCoord;
	vec4 texel = texture(sampler2D(Texture, Sampler), uv);
    vec2 maskUV = (gl_FragCoord.xy - _AlphaMaskPos) / (textureSize(AlphaMask, 0) * _ScaleFactor);

    float mask = texture(sampler2D(AlphaMask, Sampler), maskUV).r;
	float alpha = texel.a * fs_Color.a * mask;
	OutColor = vec4((texel * fs_Color * alpha).xyz, alpha);
}

#version 450

layout(set = 2, binding = 0) uniform texture2D Texture;
layout(set = 2, binding = 1) uniform sampler Sampler;

layout(location = 0) in vec4 fs_Color;
layout(location = 1) in vec2 fs_TexCoord;

layout(location = 0) out vec4 OutColor;

void main()
{
	OutColor = texture(sampler2D(Texture, Sampler), fs_TexCoord) * fs_Color;
}

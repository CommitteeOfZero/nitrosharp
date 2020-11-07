#version 450

layout(set = 1, binding = 0) uniform textureCube Texture;
layout(set = 1, binding = 1) uniform sampler Sampler;

layout(location = 0) in vec3 fs_TexCoord;
layout(location = 1) in float fs_Opacity;

layout(location = 0) out vec4 OutColor;

void main()
{
	OutColor = texture(samplerCube(Texture, Sampler), fs_TexCoord) * fs_Opacity;
}

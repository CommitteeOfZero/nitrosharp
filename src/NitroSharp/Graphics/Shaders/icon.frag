#version 450

#extension GL_EXT_samplerless_texture_functions : enable

layout(set = 1, binding = 0) uniform texture2DArray Texture;
layout(set = 1, binding = 1) uniform sampler Sampler;

layout(location = 1) in vec2 fs_TexCoord;
layout(location = 2) in float fs_Layer;

layout(location = 0) out vec4 OutColor;

void main()
{
    OutColor = texture(sampler2DArray(Texture, Sampler), vec3(fs_TexCoord, fs_Layer));
}

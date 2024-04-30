#version 450

#extension GL_EXT_samplerless_texture_functions : enable

layout(set = 0, binding = 0) uniform texture2D Input;
layout(set = 0, binding = 1) uniform sampler Sampler;

layout(location = 0) in vec2 fs_TexCoord;
layout(location = 0) out vec4 OutColor;

vec4 texel(vec2 offset, vec2 texelSize)
{
    return texture(sampler2D(Input, Sampler), fs_TexCoord + offset * texelSize);
}

void main()
{
    vec2 texelSize = vec2(1.0) / textureSize(Input, 0);
    vec4 sum = texel(vec2(-1, -1), texelSize)
        + texel(vec2(0, -1), texelSize)
        + texel(vec2(1, -1), texelSize)
        + texel(vec2(-1, 0), texelSize)
        + texel(vec2(0, 0), texelSize)
        + texel(vec2(1, 0), texelSize)
        + texel(vec2(-1, 1), texelSize)
        + texel(vec2(0, 1), texelSize)
        + texel(vec2(1, 1), texelSize);

    OutColor = (sum / vec4(9)) * 0.99f;
}

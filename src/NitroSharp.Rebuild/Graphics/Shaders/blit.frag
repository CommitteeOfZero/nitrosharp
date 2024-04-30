#version 450

layout(set = 0, binding = 0) uniform texture2D Input;
layout(set = 0, binding = 1) uniform sampler Sampler;

layout(location = 0) in vec2 fs_TexCoord;
layout(location = 0) out vec4 OutColor;

void main()
{
    OutColor = texture(sampler2D(Input, Sampler), fs_TexCoord);
}

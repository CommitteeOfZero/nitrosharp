#version 450

layout(set = 0, binding = 0) uniform ViewProjection
{
    mat4 _ViewProjection;
};

layout(location = 0) in vec2 vs_Position;
layout(location = 1) in vec2 vs_TexCoord;
layout(location = 2) in vec4 vs_Color;

layout(location = 0) out vec4 fs_Color;
layout(location = 1) out vec2 fs_TexCoord;

void main()
{
    gl_Position = _ViewProjection * vec4(vs_Position, 0, 1);
    fs_Color = vs_Color;
    fs_TexCoord = vs_TexCoord;
}

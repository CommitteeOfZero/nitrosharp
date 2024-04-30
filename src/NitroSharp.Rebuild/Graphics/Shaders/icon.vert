#version 450

layout(set = 0, binding = 0) uniform ViewProjection
{
    mat4 _ViewProjection;
};

layout(location = 0) in vec2 vs_Position;
layout(location = 1) in vec3 vs_TexCoord;
layout(location = 2) in vec3 vs_Padding;

layout(location = 0) out vec3 fs_TexCoord;

void main()
{
    gl_Position = _ViewProjection * vec4(vs_Position, 0, 1);
    fs_TexCoord = vs_TexCoord;
}

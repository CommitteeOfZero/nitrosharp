#version 450

layout(set = 0, binding = 0) uniform ViewProjection
{
    mat4 _ViewProjection;
};

layout(set = 2, binding = 0) uniform World
{
    mat4 _World;
};

layout(location = 0) in vec3 vs_Position;
layout(location = 1) in vec4 vs_Color;

layout(location = 0) out vec3 fs_TexCoord;
layout(location = 1) out vec4 fs_Color;

void main()
{
    fs_TexCoord = vec3(-vs_Position.x, vs_Position.y, vs_Position.z);
	fs_Color = vs_Color;
    gl_Position = _ViewProjection * _World * vec4(vs_Position.xyz, 1);
}

#version 450

layout(set = 0, binding = 0) uniform ViewProjection
{
    mat4 _ViewProjection;
};

layout(location = 0) in vec3 vs_Position;

layout(location = 1) in vec4 vs_Color;
layout(location = 2) in vec4 vs_Col1;
layout(location = 3) in vec4 vs_Col2;
layout(location = 4) in vec4 vs_Col3;
layout(location = 5) in vec4 vs_Col4;

layout(location = 0) out vec3 fs_TexCoord;
layout(location = 1) out vec4 fs_Color;

void main()
{
    fs_TexCoord = vec3(-vs_Position.x, vs_Position.y, vs_Position.z);
	fs_Color = vs_Color;
	mat4 world = mat4(vs_Col1.x, vs_Col2.x, vs_Col3.x, vs_Col4.x,
				      vs_Col1.y, vs_Col2.y, vs_Col3.y, vs_Col4.y,
					  vs_Col1.z, vs_Col2.z, vs_Col3.z, vs_Col4.z,
					  vs_Col1.w, vs_Col2.w, vs_Col3.w, vs_Col4.w);
    gl_Position = _ViewProjection * world * vec4(vs_Position.x, vs_Position.y, vs_Position.z, 1);
}

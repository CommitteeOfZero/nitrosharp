#version 330

layout(std140) uniform ViewProjection
{
    mat4 _ViewProjection;
} _86;

out vec3 vdspv_fsin0;
layout(location = 0) in vec3 vs_Position;
out vec4 vdspv_fsin1;
layout(location = 1) in vec4 vs_Color;
layout(location = 2) in vec4 vs_Col1;
layout(location = 3) in vec4 vs_Col2;
layout(location = 4) in vec4 vs_Col3;
layout(location = 5) in vec4 vs_Col4;

void main()
{
    vdspv_fsin0 = vec3(-vs_Position.x, vs_Position.y, vs_Position.z);
    vdspv_fsin1 = vs_Color;
    mat4 world = mat4(vec4(vs_Col1.x, vs_Col2.x, vs_Col3.x, vs_Col4.x), vec4(vs_Col1.y, vs_Col2.y, vs_Col3.y, vs_Col4.y), vec4(vs_Col1.z, vs_Col2.z, vs_Col3.z, vs_Col4.z), vec4(vs_Col1.w, vs_Col2.w, vs_Col3.w, vs_Col4.w));
    gl_Position = (_86._ViewProjection * world) * vec4(vs_Position.x, vs_Position.y, vs_Position.z, 1.0);
}


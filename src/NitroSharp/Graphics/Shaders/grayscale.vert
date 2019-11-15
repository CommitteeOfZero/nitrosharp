#version 450

layout (location = 0) out vec2 fs_TexCoord;

const vec4 FullscreenTriangle[3] =
{
    vec4(-1, -1, 0, 1),
    vec4(-1, 3, 0, -1),
    vec4(3, -1, 2, 1)
};

void main()
{
    vec4 vertex = FullscreenTriangle[gl_VertexIndex];
    gl_Position = vec4(vertex.xy, 0, 1);
    fs_TexCoord = vertex.zw;
}

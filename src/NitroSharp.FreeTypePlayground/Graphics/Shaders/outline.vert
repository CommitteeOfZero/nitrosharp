#version 450

#extension GL_EXT_samplerless_texture_functions : enable

const int MAX_GLYPHS = 4096;
const vec2 ATLAS_SIZE = vec2(512, 512);

layout(set = 0, binding = 0) uniform ViewProjection
{
    mat4 _ViewProjection;
};

layout(set = 0, binding = 1) uniform GlyphRects
{
    // x1, y1, x2, y2
    vec4 _GlyphRects[MAX_GLYPHS];
};

layout(set = 0, binding = 2) uniform utexture1D ArrayLayers;

// Fullscreen quad vertex
layout(location = 0) in vec2 vs_Position;

// Instance data
layout(location = 1) in vec4 vs_Color;
layout(location = 2) in vec2 vs_Origin;
layout(location = 3) in int vs_GlyphIndex;

layout(location = 0) out vec4 fs_Color;
layout(location = 1) out vec3 fs_TexCoord;

void main()
{
    vec4 rect = _GlyphRects[vs_GlyphIndex];
    float width = rect.z - rect.x;
    float height = rect.w - rect.y;
    vec2 pos[4] = vec2[4](
        vec2(vs_Origin.x, vs_Origin.y),
        vec2(vs_Origin.x + width, vs_Origin.y),
        vec2(vs_Origin.x, vs_Origin.y + height),
        vec2(vs_Origin.x + width, vs_Origin.y + height)
    );
    vec2 uv[4] = vec2[4](
        vec2(rect.x, rect.y),
        vec2(rect.z, rect.y),
        vec2(rect.x, rect.w),
        vec2(rect.z, rect.w)
    );

    vec2 p = pos[gl_VertexIndex];
    gl_Position = _ViewProjection * vec4(p.x, p.y, 0, 1);

    uint layer = texelFetch(ArrayLayers, vs_GlyphIndex, 0).r;
    fs_TexCoord = vec3(uv[gl_VertexIndex] / ATLAS_SIZE, layer);
    fs_Color = vs_Color;
}

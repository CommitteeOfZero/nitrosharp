#version 450

#extension GL_EXT_samplerless_texture_functions : enable

#define CACHE_TEX_SIZE vec2(512, 512)

layout(set = 0, binding = 0) uniform ViewProjection
{
    mat4 _ViewProjection;
};

layout(set = 0, binding = 1) uniform texture2D GlyphRuns;
layout(set = 0, binding = 2) uniform texture2D Transforms;
layout(set = 0, binding = 3) uniform texture2D GlyphRects;

layout(location = 0) in vec2 vs_Offset;
layout(location = 1) in int vs_GlyphRunID;
layout(location = 2) in int vs_GlyphID;
layout(location = 3) in int vs_OutlineID;
layout(location = 4) in float vs_Opacity;

layout(location = 0) out vec4 fs_Color;
layout(location = 1) out vec3 fs_TexCoord;

const uint indices[6] =
{
    0, 1, 2, 2, 1, 3
};

ivec2 getCacheEntryUV(int slot, int blocksPerSlot, int cacheTexSize)
{
    return ivec2(
        slot * blocksPerSlot % cacheTexSize,
        slot * blocksPerSlot / cacheTexSize
    );
}

void main()
{
    int glyphRunTexSize = textureSize(GlyphRuns, 0).x;
    int transformTexSize = textureSize(Transforms, 0).x;
    int glyphRectTexSize = textureSize(GlyphRects, 0).x;

    ivec2 glyphRunUV = getCacheEntryUV(vs_GlyphRunID, 2, glyphRunTexSize);
    vec4 textColor = texelFetch(GlyphRuns, glyphRunUV, 0);
    vec4 outlineColor = texelFetch(GlyphRuns, glyphRunUV + ivec2(1, 0), 0);

    ivec2 transformUV = getCacheEntryUV(vs_GlyphRunID, 4, transformTexSize);
    vec4 col1 = texelFetch(Transforms, transformUV, 0);
    vec4 col2 = texelFetch(Transforms, transformUV + ivec2(1, 0), 0);
    vec4 col3 = texelFetch(Transforms, transformUV + ivec2(2, 0), 0);
    vec4 col4 = texelFetch(Transforms, transformUV + ivec2(3, 0), 0);
    mat4 transform = mat4(col1, col2, col3, col4);

    ivec2 glyphInfoUV = getCacheEntryUV(vs_GlyphID, 2, glyphRectTexSize);
    vec4 block1 = texelFetch(GlyphRects, glyphInfoUV, 0);
    vec4 block2 = texelFetch(GlyphRects, glyphInfoUV + ivec2(1, 0), 0);
    float layer = block1.x;
    vec2 texOrigin = block1.yz;
    vec2 size = vec2(block1.w, block2.x);

    vec4 uvRect = vec4(
        texOrigin.x, texOrigin.y,
        texOrigin.x + size.x, texOrigin.y + size.y
    );
    vec2 quad[4] = vec2[4](
        vec2(vs_Offset.x, vs_Offset.y),
        vec2(vs_Offset.x + size.x, vs_Offset.y),
        vec2(vs_Offset.x, vs_Offset.y + size.y),
        vec2(vs_Offset.x + size.x, vs_Offset.y + size.y)
    );
    vec2 uv[4] = vec2[4](
        vec2(uvRect.x, uvRect.y),
        vec2(uvRect.z, uvRect.y),
        vec2(uvRect.x, uvRect.w),
        vec2(uvRect.z, uvRect.w)
    );

    uint index = indices[gl_VertexIndex];
    vec2 vert = quad[index];
    gl_Position = _ViewProjection * transform * vec4(vert.x, vert.y, 0, 1);
    fs_TexCoord = vec3(uv[index] / CACHE_TEX_SIZE, layer);
    fs_Color = textColor * vs_Opacity;
}

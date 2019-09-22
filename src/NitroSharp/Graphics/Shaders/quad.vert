#version 450

#extension GL_EXT_samplerless_texture_functions : enable

layout(set = 0, binding = 0) uniform ViewProjection
{
    mat4 _ViewProjection;
};

layout (set = 1, binding = 0) uniform texture2D CacheTexture;

layout(location = 0) out vec4 fs_Color;
layout(location = 1) out vec2 fs_TexCoord;

const int indices[6] =
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
    int instance = gl_VertexIndex / 6;
    int vertexInQuad = indices[gl_VertexIndex % 6];
    int texSize = textureSize(CacheTexture, 0).x;
    ivec2 cacheEntryUV = getCacheEntryUV(instance, 8, texSize);
    vec4 vertex = texelFetch(CacheTexture, cacheEntryUV + ivec2(vertexInQuad, 0), 0);
    vec2 position = vertex.xy;
    vec2 uv = vertex.zw;
    gl_Position = _ViewProjection * vec4(position, 0, 1);
    fs_Color = texelFetch(CacheTexture, cacheEntryUV + ivec2(4, 0), 0);
    fs_TexCoord = uv;
}

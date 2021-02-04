#version 450

#extension GL_EXT_samplerless_texture_functions : enable

layout(set = 1, binding = 0) uniform texture2D Luma;
layout(set = 1, binding = 1) uniform texture2DArray Chroma;
layout(set = 1, binding = 2) uniform sampler Sampler;

layout(set = 2, binding = 0) uniform EnableAlpha
{
    bool _EnableAlpha;
    vec3 _padding;
};

layout(location = 0) in vec4 fs_Color;
layout(location = 1) in vec2 fs_TexCoord;

layout(location = 0) out vec4 OutColor;

const mat4 yuv_to_rgb_rec601 = mat4(
    1.16438,  0.00000,  1.59603, -0.87079,
    1.16438, -0.39176, -0.81297,  0.52959,
    1.16438,  2.01723,  0.00000, -1.08139,
    0, 0, 0, 1
);

vec4 sampleTex(vec2 uv)
{
    float luma = texture(sampler2D(Luma, Sampler), uv).r;
    float cb = texture(sampler2DArray(Chroma, Sampler), vec3(uv, 0)).r;
    float cr = texture(sampler2DArray(Chroma, Sampler), vec3(uv, 1)).r;
    vec4 yuva = vec4(luma, cb, cr, 1.0);
    vec4 rgba = yuva * yuv_to_rgb_rec601;
    return rgba;
}

void main()
{
    vec4 rgba;
    if (!_EnableAlpha)
    {
        rgba = sampleTex(vec2(fs_TexCoord.x, fs_TexCoord.y));
    }
    else
    {
        rgba = sampleTex(vec2(fs_TexCoord.x, fs_TexCoord.y / 2));
        float alpha = sampleTex(vec2(fs_TexCoord.x, fs_TexCoord.y / 2 + 0.5)).r;
        rgba.a = alpha >= 0.9 ? 1.0 : alpha;
    }

    rgba *= fs_Color.a;
    OutColor = rgba;
}

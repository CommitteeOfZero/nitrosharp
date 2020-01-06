#version 450

#extension GL_EXT_samplerless_texture_functions : enable

layout(set = 1, binding = 0) uniform texture2D Texture;
layout(set = 1, binding = 1) uniform texture2D LensTexture;
layout(set = 1, binding = 2) uniform sampler Sampler;

layout(location = 0) in vec4 fs_Color;
layout(location = 1) in vec2 fs_TexCoord;
layout(location = 0) out vec4 OutColor;

vec4 sample_input(vec2 uv, vec2 offset, vec2 texelSize)
{
    return texture(sampler2D(Texture, Sampler), uv + offset * texelSize);
}

void main()
{
    vec4 mapTexel = texture(sampler2D(LensTexture, Sampler), fs_TexCoord);
    vec2 offset = mapTexel.gr;
    vec2 fRte = mapTexel.ab;
    if (mapTexel.a == 1.0)
    {
		offset.x = -offset.x;
	}
    if (mapTexel.b == 1.0)
    {
		offset.y = -offset.y;
    }

    vec2 srcTexSize = textureSize(Texture, 0);
    vec2 texelSize = vec2(1.0) / srcTexSize;
	vec2 uvOffset = (offset * 128.0) / srcTexSize;

    vec2 uv = gl_FragCoord.xy / srcTexSize + uvOffset;
    vec4 t0 = sample_input(uv, vec2(0, 0), texelSize);
    vec4 t1 = sample_input(uv, vec2(1, 0), texelSize);
    vec4 t2 = sample_input(uv, vec2(0, 1), texelSize);
    vec4 t3 = sample_input(uv, vec2(1, 1), texelSize);

	OutColor = mix(mix(t0, t1, fRte.x), mix(t2, t3, fRte.x), fRte.y);
	OutColor.a = 1.0f;
}

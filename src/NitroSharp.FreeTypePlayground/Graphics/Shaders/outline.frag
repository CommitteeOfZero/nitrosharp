#version 450

layout(set = 1, binding = 0) uniform texture2DArray Atlas;
layout(set = 1, binding = 1) uniform sampler Sampler;

layout(location = 0) in vec4 fs_Color;
layout(location = 1) in vec3 fs_TexCoord;

layout(location = 0) out vec4 OutColor;

void main()
{
    vec4 c = texture(sampler2DArray(Atlas, Sampler), fs_TexCoord);
    float alpha = c.x;
    alpha = mix(alpha, c.y, 0.60);
    alpha = mix(alpha, c.z, 0.30);
    alpha = mix(alpha, c.w, 0.15);
    //float alpha = texture(sampler2DArray(Atlas, Sampler), fs_TexCoord).r * fs_Color.w;
    OutColor = vec4(0, 0, 0, alpha);
}

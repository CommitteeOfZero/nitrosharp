#version 330

uniform samplerCube Texture;

layout(location = 0) out vec4 OutColor;
in vec3 vdspv_fsin0;
in vec4 vdspv_fsin1;

void main()
{
    OutColor = texture(Texture, vdspv_fsin0) * vdspv_fsin1;
}


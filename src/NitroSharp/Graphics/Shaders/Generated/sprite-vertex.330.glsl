#version 330

out vec2 vdspv_fsin0;
layout(location = 1) in vec2 TexCoords;
layout(location = 0) in vec2 Position;
layout(location = 2) in vec4 Color;

void main()
{
    vdspv_fsin0 = TexCoords;
    gl_Position = vec4(Position.x, Position.y, 0.0, 1.0);
}


cbuffer _86 : register(b0)
{
    row_major float4x4 _86_ViewProjection : packoffset(c0);
};

static float4 gl_Position;
static float3 fs_TexCoord;
static float3 vs_Position;
static float4 fs_Color;
static float4 vs_Color;
static float4 vs_Col1;
static float4 vs_Col2;
static float4 vs_Col3;
static float4 vs_Col4;

struct SPIRV_Cross_Input
{
    float3 vs_Position : TEXCOORD0;
    float4 vs_Color : TEXCOORD1;
    float4 vs_Col1 : TEXCOORD2;
    float4 vs_Col2 : TEXCOORD3;
    float4 vs_Col3 : TEXCOORD4;
    float4 vs_Col4 : TEXCOORD5;
};

struct SPIRV_Cross_Output
{
    float3 fs_TexCoord : TEXCOORD0;
    float4 fs_Color : TEXCOORD1;
    float4 gl_Position : SV_Position;
};

void vert_main()
{
    fs_TexCoord = float3(-vs_Position.x, vs_Position.y, vs_Position.z);
    fs_Color = vs_Color;
    float4x4 world = float4x4(float4(vs_Col1.x, vs_Col2.x, vs_Col3.x, vs_Col4.x), float4(vs_Col1.y, vs_Col2.y, vs_Col3.y, vs_Col4.y), float4(vs_Col1.z, vs_Col2.z, vs_Col3.z, vs_Col4.z), float4(vs_Col1.w, vs_Col2.w, vs_Col3.w, vs_Col4.w));
    gl_Position = mul(float4(vs_Position.x, vs_Position.y, vs_Position.z, 1.0f), mul(world, _86_ViewProjection));
}

SPIRV_Cross_Output main(SPIRV_Cross_Input stage_input)
{
    vs_Position = stage_input.vs_Position;
    vs_Color = stage_input.vs_Color;
    vs_Col1 = stage_input.vs_Col1;
    vs_Col2 = stage_input.vs_Col2;
    vs_Col3 = stage_input.vs_Col3;
    vs_Col4 = stage_input.vs_Col4;
    vert_main();
    SPIRV_Cross_Output stage_output;
    stage_output.gl_Position = gl_Position;
    stage_output.fs_TexCoord = fs_TexCoord;
    stage_output.fs_Color = fs_Color;
    return stage_output;
}

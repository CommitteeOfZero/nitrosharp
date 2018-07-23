TextureCube<float4> Texture : register(t0);
SamplerState Sampler : register(s0);

static float4 OutColor;
static float3 fs_TexCoord;
static float4 fs_Color;

struct SPIRV_Cross_Input
{
    float3 fs_TexCoord : TEXCOORD0;
    float4 fs_Color : TEXCOORD1;
};

struct SPIRV_Cross_Output
{
    float4 OutColor : SV_Target0;
};

void frag_main()
{
    OutColor = Texture.Sample(Sampler, fs_TexCoord) * fs_Color;
}

SPIRV_Cross_Output main(SPIRV_Cross_Input stage_input)
{
    fs_TexCoord = stage_input.fs_TexCoord;
    fs_Color = stage_input.fs_Color;
    frag_main();
    SPIRV_Cross_Output stage_output;
    stage_output.OutColor = OutColor;
    return stage_output;
}

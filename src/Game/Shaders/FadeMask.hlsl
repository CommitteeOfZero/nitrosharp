#define D2D_INPUT_COUNT 2
#define D2D_INPUT0_SIMPLE
#define D2D_INPUT1_SIMPLE

#include "d2d1effecthelpers.hlsli"

float opacity;
float feather;

D2D_PS_ENTRY(main)
{
    float4 color = D2DGetInput(0);
    float mask = D2DGetInput(1).r;
	
	float alpha;
	if (opacity >= mask)
		alpha = 1.0;
		
	else if (opacity + feather >= mask)
		alpha = (feather + opacity - mask) / feather;
	else
		alpha = 0.0;

    return color * alpha;
}

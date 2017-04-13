// Copyright (c) Microsoft Corporation. All rights reserved.
//
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.
// Contains modifications by @SomeAnonDev.

#define D2D_INPUT_COUNT 2
#define D2D_INPUT0_SIMPLE
#define D2D_INPUT1_SIMPLE

#include "d2d1effecthelpers.hlsli"

float opacity;
float feather = 0.1;

D2D_PS_ENTRY(main)
{
    float4 color = D2DGetInput(0);
    float mask = D2DGetInput(1).r;

	// Adjust the range of the mask threshold so that 0 and 1 map all
    // the way to fully on or off, even in the presence of feathering.
    float adjustedMask = mask * (1 - feather) + feather / 2;

    float alpha = saturate((opacity - adjustedMask) / feather + 0.5);
    return color * alpha;
}

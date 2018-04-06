using System;

namespace FreeTypeBindings
{
	public enum GlyphBBoxMode : uint
	{
		/// <summary>Return unscaled font units.</summary>
		Unscaled = 0,

		/// <summary>Return unfitted 26.6 coordinates.</summary>
		Subpixels = 0,

		/// <summary>Return grid-fitted 26.6 coordinates.</summary>
		Gridfit = 1,

		/// <summary>Return coordinates in integer pixels.</summary>
		Truncate = 2,

		/// <summary>Return grid-fitted pixel coordinates.</summary>
		Pixels = 3
	}
}

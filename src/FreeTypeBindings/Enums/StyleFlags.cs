using System;

namespace FreeTypeBindings
{
    /// <summary>
    /// A list of bit flags to indicate the style of a given face.
    /// </summary>
    [Flags]
    public enum StyleFlags : long
    {
        /// <summary>
        /// The face style is italic or oblique.
        /// </summary>
        Italic = 1,
        /// <summary>
        /// The face is bold.
        /// </summary>
        Bold = 2
    }
}

﻿namespace FreeTypeBindings
{
    /// <summary>
    /// These values determine how two joining lines are rendered in a stroker.
    /// </summary>
    public enum StrokerLineJoin
    {
        /// <summary>
        /// Used to render rounded line joins. Circular arcs are used to join two lines smoothly.
        /// </summary>
        Round = 0,
        /// <summary>
        /// Used to render beveled line joins. The outer corner of the joined lines is filled by enclosing
        /// the triangular region of the corner with a straight line between the outer corners of each stroke.
        /// </summary>
        Bevel = 1,
        /// <summary>
        /// Used to render mitered line joins, with variable bevels if the miter limit is exceeded.
        /// The intersection of the strokes is clipped at a line perpendicular to the bisector of the angle
        /// between the strokes, at the distance from the intersection of the segments equal to the product
        /// of the miter limit value and the border radius. This prevents long spikes being created.
        /// FT_STROKER_LINEJOIN_MITER generates a mitered line join as used in XPS.
        /// </summary>
        Miter = 2,
        /// <summary>
        /// Used to render mitered line joins, with fixed bevels if the miter limit is exceeded.
        /// The outer edges of the strokes for the two segments are extended until they meet at an angle.
        /// If the segments meet at too sharp an angle (such that the miter would extend from the intersection
        /// of the segments a distance greater than the product of the miter limit value and the border radius),
        /// then a bevel join (see above) is used instead. This prevents long spikes being created.
        /// FT_STROKER_LINEJOIN_MITER_FIXED generates a miter line join as used in PostScript and PDF.
        /// </summary>
        MiterFixed = 3
    }
}

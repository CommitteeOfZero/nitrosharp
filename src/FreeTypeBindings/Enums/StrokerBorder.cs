namespace FreeTypeBindings
{
    /// <summary>
    /// These values are used to select a given stroke border in
    /// FT_Stroker_GetBorderCounts and FT_Stroker_ExportBorder.
    /// </summary>
    public enum StrokerBorder
    {
        /// <summary>
        /// Select the left border, relative to the drawing direction.
        /// </summary>
        Left = 0,
        /// <summary>
        /// Select the right border, relative to the drawing direction.
        /// </summary>
        Right
    }
}

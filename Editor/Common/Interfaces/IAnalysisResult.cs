namespace dev.limitex.avatar.compressor.editor
{
    /// <summary>
    /// Common interface for analysis results.
    /// </summary>
    public interface IAnalysisResult
    {
        /// <summary>
        /// Normalized score (0-1) representing optimization potential.
        /// Higher values typically mean more optimization can be applied.
        /// </summary>
        float Score { get; }

        /// <summary>
        /// Human-readable summary of the analysis result.
        /// </summary>
        string Summary { get; }
    }
}

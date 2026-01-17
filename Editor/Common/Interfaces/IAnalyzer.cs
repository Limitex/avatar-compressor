namespace dev.limitex.avatar.compressor.editor
{
    /// <summary>
    /// Generic analyzer interface for various optimization strategies.
    /// </summary>
    /// <typeparam name="TInput">Input data type</typeparam>
    /// <typeparam name="TResult">Result type implementing IAnalysisResult</typeparam>
    public interface IAnalyzer<in TInput, out TResult>
        where TResult : IAnalysisResult
    {
        /// <summary>
        /// Analyzes the input data and returns a result.
        /// </summary>
        /// <param name="input">Input data to analyze</param>
        /// <returns>Analysis result</returns>
        TResult Analyze(TInput input);
    }
}

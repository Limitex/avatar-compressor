using UnityEditor;

namespace dev.limitex.avatar.compressor.texture.editor
{
    /// <summary>
    /// Container for SerializedProperty references used in the compressor editor UI.
    /// </summary>
    public class CompressorPropertySet
    {
        public SerializedProperty Strategy { get; }
        public SerializedProperty FastWeight { get; }
        public SerializedProperty HighAccuracyWeight { get; }
        public SerializedProperty PerceptualWeight { get; }
        public SerializedProperty HighComplexityThreshold { get; }
        public SerializedProperty LowComplexityThreshold { get; }
        public SerializedProperty MinDivisor { get; }
        public SerializedProperty MaxDivisor { get; }
        public SerializedProperty MaxResolution { get; }
        public SerializedProperty MinResolution { get; }
        public SerializedProperty ForcePowerOfTwo { get; }
        public SerializedProperty MinSourceSize { get; }
        public SerializedProperty SkipIfSmallerThan { get; }
        public SerializedProperty TargetPlatform { get; }
        public SerializedProperty UseHighQualityFormatForHighComplexity { get; }

        public CompressorPropertySet(SerializedObject serializedObject)
        {
            Strategy = serializedObject.FindProperty("Strategy");
            FastWeight = serializedObject.FindProperty("FastWeight");
            HighAccuracyWeight = serializedObject.FindProperty("HighAccuracyWeight");
            PerceptualWeight = serializedObject.FindProperty("PerceptualWeight");
            HighComplexityThreshold = serializedObject.FindProperty("HighComplexityThreshold");
            LowComplexityThreshold = serializedObject.FindProperty("LowComplexityThreshold");
            MinDivisor = serializedObject.FindProperty("MinDivisor");
            MaxDivisor = serializedObject.FindProperty("MaxDivisor");
            MaxResolution = serializedObject.FindProperty("MaxResolution");
            MinResolution = serializedObject.FindProperty("MinResolution");
            ForcePowerOfTwo = serializedObject.FindProperty("ForcePowerOfTwo");
            MinSourceSize = serializedObject.FindProperty("MinSourceSize");
            SkipIfSmallerThan = serializedObject.FindProperty("SkipIfSmallerThan");
            TargetPlatform = serializedObject.FindProperty("TargetPlatform");
            UseHighQualityFormatForHighComplexity = serializedObject.FindProperty("UseHighQualityFormatForHighComplexity");
        }
    }
}

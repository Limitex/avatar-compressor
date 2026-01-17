using dev.limitex.avatar.compressor;
using dev.limitex.avatar.compressor.editor.texture;
using dev.limitex.avatar.compressor.editor.texture.ui;
using NUnit.Framework;
using UnityEngine;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class TexturePreviewDataTests
    {
        #region Default Value Tests

        [Test]
        public void TexturePreviewData_DefaultValues_TextureNull()
        {
            var data = new TexturePreviewData();

            Assert.That(data.Texture, Is.Null);
        }

        [Test]
        public void TexturePreviewData_DefaultValues_GuidNull()
        {
            var data = new TexturePreviewData();

            Assert.That(data.Guid, Is.Null);
        }

        [Test]
        public void TexturePreviewData_DefaultValues_ComplexityZero()
        {
            var data = new TexturePreviewData();

            Assert.That(data.Complexity, Is.EqualTo(0f));
        }

        [Test]
        public void TexturePreviewData_DefaultValues_RecommendedDivisorZero()
        {
            var data = new TexturePreviewData();

            Assert.That(data.RecommendedDivisor, Is.EqualTo(0));
        }

        [Test]
        public void TexturePreviewData_DefaultValues_SizesZero()
        {
            var data = new TexturePreviewData();

            Assert.That(data.OriginalSize, Is.EqualTo(Vector2Int.zero));
            Assert.That(data.RecommendedSize, Is.EqualTo(Vector2Int.zero));
        }

        [Test]
        public void TexturePreviewData_DefaultValues_MemoryZero()
        {
            var data = new TexturePreviewData();

            Assert.That(data.OriginalMemory, Is.EqualTo(0));
            Assert.That(data.EstimatedMemory, Is.EqualTo(0));
        }

        [Test]
        public void TexturePreviewData_DefaultValues_FlagsAreFalse()
        {
            var data = new TexturePreviewData();

            Assert.That(data.IsProcessed, Is.False);
            Assert.That(data.IsNormalMap, Is.False);
            Assert.That(data.HasAlpha, Is.False);
            Assert.That(data.IsFrozen, Is.False);
        }

        [Test]
        public void TexturePreviewData_DefaultValues_SkipReasonNone()
        {
            var data = new TexturePreviewData();

            Assert.That(data.SkipReason, Is.EqualTo(SkipReason.None));
        }

        [Test]
        public void TexturePreviewData_DefaultValues_PredictedFormatNull()
        {
            var data = new TexturePreviewData();

            Assert.That(data.PredictedFormat, Is.Null);
        }

        [Test]
        public void TexturePreviewData_DefaultValues_FrozenSettingsNull()
        {
            var data = new TexturePreviewData();

            Assert.That(data.FrozenSettings, Is.Null);
        }

        #endregion

        #region Property Setter Tests

        [Test]
        public void TexturePreviewData_SetGuid_Works()
        {
            var data = new TexturePreviewData { Guid = "test-guid-12345" };

            Assert.That(data.Guid, Is.EqualTo("test-guid-12345"));
        }

        [Test]
        public void TexturePreviewData_SetComplexity_Works()
        {
            var data = new TexturePreviewData { Complexity = 0.75f };

            Assert.That(data.Complexity, Is.EqualTo(0.75f));
        }

        [Test]
        public void TexturePreviewData_SetRecommendedDivisor_Works()
        {
            var data = new TexturePreviewData { RecommendedDivisor = 4 };

            Assert.That(data.RecommendedDivisor, Is.EqualTo(4));
        }

        [Test]
        public void TexturePreviewData_SetOriginalSize_Works()
        {
            var size = new Vector2Int(1024, 512);
            var data = new TexturePreviewData { OriginalSize = size };

            Assert.That(data.OriginalSize, Is.EqualTo(size));
        }

        [Test]
        public void TexturePreviewData_SetRecommendedSize_Works()
        {
            var size = new Vector2Int(256, 128);
            var data = new TexturePreviewData { RecommendedSize = size };

            Assert.That(data.RecommendedSize, Is.EqualTo(size));
        }

        [Test]
        public void TexturePreviewData_SetTextureType_Works()
        {
            var data = new TexturePreviewData { TextureType = "Main" };

            Assert.That(data.TextureType, Is.EqualTo("Main"));
        }

        [Test]
        public void TexturePreviewData_SetIsProcessed_Works()
        {
            var data = new TexturePreviewData { IsProcessed = true };

            Assert.That(data.IsProcessed, Is.True);
        }

        [Test]
        public void TexturePreviewData_SetSkipReason_Works()
        {
            var data = new TexturePreviewData { SkipReason = SkipReason.TooSmall };

            Assert.That(data.SkipReason, Is.EqualTo(SkipReason.TooSmall));
        }

        [Test]
        public void TexturePreviewData_SetMemoryValues_Works()
        {
            var data = new TexturePreviewData
            {
                OriginalMemory = 1048576,
                EstimatedMemory = 262144,
            };

            Assert.That(data.OriginalMemory, Is.EqualTo(1048576));
            Assert.That(data.EstimatedMemory, Is.EqualTo(262144));
        }

        [Test]
        public void TexturePreviewData_SetIsNormalMap_Works()
        {
            var data = new TexturePreviewData { IsNormalMap = true };

            Assert.That(data.IsNormalMap, Is.True);
        }

        [Test]
        public void TexturePreviewData_SetPredictedFormat_Works()
        {
            var data = new TexturePreviewData { PredictedFormat = TextureFormat.BC7 };

            Assert.That(data.PredictedFormat, Is.EqualTo(TextureFormat.BC7));
        }

        [Test]
        public void TexturePreviewData_SetHasAlpha_Works()
        {
            var data = new TexturePreviewData { HasAlpha = true };

            Assert.That(data.HasAlpha, Is.True);
        }

        [Test]
        public void TexturePreviewData_SetIsFrozen_Works()
        {
            var data = new TexturePreviewData { IsFrozen = true };

            Assert.That(data.IsFrozen, Is.True);
        }

        [Test]
        public void TexturePreviewData_SetFrozenSettings_Works()
        {
            var settings = new FrozenTextureSettings("guid", 2, FrozenTextureFormat.Auto, false);
            var data = new TexturePreviewData { FrozenSettings = settings };

            Assert.That(data.FrozenSettings, Is.EqualTo(settings));
        }

        #endregion

        #region Complete Initialization Tests

        [Test]
        public void TexturePreviewData_CompleteInitialization_AllFieldsSet()
        {
            var frozenSettings = new FrozenTextureSettings(
                "test-guid",
                2,
                FrozenTextureFormat.Auto,
                false
            );

            var data = new TexturePreviewData
            {
                Guid = "test-guid",
                Complexity = 0.65f,
                RecommendedDivisor = 2,
                OriginalSize = new Vector2Int(1024, 1024),
                RecommendedSize = new Vector2Int(512, 512),
                TextureType = "Main",
                IsProcessed = true,
                SkipReason = SkipReason.None,
                OriginalMemory = 1048576,
                EstimatedMemory = 262144,
                IsNormalMap = false,
                PredictedFormat = TextureFormat.BC7,
                HasAlpha = true,
                IsFrozen = true,
                FrozenSettings = frozenSettings,
            };

            Assert.That(data.Guid, Is.EqualTo("test-guid"));
            Assert.That(data.Complexity, Is.EqualTo(0.65f));
            Assert.That(data.RecommendedDivisor, Is.EqualTo(2));
            Assert.That(data.OriginalSize.x, Is.EqualTo(1024));
            Assert.That(data.RecommendedSize.x, Is.EqualTo(512));
            Assert.That(data.TextureType, Is.EqualTo("Main"));
            Assert.That(data.IsProcessed, Is.True);
            Assert.That(data.SkipReason, Is.EqualTo(SkipReason.None));
            Assert.That(data.OriginalMemory, Is.EqualTo(1048576));
            Assert.That(data.EstimatedMemory, Is.EqualTo(262144));
            Assert.That(data.IsNormalMap, Is.False);
            Assert.That(data.PredictedFormat, Is.EqualTo(TextureFormat.BC7));
            Assert.That(data.HasAlpha, Is.True);
            Assert.That(data.IsFrozen, Is.True);
            Assert.That(data.FrozenSettings, Is.EqualTo(frozenSettings));
        }

        #endregion

        #region Memory Reduction Scenario Tests

        [Test]
        public void TexturePreviewData_MemoryReduction_CanCalculateSavings()
        {
            var data = new TexturePreviewData
            {
                OriginalMemory = 1048576, // 1 MB
                EstimatedMemory = 262144, // 256 KB
            };

            long savings = data.OriginalMemory - data.EstimatedMemory;
            float reductionPercent = (float)savings / data.OriginalMemory * 100f;

            Assert.That(savings, Is.EqualTo(786432)); // 768 KB saved
            Assert.That(reductionPercent, Is.EqualTo(75f));
        }

        #endregion

        #region Class Behavior Tests

        [Test]
        public void TexturePreviewData_IsClass_NotStruct()
        {
            var data = new TexturePreviewData();

            Assert.That(data.GetType().IsClass, Is.True);
        }

        [Test]
        public void TexturePreviewData_ReferenceAssignment_SharesSameInstance()
        {
            var original = new TexturePreviewData { Guid = "original" };
            var reference = original;

            reference.Guid = "modified";

            Assert.That(original.Guid, Is.EqualTo("modified"));
        }

        #endregion
    }
}

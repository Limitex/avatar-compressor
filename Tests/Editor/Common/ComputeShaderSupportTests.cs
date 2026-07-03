using dev.limitex.avatar.compressor.editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class ComputeShaderSupportTests
    {
        private const string ResizeShaderPath =
            "Packages/dev.limitex.avatar-compressor/"
            + "Editor/TextureCompressor/Resize/Shaders/AreaAverageResize.compute";

        private static void RequireShaderAsset()
        {
            if (AssetDatabase.LoadAssetAtPath<ComputeShader>(ResizeShaderPath) == null)
            {
                Assert.Ignore("Compute shader asset not found (not running inside Unity package)");
            }
        }

        [Test]
        public void TryLoadCompiledShader_MissingAsset_ReturnsFalseAndNullShader()
        {
            bool result = ComputeShaderSupport.TryLoadCompiledShader(
                "Packages/dev.limitex.avatar-compressor/DoesNotExist.compute",
                out var shader
            );

            Assert.IsFalse(result);
            Assert.IsNull(shader);
        }

        [Test]
        public void TryLoadCompiledShader_ExistingAssetWithKnownKernels_ReturnsTrue()
        {
            RequireShaderAsset();

            bool result = ComputeShaderSupport.TryLoadCompiledShader(
                ResizeShaderPath,
                out var shader,
                "AreaAverageHorizontal",
                "AreaAverageVertical"
            );

            Assert.IsTrue(result);
            Assert.IsNotNull(shader);
        }

        [Test]
        public void TryLoadCompiledShader_UnknownKernel_ReturnsFalseAndNullShader()
        {
            RequireShaderAsset();

            bool result = ComputeShaderSupport.TryLoadCompiledShader(
                ResizeShaderPath,
                out var shader,
                "NoSuchKernel"
            );

            Assert.IsFalse(result);
            Assert.IsNull(shader);
        }

        [Test]
        public void TryLoadCompiledShader_NoKernels_ChecksOnlyAssetPresence()
        {
            RequireShaderAsset();

            bool result = ComputeShaderSupport.TryLoadCompiledShader(
                ResizeShaderPath,
                out var shader
            );

            Assert.IsTrue(result);
            Assert.IsNotNull(shader);
        }
    }
}

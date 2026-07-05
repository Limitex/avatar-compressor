using dev.limitex.avatar.compressor.editor;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace dev.limitex.avatar.compressor.tests
{
    /// <summary>
    /// Shared Assert.Ignore guard for tests that require trustworthy GPU
    /// compute results.
    /// </summary>
    internal static class GpuTestGuard
    {
        /// <summary>
        /// Skips the running test unless real GPU hardware with compute
        /// shader support is available. On top of the production denylist,
        /// bare "Mesa" is matched too: that can skip real GPUs, but
        /// over-skipping is safe for parity tests.
        /// </summary>
        public static void RequireRealGpu()
        {
            if (!SystemInfo.supportsComputeShaders)
            {
                Assert.Ignore("Compute shaders not supported on this platform");
            }

            var deviceName = SystemInfo.graphicsDeviceName ?? "";
            bool bareMesa =
                SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLCore
                && deviceName.Contains("Mesa");
            if (ComputeShaderSupport.IsUnreliableComputeRenderer() || bareMesa)
            {
                Assert.Ignore(
                    $"Software renderer detected ({deviceName}); requires real GPU hardware"
                );
            }
        }
    }
}

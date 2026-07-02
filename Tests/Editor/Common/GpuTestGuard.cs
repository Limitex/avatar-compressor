using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace dev.limitex.avatar.compressor.tests
{
    /// <summary>
    /// Shared Assert.Ignore guard for tests that require trustworthy GPU
    /// compute results.
    /// </summary>
    public static class GpuTestGuard
    {
        /// <summary>
        /// Skips the running test unless real GPU hardware with compute
        /// shader support is available. Unlike the production capability
        /// check this also matches bare "Mesa": over-skipping is safe for
        /// parity tests.
        /// </summary>
        public static void RequireRealGpu()
        {
            if (!SystemInfo.supportsComputeShaders)
            {
                Assert.Ignore("Compute shaders not supported on this platform");
            }

            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLCore)
            {
                var deviceName = SystemInfo.graphicsDeviceName ?? "";
                if (
                    deviceName.Contains("llvmpipe")
                    || deviceName.Contains("softpipe")
                    || deviceName.Contains("SwiftShader")
                    || deviceName.Contains("Mesa")
                )
                {
                    Assert.Ignore(
                        $"Software renderer detected ({deviceName}); requires real GPU hardware"
                    );
                }
            }
        }
    }
}

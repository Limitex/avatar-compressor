using NUnit.Framework;
using UnityEngine;
using dev.limitex.avatar.compressor.editor.ui;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class MemoryCalculatorTests
    {
        #region GetBitsPerPixel Tests

        [Test]
        public void GetBitsPerPixel_DXT1_Returns4()
        {
            var result = MemoryCalculator.GetBitsPerPixel(TextureFormat.DXT1);

            Assert.That(result, Is.EqualTo(4f));
        }

        [Test]
        public void GetBitsPerPixel_DXT1Crunched_Returns4()
        {
            var result = MemoryCalculator.GetBitsPerPixel(TextureFormat.DXT1Crunched);

            Assert.That(result, Is.EqualTo(4f));
        }

        [Test]
        public void GetBitsPerPixel_DXT5_Returns8()
        {
            var result = MemoryCalculator.GetBitsPerPixel(TextureFormat.DXT5);

            Assert.That(result, Is.EqualTo(8f));
        }

        [Test]
        public void GetBitsPerPixel_BC5_Returns8()
        {
            var result = MemoryCalculator.GetBitsPerPixel(TextureFormat.BC5);

            Assert.That(result, Is.EqualTo(8f));
        }

        [Test]
        public void GetBitsPerPixel_BC7_Returns8()
        {
            var result = MemoryCalculator.GetBitsPerPixel(TextureFormat.BC7);

            Assert.That(result, Is.EqualTo(8f));
        }

        [Test]
        public void GetBitsPerPixel_ASTC4x4_Returns8()
        {
            var result = MemoryCalculator.GetBitsPerPixel(TextureFormat.ASTC_4x4);

            Assert.That(result, Is.EqualTo(8f));
        }

        [Test]
        public void GetBitsPerPixel_ASTC6x6_ReturnsCorrectValue()
        {
            var result = MemoryCalculator.GetBitsPerPixel(TextureFormat.ASTC_6x6);

            Assert.That(result, Is.EqualTo(3.56f).Within(0.01f));
        }

        [Test]
        public void GetBitsPerPixel_ASTC8x8_Returns2()
        {
            var result = MemoryCalculator.GetBitsPerPixel(TextureFormat.ASTC_8x8);

            Assert.That(result, Is.EqualTo(2f));
        }

        [Test]
        public void GetBitsPerPixel_RGBA32_Returns32()
        {
            var result = MemoryCalculator.GetBitsPerPixel(TextureFormat.RGBA32);

            Assert.That(result, Is.EqualTo(32f));
        }

        [Test]
        public void GetBitsPerPixel_RGB24_Returns24()
        {
            var result = MemoryCalculator.GetBitsPerPixel(TextureFormat.RGB24);

            Assert.That(result, Is.EqualTo(24f));
        }

        [Test]
        public void GetBitsPerPixel_UnknownFormat_ReturnsDefault32()
        {
            // Use a format not explicitly handled
            var result = MemoryCalculator.GetBitsPerPixel(TextureFormat.R8);

            Assert.That(result, Is.EqualTo(32f));
        }

        #endregion

        #region CalculateCompressedMemory Tests

        [Test]
        public void CalculateCompressedMemory_SingleMipmap_CalculatesCorrectly()
        {
            // 1024x1024 texture, DXT1 (4 bpp), 1 mipmap
            // Expected: (1024 * 1024) * 4 / 8 = 524288 bytes
            var result = MemoryCalculator.CalculateCompressedMemory(1024, 1024, TextureFormat.DXT1, 1);

            Assert.That(result, Is.EqualTo(524288));
        }

        [Test]
        public void CalculateCompressedMemory_MultipleMipmaps_IncludesAllLevels()
        {
            // 256x256 texture, DXT1 (4 bpp), multiple mipmaps
            // Mip 0: 256x256 = 32768 bytes
            // Mip 1: 128x128 = 8192 bytes
            // Mip 2: 64x64 = 2048 bytes
            // Total should be greater than single mipmap
            var singleMip = MemoryCalculator.CalculateCompressedMemory(256, 256, TextureFormat.DXT1, 1);
            var multiMip = MemoryCalculator.CalculateCompressedMemory(256, 256, TextureFormat.DXT1, 3);

            Assert.That(multiMip, Is.GreaterThan(singleMip));
        }

        [Test]
        public void CalculateCompressedMemory_ZeroMipmaps_ReturnsZero()
        {
            var result = MemoryCalculator.CalculateCompressedMemory(1024, 1024, TextureFormat.DXT1, 0);

            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        public void CalculateCompressedMemory_SmallTexture_CalculatesCorrectly()
        {
            // 64x64 texture, RGBA32 (32 bpp), 1 mipmap
            // Expected: (64 * 64) * 32 / 8 = 16384 bytes
            var result = MemoryCalculator.CalculateCompressedMemory(64, 64, TextureFormat.RGBA32, 1);

            Assert.That(result, Is.EqualTo(16384));
        }

        [Test]
        public void CalculateCompressedMemory_HigherBpp_UsesMoreMemory()
        {
            // Same size, different formats
            var dxt1Memory = MemoryCalculator.CalculateCompressedMemory(512, 512, TextureFormat.DXT1, 1);
            var bc7Memory = MemoryCalculator.CalculateCompressedMemory(512, 512, TextureFormat.BC7, 1);

            Assert.That(bc7Memory, Is.GreaterThan(dxt1Memory));
        }

        [Test]
        public void CalculateCompressedMemory_LargerTexture_UsesMoreMemory()
        {
            var smallMemory = MemoryCalculator.CalculateCompressedMemory(256, 256, TextureFormat.DXT1, 1);
            var largeMemory = MemoryCalculator.CalculateCompressedMemory(512, 512, TextureFormat.DXT1, 1);

            Assert.That(largeMemory, Is.EqualTo(smallMemory * 4));
        }

        [Test]
        public void CalculateCompressedMemory_NonSquareTexture_CalculatesCorrectly()
        {
            // 1024x512 texture, DXT1 (4 bpp), 1 mipmap
            // Expected: (1024 * 512) * 4 / 8 = 262144 bytes
            var result = MemoryCalculator.CalculateCompressedMemory(1024, 512, TextureFormat.DXT1, 1);

            Assert.That(result, Is.EqualTo(262144));
        }

        #endregion

        #region FormatBytes Tests

        [Test]
        public void FormatBytes_Bytes_ReturnsB()
        {
            var result = MemoryCalculator.FormatBytes(512);

            Assert.That(result, Is.EqualTo("512 B"));
        }

        [Test]
        public void FormatBytes_Kilobytes_ReturnsKB()
        {
            var result = MemoryCalculator.FormatBytes(2048);

            Assert.That(result, Does.Contain("KB"));
            Assert.That(result, Does.Contain("2"));
        }

        [Test]
        public void FormatBytes_Megabytes_ReturnsMB()
        {
            var result = MemoryCalculator.FormatBytes(2 * 1024 * 1024);

            Assert.That(result, Does.Contain("MB"));
            Assert.That(result, Does.Contain("2"));
        }

        [Test]
        public void FormatBytes_Zero_ReturnsZeroB()
        {
            var result = MemoryCalculator.FormatBytes(0);

            Assert.That(result, Is.EqualTo("0 B"));
        }

        [Test]
        public void FormatBytes_JustUnderKB_ReturnsB()
        {
            var result = MemoryCalculator.FormatBytes(1023);

            Assert.That(result, Does.Contain("B"));
            Assert.That(result, Does.Not.Contain("KB"));
        }

        [Test]
        public void FormatBytes_ExactlyOneKB_ReturnsKB()
        {
            var result = MemoryCalculator.FormatBytes(1024);

            Assert.That(result, Does.Contain("KB"));
        }

        [Test]
        public void FormatBytes_ExactlyOneMB_ReturnsMB()
        {
            var result = MemoryCalculator.FormatBytes(1024 * 1024);

            Assert.That(result, Does.Contain("MB"));
        }

        [Test]
        public void FormatBytes_LargeValue_FormatsCorrectly()
        {
            // 100 MB
            var result = MemoryCalculator.FormatBytes(100 * 1024 * 1024);

            Assert.That(result, Does.Contain("100"));
            Assert.That(result, Does.Contain("MB"));
        }

        #endregion
    }
}

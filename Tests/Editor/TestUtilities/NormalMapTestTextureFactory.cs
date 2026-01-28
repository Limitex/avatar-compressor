using UnityEngine;

namespace dev.limitex.avatar.compressor.tests
{
    /// <summary>
    /// Factory class for generating test normal map textures.
    /// Used for testing the preprocessing pipeline.
    /// </summary>
    public static class NormalMapTestTextureFactory
    {
        public delegate Color PixelGenerator(int x, int y, int width, int height);

        #region Tangent Space Normal Maps (Positive Z)

        /// <summary>
        /// Creates a flat normal map (all normals pointing straight up: 0, 0, 1).
        /// </summary>
        public static Texture2D CreateFlatNormal(int size, bool linear = true)
        {
            return CreateNormalMap(size, GenerateFlatNormal, linear);
        }

        /// <summary>
        /// Creates a sphere normal map (hemisphere bulging out).
        /// </summary>
        public static Texture2D CreateSphereNormal(int size, bool linear = true)
        {
            return CreateNormalMap(size, GenerateSphereNormal, linear);
        }

        /// <summary>
        /// Creates a gradient normal map (tilting in X direction).
        /// </summary>
        public static Texture2D CreateGradientNormal(int size, bool linear = true)
        {
            return CreateNormalMap(size, GenerateGradientNormal, linear);
        }

        /// <summary>
        /// Creates a high-frequency sine wave normal pattern.
        /// </summary>
        public static Texture2D CreateHighFrequencyNormal(int size, bool linear = true)
        {
            return CreateNormalMap(size, GenerateHighFrequencyNormal, linear);
        }

        /// <summary>
        /// Creates a checkerboard normal pattern with alternating tilt directions.
        /// </summary>
        public static Texture2D CreateCheckerNormal(int size, bool linear = true)
        {
            return CreateNormalMap(size, GenerateCheckerNormal, linear);
        }

        /// <summary>
        /// Creates normals at extreme angles (nearly parallel to surface).
        /// </summary>
        public static Texture2D CreateExtremeAngleNormal(int size, bool linear = true)
        {
            return CreateNormalMap(size, GenerateExtremeAngleNormal, linear);
        }

        #endregion

        #region Object Space Normal Maps (Can have Negative Z)

        /// <summary>
        /// Creates an object space normal map with negative Z values (back-facing normals).
        /// Left half: negative Z, Right half: positive Z.
        /// </summary>
        public static Texture2D CreateNegativeZNormal(int size, bool linear = true)
        {
            return CreateNormalMap(size, GenerateNegativeZNormal, linear);
        }

        /// <summary>
        /// Creates an object space normal map with Z gradient from -1 to +1.
        /// </summary>
        public static Texture2D CreateMixedZNormal(int size, bool linear = true)
        {
            return CreateNormalMap(size, GenerateMixedZNormal, linear);
        }

        /// <summary>
        /// Creates a full sphere normal map (front and back faces, object space).
        /// </summary>
        public static Texture2D CreateFullSphereNormal(int size, bool linear = true)
        {
            return CreateNormalMap(size, GenerateFullSphereNormal, linear);
        }

        #endregion

        #region Edge Case Normal Maps

        /// <summary>
        /// Creates a normal map with potentially degenerate vectors for edge case testing.
        /// </summary>
        public static Texture2D CreateDegenerateNormal(int size, bool linear = true)
        {
            return CreateNormalMap(size, GenerateDegenerateNormal, linear);
        }

        /// <summary>
        /// Creates a normal map with sharp 90-degree edges.
        /// </summary>
        public static Texture2D CreateSharpEdgeNormal(int size, bool linear = true)
        {
            return CreateNormalMap(size, GenerateSharpEdgeNormal, linear);
        }

        #endregion

        #region Generic Factory Method

        /// <summary>
        /// Creates a normal map texture using the specified pixel generator.
        /// </summary>
        /// <param name="size">Texture size (square)</param>
        /// <param name="generator">Pixel generation function</param>
        /// <param name="linear">Whether to use linear color space</param>
        /// <returns>Generated normal map texture</returns>
        public static Texture2D CreateNormalMap(
            int size,
            PixelGenerator generator,
            bool linear = true
        )
        {
            var texture = new Texture2D(
                size,
                size,
                TextureFormat.RGBA32,
                mipChain: false,
                linear: linear
            );

            var pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    pixels[y * size + x] = generator(x, y, size, size);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        #endregion

        #region Pixel Generators

        public static Color GenerateFlatNormal(int x, int y, int w, int h)
        {
            return EncodeNormal(0f, 0f, 1f);
        }

        public static Color GenerateSphereNormal(int x, int y, int w, int h)
        {
            float cx = x / (float)w - 0.5f;
            float cy = y / (float)h - 0.5f;
            float dist = Mathf.Sqrt(cx * cx + cy * cy);

            if (dist > 0.45f)
            {
                return EncodeNormal(0f, 0f, 1f);
            }

            float nx = cx * 2f;
            float ny = cy * 2f;
            float nz = Mathf.Sqrt(Mathf.Max(0, 1f - nx * nx - ny * ny));

            return EncodeNormalNormalized(nx, ny, nz);
        }

        public static Color GenerateGradientNormal(int x, int y, int w, int h)
        {
            float nx = (x / (float)w - 0.5f) * 1.4f;
            float ny = 0f;
            float nz = Mathf.Sqrt(Mathf.Max(0.01f, 1f - nx * nx - ny * ny));

            return EncodeNormalNormalized(nx, ny, nz);
        }

        public static Color GenerateHighFrequencyNormal(int x, int y, int w, int h)
        {
            float freq = 32f;
            float amplitude = 0.3f;

            float angle = (x + y) / (float)w * freq * Mathf.PI;
            float nx = Mathf.Sin(angle) * amplitude;
            float ny = Mathf.Cos(angle) * amplitude;
            float nz = Mathf.Sqrt(Mathf.Max(0.01f, 1f - nx * nx - ny * ny));

            return EncodeNormalNormalized(nx, ny, nz);
        }

        public static Color GenerateCheckerNormal(int x, int y, int w, int h)
        {
            int tileSize = w / 8;
            if (tileSize < 1)
                tileSize = 1;
            bool isWhite = ((x / tileSize) + (y / tileSize)) % 2 == 0;

            float tilt = 0.3f;
            float nx = isWhite ? tilt : -tilt;
            float ny = isWhite ? tilt : -tilt;
            float nz = Mathf.Sqrt(1f - nx * nx - ny * ny);

            return EncodeNormal(nx, ny, nz);
        }

        public static Color GenerateExtremeAngleNormal(int x, int y, int w, int h)
        {
            float cx = x / (float)w - 0.5f;
            float cy = y / (float)h - 0.5f;

            float angle = Mathf.Atan2(cy, cx);
            float extremeTilt = 0.95f;
            float nx = Mathf.Cos(angle) * extremeTilt;
            float ny = Mathf.Sin(angle) * extremeTilt;
            float nz = Mathf.Sqrt(Mathf.Max(0.01f, 1f - nx * nx - ny * ny));

            return EncodeNormalNormalized(nx, ny, nz);
        }

        public static Color GenerateNegativeZNormal(int x, int y, int w, int h)
        {
            bool isLeftHalf = x < w / 2;
            float nz = isLeftHalf ? -1f : 1f;

            return EncodeNormal(0f, 0f, nz);
        }

        public static Color GenerateMixedZNormal(int x, int y, int w, int h)
        {
            float t = x / (float)w;
            float nz = Mathf.Lerp(-1f, 1f, t);

            float nx = Mathf.Sin(y / (float)h * Mathf.PI * 4f) * 0.2f;
            float ny = 0f;

            return EncodeNormalNormalized(nx, ny, nz);
        }

        public static Color GenerateFullSphereNormal(int x, int y, int w, int h)
        {
            float cx = x / (float)w - 0.5f;
            float cy = y / (float)h - 0.5f;
            float dist = Mathf.Sqrt(cx * cx + cy * cy);

            if (dist > 0.45f)
            {
                return EncodeNormal(0f, 0f, -1f);
            }

            float nx = cx * 2f;
            float ny = cy * 2f;
            float nzSq = 1f - nx * nx - ny * ny;
            float nz = nzSq > 0 ? Mathf.Sqrt(nzSq) : 0f;

            if (cy > 0)
            {
                nz = -nz;
            }

            return EncodeNormalNormalized(nx, ny, nz);
        }

        public static Color GenerateDegenerateNormal(int x, int y, int w, int h)
        {
            int region = (x / (w / 4)) + (y / (h / 4)) * 4;

            switch (region % 4)
            {
                case 0:
                    return EncodeNormal(0f, 0f, 1f);
                case 1:
                    return EncodeNormal(0.001f, 0.001f, 1f);
                case 2:
                    return EncodeNormalNormalized(0.8f, 0.8f, 0.1f);
                default:
                    return EncodeNormal(0.01f, 0.01f, 0.99f);
            }
        }

        public static Color GenerateSharpEdgeNormal(int x, int y, int w, int h)
        {
            int gridSize = w / 4;
            if (gridSize < 1)
                gridSize = 1;
            int gx = x % gridSize;
            int gy = y % gridSize;

            int edgeThreshold = gridSize / 8;
            if (edgeThreshold < 1)
                edgeThreshold = 1;
            bool isHorizontalEdge = gy < edgeThreshold || gy > gridSize - edgeThreshold;
            bool isVerticalEdge = gx < edgeThreshold || gx > gridSize - edgeThreshold;

            if (isHorizontalEdge && !isVerticalEdge)
            {
                float sign = gy < gridSize / 2 ? 1f : -1f;
                return EncodeNormalNormalized(0f, 0.7f * sign, 0.7f);
            }
            else if (isVerticalEdge && !isHorizontalEdge)
            {
                float sign = gx < gridSize / 2 ? 1f : -1f;
                return EncodeNormalNormalized(0.7f * sign, 0f, 0.7f);
            }

            return EncodeNormal(0f, 0f, 1f);
        }

        #endregion

        #region Encoding Helpers

        /// <summary>
        /// Encodes a normal vector from [-1,1] to [0,1] color range.
        /// </summary>
        public static Color EncodeNormal(float x, float y, float z)
        {
            return new Color(x * 0.5f + 0.5f, y * 0.5f + 0.5f, z * 0.5f + 0.5f, 1f);
        }

        /// <summary>
        /// Normalizes and encodes a normal vector.
        /// </summary>
        public static Color EncodeNormalNormalized(float x, float y, float z)
        {
            float len = Mathf.Sqrt(x * x + y * y + z * z);
            if (len > 0.0001f)
            {
                x /= len;
                y /= len;
                z /= len;
            }
            else
            {
                x = 0f;
                y = 0f;
                z = 1f;
            }

            return EncodeNormal(x, y, z);
        }

        /// <summary>
        /// Decodes a color to a normal vector.
        /// </summary>
        public static Vector3 DecodeNormal(Color color)
        {
            return new Vector3(color.r * 2f - 1f, color.g * 2f - 1f, color.b * 2f - 1f);
        }

        #endregion
    }
}

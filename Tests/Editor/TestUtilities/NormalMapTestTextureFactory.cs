using dev.limitex.avatar.compressor.editor.texture;
using UnityEngine;

namespace dev.limitex.avatar.compressor.tests
{
    /// <summary>
    /// Factory for creating test normal map textures with known patterns.
    /// </summary>
    public static class NormalMapTestTextureFactory
    {
        /// <summary>
        /// Creates a flat normal map (all pixels pointing straight up: 0, 0, 1).
        /// Encoded as (128, 128, 255) in RGB.
        /// </summary>
        public static Texture2D CreateFlat(int size, bool linear = true)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, linear);
            var pixels = new Color32[size * size];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(128, 128, 255, 255);
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// Creates a sphere normal map where normals point outward from the center.
        /// </summary>
        public static Texture2D CreateSphere(int size, bool linear = true)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, linear);
            var pixels = new Color32[size * size];
            float center = size / 2f;
            float radius = size / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x - center) / radius;
                    float ny = (y - center) / radius;
                    float lenSq = nx * nx + ny * ny;

                    float nz;
                    if (lenSq <= 1f)
                    {
                        nz = Mathf.Sqrt(1f - lenSq);
                    }
                    else
                    {
                        float len = Mathf.Sqrt(lenSq);
                        nx /= len;
                        ny /= len;
                        nz = 0f;
                    }

                    pixels[y * size + x] = EncodeNormalRGB(nx, ny, nz);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// Creates a normal map with a gradient in the X direction.
        /// </summary>
        public static Texture2D CreateGradient(int size, bool linear = true)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, linear);
            var pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float t = (float)x / (size - 1);
                    float nx = Mathf.Lerp(-0.5f, 0.5f, t);
                    float nz = Mathf.Sqrt(1f - nx * nx);
                    pixels[y * size + x] = EncodeNormalRGB(nx, 0f, nz);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// Creates a high-frequency normal map with a checker pattern of alternating normals.
        /// </summary>
        public static Texture2D CreateChecker(int size, bool linear = true)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, linear);
            var pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool even = ((x + y) % 2) == 0;
                    float nx = even ? 0.3f : -0.3f;
                    float ny = even ? 0.2f : -0.2f;
                    float nz = Mathf.Sqrt(1f - nx * nx - ny * ny);
                    pixels[y * size + x] = EncodeNormalRGB(nx, ny, nz);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// Creates a normal map with extreme angle normals (nearly perpendicular to surface).
        /// </summary>
        public static Texture2D CreateExtremeAngle(int size, bool linear = true)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, linear);
            var pixels = new Color32[size * size];

            for (int i = 0; i < pixels.Length; i++)
            {
                // Near-horizontal normal (large X, small Z)
                float nx = 0.9f;
                float ny = 0.1f;
                float nz = Mathf.Sqrt(1f - nx * nx - ny * ny);
                pixels[i] = EncodeNormalRGB(nx, ny, nz);
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// Creates an object-space normal map with negative Z values (back-facing normals).
        /// </summary>
        public static Texture2D CreateNegativeZ(int size, bool linear = true)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, linear);
            var pixels = new Color32[size * size];

            for (int i = 0; i < pixels.Length; i++)
            {
                float nx = 0.2f;
                float ny = 0.3f;
                float nz = -Mathf.Sqrt(1f - nx * nx - ny * ny);
                pixels[i] = EncodeNormalRGB(nx, ny, nz);
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// Creates an object-space normal map with mixed positive and negative Z values.
        /// </summary>
        public static Texture2D CreateMixedZ(int size, bool linear = true)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, linear);
            var pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = ((float)x / size) * 0.4f - 0.2f;
                    float ny = ((float)y / size) * 0.4f - 0.2f;
                    float zMag = Mathf.Sqrt(1f - nx * nx - ny * ny);
                    // Alternate Z sign by row
                    float nz = (y % 2 == 0) ? zMag : -zMag;
                    pixels[y * size + x] = EncodeNormalRGB(nx, ny, nz);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// Creates a flat normal map in DXTnm AG channel layout (X in A, Y in G, R=255, B=255).
        /// </summary>
        public static Texture2D CreateFlatAG(int size, bool linear = true)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, linear);
            var pixels = new Color32[size * size];
            for (int i = 0; i < pixels.Length; i++)
            {
                // Flat normal in AG: X=0 -> A=128, Y=0 -> G=128, R=255, B=255
                pixels[i] = new Color32(255, 128, 255, 128);
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// Creates a sphere normal map in DXTnm AG channel layout.
        /// </summary>
        public static Texture2D CreateSphereAG(int size, bool linear = true)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, linear);
            var pixels = new Color32[size * size];
            float center = size / 2f;
            float radius = size / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x - center) / radius;
                    float ny = (y - center) / radius;
                    float lenSq = nx * nx + ny * ny;

                    if (lenSq > 1f)
                    {
                        float len = Mathf.Sqrt(lenSq);
                        nx /= len;
                        ny /= len;
                    }

                    byte encodedX = (byte)Mathf.Clamp((nx * 0.5f + 0.5f) * 255f, 0f, 255f);
                    byte encodedY = (byte)Mathf.Clamp((ny * 0.5f + 0.5f) * 255f, 0f, 255f);
                    // AG layout: X in A, Y in G, R=255, B=255
                    pixels[y * size + x] = new Color32(255, encodedY, 255, encodedX);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// Creates a sphere normal map in AG channel layout with custom R and B constant values.
        /// Unlike standard DXTnm (R=255, B=255), this allows testing non-standard R/B values
        /// to exercise detection Branch 2 (AG without R/B constants).
        /// </summary>
        public static Texture2D CreateSphereAGWithCustomRB(
            int size,
            byte rValue,
            byte bValue,
            bool linear = true
        )
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, linear);
            var pixels = new Color32[size * size];
            float center = size / 2f;
            float radius = size / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x - center) / radius;
                    float ny = (y - center) / radius;
                    float lenSq = nx * nx + ny * ny;

                    if (lenSq > 1f)
                    {
                        float len = Mathf.Sqrt(lenSq);
                        nx /= len;
                        ny /= len;
                    }

                    byte encodedX = (byte)Mathf.Clamp((nx * 0.5f + 0.5f) * 255f, 0f, 255f);
                    byte encodedY = (byte)Mathf.Clamp((ny * 0.5f + 0.5f) * 255f, 0f, 255f);
                    // AG layout: X in A, Y in G, R and B set to custom constants
                    pixels[y * size + x] = new Color32(rValue, encodedY, bValue, encodedX);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// Creates a flat normal map in RG channel layout (X in R, Y in G).
        /// </summary>
        public static Texture2D CreateFlatRG(int size, bool linear = true)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, linear);
            var pixels = new Color32[size * size];
            for (int i = 0; i < pixels.Length; i++)
            {
                // Flat normal in RG: X=0 -> R=128, Y=0 -> G=128, B=anything, A=255
                pixels[i] = new Color32(128, 128, 0, 255);
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// Creates a normal map with near-zero XY components.
        /// Encoded as (128, 128, 128) which decodes to approximately (0.004, 0.004, 0.004).
        /// Note: 8-bit encoding cannot represent exact zero (0.5*255 = 127.5).
        /// During preprocessing, Z is recalculated from the unit sphere constraint
        /// sqrt(1 - x² - y²) ≈ 1.0, so the result is an approximately flat normal.
        /// </summary>
        public static Texture2D CreateDegenerate(int size, bool linear = true)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, linear);
            var pixels = new Color32[size * size];
            for (int i = 0; i < pixels.Length; i++)
            {
                // Near-zero XY: (128/255)*2-1 ≈ 0.004 per component
                pixels[i] = new Color32(128, 128, 128, 255);
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// Creates an RGB normal map with significant alpha (cutout mask).
        /// </summary>
        public static Texture2D CreateWithAlpha(int size, bool linear = true)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, linear);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Half transparent, half opaque
                    byte alpha = (byte)((x < size / 2) ? 128 : 255);
                    pixels[y * size + x] = new Color32(128, 128, 255, alpha);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// Creates a sphere normal map in RG channel layout (X in R, Y in G, A=255).
        /// </summary>
        public static Texture2D CreateSphereRG(int size, bool linear = true)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, linear);
            var pixels = new Color32[size * size];
            float center = size / 2f;
            float radius = size / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x - center) / radius;
                    float ny = (y - center) / radius;
                    float lenSq = nx * nx + ny * ny;

                    if (lenSq > 1f)
                    {
                        float len = Mathf.Sqrt(lenSq);
                        nx /= len;
                        ny /= len;
                    }

                    byte encodedX = (byte)Mathf.Clamp((nx * 0.5f + 0.5f) * 255f, 0f, 255f);
                    byte encodedY = (byte)Mathf.Clamp((ny * 0.5f + 0.5f) * 255f, 0f, 255f);
                    pixels[y * size + x] = new Color32(encodedX, encodedY, 0, 255);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// Creates a high-frequency normal map with a sine wave pattern.
        /// </summary>
        public static Texture2D CreateHighFrequency(int size, bool linear = true)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, linear);
            var pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float freq = 32f;
                    float amplitude = 0.3f;
                    float angle = (x + y) / (float)size * freq * Mathf.PI;
                    float nx = Mathf.Sin(angle) * amplitude;
                    float ny = Mathf.Cos(angle) * amplitude;
                    float nz = Mathf.Sqrt(Mathf.Max(0.01f, 1f - nx * nx - ny * ny));
                    float len = Mathf.Sqrt(nx * nx + ny * ny + nz * nz);
                    pixels[y * size + x] = EncodeNormalRGB(nx / len, ny / len, nz / len);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// Creates a normal map with sharp 90-degree edges.
        /// </summary>
        public static Texture2D CreateSharpEdge(int size, bool linear = true)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, linear);
            var pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int gridSize = size / 4;
                    if (gridSize < 1)
                        gridSize = 1;
                    int gx = x % gridSize;
                    int gy = y % gridSize;
                    int edgeThreshold = gridSize / 8;
                    if (edgeThreshold < 1)
                        edgeThreshold = 1;
                    bool isHEdge = gy < edgeThreshold || gy > gridSize - edgeThreshold;
                    bool isVEdge = gx < edgeThreshold || gx > gridSize - edgeThreshold;

                    float nx = 0f,
                        ny = 0f,
                        nz = 1f;
                    if (isHEdge && !isVEdge)
                    {
                        float sign = gy < gridSize / 2 ? 1f : -1f;
                        nx = 0f;
                        ny = 0.7f * sign;
                        nz = 0.7f;
                        float len = Mathf.Sqrt(nx * nx + ny * ny + nz * nz);
                        ny /= len;
                        nz /= len;
                    }
                    else if (isVEdge && !isHEdge)
                    {
                        float sign = gx < gridSize / 2 ? 1f : -1f;
                        nx = 0.7f * sign;
                        ny = 0f;
                        nz = 0.7f;
                        float len = Mathf.Sqrt(nx * nx + ny * ny + nz * nz);
                        nx /= len;
                        nz /= len;
                    }
                    pixels[y * size + x] = EncodeNormalRGB(nx, ny, nz);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// Creates a full sphere normal map (front and back faces, object space).
        /// </summary>
        public static Texture2D CreateFullSphere(int size, bool linear = true)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, linear);
            var pixels = new Color32[size * size];
            float center = size / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float cx = (x - center) / center;
                    float cy = (y - center) / center;
                    float dist = Mathf.Sqrt(cx * cx + cy * cy);

                    float nx,
                        ny,
                        nz;
                    if (dist > 0.9f)
                    {
                        nx = 0f;
                        ny = 0f;
                        nz = -1f;
                    }
                    else
                    {
                        nx = cx;
                        ny = cy;
                        float nzSq = 1f - nx * nx - ny * ny;
                        nz = nzSq > 0 ? Mathf.Sqrt(nzSq) : 0f;
                        if (cy > 0)
                            nz = -nz;
                        float len = Mathf.Sqrt(nx * nx + ny * ny + nz * nz);
                        if (len > AnalysisConstants.Epsilon)
                        {
                            nx /= len;
                            ny /= len;
                            nz /= len;
                        }
                    }
                    pixels[y * size + x] = EncodeNormalRGB(nx, ny, nz);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// Encodes a normalized normal vector to Color32 in RGB layout.
        /// </summary>
        public static Color32 EncodeNormal(float x, float y, float z)
        {
            return EncodeNormalRGB(x, y, z);
        }

        /// <summary>
        /// Decodes a Color32 from RGB layout to a normal vector.
        /// </summary>
        public static Vector3 DecodeNormal(Color32 pixel)
        {
            float x = (pixel.r / 255f) * 2f - 1f;
            float y = (pixel.g / 255f) * 2f - 1f;
            float z = (pixel.b / 255f) * 2f - 1f;
            return new Vector3(x, y, z);
        }

        private static Color32 EncodeNormalRGB(float x, float y, float z)
        {
            byte r = (byte)Mathf.Clamp((x * 0.5f + 0.5f) * 255f, 0f, 255f);
            byte g = (byte)Mathf.Clamp((y * 0.5f + 0.5f) * 255f, 0f, 255f);
            byte b = (byte)Mathf.Clamp((z * 0.5f + 0.5f) * 255f, 0f, 255f);
            return new Color32(r, g, b, 255);
        }
    }
}

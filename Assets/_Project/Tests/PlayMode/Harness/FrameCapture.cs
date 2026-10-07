using System;
using System.IO;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace MoonProject.Testing
{
    /// <summary>
    /// Renders a camera into an offscreen half-float target and reads it back as sRGB, for PlayMode tests and editor
    /// automation (turntables, scene shots). The target must be HDR: URP sizes a camera's intermediate buffers from
    /// its target texture, so an 8-bit target would clamp emission and bloom and captures could not show the game's
    /// warm glows as players see them. Runtime-safe (no UnityEditor). Allocates per call: never use it every frame.
    /// Needs a real graphics device, so batch runs that capture must not pass -nographics.
    /// </summary>
    public static class FrameCapture
    {
        /// <summary>MSAA samples used for captures; low-poly edges need it to read cleanly.</summary>
        public const int Msaa = 4;

        /// <summary>
        /// Renders <paramref name="camera"/> at the given size and returns a readable sRGB texture the caller owns
        /// (destroy it with <see cref="Object.DestroyImmediate(Object)"/>).
        /// </summary>
        public static Texture2D Render(Camera camera, int width, int height)
        {
            if (camera == null)
            {
                throw new ArgumentNullException(nameof(camera));
            }

            if (width <= 0 || height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), $"Capture size must be positive ({width}x{height}).");
            }

            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                throw new InvalidOperationException(
                    "No graphics device: captures cannot run with -nographics. Re-run without --nographics.");
            }

            var descriptor = new RenderTextureDescriptor(width, height, GraphicsFormat.R16G16B16A16_SFloat, 32)
            {
                msaaSamples = Msaa,
            };
            RenderTexture multisampled = RenderTexture.GetTemporary(descriptor);
            descriptor.msaaSamples = 1;
            descriptor.depthBufferBits = 0;
            RenderTexture resolved = RenderTexture.GetTemporary(descriptor);
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = multisampled;
                camera.Render();
                Graphics.Blit(multisampled, resolved);
                RenderTexture.active = resolved;
                var linear = new Texture2D(width, height, TextureFormat.RGBAHalf, false, true);
                try
                {
                    linear.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
                    return ToSrgb(linear);
                }
                finally
                {
                    Object.DestroyImmediate(linear);
                }
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(multisampled);
                RenderTexture.ReleaseTemporary(resolved);
            }
        }

        /// <summary>
        /// Encodes the linear (post-tonemapping) pixels of <paramref name="linear"/> into a new 8-bit sRGB texture, the
        /// way the display would show them; values past 1 clip as they do on screen.
        /// </summary>
        private static Texture2D ToSrgb(Texture2D linear)
        {
            Color[] pixels = linear.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                Color c = pixels[i];
                pixels[i] = new Color(Mathf.LinearToGammaSpace(Mathf.Clamp01(c.r)),
                    Mathf.LinearToGammaSpace(Mathf.Clamp01(c.g)), Mathf.LinearToGammaSpace(Mathf.Clamp01(c.b)),
                    Mathf.Clamp01(c.a));
            }

            var image = new Texture2D(linear.width, linear.height, TextureFormat.RGBA32, false, false);
            image.SetPixels(pixels);
            image.Apply(false);
            return image;
        }

        /// <summary>Renders <paramref name="camera"/> and writes a PNG to <paramref name="path"/> (folders are created).</summary>
        public static void SavePng(Camera camera, int width, int height, string path)
        {
            Texture2D image = Render(camera, width, height);
            try
            {
                WritePng(image, path);
            }
            finally
            {
                Object.DestroyImmediate(image);
            }
        }

        /// <summary>Encodes <paramref name="image"/> as PNG at <paramref name="path"/>, creating folders as needed.</summary>
        public static void WritePng(Texture2D image, string path)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }

            string directory = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(path, image.EncodeToPNG());
        }

        /// <summary>
        /// Mean sRGB luminance (0..1) of <paramref name="image"/>, sampled on a coarse grid. Lets tests assert that
        /// a capture is not black without comparing exact pixels.
        /// </summary>
        public static float MeanLuminance(Texture2D image)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }

            const int Samples = 32;
            float sum = 0f;
            for (int y = 0; y < Samples; y++)
            {
                for (int x = 0; x < Samples; x++)
                {
                    Color c = image.GetPixel(x * image.width / Samples, y * image.height / Samples);
                    sum += 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
                }
            }

            return sum / (Samples * Samples);
        }
    }
}

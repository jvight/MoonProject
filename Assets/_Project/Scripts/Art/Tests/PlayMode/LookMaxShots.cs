using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace MoonProject.Art.PlayModeTests
{
    /// <summary>
    /// Renders one capture camera into its own sRGB target, one render per player frame so temporal anti-aliasing and
    /// STP see a real frame sequence, and either saves the converged frame or times a run of frames. A timed frame is
    /// the camera's render plus a one-pixel read-back that waits for the GPU to finish it, so the figure covers the
    /// whole frame on this machine in the batch editor (no game camera or UI drawing beside it).
    /// </summary>
    public sealed class LookMaxShots : IDisposable
    {
        private const int JpegQuality = 95;

        private readonly Camera _camera;
        private readonly RenderTexture _target;
        private readonly Texture2D _frame;
        private readonly Texture2D _pixel;
        private readonly List<double> _milliseconds = new List<double>();

        public LookMaxShots(Camera camera, int width, int height)
        {
            _camera = camera;
            _target = new RenderTexture(width, height, GraphicsFormat.R8G8B8A8_SRGB, GraphicsFormat.D32_SFloat_S8_UInt);
            _target.Create();
            _camera.targetTexture = _target;
            _frame = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
            _pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false, false);
        }

        /// <summary>Median and 90th-percentile frame time of the last <see cref="Time"/> run, in milliseconds.</summary>
        public string TimingSummary
        {
            get
            {
                if (_milliseconds.Count == 0)
                {
                    return "not timed";
                }

                var sorted = new List<double>(_milliseconds);
                sorted.Sort();
                double median = sorted[sorted.Count / 2];
                double p90 = sorted[Mathf.Min(sorted.Count - 1, sorted.Count * 9 / 10)];
                return $"median {median:0.0} ms, p90 {p90:0.0} ms over {sorted.Count} frames at " +
                    $"{_target.width}x{_target.height}";
            }
        }

        /// <summary>The game camera's URP settings (post-processing, anti-aliasing); CopyFrom skips them.</summary>
        public static void CopyPipelineSettings(Camera from, Camera to)
        {
            UniversalAdditionalCameraData source = from.GetUniversalAdditionalCameraData();
            UniversalAdditionalCameraData target = to.GetUniversalAdditionalCameraData();
            target.renderPostProcessing = source.renderPostProcessing;
            target.antialiasing = source.antialiasing;
            target.antialiasingQuality = source.antialiasingQuality;
            target.renderShadows = source.renderShadows;
            target.dithering = source.dithering;
        }

        /// <summary>Renders <paramref name="frames"/> frames from where the camera stands, then saves the last one.</summary>
        public IEnumerator Shoot(int frames, string path)
        {
            for (int i = 0; i < frames; i++)
            {
                _camera.Render();
                yield return null;
            }

            Save(path);
        }

        /// <summary>Renders the current frame once and saves it (for a frame sequence, one call per frame).</summary>
        public void RenderAndSave(string path)
        {
            _camera.Render();
            Save(path);
        }

        /// <summary>Times <paramref name="frames"/> frames from where the camera stands.</summary>
        public IEnumerator Time(int frames)
        {
            _milliseconds.Clear();
            var clock = new Stopwatch();
            for (int i = 0; i < frames; i++)
            {
                clock.Restart();
                _camera.Render();
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = _target;
                _pixel.ReadPixels(new Rect(0, 0, 1, 1), 0, 0, false);
                RenderTexture.active = previous;
                clock.Stop();
                _milliseconds.Add(clock.Elapsed.TotalMilliseconds);
                yield return null;
            }
        }

        public void Dispose()
        {
            _camera.targetTexture = null;
            _target.Release();
            Object.Destroy(_target);
            Object.Destroy(_frame);
            Object.Destroy(_pixel);
        }

        private void Save(string path)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = _target;
            _frame.ReadPixels(new Rect(0, 0, _target.width, _target.height), 0, 0, false);
            _frame.Apply(false);
            RenderTexture.active = previous;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            File.WriteAllBytes(path, path.EndsWith(".jpg", StringComparison.Ordinal)
                ? _frame.EncodeToJPG(JpegQuality)
                : _frame.EncodeToPNG());
        }
    }
}

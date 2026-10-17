using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace MoonProject.UI
{
    /// <summary>
    /// Full-monitor CRT post-pass over the title screen: horizontal scanlines, a subtle rolling refresh band,
    /// edge vignette, and the barrel-curved dark CRT bezel (#0E0905) with a faint warm inner rim.
    /// </summary>
    internal sealed class TitleCrtOverlayPainter : StylePainter
    {
        private readonly TitleSettings _settings;
        private float _timeSeconds = 1.1f;
        private bool _standaloneFraming;

        public TitleCrtOverlayPainter(VisualElement element, TitleSettings settings)
            : base(element)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public void SetTime(float timeSeconds, bool standaloneFraming = false)
        {
            if (Mathf.Approximately(_timeSeconds, timeSeconds) && _standaloneFraming == standaloneFraming)
            {
                return;
            }

            _timeSeconds = timeSeconds;
            _standaloneFraming = standaloneFraming;
            Element.MarkDirtyRepaint();
        }

        protected override void ReadStyle(ICustomStyle style)
        {
        }

        protected override void Paint(MeshGenerationContext context)
        {
            Rect rect = Element.contentRect;
            float w = rect.width;
            float h = rect.height;
            if (w <= 4f || h <= 4f)
            {
                return;
            }

            Painter2D painter = context.painter2D;
            Vector2 refSize = _settings.StandaloneReferenceSize;
            float s = _standaloneFraming ? (h / refSize.y) : Mathf.Min(w / refSize.x, h / refSize.y);
            PaintScanlines(painter, w, h);
            PaintRefreshBand(painter, w, h, s);
            PaintVignetteAndBezel(painter, w, h, s);
        }

        private void PaintScanlines(Painter2D painter, float w, float h)
        {
            int count = Mathf.Max(30, _settings.CrtScanlineCount);
            float alpha = _settings.CrtScanlineAlpha;
            if (alpha <= 0.001f)
            {
                return;
            }

            float step = h / count;
            painter.strokeColor = new Color(0f, 0f, 0f, alpha);
            painter.lineWidth = Mathf.Max(1f, step * 0.38f);
            for (int i = 0; i < count; i++)
            {
                float y = (i + 0.5f) * step;
                painter.BeginPath();
                painter.MoveTo(new Vector2(0f, y));
                painter.LineTo(new Vector2(w, y));
                painter.Stroke();
            }
        }

        private void PaintRefreshBand(Painter2D painter, float w, float h, float s)
        {
            Color baseCol = WithAlpha(_settings.Amber, 0.0025f);
            float periodHeight = _settings.StandaloneReferenceSize.y + _settings.CrtRollBandHeight;
            float phase = ((_timeSeconds * _settings.CrtRollSpeed) % periodHeight) / periodHeight;
            if (phase < 0f)
            {
                phase += 1f;
            }

            float bandY = phase * h;
            float halfBand = 0.5f * _settings.CrtRollBandHeight * s;

            // Approximate the soft vertical linear gradient with 3 overlapping horizontal strips.
            FillHorizontalStrip(painter, w, bandY - halfBand, bandY + halfBand, WithAlpha(baseCol, baseCol.a * 0.35f));
            FillHorizontalStrip(painter, w, bandY - halfBand * 0.6f, bandY + halfBand * 0.6f, WithAlpha(baseCol, baseCol.a * 0.45f));
            FillHorizontalStrip(painter, w, bandY - halfBand * 0.25f, bandY + halfBand * 0.25f, WithAlpha(baseCol, baseCol.a * 0.40f));
        }

        private static void FillHorizontalStrip(Painter2D painter, float w, float y0, float y1, Color color)
        {
            painter.fillColor = color;
            painter.BeginPath();
            painter.MoveTo(new Vector2(0f, y0));
            painter.LineTo(new Vector2(w, y0));
            painter.LineTo(new Vector2(w, y1));
            painter.LineTo(new Vector2(0f, y1));
            painter.ClosePath();
            painter.Fill();
        }

        private void PaintVignetteAndBezel(Painter2D painter, float w, float h, float s)
        {
            if (_standaloneFraming)
            {
                PaintStandaloneBezel(painter, w, h, s);
                return;
            }

            Vector2 bezelInset = _settings.CrtBezelInset;
            float padX = bezelInset.x * s;
            float padY = bezelInset.y * s;
            float cornerR = 30f * s;
            float sagX = 3.5f * s;
            float sagY = 3.5f * s;

            Color vig = WithAlpha(_settings.BgDeep, _settings.CrtVignetteAlpha);
            if (vig.a > 0.001f)
            {
                StrokeRoundedCrtLoop(painter, w, h, padX + 26f * s, padY + 26f * s, Mathf.Max(4f * s, cornerR - 14f * s), sagX, sagY, 52f * s, WithAlpha(vig, vig.a * 0.22f));
                StrokeRoundedCrtLoop(painter, w, h, padX + 12f * s, padY + 12f * s, Mathf.Max(6f * s, cornerR - 6f * s), sagX, sagY, 26f * s, WithAlpha(vig, vig.a * 0.38f));
            }

            // Fill outer bezel by stroking a thick outer loop outside the CRT screen contour.
            float outerStroke = Mathf.Max(padX, padY) * 2.4f;
            StrokeRoundedCrtLoop(
                painter,
                w,
                h,
                padX - outerStroke * 0.5f,
                padY - outerStroke * 0.5f,
                cornerR + outerStroke * 0.5f,
                sagX,
                sagY,
                outerStroke,
                _settings.BgDeep);

            // Subtle warm CRT bezel rim matching menu_v1_vi.png / menu_v1_en.png.
            Color rimBase = _settings.AmberDim;
            StrokeRoundedCrtLoop(painter, w, h, padX, padY, cornerR, sagX, sagY, 9f * s, WithAlpha(rimBase, 0.05f));
            StrokeRoundedCrtLoop(painter, w, h, padX, padY, cornerR, sagX, sagY, 4.0f * s, WithAlpha(rimBase, 0.12f));
            StrokeRoundedCrtLoop(painter, w, h, padX, padY, cornerR, sagX, sagY, 1.6f * s, WithAlpha(rimBase, 0.24f));
        }

        private static void StrokeRoundedCrtLoop(
            Painter2D painter,
            float w,
            float h,
            float padX,
            float padY,
            float cornerR,
            float sagX,
            float sagY,
            float lineWidth,
            Color color)
        {
            if (color.a <= 0.001f)
            {
                return;
            }

            const int cornerSteps = 10;
            const int edgeSteps = 20;

            float x0 = padX;
            float y0 = padY;
            float x1 = w - padX;
            float y1 = h - padY;
            float r = Mathf.Min(cornerR, Mathf.Min(x1 - x0, y1 - y0) * 0.45f);

            painter.strokeColor = color;
            painter.lineWidth = lineWidth;
            painter.BeginPath();

            // Top edge (left to right)
            for (int i = 0; i <= edgeSteps; i++)
            {
                float t = (float)i / edgeSteps;
                float u = t * 2f - 1f;
                float x = Mathf.Lerp(x0 + r, x1 - r, t);
                float y = y0 - (1f - u * u) * sagY;
                if (i == 0)
                {
                    painter.MoveTo(new Vector2(x, y));
                }
                else
                {
                    painter.LineTo(new Vector2(x, y));
                }
            }

            // Top-right corner
            for (int i = 1; i <= cornerSteps; i++)
            {
                float a = Mathf.Lerp(-Mathf.PI * 0.5f, 0f, (float)i / cornerSteps);
                painter.LineTo(new Vector2(x1 - r + Mathf.Cos(a) * r, y0 + r + Mathf.Sin(a) * r));
            }

            // Right edge (top to bottom)
            for (int i = 1; i <= edgeSteps; i++)
            {
                float t = (float)i / edgeSteps;
                float u = t * 2f - 1f;
                float y = Mathf.Lerp(y0 + r, y1 - r, t);
                float x = x1 + (1f - u * u) * sagX;
                painter.LineTo(new Vector2(x, y));
            }

            // Bottom-right corner
            for (int i = 1; i <= cornerSteps; i++)
            {
                float a = Mathf.Lerp(0f, Mathf.PI * 0.5f, (float)i / cornerSteps);
                painter.LineTo(new Vector2(x1 - r + Mathf.Cos(a) * r, y1 - r + Mathf.Sin(a) * r));
            }

            // Bottom edge (right to left)
            for (int i = 1; i <= edgeSteps; i++)
            {
                float t = (float)i / edgeSteps;
                float u = t * 2f - 1f;
                float x = Mathf.Lerp(x1 - r, x0 + r, t);
                float y = y1 + (1f - u * u) * sagY;
                painter.LineTo(new Vector2(x, y));
            }

            // Bottom-left corner
            for (int i = 1; i <= cornerSteps; i++)
            {
                float a = Mathf.Lerp(Mathf.PI * 0.5f, Mathf.PI, (float)i / cornerSteps);
                painter.LineTo(new Vector2(x0 + r + Mathf.Cos(a) * r, y1 - r + Mathf.Sin(a) * r));
            }

            // Left edge (bottom to top)
            for (int i = 1; i <= edgeSteps; i++)
            {
                float t = (float)i / edgeSteps;
                float u = t * 2f - 1f;
                float y = Mathf.Lerp(y1 - r, y0 + r, t);
                float x = x0 - (1f - u * u) * sagX;
                painter.LineTo(new Vector2(x, y));
            }

            // Top-left corner
            for (int i = 1; i < cornerSteps; i++)
            {
                float a = Mathf.Lerp(Mathf.PI, Mathf.PI * 1.5f, (float)i / cornerSteps);
                painter.LineTo(new Vector2(x0 + r + Mathf.Cos(a) * r, y0 + r + Mathf.Sin(a) * r));
            }

            painter.ClosePath();
            painter.Stroke();
        }

        private void PaintStandaloneBezel(Painter2D painter, float w, float h, float s)
        {
            Vector2 bezelInset = _settings.StandaloneBezelInset;
            float padX = bezelInset.x * s;
            float padY = bezelInset.y * s;
            float cornerR = 22f * s;
            float sagX = 2.5f * s;
            float sagY = 2.5f * s;

            Color vig = WithAlpha(_settings.BgDeep, _settings.CrtVignetteAlpha);
            if (vig.a > 0.001f)
            {
                StrokeRoundedCrtLoop(painter, w, h, padX + 28f * s, padY + 28f * s, Mathf.Max(4f * s, cornerR - 14f * s), sagX, sagY, 56f * s, WithAlpha(vig, vig.a * 0.24f));
                StrokeRoundedCrtLoop(painter, w, h, padX + 14f * s, padY + 14f * s, Mathf.Max(6f * s, cornerR - 6f * s), sagX, sagY, 28f * s, WithAlpha(vig, vig.a * 0.38f));
            }

            float outerStroke = Mathf.Max(padX, padY) * 2.4f;
            StrokeRoundedCrtLoop(
                painter,
                w,
                h,
                padX - outerStroke * 0.5f,
                padY - outerStroke * 0.5f,
                cornerR + outerStroke * 0.5f,
                sagX,
                sagY,
                outerStroke,
                _settings.BgDeep);
        }

        private static Color WithAlpha(Color c, float a)
        {
            return new Color(c.r, c.g, c.b, Mathf.Clamp01(a));
        }
    }
}

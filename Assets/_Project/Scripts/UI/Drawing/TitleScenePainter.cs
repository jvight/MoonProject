using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace MoonProject.UI
{
    /// <summary>
    /// Paints the TITLE-SCREEN v1 2D CRT line-art scene using <see cref="Painter2D"/>:
    /// the warm CRT phosphor background, twinkling stars, Earth crescent and cyan L4 Ark beacon, the rotating Moon
    /// with its schematic grid, craters, ridges, and passing rim landmarks (with warm phosphor persistence trails),
    /// and 07 driving across the top crown with turning wheels, suspension bob/pitch, nodding CRT head, and Morse
    /// <c>· · · —</c> antenna beacon. Zero per-frame GC allocations.
    /// </summary>
    internal sealed class TitleScenePainter : StylePainter
    {
        private const float RefRoverScale = 82f;

        private readonly TitleSettings _settings;
        private readonly TitleAnimationState _state;
        private readonly TitleAnimationState _trailState;
        private Painter2D _painter;
        private float _timeSeconds = 1.1f;
        private bool _standaloneFraming;
        private float _scale = 1f;
        private float _offsetX;
        private float _width;
        private float _height;
        private float _roverPxScale;
        private float _trailMultiplier = 1f;

        public TitleScenePainter(VisualElement element, TitleSettings settings) : base(element)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _state = new TitleAnimationState(settings);
            _trailState = new TitleAnimationState(settings);
        }

        public float TimeSeconds => _timeSeconds;

        public bool StandaloneFraming => _standaloneFraming;

        public TitleAnimationState State => _state;

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
            if (rect.width <= 4f || rect.height <= 4f)
            {
                return;
            }

            _width = rect.width;
            _height = rect.height;
            _state.Evaluate(_timeSeconds, _standaloneFraming);
            _trailState.Evaluate(Mathf.Max(0f, _timeSeconds - _settings.TrailLagSeconds), _standaloneFraming);

            Vector2 refSize = _state.ReferenceSize;
            _scale = _height / refSize.y;
            _offsetX = (_width - refSize.x * _scale) * 0.5f;

            _painter = context.painter2D;
            _painter.lineCap = LineCap.Round;
            _painter.lineJoin = LineJoin.Round;
            _trailMultiplier = 1f;

            PaintBackground();
            PaintStars();
            PaintEarth();
            PaintMoonAndLandmarks();
            PaintRover07();

            _painter = null;
        }

        private Vector2 Map(float refX, float refY)
        {
            if (!_standaloneFraming && _width < _state.ReferenceSize.x * _scale)
            {
                return new Vector2((refX / _state.ReferenceSize.x) * _width, refY * _scale);
            }

            return new Vector2(_offsetX + refX * _scale, refY * _scale);
        }

        private void PaintBackground()
        {
            _painter.fillColor = _settings.BgCorner;
            _painter.BeginPath();
            _painter.MoveTo(Vector2.zero);
            _painter.LineTo(new Vector2(_width, 0f));
            _painter.LineTo(new Vector2(_width, _height));
            _painter.LineTo(new Vector2(0f, _height));
            _painter.ClosePath();
            _painter.Fill();

            // Soft concentric warm phosphor glow centered over the screen/Moon crown.
            Vector2 glowCenter = _standaloneFraming
                ? new Vector2(_width * 0.5f, _height * 0.48f)
                : Map(_state.MoonCenter.x, _state.MoonCenter.y - _state.MoonRadius * 0.65f);
            float maxR = Mathf.Max(_width, _height) * 0.68f;
            const int glowSteps = 6;
            for (int i = glowSteps; i >= 1; i--)
            {
                float frac = (float)i / glowSteps;
                Color c = _settings.BgWarm;
                c.a = 0.18f * (1f - frac * 0.65f);
                Shapes.Disc(_painter, glowCenter, maxR * frac, c);
            }
        }

        private void PaintStars()
        {
            TitleStarEntry[] stars = _settings.Stars;
            float[] twinkles = _state.StarTwinkles;
            if (stars == null)
            {
                return;
            }

            int count = Mathf.Min(stars.Length, twinkles.Length);
            for (int i = 0; i < count; i++)
            {
                TitleStarEntry s = stars[i];
                float tw = twinkles[i];
                float u = _standaloneFraming ? s.U : (0.40f + s.U * 0.54f);
                float v = _standaloneFraming ? s.V : (s.V * 0.90f);
                Vector2 pos = new Vector2(u * _width, v * _height);
                float r = s.Size * _scale;
                Color starCol = s.Flare ? _settings.Cream : _settings.AmberHi;
                Shapes.Disc(_painter, pos, r * 2.2f, WithAlpha(starCol, tw * 0.22f));
                Shapes.Disc(_painter, pos, r, WithAlpha(starCol, tw));
                if (s.Flare)
                {
                    float arm = (4.5f + 2.5f * tw) * _scale;
                    StrokeSegment(pos + new Vector2(-arm, 0f), pos + new Vector2(arm, 0f), _settings.Amber, 1.0f,
                        tw * 0.85f, false);
                    StrokeSegment(pos + new Vector2(0f, -arm), pos + new Vector2(0f, arm), _settings.Amber, 1.0f,
                        tw * 0.85f, false);
                }
            }
        }

        private void PaintEarth()
        {
            Vector2 center = Map(_state.EarthCenter.x, _state.EarthCenter.y);
            float r = _state.EarthRadius * _scale;

            Shapes.Disc(_painter, center, r, _settings.BgDeep);
            StrokeArc(center, r, 0f, Mathf.PI * 2f, _settings.Amber, 1.1f, 0.18f, 48, false);
            StrokeArc(center, r, -0.78f * Mathf.PI, 0.16f * Mathf.PI, _settings.AmberHi, 2.4f, 0.96f, 36, true);
            StrokeArc(center + new Vector2(-0.8f * _scale, 0.5f * _scale), r - 0.8f * _scale, -0.62f * Mathf.PI,
                0.02f * Mathf.PI, _settings.Cream, 1.2f, 0.92f, 28, true);
            StrokeArc(center, r + 1.8f * _scale, -0.84f * Mathf.PI, -0.60f * Mathf.PI, _settings.Cyan, 1.1f, 0.45f,
                14, false);
            StrokeArc(center, r + 1.8f * _scale, 0.04f * Mathf.PI, 0.24f * Mathf.PI, _settings.Cyan, 1.1f, 0.45f,
                14, false);

            Vector2 ark = Map(_state.ArkPosition.x, _state.ArkPosition.y);
            float pulse = _state.ArkPulse;
            Shapes.Disc(_painter, ark, 4.2f * _scale, WithAlpha(_settings.Cyan, 0.24f * pulse));
            Shapes.Disc(_painter, ark, 1.6f * _scale, WithAlpha(_settings.Cyan, 0.95f * pulse));
        }

        private void PaintMoonAndLandmarks()
        {
            Vector2 moonCenter = Map(_state.MoonCenter.x, _state.MoonCenter.y);
            float moonR = _state.MoonRadius * _scale;
            float featureScale = (_state.MoonRadius / _settings.StandaloneMoonRadius) * _scale;

            // 1. Rim landmarks drawn BEFORE Moon disc fill so their bases are cleanly occluded by the lunar horizon.
            if (_settings.TrailLagSeconds > 0f)
            {
                _trailMultiplier = 0.28f;
                PaintLandmarks(_trailState, featureScale, true);
                _trailMultiplier = 1f;
            }

            PaintLandmarks(_state, featureScale, false);

            // 2. Occluded lunar disc interior.
            Shapes.Disc(_painter, moonCenter, moonR, _settings.BgMoon);

            // 3. Subtle dashed schematic latitude rings (0.86R and 0.58R).
            PaintDashedCircle(moonCenter, moonR * _settings.GridOuterRadiusShare, _settings.Amber, 1.0f, 0.14f);
            PaintDashedCircle(moonCenter, moonR * _settings.GridInnerRadiusShare, _settings.Amber, 1.0f, 0.14f);

            // 4. Warm phosphor persistence trail & crisp pass for rotating craters and ridges.
            if (_settings.TrailLagSeconds > 0f)
            {
                _trailMultiplier = 0.28f;
                PaintRotatingMoonFeatures(_trailState, moonCenter, moonR, featureScale, true);
                _trailMultiplier = 1f;
            }

            PaintRotatingMoonFeatures(_state, moonCenter, moonR, featureScale, false);

            // 5. Primary Moon limb stroke with rolling terrain micro-undulations + hot top-crown arc under 07.
            PaintLimbUndulations(moonCenter, moonR, featureScale);
            StrokeArc(moonCenter, moonR + 0.5f * featureScale, -Mathf.PI * 0.5f - 0.36f, -Mathf.PI * 0.5f + 0.36f,
                _settings.AmberHi, 1.6f, 0.92f, 36, true);
        }

        private void PaintDashedCircle(Vector2 center, float radius, Color color, float width, float alpha)
        {
            const int dashes = 72;
            float step = (Mathf.PI * 2f) / dashes;
            float dashSpan = step * 0.42f;
            _painter.strokeColor = WithAlpha(color, alpha);
            _painter.lineWidth = width * _scale;
            for (int i = 0; i < dashes; i++)
            {
                float a0 = i * step;
                float a1 = a0 + dashSpan;
                Vector2 p0 = center + new Vector2(Mathf.Cos(a0) * radius, Mathf.Sin(a0) * radius);
                if (p0.y > _height + 16f)
                {
                    continue;
                }

                Vector2 p1 = center + new Vector2(Mathf.Cos(a1) * radius, Mathf.Sin(a1) * radius);
                _painter.BeginPath();
                _painter.MoveTo(p0);
                _painter.LineTo(p1);
                _painter.Stroke();
            }
        }

        private void PaintRotatingMoonFeatures(TitleAnimationState eval, Vector2 moonCenter, float moonR,
            float featureScale, bool isTrail)
        {
            float moonRot = eval.MoonRotationRadians;
            Color baseAmber = isTrail ? _settings.AmberEdge : _settings.Amber;
            Color hiAmber = isTrail ? _settings.AmberEdge : _settings.AmberHi;
            Color dimAmber = isTrail ? _settings.AmberEdge : _settings.AmberDim;

            TitleRidgeEntry[] ridges = _settings.Ridges;
            if (ridges != null)
            {
                for (int i = 0; i < ridges.Length; i++)
                {
                    TitleRidgeEntry rg = ridges[i];
                    float a0 = rg.Angle + moonRot;
                    float a1 = a0 + rg.Span;
                    float midY = moonCenter.y + Mathf.Sin((a0 + a1) * 0.5f) * moonR * rg.RadialShare;
                    if (midY > _height + 24f)
                    {
                        continue;
                    }

                    StrokeArc(moonCenter, moonR * rg.RadialShare, a0, a1, dimAmber, 1.35f, 0.82f * _trailMultiplier,
                        18, !isTrail);
                }
            }

            TitleCraterEntry[] craters = _settings.Craters;
            if (craters != null)
            {
                for (int i = 0; i < craters.Length; i++)
                {
                    TitleCraterEntry c = craters[i];
                    float ang = c.Angle + moonRot;
                    float r = moonR * c.RadialShare;
                    Vector2 pos = moonCenter + new Vector2(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r);
                    float rx = c.RadiusX * featureScale;
                    if (pos.y - rx > _height + 20f || pos.y + rx < -20f)
                    {
                        continue;
                    }

                    float limbFactor = Mathf.Max(0.32f, 1f - 0.52f * c.RadialShare * c.RadialShare);
                    float ry = c.RadiusY * limbFactor * featureScale;
                    float rot = ang + Mathf.PI * 0.5f + c.Tilt;
                    Color outerCol = c.SunlitRim ? baseAmber : dimAmber;
                    float outerW = c.SunlitRim ? 1.6f : 1.2f;
                    StrokeEllipse(pos, rx, ry, rot, 0f, Mathf.PI * 2f, outerCol, outerW, 0.85f * _trailMultiplier,
                        24, !isTrail);
                    if (c.SunlitRim && !isTrail)
                    {
                        Vector2 rimCenter = pos + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * (1.5f * featureScale);
                        StrokeEllipse(rimCenter, rx * 0.82f, ry * 0.72f, rot, 0.12f * Mathf.PI, 0.88f * Mathf.PI,
                            hiAmber, 1.2f, 0.88f, 16, false);
                    }
                }
            }
        }

        private void PaintLimbUndulations(Vector2 moonCenter, float moonR, float featureScale)
        {
            const int steps = 180;
            float moonRot = _state.MoonRotationRadians;

            for (int pass = 0; pass < 2; pass++)
            {
                bool glow = pass == 0;
                _painter.strokeColor = WithAlpha(_settings.Amber, glow ? 0.22f : 0.94f);
                _painter.lineWidth = (glow ? 6.8f : 2.4f) * _scale;
                _painter.BeginPath();
                for (int i = 0; i <= steps; i++)
                {
                    float theta = (i / (float)steps) * Mathf.PI * 2f;
                    float worldAng = theta - moonRot;
                    float und = (Mathf.Sin(worldAng * 12f) * 0.9f + Mathf.Cos(worldAng * 23f) * 0.5f) * featureScale;
                    float rr = moonR + und;
                    Vector2 pt = moonCenter + new Vector2(Mathf.Cos(theta) * rr, Mathf.Sin(theta) * rr);
                    if (i == 0)
                    {
                        _painter.MoveTo(pt);
                    }
                    else
                    {
                        _painter.LineTo(pt);
                    }
                }

                _painter.ClosePath();
                _painter.Stroke();
            }
        }

        private void PaintLandmarks(TitleAnimationState eval, float featureScale, bool isTrail)
        {
            TitleLandmarkPose[] poses = eval.Landmarks;
            if (poses == null)
            {
                return;
            }

            Color colMain = isTrail ? _settings.AmberEdge : _settings.Amber;
            Color colHi = isTrail ? _settings.AmberEdge : _settings.AmberHi;
            Color colDim = isTrail ? _settings.AmberEdge : _settings.AmberDim;
            Color colCyan = isTrail ? _settings.CyanDim : _settings.Cyan;

            for (int i = 0; i < poses.Length; i++)
            {
                TitleLandmarkPose lm = poses[i];
                if (!lm.Visible || lm.Alpha <= 0.01f)
                {
                    continue;
                }

                Vector2 origin = Map(lm.Position.x, lm.Position.y);
                float sc = lm.Scale * featureScale;
                float a = lm.Alpha * 0.92f * _trailMultiplier;
                bool glow = !isTrail;

                switch (lm.Kind)
                {
                    case TitleLandmarkKind.Lander:
                        PaintLander(origin, lm.Right, lm.Up, sc, a, colMain, colHi, colDim, glow);
                        break;
                    case TitleLandmarkKind.Tower:
                        PaintTower(origin, lm.Right, lm.Up, sc, a, lm.BeaconBlink, colMain, colHi, colDim, colCyan,
                            glow);
                        break;
                    case TitleLandmarkKind.Solar:
                        PaintSolarArray(origin, lm.Right, lm.Up, sc, a, colMain, colHi, colDim, glow);
                        break;
                    case TitleLandmarkKind.Marker:
                        PaintMarker(origin, lm.Right, lm.Up, sc, a, colMain, colDim, glow);
                        break;
                }
            }
        }

        private static Vector2 LmPt(Vector2 origin, Vector2 right, Vector2 up, float sc, float u, float v)
        {
            return new Vector2(
                origin.x + right.x * (u * sc) + up.x * (v * sc),
                origin.y + right.y * (u * sc) + up.y * (v * sc));
        }

        private void PaintLander(Vector2 o, Vector2 r, Vector2 u, float sc, float a, Color main, Color hi, Color dim,
            bool glow)
        {
            // Descent stage box.
            FillAndStrokeQuad(LmPt(o, r, u, sc, -18f, 10f), LmPt(o, r, u, sc, 18f, 10f),
                LmPt(o, r, u, sc, 22f, 22f), LmPt(o, r, u, sc, -22f, 22f), _settings.BgCorner, main, 1.8f, a, glow);

            // Internal truss X on descent stage.
            StrokeSegment(LmPt(o, r, u, sc, -16f, 10f), LmPt(o, r, u, sc, 16f, 22f), dim, 1.1f, a, false);
            StrokeSegment(LmPt(o, r, u, sc, 16f, 10f), LmPt(o, r, u, sc, -16f, 22f), dim, 1.1f, a, false);

            // Upper octagonal cabin.
            _painter.fillColor = _settings.BgCorner;
            _painter.BeginPath();
            _painter.MoveTo(LmPt(o, r, u, sc, -14f, 22f));
            _painter.LineTo(LmPt(o, r, u, sc, -18f, 32f));
            _painter.LineTo(LmPt(o, r, u, sc, -11f, 42f));
            _painter.LineTo(LmPt(o, r, u, sc, 11f, 42f));
            _painter.LineTo(LmPt(o, r, u, sc, 18f, 32f));
            _painter.LineTo(LmPt(o, r, u, sc, 14f, 22f));
            _painter.ClosePath();
            _painter.Fill();
            StrokeSegment(LmPt(o, r, u, sc, -14f, 22f), LmPt(o, r, u, sc, -18f, 32f), hi, 1.8f, a, glow);
            StrokeSegment(LmPt(o, r, u, sc, -18f, 32f), LmPt(o, r, u, sc, -11f, 42f), hi, 1.8f, a, glow);
            StrokeSegment(LmPt(o, r, u, sc, -11f, 42f), LmPt(o, r, u, sc, 11f, 42f), hi, 1.8f, a, glow);
            StrokeSegment(LmPt(o, r, u, sc, 11f, 42f), LmPt(o, r, u, sc, 18f, 32f), hi, 1.8f, a, glow);
            StrokeSegment(LmPt(o, r, u, sc, 18f, 32f), LmPt(o, r, u, sc, 14f, 22f), hi, 1.8f, a, glow);

            // Viewport window & hatch.
            StrokeQuad(LmPt(o, r, u, sc, -5f, 27f), LmPt(o, r, u, sc, 5f, 27f),
                LmPt(o, r, u, sc, 5f, 36f), LmPt(o, r, u, sc, -5f, 36f), main, 1.3f, a, false);
            StrokeSegment(LmPt(o, r, u, sc, -11f, 33f), LmPt(o, r, u, sc, -7f, 33f), main, 1.3f, a, false);
            StrokeSegment(LmPt(o, r, u, sc, 7f, 33f), LmPt(o, r, u, sc, 11f, 33f), main, 1.3f, a, false);

            // Splayed landing legs, shock struts & footpads.
            StrokeSegment(LmPt(o, r, u, sc, -19f, 18f), LmPt(o, r, u, sc, -30f, 0f), main, 1.7f, a, glow);
            StrokeSegment(LmPt(o, r, u, sc, -16f, 11f), LmPt(o, r, u, sc, -26f, 5f), main, 1.7f, a, false);
            StrokeSegment(LmPt(o, r, u, sc, -34f, 0f), LmPt(o, r, u, sc, -25f, 0f), main, 1.7f, a, glow);

            StrokeSegment(LmPt(o, r, u, sc, 19f, 18f), LmPt(o, r, u, sc, 30f, 0f), main, 1.7f, a, glow);
            StrokeSegment(LmPt(o, r, u, sc, 16f, 11f), LmPt(o, r, u, sc, 26f, 5f), main, 1.7f, a, false);
            StrokeSegment(LmPt(o, r, u, sc, 25f, 0f), LmPt(o, r, u, sc, 34f, 0f), main, 1.7f, a, glow);

            // Center boarding ladder.
            StrokeSegment(LmPt(o, r, u, sc, -3f, 10f), LmPt(o, r, u, sc, -5f, 0f), main, 1.5f, a, false);
            StrokeSegment(LmPt(o, r, u, sc, 3f, 10f), LmPt(o, r, u, sc, 5f, 0f), main, 1.5f, a, false);
            StrokeSegment(LmPt(o, r, u, sc, -4f, 7f), LmPt(o, r, u, sc, 4f, 7f), main, 1.4f, a, false);
            StrokeSegment(LmPt(o, r, u, sc, -4.5f, 3.5f), LmPt(o, r, u, sc, 4.5f, 3.5f), main, 1.4f, a, false);

            // Roof comm dish & whip.
            StrokeSegment(LmPt(o, r, u, sc, 0f, 42f), LmPt(o, r, u, sc, 0f, 50f), main, 1.3f, a, false);
            float baseAng = Mathf.Atan2(r.y, r.x);
            StrokeArc(LmPt(o, r, u, sc, 0f, 53f), 5f * sc, baseAng + 0.25f * Mathf.PI, baseAng + 0.85f * Mathf.PI,
                main, 1.3f, a, 12, false);
            StrokeSegment(LmPt(o, r, u, sc, -8f, 42f), LmPt(o, r, u, sc, -10f, 55f), main, 1.3f, a, false);
        }

        private void PaintTower(Vector2 o, Vector2 r, Vector2 u, float sc, float a, float blink, Color main, Color hi,
            Color dim, Color cyan, bool glow)
        {
            // Base footing pads & lower intact lattice section.
            StrokeSegment(LmPt(o, r, u, sc, -14f, 0f), LmPt(o, r, u, sc, 14f, 0f), main, 1.7f, a, glow);
            StrokeSegment(LmPt(o, r, u, sc, -9f, 0f), LmPt(o, r, u, sc, -5f, 32f), main, 1.7f, a, glow);
            StrokeSegment(LmPt(o, r, u, sc, 9f, 0f), LmPt(o, r, u, sc, 5f, 32f), main, 1.7f, a, glow);

            // Lattice cross-bracing.
            StrokeSegment(LmPt(o, r, u, sc, -8f, 4f), LmPt(o, r, u, sc, 6f, 14f), dim, 1.1f, a, false);
            StrokeSegment(LmPt(o, r, u, sc, 8f, 4f), LmPt(o, r, u, sc, -6f, 14f), dim, 1.1f, a, false);
            StrokeSegment(LmPt(o, r, u, sc, -6f, 16f), LmPt(o, r, u, sc, 5f, 28f), dim, 1.1f, a, false);
            StrokeSegment(LmPt(o, r, u, sc, 6f, 16f), LmPt(o, r, u, sc, -5f, 28f), dim, 1.1f, a, false);

            // Broken / bent upper mast section leaning right + dipole cross-arms.
            StrokeSegment(LmPt(o, r, u, sc, -4f, 32f), LmPt(o, r, u, sc, 12f, 58f), hi, 1.7f, a, glow);
            StrokeSegment(LmPt(o, r, u, sc, 4f, 32f), LmPt(o, r, u, sc, 15f, 56f), hi, 1.7f, a, glow);
            StrokeSegment(LmPt(o, r, u, sc, 0f, 44f), LmPt(o, r, u, sc, 16f, 39f), hi, 1.7f, a, glow);
            StrokeSegment(LmPt(o, r, u, sc, 5f, 53f), LmPt(o, r, u, sc, 19f, 49f), hi, 1.7f, a, glow);

            // Snapped guy wire & slack cable.
            StrokeQuadratic(LmPt(o, r, u, sc, -5f, 30f), LmPt(o, r, u, sc, -16f, 18f), LmPt(o, r, u, sc, -22f, 0f),
                dim, 1.1f, a, false);
            StrokeQuadratic(LmPt(o, r, u, sc, 13f, 52f), LmPt(o, r, u, sc, 22f, 40f), LmPt(o, r, u, sc, 20f, 29f),
                dim, 1.1f, a, false);

            // Tiny intermittent spark / beacon at tip.
            Vector2 tip = LmPt(o, r, u, sc, 14f, 59f);
            Color tipCol = blink > 0.5f ? cyan : main;
            StrokeArc(tip, 2.2f * sc, 0f, Mathf.PI * 2f, tipCol, 1.5f, a, 12, glow && blink > 0.5f);
        }

        private void PaintSolarArray(Vector2 o, Vector2 r, Vector2 u, float sc, float a, Color main, Color hi,
            Color dim, bool glow)
        {
            // Tripod base & mast.
            StrokeSegment(LmPt(o, r, u, sc, -12f, 0f), LmPt(o, r, u, sc, 12f, 0f), main, 1.7f, a, glow);
            StrokeSegment(LmPt(o, r, u, sc, -8f, 0f), LmPt(o, r, u, sc, 0f, 10f), main, 1.7f, a, glow);
            StrokeSegment(LmPt(o, r, u, sc, 8f, 0f), LmPt(o, r, u, sc, 0f, 10f), main, 1.7f, a, glow);
            StrokeSegment(LmPt(o, r, u, sc, 0f, 0f), LmPt(o, r, u, sc, 0f, 28f), main, 1.7f, a, glow);

            // Junction box on mast.
            FillAndStrokeQuad(LmPt(o, r, u, sc, -4f, 12f), LmPt(o, r, u, sc, 4f, 12f),
                LmPt(o, r, u, sc, 4f, 19f), LmPt(o, r, u, sc, -4f, 19f), _settings.BgWarm, main, 1.3f, a, false);

            // Tilted solar wing frame around (0, 29) rotated by -0.22 rad in canvas coords.
            const float cosT = 0.9758974f; // cos(-0.22)
            const float sinT = -0.2182296f; // sin(-0.22)
            Vector2 WingPt(float gx, float gyCanvas)
            {
                float rx = gx * cosT - gyCanvas * sinT;
                float ryCanvas = gx * sinT + gyCanvas * cosT;
                return LmPt(o, r, u, sc, rx, 29f - ryCanvas);
            }

            StrokeQuad(WingPt(-28f, -9f), WingPt(-3f, -9f), WingPt(-3f, 4f), WingPt(-28f, 4f), hi, 1.8f, a, glow);
            StrokeQuad(WingPt(3f, -9f), WingPt(28f, -9f), WingPt(28f, 4f), WingPt(3f, 4f), hi, 1.8f, a, glow);
            StrokeSegment(WingPt(-3f, -2f), WingPt(3f, -2f), hi, 1.8f, a, false);

            // Photovoltaic cell grid lines.
            StrokeSegment(WingPt(-20f, -9f), WingPt(-20f, 4f), dim, 1.0f, a, false);
            StrokeSegment(WingPt(-12f, -9f), WingPt(-12f, 4f), dim, 1.0f, a, false);
            StrokeSegment(WingPt(11f, -9f), WingPt(11f, 4f), dim, 1.0f, a, false);
            StrokeSegment(WingPt(19f, -9f), WingPt(19f, 4f), dim, 1.0f, a, false);
            StrokeSegment(WingPt(-28f, -2.5f), WingPt(-3f, -2.5f), dim, 1.0f, a, false);
            StrokeSegment(WingPt(3f, -2.5f), WingPt(28f, -2.5f), dim, 1.0f, a, false);
        }

        private void PaintMarker(Vector2 o, Vector2 r, Vector2 u, float sc, float a, Color main, Color dim, bool glow)
        {
            StrokeSegment(LmPt(o, r, u, sc, -6f, 0f), LmPt(o, r, u, sc, 0f, 14f), dim, 1.3f, a, false);
            StrokeSegment(LmPt(o, r, u, sc, 0f, 14f), LmPt(o, r, u, sc, 6f, 0f), dim, 1.3f, a, false);
            StrokeSegment(LmPt(o, r, u, sc, 0f, 14f), LmPt(o, r, u, sc, 0f, 22f), dim, 1.3f, a, glow);
            StrokeArc(LmPt(o, r, u, sc, 0f, 24f), 2.0f * sc, 0f, Mathf.PI * 2f, main, 1.3f, a, 10, glow);
        }

        private void PaintRover07()
        {
            Vector2 moonCenter = Map(_state.MoonCenter.x, _state.MoonCenter.y);
            float moonR = _state.MoonRadius * _scale;
            float roverScaleMetersToPx = _state.RoverScale * _scale;
            _roverPxScale = roverScaleMetersToPx / RefRoverScale;

            float wheelR = _settings.WheelRadius * roverScaleMetersToPx;
            float halfWheelbase = _settings.WheelbaseHalf * roverScaleMetersToPx;
            float moonDropAtAxle = moonR * (1f - Mathf.Cos(halfWheelbase / moonR));

            float bumpRearPx = _state.BumpRear * roverScaleMetersToPx;
            float crownX = moonCenter.x;
            float crownY = moonCenter.y - moonR;
            float wyRear = (crownY + moonDropAtAxle) - wheelR + bumpRearPx;

            var pose = new RoverPose
            {
                WheelAngle = _state.WheelAngleRadians,
                Bob = _state.BodyBob,
                Pitch = _state.BodyPitch,
                BumpRear = _state.BumpRear,
                BumpFront = _state.BumpFront,
                MoonDropAtAxle = moonDropAtAxle,
                ScoopNod = _state.ScoopNod,
                SolarTilt = _state.SolarTilt,
                AntennaSway = _state.AntennaSway,
                HeadNod = _state.HeadNod,
                MorseOn = _state.MorseLit,
                EyeBlink = _state.EyeBlinking,
                DrawGroundArc = false,
                AlwaysDrawHeadlampBeam = true,
                InstalledSocketsMask = 0u,
            };

            RoverLineArtPainter.DrawRover07(_painter, new Vector2(crownX, crownY), roverScaleMetersToPx, in pose,
                _settings);
            PaintRegolithDust(crownX - halfWheelbase, wyRear + wheelR);
        }

        private void PaintRegolithDust(float rearWheelX, float groundY)
        {
            for (int d = 0; d < 4; d++)
            {
                float phase = (_timeSeconds * 3.2f + d * 0.25f) % 1.0f;
                float dx = rearWheelX - (14f + phase * 26f) * _roverPxScale;
                float dy = groundY - 2f * _roverPxScale - Mathf.Sin(phase * Mathf.PI) * (5f + d * 2f) * _roverPxScale;
                float da = (1f - phase) * 0.65f;
                StrokeArc(new Vector2(dx, dy), 1.3f * _roverPxScale, 0f, Mathf.PI * 2f, _settings.AmberDim, 1.3f, da,
                    8, false);
            }
        }

        private void AppendQuadraticTo(Vector2 p0, Vector2 control, Vector2 p1, int steps)
        {
            for (int i = 1; i <= steps; i++)
            {
                float t = (float)i / steps;
                float mt = 1f - t;
                Vector2 pt = mt * mt * p0 + 2f * mt * t * control + t * t * p1;
                _painter.LineTo(pt);
            }
        }

        private void StrokeQuadratic(Vector2 p0, Vector2 control, Vector2 p1, Color color, float width, float alpha,
            bool glow)
        {
            if (alpha <= 0.005f)
            {
                return;
            }

            int passes = glow ? 2 : 1;
            for (int p = 0; p < passes; p++)
            {
                bool isGlow = glow && p == 0;
                _painter.strokeColor = WithAlpha(color, isGlow ? alpha * 0.20f : alpha);
                _painter.lineWidth = (isGlow ? width * 3.0f : width) * _scale;
                _painter.BeginPath();
                _painter.MoveTo(p0);
                AppendQuadraticTo(p0, control, p1, 10);
                _painter.Stroke();
            }
        }

        private void FillEllipse(Vector2 center, float rx, float ry, Color color, float alpha)
        {
            const int steps = 14;
            _painter.fillColor = WithAlpha(color, alpha);
            _painter.BeginPath();
            for (int i = 0; i < steps; i++)
            {
                float a = (i * Mathf.PI * 2f) / steps;
                Vector2 pt = new Vector2(center.x + Mathf.Cos(a) * rx, center.y + Mathf.Sin(a) * ry);
                if (i == 0)
                {
                    _painter.MoveTo(pt);
                }
                else
                {
                    _painter.LineTo(pt);
                }
            }

            _painter.ClosePath();
            _painter.Fill();
        }

        private void StrokeSegment(Vector2 a, Vector2 b, Color color, float width, float alpha, bool glow)
        {
            if (alpha <= 0.005f)
            {
                return;
            }

            if (glow)
            {
                _painter.strokeColor = WithAlpha(color, alpha * 0.20f);
                _painter.lineWidth = width * 3.0f * _scale;
                _painter.BeginPath();
                _painter.MoveTo(a);
                _painter.LineTo(b);
                _painter.Stroke();
            }

            _painter.strokeColor = WithAlpha(color, alpha);
            _painter.lineWidth = width * _scale;
            _painter.BeginPath();
            _painter.MoveTo(a);
            _painter.LineTo(b);
            _painter.Stroke();
        }

        private void StrokeQuad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color, float width, float alpha,
            bool glow)
        {
            if (alpha <= 0.005f)
            {
                return;
            }

            if (glow)
            {
                _painter.strokeColor = WithAlpha(color, alpha * 0.20f);
                _painter.lineWidth = width * 3.0f * _scale;
                _painter.BeginPath();
                _painter.MoveTo(a);
                _painter.LineTo(b);
                _painter.LineTo(c);
                _painter.LineTo(d);
                _painter.ClosePath();
                _painter.Stroke();
            }

            _painter.strokeColor = WithAlpha(color, alpha);
            _painter.lineWidth = width * _scale;
            _painter.BeginPath();
            _painter.MoveTo(a);
            _painter.LineTo(b);
            _painter.LineTo(c);
            _painter.LineTo(d);
            _painter.ClosePath();
            _painter.Stroke();
        }

        private void FillAndStrokeQuad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color fill, Color stroke,
            float width, float alpha, bool glow)
        {
            _painter.fillColor = fill;
            _painter.BeginPath();
            _painter.MoveTo(a);
            _painter.LineTo(b);
            _painter.LineTo(c);
            _painter.LineTo(d);
            _painter.ClosePath();
            _painter.Fill();
            StrokeQuad(a, b, c, d, stroke, width, alpha, glow);
        }

        private void StrokeArc(Vector2 center, float radius, float a0, float a1, Color color, float width, float alpha,
            int steps, bool glow)
        {
            StrokeEllipse(center, radius, radius, 0f, a0, a1, color, width, alpha, steps, glow);
        }

        private void StrokeEllipse(Vector2 center, float rx, float ry, float rot, float a0, float a1, Color color,
            float width, float alpha, int steps, bool glow)
        {
            if (alpha <= 0.005f || steps < 2)
            {
                return;
            }

            float cosR = Mathf.Cos(rot);
            float sinR = Mathf.Sin(rot);
            int passes = glow ? 2 : 1;
            for (int p = 0; p < passes; p++)
            {
                bool isGlow = glow && p == 0;
                _painter.strokeColor = WithAlpha(color, isGlow ? alpha * 0.20f : alpha);
                _painter.lineWidth = (isGlow ? width * 3.0f : width) * _scale;
                _painter.BeginPath();
                for (int i = 0; i <= steps; i++)
                {
                    float a = a0 + (a1 - a0) * i / steps;
                    float ex = Mathf.Cos(a) * rx;
                    float ey = Mathf.Sin(a) * ry;
                    Vector2 pt = new Vector2(center.x + ex * cosR - ey * sinR, center.y + ex * sinR + ey * cosR);
                    if (i == 0)
                    {
                        _painter.MoveTo(pt);
                    }
                    else
                    {
                        _painter.LineTo(pt);
                    }
                }

                _painter.Stroke();
            }
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            return color;
        }
    }
}

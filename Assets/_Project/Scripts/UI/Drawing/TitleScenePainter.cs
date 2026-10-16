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
        private float _originX;
        private float _originY;
        private float _roverPxScale;
        private float _bodyPitchCos = 1f;
        private float _bodyPitchSin;
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

        // Converts 07 body-frame canvas coordinates (px, py where py is negative upward, at 82px/m scale) to screen space.
        private Vector2 BP(float px, float py)
        {
            float rx = px * _bodyPitchCos - py * _bodyPitchSin;
            float ry = px * _bodyPitchSin + py * _bodyPitchCos;
            return new Vector2(_originX + rx * _roverPxScale, _originY + ry * _roverPxScale);
        }

        // Converts 07 body-frame meter coordinates (mx, my where my is negative upward) to screen space.
        private Vector2 BM(float mx, float my)
        {
            return BP(mx * RefRoverScale, my * RefRoverScale);
        }

        private void PaintRover07()
        {
            Vector2 moonCenter = Map(_state.MoonCenter.x, _state.MoonCenter.y);
            float moonR = _state.MoonRadius * _scale;
            float roverScaleMetersToPx = _state.RoverScale * _scale;
            _roverPxScale = roverScaleMetersToPx / RefRoverScale;

            float wheelR = _settings.WheelRadius * roverScaleMetersToPx;
            float rimR = _settings.WheelRimRadius * roverScaleMetersToPx;
            float halfWheelbase = _settings.WheelbaseHalf * roverScaleMetersToPx;
            float moonDropAtAxle = moonR * (1f - Mathf.Cos(halfWheelbase / moonR));

            float bumpRearPx = _state.BumpRear * roverScaleMetersToPx;
            float bumpFrontPx = _state.BumpFront * roverScaleMetersToPx;
            float crownX = moonCenter.x;
            float crownY = moonCenter.y - moonR;
            float wyRear = (crownY + moonDropAtAxle) - wheelR + bumpRearPx;
            float wyFront = (crownY + moonDropAtAxle) - wheelR + bumpFrontPx;
            float bodyBobPx = _state.BodyBob * roverScaleMetersToPx;

            _originX = crownX;
            _originY = crownY + bodyBobPx;
            _bodyPitchCos = Mathf.Cos(_state.BodyPitch);
            _bodyPitchSin = Mathf.Sin(_state.BodyPitch);
            float wheelAngle = _state.WheelAngleRadians;

            // 1. Far-side wheels (Wheel_FL and Wheel_RL, recessed in 3/4 schematic depth).
            PaintWheel(new Vector2(_originX - halfWheelbase + 6f * _roverPxScale, wyRear - 4f * _roverPxScale),
                wheelR * _settings.FarWheelScale, rimR * _settings.FarWheelScale, wheelAngle + 0.3f, true);
            PaintWheel(new Vector2(_originX + halfWheelbase + 6f * _roverPxScale, wyFront - 4f * _roverPxScale),
                wheelR * _settings.FarWheelScale, rimR * _settings.FarWheelScale, wheelAngle + 0.3f, true);

            // 2. Suspension struts connecting chassis to front and rear axles.
            float rearAxleLocalY = -_settings.WheelRadius * RefRoverScale + (_state.BumpRear - _state.BodyBob) * RefRoverScale;
            float frontAxleLocalY = -_settings.WheelRadius * RefRoverScale + (_state.BumpFront - _state.BodyBob) * RefRoverScale;
            float wbRef = _settings.WheelbaseHalf * RefRoverScale;
            StrokeSegment(BP(-26f, -0.46f * RefRoverScale), BP(-wbRef, rearAxleLocalY), _settings.Amber, 2.0f, 0.90f, true);
            StrokeSegment(BP(-12f, -0.40f * RefRoverScale), BP(-wbRef, rearAxleLocalY), _settings.Amber, 2.0f, 0.90f, false);
            StrokeSegment(BP(26f, -0.46f * RefRoverScale), BP(wbRef, frontAxleLocalY), _settings.Amber, 2.0f, 0.90f, true);
            StrokeSegment(BP(12f, -0.40f * RefRoverScale), BP(wbRef, frontAxleLocalY), _settings.Amber, 2.0f, 0.90f, false);

            // 3. Front lower dig scoop & rear tow hook.
            PaintScoopAndTowHook();

            // 4. Rounded Beetle-Carapace Body Tub, arched fenders, beltline seams, hatch & rear battery bar.
            PaintCarapaceAndBattery(wbRef);

            // 5. Chest utility cutter turret, headlamps + beam cone, and roof solar wing.
            PaintFrontRigAndSolar();

            // 6. Rear whip antenna, diamond sonar loop & Morse beacon, neck bellows & CRT monitor head.
            PaintAntennaAndHead();

            // 7. Near-side wheels (Wheel_RL and Wheel_FR) in foreground & regolith dust ticks.
            PaintWheel(new Vector2(_originX - halfWheelbase, wyRear), wheelR, rimR, wheelAngle, false);
            PaintWheel(new Vector2(_originX + halfWheelbase, wyFront), wheelR, rimR, wheelAngle + 0.5f, false);
            PaintRegolithDust(_originX - halfWheelbase, wyRear + wheelR);
        }

        private void PaintScoopAndTowHook()
        {
            float nodCos = Mathf.Cos(_state.ScoopNod);
            float nodSin = Mathf.Sin(_state.ScoopNod);
            Vector2 SP(float dx, float dy)
            {
                float rx = 0.50f * RefRoverScale + dx * nodCos - dy * nodSin;
                float ry = -0.34f * RefRoverScale + dx * nodSin + dy * nodCos;
                return BP(rx, ry);
            }

            StrokeSegment(SP(0f, 0f), SP(16f, 12f), _settings.Amber, 1.9f, 0.92f, true);
            StrokeSegment(SP(2f, -5f), SP(18f, 6f), _settings.Amber, 1.9f, 0.92f, false);
            FillAndStrokeQuad(SP(15f, 3f), SP(29f, 6f), SP(34f, 15f), SP(16f, 15f),
                WithAlpha(_settings.BgCorner, 0.72f), _settings.Amber, 1.9f, 0.95f, true);
            StrokeSegment(SP(34f, 15f), SP(38f, 14f), _settings.AmberHi, 1.9f, 0.96f, true);

            // Rear tow hook.
            StrokeSegment(BM(-0.71f, -0.44f), BM(-0.80f, -0.42f), _settings.Amber, 1.8f, 0.88f, true);
            StrokeArc(BM(-0.82f, -0.39f), 4f * _roverPxScale, -Mathf.PI * 0.5f, Mathf.PI * 0.8f, _settings.Amber, 1.8f,
                0.88f, 10, true);
        }

        private void PaintCarapaceAndBattery(float wbRef)
        {
            // Rounded Beetle-Carapace Body Tub (with quadratic curves matching anim.html lines 915-928).
            _painter.fillColor = WithAlpha(_settings.BgCorner, 0.72f);
            _painter.BeginPath();
            _painter.MoveTo(BM(-0.68f, -0.42f));
            _painter.LineTo(BM(-0.74f, -0.56f));
            AppendQuadraticTo(BM(-0.74f, -0.56f), BM(-0.72f, -0.86f), BM(-0.46f, -0.94f), 10);
            _painter.LineTo(BM(0.32f, -0.96f));
            AppendQuadraticTo(BM(0.32f, -0.96f), BM(0.64f, -0.92f), BM(0.72f, -0.64f), 10);
            _painter.LineTo(BM(0.66f, -0.42f));
            _painter.LineTo(BM(0.22f, -0.42f));
            _painter.LineTo(BM(-0.22f, -0.42f));
            _painter.ClosePath();
            _painter.Fill();
            _painter.strokeColor = WithAlpha(_settings.AmberHi, 0.22f);
            _painter.lineWidth = 6.8f * _scale;
            _painter.Stroke();
            _painter.strokeColor = WithAlpha(_settings.AmberHi, 0.96f);
            _painter.lineWidth = 2.3f * _scale;
            _painter.Stroke();

            // Arched wheel fenders (front & rear flares).
            float wheelRefR = _settings.WheelRadius * RefRoverScale;
            PaintFenderArc(-wbRef, -wheelRefR, wheelRefR + 7f);
            PaintFenderArc(wbRef, -wheelRefR, wheelRefR + 7f);

            // Horizontal beltline seams.
            StrokeSegment(BM(-0.72f, -0.61f), BM(0.70f, -0.61f), _settings.Amber, 1.5f, 0.88f, true);
            StrokeSegment(BM(-0.69f, -0.55f), BM(0.67f, -0.55f), _settings.Amber, 1.5f, 0.88f, false);

            // Side inspection hatch & louver vents on carapace.
            StrokeQuad(BM(-0.22f, -0.84f), BM(0.20f, -0.84f), BM(0.20f, -0.65f), BM(-0.22f, -0.65f),
                _settings.AmberDim, 1.2f, 0.85f, false);
            for (int i = 0; i < 4; i++)
            {
                float lv = -0.48f + i * 0.05f;
                StrokeSegment(BM(lv, -0.82f), BM(lv, -0.68f), _settings.AmberDim, 1.2f, 0.82f, false);
            }

            // Rear 6-segment battery bar indicator.
            for (int seg = 0; seg < 6; seg++)
            {
                float sy = -0.66f - seg * 0.036f;
                StrokeSegment(BM(-0.62f, sy), BM(-0.54f, sy), _settings.AmberHi, 1.4f, 0.92f, true);
            }
        }

        private void PaintFenderArc(float cx, float cy, float r)
        {
            const int steps = 16;
            _painter.strokeColor = WithAlpha(_settings.Amber, 0.90f);
            _painter.lineWidth = 2.0f * _scale;
            _painter.BeginPath();
            for (int i = 0; i <= steps; i++)
            {
                float a = Mathf.PI * 1.12f + (Mathf.PI * 0.76f * i) / steps;
                Vector2 pt = BP(cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r);
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

        private void PaintFrontRigAndSolar()
        {
            // Chest utility cutter turret & twin front headlamps at (0.70*S, -0.62*S).
            Vector2 lampCenter = BM(0.70f, -0.62f);
            Shapes.Disc(_painter, lampCenter, 4.5f * _roverPxScale, _settings.Cream);
            StrokeArc(lampCenter, 4.5f * _roverPxScale, -Mathf.PI * 0.5f, Mathf.PI * 0.5f, _settings.AmberHi, 1.6f,
                0.96f, 12, true);

            // Soft headlamp beam cone ahead of 07 (3 layered alpha slices matching beamGrad).
            Vector2 b0 = BM(0.73f, -0.64f);
            Vector2 b1 = BM(0.73f, -0.59f);
            for (int slice = 1; slice <= 3; slice++)
            {
                float frac = slice / 3f;
                Vector2 topFar = BM(Mathf.Lerp(0.73f, 1.58f, frac), Mathf.Lerp(-0.64f, -0.52f, frac));
                Vector2 botFar = BM(Mathf.Lerp(0.73f, 1.48f, frac), Mathf.Lerp(-0.59f, -0.22f, frac));
                _painter.fillColor = WithAlpha(_settings.AmberHi, (4 - slice) * 0.045f);
                _painter.BeginPath();
                _painter.MoveTo(b0);
                _painter.LineTo(topFar);
                _painter.LineTo(botFar);
                _painter.LineTo(b1);
                _painter.ClosePath();
                _painter.Fill();
            }

            // Roof Solar Panel Wing (piv_solar_wing at (-0.08*S, -0.96*S), tilted -0.11 rad).
            float wingAngle = -0.11f + _state.SolarTilt;
            float wCos = Mathf.Cos(wingAngle);
            float wSin = Mathf.Sin(wingAngle);
            Vector2 WP(float dx, float dy)
            {
                float rx = -0.08f * RefRoverScale + dx * wCos - dy * wSin;
                float ry = -0.96f * RefRoverScale + dx * wSin + dy * wCos;
                return BP(rx, ry);
            }

            StrokeSegment(WP(0f, 0f), WP(0f, -6f), _settings.Amber, 1.8f, 0.92f, false);
            StrokeQuad(WP(-0.32f * RefRoverScale, -6f), WP(0.24f * RefRoverScale, -6f),
                WP(0.20f * RefRoverScale, -11f), WP(-0.36f * RefRoverScale, -11f), _settings.Amber, 1.8f, 0.92f, true);
        }

        private void PaintAntennaAndHead()
        {
            // Rear Whip Antenna with Diamond Sonar Loop at (-0.46*S, -0.92*S).
            float aCos = Mathf.Cos(_state.AntennaSway);
            float aSin = Mathf.Sin(_state.AntennaSway);
            Vector2 AP(float dx, float dy)
            {
                float rx = -0.46f * RefRoverScale + dx * aCos - dy * aSin;
                float ry = -0.92f * RefRoverScale + dx * aSin + dy * aCos;
                return BP(rx, ry);
            }

            StrokeSegment(AP(0f, 0f), AP(0f, -8f), _settings.Amber, 1.8f, 0.92f, false);
            StrokeQuadratic(AP(0f, -8f), AP(-4f, -32f), AP(-8f, -56f), _settings.Amber, 1.5f, 0.92f, true);
            StrokeQuad(AP(-4f, -30f), AP(3f, -37f), AP(-5f, -44f), AP(-12f, -37f), _settings.AmberHi, 1.4f, 0.92f,
                true);

            // Glowing beacon tip blinking 07 Morse call-sign (· · · —).
            Vector2 antTip = AP(-8f, -58f);
            bool lit = _state.MorseLit;
            Color tipCol = lit ? _settings.Cyan : _settings.AmberHi;
            Shapes.Disc(_painter, antTip, (lit ? 3.2f : 2.2f) * _roverPxScale, tipCol);
            if (lit)
            {
                Shapes.Disc(_painter, antTip, 6.2f * _roverPxScale, WithAlpha(_settings.Cyan, 0.24f));
                StrokeArc(antTip, 7.0f * _roverPxScale, 0f, Mathf.PI * 2f, _settings.Cyan, 1.2f, 0.55f, 16, false);
            }

            // Ribbed Rubber Neck Bellows (3 horizontal rounded plates).
            for (int r = 0; r < 3; r++)
            {
                float ry = (-0.96f - r * 0.04f) * RefRoverScale;
                StrokeQuad(BP(0.04f * RefRoverScale, ry - 3f), BP(0.22f * RefRoverScale, ry - 3f),
                    BP(0.22f * RefRoverScale, ry + 0.2f), BP(0.04f * RefRoverScale, ry + 0.2f),
                    _settings.Amber, 1.5f, 0.88f, false);
            }

            // CRT Monitor Head at (0.15*S, -1.06*S) rotated by HeadNod.
            float hCos = Mathf.Cos(_state.HeadNod);
            float hSin = Mathf.Sin(_state.HeadNod);
            Vector2 HP(float mx, float my)
            {
                float dx = mx * RefRoverScale;
                float dy = my * RefRoverScale;
                float rx = 0.15f * RefRoverScale + dx * hCos - dy * hSin;
                float ry = -1.06f * RefRoverScale + dx * hSin + dy * hCos;
                return BP(rx, ry);
            }

            _painter.fillColor = WithAlpha(_settings.BgCorner, 0.72f);
            _painter.BeginPath();
            _painter.MoveTo(HP(-0.24f, -0.04f));
            _painter.LineTo(HP(-0.28f, -0.34f));
            AppendQuadraticTo(HP(-0.28f, -0.34f), HP(-0.26f, -0.45f), HP(-0.10f, -0.47f), 8);
            _painter.LineTo(HP(0.24f, -0.47f));
            _painter.LineTo(HP(0.31f, -0.43f));
            _painter.LineTo(HP(0.28f, -0.02f));
            _painter.LineTo(HP(-0.18f, -0.02f));
            _painter.ClosePath();
            _painter.Fill();
            _painter.strokeColor = WithAlpha(_settings.AmberHi, 0.22f);
            _painter.lineWidth = 6.4f * _scale;
            _painter.Stroke();
            _painter.strokeColor = WithAlpha(_settings.AmberHi, 0.96f);
            _painter.lineWidth = 2.2f * _scale;
            _painter.Stroke();

            // Inner recessed screen bezel line.
            StrokeQuadratic(HP(0.24f, -0.41f), HP(0.28f, -0.22f), HP(0.24f, -0.06f), _settings.Amber, 1.5f, 0.90f,
                true);

            // Glowing phosphor eye on the slightly turned screen face (blinks once every 6.5s).
            if (_state.EyeBlinking)
            {
                StrokeSegment(HP(0.17f, -0.24f), HP(0.24f, -0.24f), _settings.Cream, 1.8f, 0.95f, false);
            }
            else
            {
                Vector2 eyeCenter = HP(0.21f, -0.25f);
                FillEllipse(eyeCenter, 3.0f * _roverPxScale, 5.2f * _roverPxScale, _settings.Cream, 0.98f);
                StrokeEllipse(eyeCenter, 3.0f * _roverPxScale, 5.2f * _roverPxScale, 0f, 0f, Mathf.PI * 2f,
                    _settings.AmberHi, 1.4f, 0.90f, 14, true);
            }

            // Cheek Brass Lens Pod (circular r = 0.115*S and inner r = 0.065*S + cyan glint).
            Vector2 podCenter = HP(-0.02f, -0.24f);
            Shapes.Disc(_painter, podCenter, 0.115f * RefRoverScale * _roverPxScale, WithAlpha(_settings.BgCorner, 0.85f));
            StrokeArc(podCenter, 0.115f * RefRoverScale * _roverPxScale, 0f, Mathf.PI * 2f, _settings.Amber, 1.7f,
                0.92f, 20, true);
            StrokeArc(podCenter, 0.065f * RefRoverScale * _roverPxScale, 0f, Mathf.PI * 2f, _settings.AmberHi, 1.2f,
                0.90f, 16, false);
            Shapes.Disc(_painter, HP(-0.01f, -0.25f), 1.8f * _roverPxScale, _settings.Cyan);
        }

        private void PaintWheel(Vector2 center, float radius, float rimRadius, float wheelAngle, bool isFarSide)
        {
            float alpha = isFarSide ? _settings.FarWheelAlpha : 1.0f;
            bool glow = !isFarSide;

            if (!isFarSide)
            {
                Shapes.Disc(_painter, center, radius, WithAlpha(_settings.BgCorner, 0.82f));
            }

            StrokeArc(center, radius, 0f, Mathf.PI * 2f, isFarSide ? _settings.AmberDim : _settings.AmberHi,
                isFarSide ? 1.6f : 2.3f, 0.95f * alpha, 32, glow);
            StrokeArc(center, radius * 0.80f, 0f, Mathf.PI * 2f, _settings.Amber, 1.2f, 0.80f * alpha, 24, false);
            StrokeArc(center, rimRadius, 0f, Mathf.PI * 2f, _settings.Amber, 1.6f, 0.90f * alpha, 20, glow);

            int lugs = _settings.WheelLugCount;
            Color lugCol = isFarSide ? _settings.AmberDim : _settings.Amber;
            for (int i = 0; i < lugs; i++)
            {
                float a = wheelAngle + (i * Mathf.PI * 2f) / lugs;
                Vector2 p0 = center + new Vector2(Mathf.Cos(a) * (radius - 1.5f * _roverPxScale),
                    Mathf.Sin(a) * (radius - 1.5f * _roverPxScale));
                Vector2 p1 = center + new Vector2(Mathf.Cos(a) * (radius + 2.8f * _roverPxScale),
                    Mathf.Sin(a) * (radius + 2.8f * _roverPxScale));
                StrokeSegment(p0, p1, lugCol, 1.6f, 0.90f * alpha, false);
            }

            int spokes = _settings.WheelSpokeCount;
            for (int i = 0; i < spokes; i++)
            {
                float a = wheelAngle + (i * Mathf.PI * 2f) / spokes;
                Vector2 s0 = center + new Vector2(Mathf.Cos(a) * (5.0f * _roverPxScale),
                    Mathf.Sin(a) * (5.0f * _roverPxScale));
                Vector2 s1 = center + new Vector2(Mathf.Cos(a) * rimRadius, Mathf.Sin(a) * rimRadius);
                StrokeSegment(s0, s1, _settings.Amber, 1.4f, 0.85f * alpha, false);
            }

            Shapes.Disc(_painter, center, 4.2f * _roverPxScale, WithAlpha(_settings.AmberHi, 0.95f * alpha));
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

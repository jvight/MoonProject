using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// Draws a clean 2D 3/4-perspective technical line-art diagram of 07 v3 (<c>Assets/_Project/Art/R1/Models/Rover/</c>)
    /// on the Tab "07" equipment screen: four wheels with chevron treads, suspension struts, beetle carapace body,
    /// front toothed dig scoop, twin headlamps, chest cutter turret, ribbed neck, CRT monitor head with visor, left
    /// lens pod, right cassette tape deck, roof solar wing, rear clamshell hatch + 8-segment battery bar, tow hook,
    /// and rear antenna with sonar loop. Projected with the same 3/4 camera framing as the 12 socket anchors so every
    /// socket dot and cyan leader line lands directly on its physical component.
    /// </summary>
    internal sealed class RoverDiagramPainter : StylePainter
    {
        public const float Aspect = 1.5f;

        private const float YawRad = 55f * Mathf.Deg2Rad;
        private const float ElevRad = 14f * Mathf.Deg2Rad;
        private const float CameraDistance = 5.6f;
        private const float LookHeight = 0.6f;
        private const float TanHalfFov = 0.26794919f; // tan(15 deg)

        private static readonly CustomStyleProperty<Color> LineColorProperty =
            new CustomStyleProperty<Color>("--diagram-line");

        private static readonly CustomStyleProperty<Color> BrightColorProperty =
            new CustomStyleProperty<Color>("--diagram-bright");

        private static readonly CustomStyleProperty<Color> DimColorProperty =
            new CustomStyleProperty<Color>("--diagram-dim");

        private static readonly CustomStyleProperty<Color> FillColorProperty =
            new CustomStyleProperty<Color>("--diagram-fill");

        private static readonly CustomStyleProperty<Color> GridColorProperty =
            new CustomStyleProperty<Color>("--diagram-grid");

        private static readonly Vector3 CamPos;
        private static readonly Vector3 CamRight;
        private static readonly Vector3 CamUp;
        private static readonly Vector3 CamFwd;

        // 07 v3 socket anchor positions in Unity rover-local metres (+X right, +Y up, +Z forward), matching
        // Core.RoverSocket enum order (0..11).
        private static readonly Vector3[] SocketPoints =
        {
            new Vector3(-0.04f, 0.88f, 0.58f),  // 0 Cutter (front chest cutter turret)
            new Vector3(0.00f, 0.36f, -0.66f),  // 1 Tether (rear tow hook)
            new Vector3(-0.18f, 0.68f, 0.65f),  // 2 Lamp (front twin headlamps)
            new Vector3(-0.06f, 0.24f, 0.66f),  // 3 Belly (under-chassis hover/belly mount)
            new Vector3(0.48f, 0.78f, -0.18f),  // 4 Flanks (upper flank capacitor mount)
            new Vector3(-0.12f, 0.48f, 0.60f),  // 5 Panniers (front-flank side bin mount)
            new Vector3(0.08f, 0.84f, -0.36f),  // 6 Back (rear-top spine mount)
            new Vector3(0.12f, 0.56f, -0.58f),  // 7 Hold (rear clamshell hatch)
            new Vector3(0.12f, 0.97f, -0.28f),  // 8 Wing (roof solar wing)
            new Vector3(0.34f, 1.23f, 0.04f),   // 9 Deck (right cheek tape deck pod)
            new Vector3(-0.48f, 0.28f, 0.56f),  // 10 Wheels (front wheel drive hub)
            new Vector3(0.36f, 1.14f, -0.46f),  // 11 Antenna (rear whip antenna + sonar loop)
        };

        private Color _lineColor = new Color(1f, 0.71f, 0.28f, 0.95f);
        private Color _brightColor = new Color(1f, 0.84f, 0.50f, 1f);
        private Color _dimColor = new Color(1f, 0.71f, 0.28f, 0.52f);
        private Color _fillColor = new Color(0.055f, 0.035f, 0.02f, 0.96f);
        private Color _gridColor = new Color(0.37f, 0.95f, 1f, 0.22f);
        private Painter2D _painter;
        private Rect _frame;

        static RoverDiagramPainter()
        {
            float cosElev = Mathf.Cos(ElevRad);
            var dir = new Vector3(Mathf.Sin(YawRad) * cosElev, Mathf.Sin(ElevRad), Mathf.Cos(YawRad) * cosElev);
            CamPos = new Vector3(0f, LookHeight, 0f) + dir * CameraDistance;
            CamFwd = -dir;
            CamRight = new Vector3(CamFwd.z, 0f, -CamFwd.x).normalized;
            CamUp = Vector3.Cross(CamFwd, CamRight);
        }

        public RoverDiagramPainter(VisualElement element) : base(element)
        {
        }

        /// <summary>Viewport coordinates (0..1, bottom-left origin) for <paramref name="socket"/> on 07 v3.</summary>
        public static Vector2 SocketViewport(RoverSocket socket)
        {
            int index = (int)socket;
            if (index < 0 || index >= SocketPoints.Length)
            {
                return new Vector2(0.5f, 0.5f);
            }

            return ProjectUv(SocketPoints[index]);
        }

        /// <summary>Aspect-fitted 3:2 frame inside <paramref name="box"/> where the diagram and sockets live.</summary>
        public static Rect FrameRect(Rect box)
        {
            float boxAspect = box.height > 0f ? box.width / box.height : Aspect;
            if (boxAspect > Aspect)
            {
                float width = box.height * Aspect;
                return new Rect(box.x + (box.width - width) * 0.5f, box.y, width, box.height);
            }

            float height = box.width / Aspect;
            return new Rect(box.x, box.y + (box.height - height) * 0.5f, box.width, height);
        }

        protected override void ReadStyle(ICustomStyle style)
        {
            style.TryGetValue(LineColorProperty, out _lineColor);
            style.TryGetValue(BrightColorProperty, out _brightColor);
            style.TryGetValue(DimColorProperty, out _dimColor);
            style.TryGetValue(FillColorProperty, out _fillColor);
            style.TryGetValue(GridColorProperty, out _gridColor);
        }

        protected override void Paint(MeshGenerationContext context)
        {
            Rect box = Element.contentRect;
            if (box.width <= 4f || box.height <= 4f)
            {
                return;
            }

            _frame = FrameRect(box);
            _painter = context.painter2D;
            _painter.lineCap = LineCap.Round;
            _painter.lineJoin = LineJoin.Round;

            PaintGridAndWheelPair(false);
            PaintUndercarriageAndScoop();
            PaintCarapaceAndRear();
            PaintFrontRigAndRoof();
            PaintHeadAndAntenna();
            PaintGridAndWheelPair(true);
            _painter = null;
        }

        private void PaintGridAndWheelPair(bool nearSide)
        {
            if (!nearSide)
            {
                PaintWheel(new Vector3(-0.62f, 0.35f, -0.53f), false);
                PaintWheel(new Vector3(-0.62f, 0.35f, 0.53f), false);
                return;
            }

            PaintWheel(new Vector3(0.62f, 0.35f, -0.53f), true);
            PaintWheel(new Vector3(0.62f, 0.35f, 0.53f), true);
        }

        private void PaintWheel(Vector3 center, bool near)
        {
            float halfW = 0.13f;
            float r = 0.34f;
            float xOut = center.x + (center.x > 0f ? halfW : -halfW);
            float xIn = center.x - (center.x > 0f ? halfW : -halfW);

            _painter.fillColor = _fillColor;
            _painter.strokeColor = near ? _lineColor : _dimColor;
            _painter.lineWidth = near ? 2f : 1.4f;

            PaintWheelRing(new Vector3(xIn, center.y, center.z), r, true);
            PaintWheelTreads(center, r);
            PaintWheelRing(new Vector3(xOut, center.y, center.z), r, true);
            PaintWheelRing(new Vector3(xOut, center.y, center.z), r * 0.55f, false);
            PaintWheelRing(new Vector3(xOut, center.y, center.z), r * 0.22f, false);
        }

        private void PaintWheelRing(Vector3 faceCenter, float radius, bool fill)
        {
            const int steps = 16;
            _painter.BeginPath();
            for (int i = 0; i <= steps; i++)
            {
                float a = i * (Mathf.PI * 2f / steps);
                Vector2 pt = P(faceCenter.x, faceCenter.y + Mathf.Sin(a) * radius,
                    faceCenter.z + Mathf.Cos(a) * radius);
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
            if (fill)
            {
                _painter.Fill();
            }

            _painter.Stroke();
        }

        private void PaintWheelTreads(Vector3 center, float radius)
        {
            float x0 = center.x - 0.13f;
            float x1 = center.x + 0.13f;
            for (int i = -2; i <= 4; i++)
            {
                float a = i * (Mathf.PI * 2f / 12f);
                float aMid = a + 0.14f;
                float y0 = center.y + Mathf.Sin(a) * radius;
                float z0 = center.z + Mathf.Cos(a) * radius;
                float ym = center.y + Mathf.Sin(aMid) * radius;
                float zm = center.z + Mathf.Cos(aMid) * radius;
                Segment(P(x0, y0, z0), P(center.x, ym, zm));
                Segment(P(center.x, ym, zm), P(x1, y0, z0));
            }
        }

        private void PaintUndercarriageAndScoop()
        {
            _painter.fillColor = _fillColor;
            _painter.strokeColor = _dimColor;
            _painter.lineWidth = 1.6f;
            QuadFill(new Vector3(-0.34f, 0.28f, -0.44f), new Vector3(0.34f, 0.28f, -0.44f),
                new Vector3(0.34f, 0.28f, 0.44f), new Vector3(-0.34f, 0.28f, 0.44f));

            // Suspension arms & struts to right wheels + rear tow hook (Tether socket).
            Segment(P(0.36f, 0.46f, 0.42f), P(0.56f, 0.35f, 0.53f));
            Segment(P(0.36f, 0.46f, -0.42f), P(0.56f, 0.35f, -0.53f));
            _painter.strokeColor = _lineColor;
            Segment(P(0f, 0.42f, -0.54f), P(0f, 0.34f, -0.66f));
            Segment(P(0f, 0.34f, -0.66f), P(0f, 0.41f, -0.68f));

            // Front dig scoop blade, hydraulic arms & 5 cutting teeth.
            Segment(P(0.36f, 0.44f, 0.54f), P(0.44f, 0.30f, 0.78f));
            Segment(P(-0.36f, 0.44f, 0.54f), P(-0.44f, 0.30f, 0.78f));
            QuadFill(new Vector3(-0.50f, 0.42f, 0.74f), new Vector3(0.50f, 0.42f, 0.74f),
                new Vector3(0.54f, 0.20f, 0.90f), new Vector3(-0.54f, 0.20f, 0.90f));
            for (int t = -2; t <= 2; t++)
            {
                float tx = t * 0.19f;
                Segment(P(tx - 0.05f, 0.20f, 0.90f), P(tx, 0.16f, 0.96f));
                Segment(P(tx, 0.16f, 0.96f), P(tx + 0.05f, 0.20f, 0.90f));
            }
        }

        private void PaintCarapaceAndRear()
        {
            _painter.fillColor = _fillColor;
            _painter.strokeColor = _lineColor;
            _painter.lineWidth = 2.1f;

            // Lower chassis tub & upper beetle carapace dome.
            QuadFill(new Vector3(-0.46f, 0.38f, -0.58f), new Vector3(0.46f, 0.38f, -0.58f),
                new Vector3(0.46f, 0.72f, -0.52f), new Vector3(-0.46f, 0.72f, -0.52f));
            QuadFill(new Vector3(-0.46f, 0.38f, 0.60f), new Vector3(0.46f, 0.38f, 0.60f),
                new Vector3(0.44f, 0.72f, 0.56f), new Vector3(-0.44f, 0.72f, 0.56f));
            QuadFill(new Vector3(0.46f, 0.38f, -0.58f), new Vector3(0.46f, 0.38f, 0.60f),
                new Vector3(0.44f, 0.72f, 0.56f), new Vector3(0.44f, 0.72f, -0.52f));
            QuadFill(new Vector3(-0.44f, 0.72f, -0.52f), new Vector3(0.44f, 0.72f, -0.52f),
                new Vector3(0.38f, 0.94f, 0.42f), new Vector3(-0.38f, 0.94f, 0.42f));
            QuadFill(new Vector3(-0.44f, 0.72f, 0.56f), new Vector3(0.44f, 0.72f, 0.56f),
                new Vector3(0.38f, 0.90f, 0.44f), new Vector3(-0.38f, 0.90f, 0.44f));

            // Beltline & right-flank pannier bin (Panniers) + capacitor drum (Flanks).
            _painter.lineWidth = 1.5f;
            Segment(P(0.47f, 0.62f, -0.56f), P(0.47f, 0.62f, 0.58f));
            QuadFill(new Vector3(0.47f, 0.50f, 0.18f), new Vector3(0.47f, 0.50f, 0.46f),
                new Vector3(0.54f, 0.66f, 0.46f), new Vector3(0.54f, 0.66f, 0.18f));
            QuadFill(new Vector3(0.45f, 0.74f, -0.32f), new Vector3(0.45f, 0.74f, -0.04f),
                new Vector3(0.52f, 0.86f, -0.04f), new Vector3(0.52f, 0.86f, -0.32f));

            // Rear clamshell hatch (Hold) & 8-segment battery bar along rear lip.
            QuadStroke(new Vector3(-0.34f, 0.44f, -0.59f), new Vector3(0.34f, 0.44f, -0.59f),
                new Vector3(0.32f, 0.70f, -0.56f), new Vector3(-0.32f, 0.70f, -0.56f));
            _painter.strokeColor = _brightColor;
            for (int b = 0; b < 8; b++)
            {
                float bx = -0.30f + b * 0.085f;
                Segment(P(bx, 0.75f, -0.54f), P(bx + 0.055f, 0.75f, -0.54f));
            }
        }

        private void PaintFrontRigAndRoof()
        {
            // Twin front headlamps (Lamp) & chest cutter turret (Cutter).
            _painter.strokeColor = _brightColor;
            _painter.lineWidth = 1.8f;
            PaintHeadlamp(new Vector3(-0.18f, 0.65f, 0.64f));
            PaintHeadlamp(new Vector3(0.18f, 0.65f, 0.64f));
            QuadFill(new Vector3(-0.11f, 0.72f, 0.50f), new Vector3(0.11f, 0.72f, 0.50f),
                new Vector3(0.09f, 0.84f, 0.68f), new Vector3(-0.09f, 0.84f, 0.68f));
            Segment(P(0f, 0.78f, 0.68f), P(0f, 0.78f, 0.80f));

            // Roof solar wing (Wing) & rear spine (Back).
            _painter.strokeColor = _lineColor;
            _painter.lineWidth = 1.7f;
            QuadFill(new Vector3(-0.34f, 0.97f, -0.46f), new Vector3(0.34f, 0.97f, -0.46f),
                new Vector3(0.34f, 0.97f, -0.06f), new Vector3(-0.34f, 0.97f, -0.06f));
            _painter.strokeColor = _dimColor;
            _painter.lineWidth = 1.1f;
            Segment(P(-0.11f, 0.975f, -0.46f), P(-0.11f, 0.975f, -0.06f));
            Segment(P(0.11f, 0.975f, -0.46f), P(0.11f, 0.975f, -0.06f));
            Segment(P(-0.34f, 0.975f, -0.26f), P(0.34f, 0.975f, -0.26f));
        }

        private void PaintHeadAndAntenna()
        {
            // Rear whip antenna & diamond sonar loop (Antenna).
            _painter.strokeColor = _lineColor;
            _painter.lineWidth = 1.7f;
            Segment(P(0.36f, 0.88f, -0.46f), P(0.36f, 1.44f, -0.48f));
            QuadStroke(new Vector3(0.36f, 1.16f, -0.47f), new Vector3(0.43f, 1.24f, -0.47f),
                new Vector3(0.36f, 1.32f, -0.47f), new Vector3(0.29f, 1.24f, -0.47f));

            // Ribbed neck, left cheek lens pod & CRT monitor head box.
            for (int r = 0; r < 3; r++)
            {
                float ny = 0.95f + r * 0.045f;
                Segment(P(-0.09f, ny, 0.04f), P(0.09f, ny, 0.04f));
            }

            QuadFill(new Vector3(-0.38f, 1.16f, -0.04f), new Vector3(-0.27f, 1.16f, -0.04f),
                new Vector3(-0.27f, 1.30f, 0.14f), new Vector3(-0.38f, 1.30f, 0.14f));
            _painter.lineWidth = 2.1f;
            QuadFill(new Vector3(-0.27f, 1.07f, -0.14f), new Vector3(0.27f, 1.07f, -0.14f),
                new Vector3(0.27f, 1.41f, -0.14f), new Vector3(-0.27f, 1.41f, -0.14f));
            QuadFill(new Vector3(0.27f, 1.07f, -0.14f), new Vector3(0.27f, 1.07f, 0.22f),
                new Vector3(0.27f, 1.41f, 0.22f), new Vector3(0.27f, 1.41f, -0.14f));
            QuadFill(new Vector3(-0.27f, 1.07f, 0.22f), new Vector3(0.27f, 1.07f, 0.22f),
                new Vector3(0.27f, 1.41f, 0.22f), new Vector3(-0.27f, 1.41f, 0.22f));

            // CRT screen bezel, friendly amber eyes & right-cheek tape deck pod (Deck).
            _painter.strokeColor = _brightColor;
            _painter.lineWidth = 1.7f;
            QuadStroke(new Vector3(-0.21f, 1.13f, 0.23f), new Vector3(0.21f, 1.13f, 0.23f),
                new Vector3(0.21f, 1.35f, 0.23f), new Vector3(-0.21f, 1.35f, 0.23f));
            Segment(P(-0.13f, 1.25f, 0.235f), P(-0.04f, 1.25f, 0.235f));
            Segment(P(0.04f, 1.25f, 0.235f), P(0.13f, 1.25f, 0.235f));
            QuadFill(new Vector3(0.28f, 1.15f, -0.07f), new Vector3(0.39f, 1.15f, -0.07f),
                new Vector3(0.39f, 1.31f, 0.13f), new Vector3(0.28f, 1.31f, 0.13f));
        }

        private void PaintHeadlamp(Vector3 pos)
        {
            Segment(P(pos.x - 0.05f, pos.y - 0.04f, pos.z), P(pos.x + 0.05f, pos.y - 0.04f, pos.z));
            Segment(P(pos.x - 0.05f, pos.y + 0.04f, pos.z), P(pos.x + 0.05f, pos.y + 0.04f, pos.z));
        }

        private void QuadFill(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            _painter.BeginPath();
            _painter.MoveTo(P(a.x, a.y, a.z));
            _painter.LineTo(P(b.x, b.y, b.z));
            _painter.LineTo(P(c.x, c.y, c.z));
            _painter.LineTo(P(d.x, d.y, d.z));
            _painter.ClosePath();
            _painter.Fill();
            _painter.Stroke();
        }

        private void QuadStroke(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            _painter.BeginPath();
            _painter.MoveTo(P(a.x, a.y, a.z));
            _painter.LineTo(P(b.x, b.y, b.z));
            _painter.LineTo(P(c.x, c.y, c.z));
            _painter.LineTo(P(d.x, d.y, d.z));
            _painter.ClosePath();
            _painter.Stroke();
        }

        private void Segment(Vector2 a, Vector2 b)
        {
            _painter.BeginPath();
            _painter.MoveTo(a);
            _painter.LineTo(b);
            _painter.Stroke();
        }

        private Vector2 P(float x, float y, float z)
        {
            Vector2 uv = ProjectUv(new Vector3(x, y, z));
            return new Vector2(_frame.x + uv.x * _frame.width, _frame.y + (1f - uv.y) * _frame.height);
        }

        private static Vector2 ProjectUv(Vector3 point)
        {
            Vector3 d = point - CamPos;
            float cx = Vector3.Dot(d, CamRight);
            float cy = Vector3.Dot(d, CamUp);
            float cz = Mathf.Max(0.1f, Vector3.Dot(d, CamFwd));
            float u = 0.5f + cx / (cz * TanHalfFov * Aspect) * 0.5f;
            float v = 0.5f + cy / (cz * TanHalfFov) * 0.5f;
            return new Vector2(u, v);
        }
    }
}

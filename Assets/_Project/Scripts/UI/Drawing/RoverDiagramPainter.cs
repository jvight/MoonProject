using System;
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
    /// and rear antenna with sonar loop. Socket anchor positions come from <see cref="RoverDiagramData"/> (extracted at
    /// build time from <c>RoverModel.prefab</c>) and camera framing comes from <see cref="CargoScreenSettings"/>.
    /// </summary>
    internal sealed class RoverDiagramPainter : StylePainter
    {
        public const float Aspect = 1.5f;
        private const float MaxFrameWidthShare = 0.77f;

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

        private readonly RoverDiagramData _data;
        private readonly CargoScreenSettings _settings;
        private Color _lineColor = new Color(1f, 0.71f, 0.28f, 0.95f);
        private Color _brightColor = new Color(1f, 0.84f, 0.50f, 1f);
        private Color _dimColor = new Color(1f, 0.71f, 0.28f, 0.52f);
        private Color _fillColor = new Color(0.055f, 0.035f, 0.02f, 0.96f);
        private Color _gridColor = new Color(0.37f, 0.95f, 1f, 0.22f);
        private Vector3 _wheelFL;
        private Vector3 _wheelFR;
        private Vector3 _wheelRL;
        private Vector3 _wheelRR;
        private float _wheelHalfW;
        private float _wheelR;
        private float _bodyHalfX;
        private float _bodyBotY;
        private float _bodyMidY;
        private float _bodyTopY;
        private float _bodyRearZ;
        private float _bodyFrontZ;
        private float _cachedYaw = float.NaN;
        private float _cachedElevation = float.NaN;
        private float _cachedDistance = float.NaN;
        private float _cachedLookHeight = float.NaN;
        private float _cachedFov = float.NaN;
        private Vector3 _camPos;
        private Vector3 _camRight;
        private Vector3 _camUp;
        private Vector3 _camFwd;
        private float _tanHalfFov;
        private bool _measuringBounds;
        private float _rawUMin;
        private float _rawUMax;
        private float _rawVMin;
        private float _rawVMax;
        private float _uShift;
        private float _vShift;
        private float _uMin;
        private float _uMax;
        private float _vMin;
        private float _vMax;
        private Painter2D _painter;
        private Rect _frame;

        public RoverDiagramPainter(VisualElement element, RoverDiagramData data, CargoScreenSettings settings)
            : base(element)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            CacheModelExtents();
            EnsureFraming();
        }

        /// <summary>Viewport coordinates (0..1, bottom-left origin) for <paramref name="socket"/> on 07 v3.</summary>
        public Vector2 SocketViewport(RoverSocket socket)
        {
            EnsureFraming();
            return ProjectUv(_data.SocketPosition(socket));
        }

        /// <summary>Pixel bounding box inside <paramref name="box"/> of the rendered 07 schematic strokes.</summary>
        public Rect DrawingBounds(Rect box)
        {
            EnsureFraming();
            Rect frame = FrameRect(box);
            float xMin = frame.x + _uMin * frame.width;
            float xMax = frame.x + _uMax * frame.width;
            float yMin = frame.y + (1f - _vMax) * frame.height;
            float yMax = frame.y + (1f - _vMin) * frame.height;
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        /// <summary>Aspect-fitted 3:2 frame inside <paramref name="box"/> where the diagram and sockets live.</summary>
        public static Rect FrameRect(Rect box)
        {
            float width = Mathf.Min(box.width * MaxFrameWidthShare, box.height * Aspect);
            float height = width / Aspect;
            return new Rect(box.x + (box.width - width) * 0.5f, box.y + (box.height - height) * 0.5f, width, height);
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

            EnsureFraming();
            _frame = FrameRect(box);
            _painter = context.painter2D;
            _painter.lineCap = LineCap.Round;
            _painter.lineJoin = LineJoin.Round;
            PaintScene();
            _painter = null;
        }

        private void PaintScene()
        {
            PaintGridAndWheelPair(false);
            PaintUndercarriageAndScoop();
            PaintCarapaceAndRear();
            PaintFrontRigAndRoof();
            PaintHeadAndAntenna();
            PaintGridAndWheelPair(true);
        }

        private void CacheModelExtents()
        {
            _wheelFL = _data.WheelPosition(0);
            _wheelFR = _data.WheelPosition(1);
            _wheelRL = _data.WheelPosition(2);
            _wheelRR = _data.WheelPosition(3);
            Vector3 wheelsSock = _data.SocketPosition(RoverSocket.Wheels);
            Vector3 tether = _data.SocketPosition(RoverSocket.Tether);
            Vector3 lamp = _data.SocketPosition(RoverSocket.Lamp);
            Vector3 flanks = _data.SocketPosition(RoverSocket.Flanks);
            Vector3 cutter = _data.SocketPosition(RoverSocket.Cutter);
            Vector3 wing = _data.SocketPosition(RoverSocket.Wing);
            Vector3 hold = _data.SocketPosition(RoverSocket.Hold);

            _wheelHalfW = Mathf.Max(0.08f, Mathf.Abs(wheelsSock.x - _wheelRR.x));
            _wheelR = Mathf.Max(0.20f, _wheelFR.y * 0.97f);
            _bodyHalfX = flanks.x;
            _bodyBotY = tether.y;
            _bodyMidY = cutter.y - 0.01f;
            _bodyTopY = wing.y - 0.02f;
            _bodyRearZ = hold.z;
            _bodyFrontZ = lamp.z - 0.02f;
        }

        private void EnsureFraming()
        {
            float yaw = _settings.DiagramYaw;
            float elevation = _settings.DiagramElevation;
            float distance = _settings.DiagramDistance;
            float lookHeight = _settings.DiagramLookHeight;
            float fov = _settings.DiagramFieldOfView;
            if (yaw == _cachedYaw && elevation == _cachedElevation && distance == _cachedDistance &&
                lookHeight == _cachedLookHeight && fov == _cachedFov)
            {
                return;
            }

            _cachedYaw = yaw;
            _cachedElevation = elevation;
            _cachedDistance = distance;
            _cachedLookHeight = lookHeight;
            _cachedFov = fov;
            float yawRad = yaw * Mathf.Deg2Rad;
            float elevRad = elevation * Mathf.Deg2Rad;
            float cosElev = Mathf.Cos(elevRad);
            var dir = new Vector3(Mathf.Sin(yawRad) * cosElev, Mathf.Sin(elevRad), Mathf.Cos(yawRad) * cosElev);
            _camPos = new Vector3(0f, lookHeight, 0f) + dir * distance;
            _camFwd = -dir;
            _camRight = new Vector3(-Mathf.Cos(yawRad), 0f, Mathf.Sin(yawRad));
            _camUp = Vector3.Cross(_camFwd, _camRight);
            _tanHalfFov = Mathf.Max(0.05f, Mathf.Tan(0.5f * fov * Mathf.Deg2Rad));

            _rawUMin = float.MaxValue;
            _rawUMax = float.MinValue;
            _rawVMin = float.MaxValue;
            _rawVMax = float.MinValue;
            _uShift = 0f;
            _vShift = 0f;
            _measuringBounds = true;
            PaintScene();
            _measuringBounds = false;
            _uShift = 0.5f - (_rawUMin + _rawUMax) * 0.5f;
            _vShift = 0.5f - (_rawVMin + _rawVMax) * 0.5f;
            _uMin = _rawUMin + _uShift;
            _uMax = _rawUMax + _uShift;
            _vMin = _rawVMin + _vShift;
            _vMax = _rawVMax + _vShift;
        }

        private void PaintGridAndWheelPair(bool nearSide)
        {
            if (!nearSide)
            {
                PaintWheel(_wheelRL, false);
                PaintWheel(_wheelFL, false);
                return;
            }

            PaintWheel(_wheelRR, true);
            PaintWheel(_wheelFR, true);
        }

        private void PaintWheel(Vector3 center, bool near)
        {
            float halfW = _wheelHalfW;
            float r = _wheelR;
            float xOut = center.x + (center.x > 0f ? halfW : -halfW);
            float xIn = center.x - (center.x > 0f ? halfW : -halfW);

            if (_painter != null)
            {
                _painter.fillColor = _fillColor;
                _painter.strokeColor = near ? _lineColor : _dimColor;
                _painter.lineWidth = near ? 2f : 1.4f;
            }

            PaintWheelRing(new Vector3(xIn, center.y, center.z), r, true);
            PaintWheelTreads(center, r);
            PaintWheelRing(new Vector3(xOut, center.y, center.z), r, true);
            PaintWheelRing(new Vector3(xOut, center.y, center.z), r * 0.55f, false);
            PaintWheelRing(new Vector3(xOut, center.y, center.z), r * 0.22f, false);
        }

        private void PaintWheelRing(Vector3 faceCenter, float radius, bool fill)
        {
            const int steps = 16;
            if (_painter != null)
            {
                _painter.BeginPath();
            }

            for (int i = 0; i <= steps; i++)
            {
                float a = i * (Mathf.PI * 2f / steps);
                Vector2 pt = P(faceCenter.x, faceCenter.y + Mathf.Sin(a) * radius,
                    faceCenter.z + Mathf.Cos(a) * radius);
                if (_painter == null)
                {
                    continue;
                }

                if (i == 0)
                {
                    _painter.MoveTo(pt);
                }
                else
                {
                    _painter.LineTo(pt);
                }
            }

            if (_painter == null)
            {
                return;
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
            float x0 = center.x - _wheelHalfW;
            float x1 = center.x + _wheelHalfW;
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
            Vector3 pannier = _data.SocketPosition(RoverSocket.Panniers);
            Vector3 flanks = _data.SocketPosition(RoverSocket.Flanks);
            Vector3 belly = _data.SocketPosition(RoverSocket.Belly);
            Vector3 tether = _data.SocketPosition(RoverSocket.Tether);
            Vector3 hold = _data.SocketPosition(RoverSocket.Hold);
            Vector3 cutter = _data.SocketPosition(RoverSocket.Cutter);

            if (_painter != null)
            {
                _painter.fillColor = _fillColor;
                _painter.strokeColor = _dimColor;
                _painter.lineWidth = 1.6f;
            }

            // Left-flank pannier bin (BinSocket_L) & undercarriage belly plate (BellySocket).
            float binOutX = pannier.x - _wheelHalfW * 0.67f;
            float binInX = pannier.x;
            float binBotY = pannier.y - 0.10f;
            float binTopY = pannier.y + 0.06f;
            float binRearZ = flanks.z;
            float binFrontZ = pannier.z + (pannier.z - flanks.z);
            QuadFill(new Vector3(binOutX, binBotY, binRearZ), new Vector3(binOutX, binBotY, binFrontZ),
                new Vector3(binInX, binTopY, binFrontZ), new Vector3(binInX, binTopY, binRearZ));

            float bellyHalfX = _bodyHalfX * 0.74f;
            float bellyY = belly.y;
            float bellyHalfZ = _wheelFR.z * 0.83f;
            QuadFill(new Vector3(-bellyHalfX, bellyY, -bellyHalfZ), new Vector3(bellyHalfX, bellyY, -bellyHalfZ),
                new Vector3(bellyHalfX, bellyY, bellyHalfZ), new Vector3(-bellyHalfX, bellyY, bellyHalfZ));

            // Suspension arms & struts to right wheels + rear tow hook (TetherSocket).
            float strutInX = _bodyHalfX * 0.78f;
            float strutInY = hold.y - 0.02f;
            float strutInZ = _wheelFR.z * 0.79f;
            float wheelHubX = _wheelFR.x - _wheelHalfW * 0.42f;
            Segment(P(strutInX, strutInY, strutInZ), P(wheelHubX, _wheelFR.y, _wheelFR.z));
            Segment(P(strutInX, strutInY, -strutInZ), P(wheelHubX, _wheelRR.y, _wheelRR.z));
            if (_painter != null)
            {
                _painter.strokeColor = _lineColor;
            }

            Segment(P(tether.x, tether.y + 0.04f, _bodyRearZ + 0.04f),
                P(tether.x, tether.y - 0.04f, tether.z - 0.06f));
            Segment(P(tether.x, tether.y - 0.04f, tether.z - 0.06f),
                P(tether.x, tether.y + 0.03f, tether.z - 0.08f));

            // Front dig scoop blade, hydraulic arms & 5 cutting teeth.
            float armOutX = _bodyHalfX * 0.96f;
            float scoopTopY = _bodyBotY + 0.04f;
            float scoopBotY = belly.y - 0.08f;
            float scoopRearZ = cutter.z + 0.02f;
            float scoopFrontZ = cutter.z + _wheelR * 0.53f;
            Segment(P(strutInX, scoopTopY + 0.02f, _bodyFrontZ - 0.06f),
                P(armOutX, belly.y + 0.02f, scoopRearZ + 0.04f));
            Segment(P(-strutInX, scoopTopY + 0.02f, _bodyFrontZ - 0.06f),
                P(-armOutX, belly.y + 0.02f, scoopRearZ + 0.04f));
            float bladeTopX = _bodyHalfX * 1.09f;
            float bladeBotX = _bodyHalfX * 1.17f;
            QuadFill(new Vector3(-bladeTopX, scoopTopY, scoopRearZ), new Vector3(bladeTopX, scoopTopY, scoopRearZ),
                new Vector3(bladeBotX, scoopBotY, scoopFrontZ), new Vector3(-bladeBotX, scoopBotY, scoopFrontZ));
            float toothStepX = _bodyHalfX * 0.41f;
            float toothHalfW = _wheelHalfW * 0.42f;
            float toothTipY = scoopBotY - 0.04f;
            float toothTipZ = scoopFrontZ + 0.06f;
            for (int t = -2; t <= 2; t++)
            {
                float tx = t * toothStepX;
                Segment(P(tx - toothHalfW, scoopBotY, scoopFrontZ), P(tx, toothTipY, toothTipZ));
                Segment(P(tx, toothTipY, toothTipZ), P(tx + toothHalfW, scoopBotY, scoopFrontZ));
            }
        }

        private void PaintCarapaceAndRear()
        {
            Vector3 pannier = _data.SocketPosition(RoverSocket.Panniers);
            Vector3 flanks = _data.SocketPosition(RoverSocket.Flanks);
            Vector3 hold = _data.SocketPosition(RoverSocket.Hold);

            if (_painter != null)
            {
                _painter.fillColor = _fillColor;
                _painter.strokeColor = _lineColor;
                _painter.lineWidth = 2.1f;
            }

            // Lower chassis tub & upper beetle carapace dome derived from socket/body extents.
            float midHalfX = _bodyHalfX - _wheelHalfW * 0.16f;
            float topHalfX = _bodyHalfX * 0.83f;
            float midRearZ = _bodyRearZ + 0.06f;
            float midFrontZ = _bodyFrontZ - 0.04f;
            float topFrontZ = _wheelFR.z * 0.80f;
            float noseTopY = _bodyTopY - 0.04f;
            QuadFill(new Vector3(-_bodyHalfX, _bodyBotY, _bodyRearZ), new Vector3(_bodyHalfX, _bodyBotY, _bodyRearZ),
                new Vector3(_bodyHalfX, _bodyMidY, midRearZ), new Vector3(-_bodyHalfX, _bodyMidY, midRearZ));
            QuadFill(new Vector3(-_bodyHalfX, _bodyBotY, _bodyFrontZ), new Vector3(_bodyHalfX, _bodyBotY, _bodyFrontZ),
                new Vector3(midHalfX, _bodyMidY, midFrontZ), new Vector3(-midHalfX, _bodyMidY, midFrontZ));
            QuadFill(new Vector3(_bodyHalfX, _bodyBotY, _bodyRearZ), new Vector3(_bodyHalfX, _bodyBotY, _bodyFrontZ),
                new Vector3(midHalfX, _bodyMidY, midFrontZ), new Vector3(midHalfX, _bodyMidY, midRearZ));
            QuadFill(new Vector3(-midHalfX, _bodyMidY, midRearZ), new Vector3(midHalfX, _bodyMidY, midRearZ),
                new Vector3(topHalfX, _bodyTopY, topFrontZ), new Vector3(-topHalfX, _bodyTopY, topFrontZ));
            QuadFill(new Vector3(-midHalfX, _bodyMidY, midFrontZ), new Vector3(midHalfX, _bodyMidY, midFrontZ),
                new Vector3(topHalfX, noseTopY, topFrontZ), new Vector3(-topHalfX, noseTopY, topFrontZ));

            // Beltline & right-flank pannier bin (BinSocket_R / PanniersSocket) + capacitor drum (FlanksSocket).
            if (_painter != null)
            {
                _painter.lineWidth = 1.5f;
            }

            float beltX = _bodyHalfX + 0.01f;
            float beltY = pannier.y + 0.02f;
            Segment(P(beltX, beltY, _bodyRearZ + 0.02f), P(beltX, beltY, _bodyFrontZ - 0.02f));
            float binOutX = -pannier.x + _wheelHalfW * 0.67f;
            float binBotY = pannier.y - 0.10f;
            float binTopY = pannier.y + 0.06f;
            float binRearZ = flanks.z;
            float binFrontZ = pannier.z + (pannier.z - flanks.z);
            QuadFill(new Vector3(beltX, binBotY, binRearZ), new Vector3(beltX, binBotY, binFrontZ),
                new Vector3(binOutX, binTopY, binFrontZ), new Vector3(binOutX, binTopY, binRearZ));
            QuadFill(new Vector3(_bodyHalfX - 0.01f, flanks.y - 0.10f, flanks.z - 0.18f),
                new Vector3(_bodyHalfX - 0.01f, flanks.y - 0.10f, flanks.z + 0.10f),
                new Vector3(_bodyHalfX + 0.06f, flanks.y + 0.02f, flanks.z + 0.10f),
                new Vector3(_bodyHalfX + 0.06f, flanks.y + 0.02f, flanks.z - 0.18f));

            // Rear clamshell hatch (HoldSocket) & 8-segment battery bar along rear lip.
            float hatchHalfX = _bodyHalfX * 0.74f;
            float hatchTopX = _bodyHalfX * 0.70f;
            QuadStroke(new Vector3(-hatchHalfX, hold.y - 0.04f, _bodyRearZ - 0.01f),
                new Vector3(hatchHalfX, hold.y - 0.04f, _bodyRearZ - 0.01f),
                new Vector3(hatchTopX, _bodyMidY - 0.02f, _bodyRearZ + 0.02f),
                new Vector3(-hatchTopX, _bodyMidY - 0.02f, _bodyRearZ + 0.02f));
            if (_painter != null)
            {
                _painter.strokeColor = _brightColor;
            }

            float barY = _bodyMidY + 0.03f;
            float barZ = _bodyRearZ + 0.04f;
            float barStart = -_bodyHalfX * 0.65f;
            float barStep = (_bodyHalfX * 1.30f) / 7.65f;
            for (int b = 0; b < 8; b++)
            {
                float bx = barStart + b * barStep;
                Segment(P(bx, barY, barZ), P(bx + barStep * 0.65f, barY, barZ));
            }
        }

        private void PaintFrontRigAndRoof()
        {
            Vector3 lamp = _data.SocketPosition(RoverSocket.Lamp);
            Vector3 cutter = _data.SocketPosition(RoverSocket.Cutter);
            Vector3 wing = _data.SocketPosition(RoverSocket.Wing);
            Vector3 back = _data.SocketPosition(RoverSocket.Back);

            // Twin front headlamps & lamp bar (LampSocket) + chest cutter turret (CutterSocket).
            if (_painter != null)
            {
                _painter.strokeColor = _brightColor;
                _painter.lineWidth = 1.8f;
            }

            float lampSpanX = _bodyHalfX * 0.39f;
            PaintHeadlamp(new Vector3(-lampSpanX, lamp.y - 0.01f, lamp.z + 0.02f));
            PaintHeadlamp(new Vector3(lampSpanX, lamp.y - 0.01f, lamp.z + 0.02f));
            Segment(P(-lampSpanX, lamp.y, lamp.z), P(lampSpanX, lamp.y, lamp.z));
            float turretRearX = _bodyHalfX * 0.24f;
            float turretFrontX = _bodyHalfX * 0.20f;
            QuadFill(new Vector3(-turretRearX, cutter.y - 0.03f, _bodyFrontZ - 0.08f),
                new Vector3(turretRearX, cutter.y - 0.03f, _bodyFrontZ - 0.08f),
                new Vector3(turretFrontX, cutter.y + 0.09f, cutter.z),
                new Vector3(-turretFrontX, cutter.y + 0.09f, cutter.z));
            Segment(P(cutter.x, cutter.y, cutter.z), P(cutter.x, cutter.y, cutter.z + 0.10f));

            // Roof solar wing (WingSocket) & rear spine (BackSocket).
            if (_painter != null)
            {
                _painter.strokeColor = _lineColor;
                _painter.lineWidth = 1.7f;
            }

            float wingHalfX = _bodyHalfX * 0.74f;
            float wingRearZ = wing.z - 0.16f;
            float wingFrontZ = back.z + 0.02f;
            QuadFill(new Vector3(-wingHalfX, wing.y, wingRearZ), new Vector3(wingHalfX, wing.y, wingRearZ),
                new Vector3(wingHalfX, wing.y, wingFrontZ), new Vector3(-wingHalfX, wing.y, wingFrontZ));
            if (_painter != null)
            {
                _painter.strokeColor = _dimColor;
                _painter.lineWidth = 1.1f;
            }

            float wingThirdX = wingHalfX / 3f;
            float wingMidZ = (wingRearZ + wingFrontZ) * 0.5f;
            Segment(P(-wingThirdX, wing.y + 0.005f, wingRearZ), P(-wingThirdX, wing.y + 0.005f, wingFrontZ));
            Segment(P(wingThirdX, wing.y + 0.005f, wingRearZ), P(wingThirdX, wing.y + 0.005f, wingFrontZ));
            Segment(P(-wingHalfX, wing.y + 0.005f, wingMidZ), P(wingHalfX, wing.y + 0.005f, wingMidZ));
        }

        private void PaintHeadAndAntenna()
        {
            Vector3 antenna = _data.SocketPosition(RoverSocket.Antenna);
            Vector3 wing = _data.SocketPosition(RoverSocket.Wing);
            Vector3 deck = _data.SocketPosition(RoverSocket.Deck);

            // Rear whip antenna & diamond sonar loop (AntennaSocket).
            if (_painter != null)
            {
                _painter.strokeColor = _lineColor;
                _painter.lineWidth = 1.7f;
            }

            float antBaseY = antenna.y - 0.03f;
            float antTipY = antenna.y + _wheelR * 1.56f;
            Segment(P(antenna.x, antBaseY, antenna.z), P(antenna.x, antTipY, antenna.z - 0.02f));
            float loopMidY = (antBaseY + antTipY) * 0.5f + 0.08f;
            float loopSpan = _wheelHalfW * 0.58f;
            QuadStroke(new Vector3(antenna.x, loopMidY - 0.08f, antenna.z - 0.01f),
                new Vector3(antenna.x + loopSpan, loopMidY, antenna.z - 0.01f),
                new Vector3(antenna.x, loopMidY + 0.08f, antenna.z - 0.01f),
                new Vector3(antenna.x - loopSpan, loopMidY, antenna.z - 0.01f));

            // Ribbed neck, left cheek lens pod & CRT monitor head box derived from DeckSocket & WingSocket.
            float neckHalfX = deck.x * 0.25f;
            for (int r = 0; r < 3; r++)
            {
                float ny = wing.y - 0.01f + r * 0.045f;
                Segment(P(-neckHalfX, ny, deck.z), P(neckHalfX, ny, deck.z));
            }

            float headHalfX = deck.x - _wheelHalfW * 0.78f;
            float podOutX = deck.x + _wheelHalfW * 0.21f;
            float headBotY = deck.y - _wheelR * 0.47f;
            float headTopY = deck.y + _wheelR * 0.53f;
            float headRearZ = deck.z - _wheelR * 0.53f;
            float headFrontZ = deck.z + _wheelR * 0.53f;
            QuadFill(new Vector3(-podOutX, deck.y - 0.07f, deck.z - 0.08f),
                new Vector3(-headHalfX, deck.y - 0.07f, deck.z - 0.08f),
                new Vector3(-headHalfX, deck.y + 0.07f, deck.z + 0.10f),
                new Vector3(-podOutX, deck.y + 0.07f, deck.z + 0.10f));
            if (_painter != null)
            {
                _painter.lineWidth = 2.1f;
            }

            QuadFill(new Vector3(-headHalfX, headBotY, headRearZ), new Vector3(headHalfX, headBotY, headRearZ),
                new Vector3(headHalfX, headTopY, headRearZ), new Vector3(-headHalfX, headTopY, headRearZ));
            QuadFill(new Vector3(headHalfX, headBotY, headRearZ), new Vector3(headHalfX, headBotY, headFrontZ),
                new Vector3(headHalfX, headTopY, headFrontZ), new Vector3(headHalfX, headTopY, headRearZ));
            QuadFill(new Vector3(-headHalfX, headBotY, headFrontZ), new Vector3(headHalfX, headBotY, headFrontZ),
                new Vector3(headHalfX, headTopY, headFrontZ), new Vector3(-headHalfX, headTopY, headFrontZ));

            // CRT screen bezel, friendly amber eyes & right-cheek tape deck pod (DeckSocket).
            if (_painter != null)
            {
                _painter.strokeColor = _brightColor;
                _painter.lineWidth = 1.7f;
            }

            float screenHalfX = headHalfX * 0.78f;
            float screenZ = headFrontZ + 0.01f;
            QuadStroke(new Vector3(-screenHalfX, headBotY + 0.06f, screenZ),
                new Vector3(screenHalfX, headBotY + 0.06f, screenZ),
                new Vector3(screenHalfX, headTopY - 0.06f, screenZ),
                new Vector3(-screenHalfX, headTopY - 0.06f, screenZ));
            float eyeY = deck.y + 0.02f;
            Segment(P(-screenHalfX * 0.62f, eyeY, screenZ + 0.005f), P(-screenHalfX * 0.19f, eyeY, screenZ + 0.005f));
            Segment(P(screenHalfX * 0.19f, eyeY, screenZ + 0.005f), P(screenHalfX * 0.62f, eyeY, screenZ + 0.005f));
            QuadFill(new Vector3(headHalfX + 0.01f, deck.y - 0.08f, deck.z - 0.11f),
                new Vector3(podOutX, deck.y - 0.08f, deck.z - 0.11f),
                new Vector3(podOutX, deck.y + 0.08f, deck.z + 0.09f),
                new Vector3(headHalfX + 0.01f, deck.y + 0.08f, deck.z + 0.09f));
        }

        private void PaintHeadlamp(Vector3 pos)
        {
            float halfW = _wheelHalfW * 0.38f;
            float halfH = _wheelR * 0.12f;
            Segment(P(pos.x - halfW, pos.y - halfH, pos.z), P(pos.x + halfW, pos.y - halfH, pos.z));
            Segment(P(pos.x - halfW, pos.y + halfH, pos.z), P(pos.x + halfW, pos.y + halfH, pos.z));
        }

        private void QuadFill(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            Vector2 pa = P(a.x, a.y, a.z);
            Vector2 pb = P(b.x, b.y, b.z);
            Vector2 pc = P(c.x, c.y, c.z);
            Vector2 pd = P(d.x, d.y, d.z);
            if (_painter == null)
            {
                return;
            }

            _painter.BeginPath();
            _painter.MoveTo(pa);
            _painter.LineTo(pb);
            _painter.LineTo(pc);
            _painter.LineTo(pd);
            _painter.ClosePath();
            _painter.Fill();
            _painter.Stroke();
        }

        private void QuadStroke(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            Vector2 pa = P(a.x, a.y, a.z);
            Vector2 pb = P(b.x, b.y, b.z);
            Vector2 pc = P(c.x, c.y, c.z);
            Vector2 pd = P(d.x, d.y, d.z);
            if (_painter == null)
            {
                return;
            }

            _painter.BeginPath();
            _painter.MoveTo(pa);
            _painter.LineTo(pb);
            _painter.LineTo(pc);
            _painter.LineTo(pd);
            _painter.ClosePath();
            _painter.Stroke();
        }

        private void Segment(Vector2 a, Vector2 b)
        {
            if (_painter == null)
            {
                return;
            }

            _painter.BeginPath();
            _painter.MoveTo(a);
            _painter.LineTo(b);
            _painter.Stroke();
        }

        private Vector2 P(float x, float y, float z)
        {
            if (_measuringBounds)
            {
                Vector2 raw = RawProjectUv(new Vector3(x, y, z));
                if (raw.x < _rawUMin)
                {
                    _rawUMin = raw.x;
                }

                if (raw.x > _rawUMax)
                {
                    _rawUMax = raw.x;
                }

                if (raw.y < _rawVMin)
                {
                    _rawVMin = raw.y;
                }

                if (raw.y > _rawVMax)
                {
                    _rawVMax = raw.y;
                }

                return Vector2.zero;
            }

            Vector2 uv = ProjectUv(new Vector3(x, y, z));
            return new Vector2(_frame.x + uv.x * _frame.width, _frame.y + (1f - uv.y) * _frame.height);
        }

        private Vector2 ProjectUv(Vector3 point)
        {
            Vector2 raw = RawProjectUv(point);
            return new Vector2(raw.x + _uShift, raw.y + _vShift);
        }

        private Vector2 RawProjectUv(Vector3 point)
        {
            Vector3 d = point - _camPos;
            // _camPos lies in the vertical plane spanned by _camFwd and +Y, so Dot(_camPos, _camRight) == 0.
            float cx = Vector3.Dot(point, _camRight);
            float cy = Vector3.Dot(d, _camUp);
            float cz = Mathf.Max(0.1f, Vector3.Dot(d, _camFwd));
            float u = 0.5f + cx / (cz * _tanHalfFov * Aspect) * 0.5f;
            float v = 0.5f + cy / (cz * _tanHalfFov) * 0.5f;
            return new Vector2(u, v);
        }
    }
}

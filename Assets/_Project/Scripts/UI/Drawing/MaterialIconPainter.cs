using System;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// A salvage material's icon, drawn to match art's bundles (docs/features/M3-13) with a silhouette of its own
    /// (docs/design/first-hour-v2 6.5), sized to its element: Metal is a hex plate with a bolt, Wiring a copper cable
    /// coil on a dark hub, Optics a lens in a steel frame. USS: --icon-color (the material), --icon-detail (the
    /// plate's rim, the coil's inner turn, the lens's frame and glint), --icon-hole-color (bolt, hub).
    /// </summary>
    internal sealed class MaterialIconPainter : StylePainter
    {
        private const float HexRadius = 0.48f;
        private const float HexRimScale = 0.72f;
        private const float RimWidth = 0.07f;
        private const float BoltRadius = 0.11f;
        private const float CoilOuter = 0.4f;
        private const float CoilInner = 0.27f;
        private const float CoilWidth = 0.12f;
        private const float HubRadius = 0.15f;
        private const float TailWidth = 0.09f;
        private const float TailEndX = 0.5f;
        private const float TailEndY = 0.42f;
        private const int HexSides = 6;
        private const float LensRadius = 0.4f;
        private const float LensFrameWidth = 0.1f;
        private const float GlintOffset = 0.14f;
        private const float GlintRadius = 0.07f;

        private static readonly CustomStyleProperty<Color> IconColorProperty =
            new CustomStyleProperty<Color>("--icon-color");
        private static readonly CustomStyleProperty<Color> DetailColorProperty =
            new CustomStyleProperty<Color>("--icon-detail");
        private static readonly CustomStyleProperty<Color> HoleColorProperty =
            new CustomStyleProperty<Color>("--icon-hole-color");

        private readonly SalvageMaterial _material;
        private Color _iconColor = Color.clear;
        private Color _detailColor = Color.clear;
        private Color _holeColor = Color.clear;

        public MaterialIconPainter(VisualElement element, SalvageMaterial material) : base(element)
        {
            _material = material;
        }

        protected override void ReadStyle(ICustomStyle style)
        {
            style.TryGetValue(IconColorProperty, out _iconColor);
            style.TryGetValue(DetailColorProperty, out _detailColor);
            style.TryGetValue(HoleColorProperty, out _holeColor);
        }

        protected override void Paint(MeshGenerationContext context)
        {
            Rect rect = Element.contentRect;
            float size = Mathf.Min(rect.width, rect.height);
            if (size <= 0f)
            {
                return;
            }

            Painter2D painter = context.painter2D;
            Vector2 centre = rect.center;
            switch (_material)
            {
                case SalvageMaterial.Metal:
                    Hex(painter, centre, size);
                    break;
                case SalvageMaterial.Wiring:
                    Coil(painter, centre, size);
                    break;
                case SalvageMaterial.Optics:
                    Lens(painter, centre, size);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(_material), _material, "Unknown salvage material.");
            }
        }

        private void Hex(Painter2D painter, Vector2 centre, float size)
        {
            HexPath(painter, centre, size * HexRadius);
            painter.fillColor = _iconColor;
            painter.Fill();
            painter.lineWidth = size * RimWidth;
            painter.lineJoin = LineJoin.Round;
            painter.strokeColor = _detailColor;
            HexPath(painter, centre, size * HexRadius * HexRimScale);
            painter.Stroke();
            Shapes.Disc(painter, centre, size * BoltRadius, _holeColor);
        }

        private static void HexPath(Painter2D painter, Vector2 centre, float radius)
        {
            painter.BeginPath();
            for (int i = 0; i < HexSides; i++)
            {
                float angle = Mathf.PI * 2f * i / HexSides;
                var corner = new Vector2(centre.x + Mathf.Cos(angle) * radius, centre.y + Mathf.Sin(angle) * radius);
                if (i == 0)
                {
                    painter.MoveTo(corner);
                }
                else
                {
                    painter.LineTo(corner);
                }
            }

            painter.ClosePath();
        }

        private void Coil(Painter2D painter, Vector2 centre, float size)
        {
            Shapes.Disc(painter, centre, size * HubRadius, _holeColor);
            Shapes.Ring(painter, centre, size * CoilInner, size * CoilWidth, _detailColor);
            Shapes.Ring(painter, centre, size * CoilOuter, size * CoilWidth, _iconColor);
            painter.lineWidth = size * TailWidth;
            painter.lineCap = LineCap.Round;
            painter.strokeColor = _iconColor;
            painter.BeginPath();
            painter.MoveTo(new Vector2(centre.x + size * CoilOuter, centre.y));
            painter.LineTo(new Vector2(centre.x + size * TailEndX, centre.y + size * TailEndY));
            painter.Stroke();
        }

        private void Lens(Painter2D painter, Vector2 centre, float size)
        {
            Shapes.Disc(painter, centre, size * LensRadius, _iconColor);
            Shapes.Ring(painter, centre, size * LensRadius, size * LensFrameWidth, _detailColor);
            Shapes.Disc(painter, new Vector2(centre.x - size * GlintOffset, centre.y - size * GlintOffset),
                size * GlintRadius, _detailColor);
        }
    }
}

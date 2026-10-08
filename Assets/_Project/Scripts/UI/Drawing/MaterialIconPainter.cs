using System;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// A salvage material's icon, drawn to match art's bundles (docs/features/M3-13), sized to its element: Metal is
    /// two riveted steel plates, Wiring a copper cable coil on a dark hub, Optics a violet solar cell in a steel
    /// frame. USS: --icon-color (the material), --icon-detail (plates behind, the coil's inner turn, the cell's frame),
    /// --icon-hole-color (bolts, hub, cell seams).
    /// </summary>
    internal sealed class MaterialIconPainter : StylePainter
    {
        private const float PlateWidth = 0.84f;
        private const float PlateHeight = 0.5f;
        private const float PlateOffset = 0.16f;
        private const float PlateRound = 0.12f;
        private const float BoltRadius = 0.04f;
        private const float BoltInset = 0.12f;
        private const float CoilOuter = 0.4f;
        private const float CoilInner = 0.27f;
        private const float CoilWidth = 0.12f;
        private const float HubRadius = 0.15f;
        private const float TailWidth = 0.09f;
        private const float TailEndX = 0.5f;
        private const float TailEndY = 0.42f;
        private const float CellWidth = 0.8f;
        private const float CellHeight = 0.86f;
        private const float CellRound = 0.1f;
        private const float FrameWidth = 0.08f;
        private const float SeamWidth = 0.05f;

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
                    Plates(painter, centre, size);
                    break;
                case SalvageMaterial.Wiring:
                    Coil(painter, centre, size);
                    break;
                case SalvageMaterial.Optics:
                    Cell(painter, centre, size);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(_material), _material, "Unknown salvage material.");
            }
        }

        private void Plates(Painter2D painter, Vector2 centre, float size)
        {
            var plate = new Vector2(size * PlateWidth, size * PlateHeight);
            float offset = size * PlateOffset;
            Fill(painter, new Rect(centre.x - plate.x * 0.5f, centre.y - plate.y * 0.5f - offset, plate.x, plate.y),
                size * PlateRound, _detailColor);
            var front = new Rect(centre.x - plate.x * 0.5f, centre.y - plate.y * 0.5f + offset * 0.5f, plate.x,
                plate.y);
            Fill(painter, front, size * PlateRound, _iconColor);
            float inset = size * BoltInset;
            float bolt = size * BoltRadius;
            Shapes.Disc(painter, new Vector2(front.xMin + inset, front.yMin + inset), bolt, _holeColor);
            Shapes.Disc(painter, new Vector2(front.xMax - inset, front.yMin + inset), bolt, _holeColor);
            Shapes.Disc(painter, new Vector2(front.xMin + inset, front.yMax - inset), bolt, _holeColor);
            Shapes.Disc(painter, new Vector2(front.xMax - inset, front.yMax - inset), bolt, _holeColor);
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

        private void Cell(Painter2D painter, Vector2 centre, float size)
        {
            var cell = new Rect(centre.x - size * CellWidth * 0.5f, centre.y - size * CellHeight * 0.5f,
                size * CellWidth, size * CellHeight);
            Fill(painter, cell, size * CellRound, _iconColor);
            painter.lineWidth = size * SeamWidth;
            painter.lineCap = LineCap.Butt;
            painter.strokeColor = _holeColor;
            painter.BeginPath();
            painter.MoveTo(new Vector2(cell.center.x, cell.yMin));
            painter.LineTo(new Vector2(cell.center.x, cell.yMax));
            painter.MoveTo(new Vector2(cell.xMin, cell.center.y));
            painter.LineTo(new Vector2(cell.xMax, cell.center.y));
            painter.Stroke();
            painter.lineWidth = size * FrameWidth;
            painter.strokeColor = _detailColor;
            painter.BeginPath();
            Shapes.RoundedRect(painter, cell, size * CellRound);
            painter.Stroke();
        }

        private static void Fill(Painter2D painter, Rect rect, float radius, Color color)
        {
            painter.fillColor = color;
            painter.BeginPath();
            Shapes.RoundedRect(painter, rect, radius);
            painter.Fill();
        }
    }
}

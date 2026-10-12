using System;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// A material's icon, drawn to match art's bundles (docs/features/M3-13) with a silhouette of its own
    /// (docs/design/first-hour-v2 6.5), sized to its element: Metal is a hex plate with a bolt, Wiring a copper cable
    /// coil on a dark hub, Optics a lens in a steel frame; the others (docs/features/M3-27) are a chip, beads, a rock, a
    /// crystal, an ingot, a drop and a ring. USS: --icon-color (the material), --icon-detail (rims, facets, glints),
    /// --icon-hole-color (bolt, hub). A row that shows different materials in turn changes it with
    /// <see cref="SetMaterial"/>.
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

        private static readonly Vector2[] Rock =
        {
            new Vector2(-0.42f, 0.1f), new Vector2(-0.2f, -0.34f), new Vector2(0.26f, -0.38f),
            new Vector2(0.44f, 0.06f), new Vector2(0.12f, 0.4f), new Vector2(-0.3f, 0.32f),
        };

        private static readonly Vector2[] Diamond =
        {
            new Vector2(0f, -0.46f), new Vector2(0.34f, -0.1f), new Vector2(0f, 0.46f), new Vector2(-0.34f, -0.1f),
        };

        private static readonly Vector2[] Gem =
        {
            new Vector2(-0.3f, -0.4f), new Vector2(0.3f, -0.4f), new Vector2(0.46f, -0.04f), new Vector2(0f, 0.44f),
            new Vector2(-0.46f, -0.04f),
        };

        private static readonly Vector2[] IngotTop =
        {
            new Vector2(-0.3f, -0.3f), new Vector2(0.3f, -0.3f), new Vector2(0.44f, -0.06f), new Vector2(-0.44f, -0.06f),
        };

        private static readonly Vector2[] IngotFront =
        {
            new Vector2(-0.44f, -0.06f), new Vector2(0.44f, -0.06f), new Vector2(0.44f, 0.3f), new Vector2(-0.44f, 0.3f),
        };

        private static readonly Vector2[] Drop =
        {
            new Vector2(0f, -0.46f), new Vector2(0.3f, 0.08f), new Vector2(-0.3f, 0.08f),
        };

        private SalvageMaterial _material;
        private Color _iconColor = Color.clear;
        private Color _detailColor = Color.clear;
        private Color _holeColor = Color.clear;

        public MaterialIconPainter(VisualElement element, SalvageMaterial material) : base(element)
        {
            _material = material;
            element.AddToClassList(ModifierClass(material));
        }

        /// <summary>Shows another material: its colours and its shape.</summary>
        public void SetMaterial(SalvageMaterial material)
        {
            if (material == _material)
            {
                return;
            }

            Element.RemoveFromClassList(ModifierClass(_material));
            _material = material;
            Element.AddToClassList(ModifierClass(material));
            Repaint();
        }

        private static string ModifierClass(SalvageMaterial material)
        {
            return MaterialSlots.IconClass + "--" + Materials.ClassSuffix(material);
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
                case SalvageMaterial.CircuitChips:
                    Chip(painter, centre, size);
                    break;
                case SalvageMaterial.OrangeBeads:
                    Beads(painter, centre, size);
                    break;
                case SalvageMaterial.Ilmenite:
                    Poly(painter, Rock, centre, size, _iconColor);
                    Facet(painter, centre, size, new Vector2(-0.2f, -0.34f), new Vector2(0.12f, 0.4f));
                    break;
                case SalvageMaterial.WaterIce:
                case SalvageMaterial.Crystal:
                    Poly(painter, Diamond, centre, size, _iconColor);
                    Facet(painter, centre, size, new Vector2(0f, -0.46f), new Vector2(0f, 0.46f));
                    Facet(painter, centre, size, new Vector2(-0.34f, -0.1f), new Vector2(0.34f, -0.1f));
                    break;
                case SalvageMaterial.RareEarths:
                    Poly(painter, Gem, centre, size, _iconColor);
                    Facet(painter, centre, size, new Vector2(-0.46f, -0.04f), new Vector2(0.46f, -0.04f));
                    Facet(painter, centre, size, new Vector2(0f, -0.04f), new Vector2(0f, 0.44f));
                    break;
                case SalvageMaterial.Glass:
                    Shapes.Disc(painter, centre, size * LensRadius, _iconColor);
                    Shapes.Disc(painter, new Vector2(centre.x - size * GlintOffset, centre.y - size * GlintOffset),
                        size * GlintRadius * 1.6f, _detailColor);
                    break;
                case SalvageMaterial.Alloy:
                    Poly(painter, IngotFront, centre, size, _iconColor);
                    Poly(painter, IngotTop, centre, size, _detailColor);
                    break;
                case SalvageMaterial.Coolant:
                    Shapes.Disc(painter, new Vector2(centre.x, centre.y + size * 0.14f), size * 0.3f, _iconColor);
                    Poly(painter, Drop, centre, size, _iconColor);
                    Shapes.Disc(painter, new Vector2(centre.x - size * 0.1f, centre.y + size * 0.08f),
                        size * GlintRadius, _detailColor);
                    break;
                case SalvageMaterial.Superconductor:
                    Shapes.Ring(painter, centre, size * LensRadius, size * CoilWidth, _iconColor);
                    Shapes.Ring(painter, centre, size * CoilInner * 0.7f, size * CoilWidth * 0.6f, _detailColor);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(_material), _material, "Unknown salvage material.");
            }
        }

        private static void Poly(Painter2D painter, Vector2[] unit, Vector2 centre, float size, Color color)
        {
            painter.BeginPath();
            for (int i = 0; i < unit.Length; i++)
            {
                Vector2 corner = centre + unit[i] * size;
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
            painter.fillColor = color;
            painter.Fill();
        }

        private void Facet(Painter2D painter, Vector2 centre, float size, Vector2 from, Vector2 to)
        {
            painter.lineWidth = size * RimWidth * 0.7f;
            painter.lineCap = LineCap.Round;
            painter.strokeColor = _detailColor;
            painter.BeginPath();
            painter.MoveTo(centre + from * size);
            painter.LineTo(centre + to * size);
            painter.Stroke();
        }

        private void Chip(Painter2D painter, Vector2 centre, float size)
        {
            float half = size * 0.3f;
            painter.lineWidth = size * RimWidth * 1.3f;
            painter.lineCap = LineCap.Round;
            painter.strokeColor = _detailColor;
            for (int i = -1; i <= 1; i++)
            {
                float y = centre.y + i * size * 0.2f;
                painter.BeginPath();
                painter.MoveTo(new Vector2(centre.x - size * 0.46f, y));
                painter.LineTo(new Vector2(centre.x + size * 0.46f, y));
                painter.Stroke();
            }

            painter.BeginPath();
            Shapes.RoundedRect(painter, new Rect(centre.x - half, centre.y - half, half * 2f, half * 2f), size * 0.06f);
            painter.fillColor = _iconColor;
            painter.Fill();
            Shapes.Disc(painter, new Vector2(centre.x - half * 0.45f, centre.y - half * 0.45f), size * GlintRadius,
                _detailColor);
        }

        private void Beads(Painter2D painter, Vector2 centre, float size)
        {
            float r = size * 0.21f;
            Vector2[] places =
            {
                new Vector2(-0.2f, 0.16f), new Vector2(0.2f, 0.16f), new Vector2(0f, -0.2f),
            };
            for (int i = 0; i < places.Length; i++)
            {
                Vector2 at = centre + places[i] * size;
                Shapes.Disc(painter, at, r, _iconColor);
                Shapes.Disc(painter, at + new Vector2(-r * 0.35f, -r * 0.35f), r * 0.32f, _detailColor);
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

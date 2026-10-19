using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace MoonProject.UI
{
    /// <summary>
    /// The tube's dark over the part of a face page not yet drawn in (<see cref="PageFlip.Drawn"/>): a solid body below
    /// a soft scan edge that fades from clear to dark, so the new page appears from the top as a CRT draws it. It sits
    /// over the page and under the tabs; at rest it is gone.
    /// </summary>
    internal sealed class PageShutter
    {
        private const int EdgeTexels = 32;

        // The tube's dark (the face texture's own black, which the CRT shows as its glass): no glow, no flash.
        private static readonly Color32 TubeDark = new Color32(19, 18, 16, 255);

        private readonly DatabankSettings _settings;
        private readonly VisualElement _edge;
        private readonly VisualElement _body;
        private float _scale = 1f;

        public PageShutter(VisualElement parent, DatabankSettings settings)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _edge = new VisualElement { name = "page-shutter-edge", pickingMode = PickingMode.Ignore };
            _edge.style.position = Position.Absolute;
            _edge.style.backgroundImage = new StyleBackground(MakeRamp());
            _edge.style.backgroundSize =
                new StyleBackgroundSize(new BackgroundSize(Length.Percent(100f), Length.Percent(100f)));
            _body = new VisualElement { name = "page-shutter-body", pickingMode = PickingMode.Ignore };
            _body.style.position = Position.Absolute;
            _body.style.backgroundColor = (Color)TubeDark;
            parent.Add(_edge);
            parent.Add(_body);
            Draw(1f);
        }

        /// <summary>The shutter's elements, which a page change never fades or hides.</summary>
        public VisualElement Edge => _edge;

        public VisualElement Body => _body;

        public void Layout(float scale)
        {
            _scale = scale;
        }

        /// <summary>Covers the page below <paramref name="drawn"/> (share drawn from the top; 1 hides the shutter).</summary>
        public void Draw(float drawn)
        {
            bool shown = drawn < 1f;
            _edge.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
            _body.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
            if (!shown)
            {
                return;
            }

            float height = FaceInventoryView.ReferenceHeight;
            float edge = _settings.FlipEdgePixels;
            float edgeTop = drawn * (height + edge) - edge;
            Place(_edge, edgeTop, edge);
            Place(_body, edgeTop + edge, height - edgeTop - edge);
        }

        /// <summary>Keeps the shutter over the page that was just put on the tube.</summary>
        public void BringToFront()
        {
            _edge.BringToFront();
            _body.BringToFront();
        }

        private void Place(VisualElement element, float top, float height)
        {
            element.style.left = 0f;
            element.style.width = FaceInventoryView.ReferenceWidth * _scale;
            element.style.top = top * _scale;
            element.style.height = Mathf.Max(0f, height) * _scale;
        }

        /// <summary>A 1-wide ramp from clear at the top to the tube's dark at the bottom, eased.</summary>
        private static Texture2D MakeRamp()
        {
            var texture = new Texture2D(1, EdgeTexels, TextureFormat.RGBA32, false)
            {
                name = "PageShutterEdge",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var pixels = new Color32[EdgeTexels];
            for (int i = 0; i < EdgeTexels; i++)
            {
                // Row 0 is the texture's bottom: the dark end of the ramp.
                float clear = Mathf.SmoothStep(0f, 1f, i / (EdgeTexels - 1f));
                pixels[i] = new Color32(TubeDark.r, TubeDark.g, TubeDark.b, (byte)Mathf.RoundToInt(255f * (1f - clear)));
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }
    }
}

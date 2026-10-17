using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace MoonProject.UI
{
    /// <summary>
    /// The safe-area root (docs/features/M3-24 part A): keeps the HUD and the title and pause pages
    /// in a frame no wider than the tuned shape and inside the device's safe area, so nothing sits out in the corner
    /// of an ultra-wide screen or under a notch. The world anchors, the veils and the curtain stay full-screen. The
    /// insets are written when the panel's size changes, never per frame.
    /// </summary>
    internal sealed class SafeFrame
    {
        private readonly SafeFrameSettings _settings;
        private readonly VisualElement _root;
        private readonly VisualElement _hudFrame;
        private readonly VisualElement _menuFrame;
        private Vector2 _written = new Vector2(float.NaN, float.NaN);

        public SafeFrame(UiLayout layout, SafeFrameSettings settings)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _root = layout.Root;
            _hudFrame = layout.HudFrame;
            _menuFrame = layout.MenuFrame;
            _root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            Apply();
        }

        /// <summary>
        /// The insets of a frame no wider than <paramref name="maxAspect"/> (width / height) in a panel of this size,
        /// pulled in further where the device's safe area is. <paramref name="deviceSafe"/> is the safe area as shares
        /// of the screen (x, y, width, height, from the bottom left, like <see cref="Screen.safeArea"/> / the screen).
        /// <paramref name="margin"/> is then kept clear on every side.
        /// </summary>
        internal static FrameInsets Compute(float width, float height, float maxAspect, Rect deviceSafe,
            float margin)
        {
            if (width <= 0f || height <= 0f)
            {
                return new FrameInsets(0f, 0f, 0f, 0f);
            }

            float side = Mathf.Max(0f, (width - height * maxAspect) * 0.5f);
            float left = Mathf.Max(side, deviceSafe.xMin * width) + margin;
            float right = Mathf.Max(side, (1f - deviceSafe.xMax) * width) + margin;
            float bottom = deviceSafe.yMin * height + margin;
            float top = (1f - deviceSafe.yMax) * height + margin;
            return new FrameInsets(left, top, right, bottom);
        }

        private void OnGeometryChanged(GeometryChangedEvent change)
        {
            Apply();
        }

        private void Apply()
        {
            Vector2 size = _root.layout.size;
            if (float.IsNaN(size.x) || size.x <= 0f || size.y <= 0f || size == _written)
            {
                return;
            }

            _written = size;
            Rect device = DeviceSafeShare();
            Place(_hudFrame, Compute(size.x, size.y, _settings.HudAspect, device, _settings.EdgeMargin));
            Place(_menuFrame, Compute(size.x, size.y, _settings.MenuAspect, device, _settings.EdgeMargin));
        }

        private static Rect DeviceSafeShare()
        {
            if (Screen.width <= 0 || Screen.height <= 0)
            {
                return new Rect(0f, 0f, 1f, 1f);
            }

            Rect safe = Screen.safeArea;
            return new Rect(safe.x / Screen.width, safe.y / Screen.height, safe.width / Screen.width,
                safe.height / Screen.height);
        }

        private static void Place(VisualElement frame, FrameInsets insets)
        {
            frame.style.left = insets.Left;
            frame.style.right = insets.Right;
            frame.style.top = insets.Top;
            frame.style.bottom = insets.Bottom;
        }
    }
}

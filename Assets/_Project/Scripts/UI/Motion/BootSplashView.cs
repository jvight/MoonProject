using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace MoonProject.UI
{
    /// <summary>
    /// Draws <see cref="BootSplash"/> on the boot's dark curtain: the four studio layers stacked in one square,
    /// centred, the stroke revealed by a clip that grows across its span as it draws out of the ring, the glint
    /// blooming about its own centre and the wordmark rising into place. It writes styles only while the splash plays.
    /// </summary>
    internal sealed class BootSplashView
    {
        private readonly BootSplashSettings _settings;
        private readonly VisualElement _root;
        private readonly VisualElement _square;
        private readonly VisualElement _ring;
        private readonly VisualElement _strokeClip;
        private readonly VisualElement _stroke;
        private readonly VisualElement _glint;
        private readonly VisualElement _wordmark;
        private bool _shown = true;

        public BootSplashView(VisualElement curtain, BootSplashSettings settings)
        {
            if (curtain == null)
            {
                throw new ArgumentNullException(nameof(curtain));
            }

            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _root = Box("boot-splash", "boot-splash");
            _square = Box("boot-splash-square", "boot-splash__square");
            _root.Add(_square);
            _ring = Layer("boot-splash-ring", settings.Ring);
            _strokeClip = Box("boot-splash-stroke-clip", "boot-splash__clip");
            _square.Add(_strokeClip);
            _stroke = Layer("boot-splash-stroke", settings.Stroke, _strokeClip);
            _glint = Layer("boot-splash-glint", settings.Glint);
            Vector2 centre = settings.GlintCentre;
            _glint.style.transformOrigin = new TransformOrigin(Length.Percent(centre.x * 100f),
                Length.Percent(centre.y * 100f));
            _wordmark = Layer("boot-splash-wordmark", settings.Wordmark);
            curtain.Add(_root);
            SetShown(false);
        }

        public bool IsShown => _shown;

        /// <summary>
        /// The splash at its time now, laid out for a panel <paramref name="panelHeight"/> pixels high.
        /// </summary>
        public void Apply(BootSplash splash, float panelHeight)
        {
            SetShown(splash.IsPlaying);
            if (!splash.IsPlaying || float.IsNaN(panelHeight) || panelHeight <= 0f)
            {
                return;
            }

            float side = panelHeight * _settings.HeightShare;
            _square.style.width = side;
            _square.style.height = side;
            _ring.style.opacity = splash.RingAlpha;
            Vector2 span = _settings.StrokeSpan;
            _strokeClip.style.width = side * Mathf.Lerp(span.x, span.y, splash.StrokeProgress);
            _stroke.style.width = side;
            _stroke.style.opacity = splash.StrokeAlpha;
            _glint.style.opacity = splash.GlintAlpha;
            float scale = splash.GlintScale;
            _glint.style.scale = new Scale(new Vector3(scale, scale, 1f));
            _wordmark.style.opacity = splash.WordAlpha;
            _wordmark.style.translate = new Translate(0f, side * splash.WordDrop);
        }

        private void SetShown(bool shown)
        {
            if (shown == _shown)
            {
                return;
            }

            _shown = shown;
            _root.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private VisualElement Layer(string name, Texture2D image, VisualElement parent = null)
        {
            VisualElement layer = Box(name, "boot-splash__layer");
            layer.style.backgroundImage = new StyleBackground(image);
            (parent ?? _square).Add(layer);
            return layer;
        }

        private static VisualElement Box(string name, string className)
        {
            var box = new VisualElement { name = name, pickingMode = PickingMode.Ignore };
            box.AddToClassList(className);
            return box;
        }
    }
}

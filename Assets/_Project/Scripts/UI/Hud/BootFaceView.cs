using System;
using UnityEngine.UIElements;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// Draws <see cref="BootFace"/> on 07's CRT, in the face panel beside the self-test's column: the waking raster
    /// (phosphor amber, brighter along its middle, so it opens as a glow rather than a slab),
    /// the maker's mark (<see cref="LogoTracePainter"/>) with its wordmark typing under it, and the load line with its
    /// bar of steps. It writes labels and classes only when what they show changes.
    /// </summary>
    internal sealed class BootFaceView
    {
        public const string RootClass = "boot-face";
        public const string StepFilledClass = "boot-face__step--filled";

        private const float Percent = 100f;

        private readonly BootFace _face;
        private readonly ILocalization _localization;
        private readonly VisualElement _root;
        private readonly VisualElement _raster;
        private readonly VisualElement _logo;
        private readonly Label _wordmark;
        private readonly VisualElement _load;
        private readonly Label _loadText;
        private readonly VisualElement[] _steps;
        private readonly LogoTracePainter _trace;
        private string _wordmarkText = string.Empty;
        private string _loadLine = string.Empty;
        private int _shownWordmark = -1;
        private int _shownLoad = -1;
        private int _shownSteps = -1;
        private BootFaceStage _shownStage = BootFaceStage.None;

        public BootFaceView(VisualElement parent, BootFace face, MakerLinework linework, ILocalization localization)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            _face = face ?? throw new ArgumentNullException(nameof(face));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _root = Element("boot-face", RootClass);
            // Hidden until a boot begins: a loaded game never boots the face, and other content shares the panel.
            _root.style.display = DisplayStyle.None;
            _raster = Element("boot-face-raster", "boot-face__raster");
            _raster.Add(Element("boot-face-raster-core", "boot-face__raster-core"));
            _logo = Element("boot-face-logo", "boot-face__logo");
            _wordmark = new Label { name = "boot-face-wordmark", pickingMode = PickingMode.Ignore };
            _wordmark.AddToClassList("boot-face__wordmark");
            _load = Element("boot-face-load", "boot-face__load");
            _loadText = new Label { name = "boot-face-load-text", pickingMode = PickingMode.Ignore };
            _loadText.AddToClassList("boot-face__load-text");
            _steps = new VisualElement[face.LoadSteps];
            _load.Add(_loadText);
            for (int i = 0; i < _steps.Length; i++)
            {
                _steps[i] = Element("boot-face-step-" + i, "boot-face__step");
                _load.Add(_steps[i]);
            }

            _root.Add(_raster);
            _root.Add(_logo);
            _root.Add(_wordmark);
            _root.Add(_load);
            parent.Add(_root);
            _trace = new LogoTracePainter(_logo, linework, face);
            Relocalize();
            Refresh();
        }

        /// <summary>The boot's layer on the face (tests and captures).</summary>
        public VisualElement Root => _root;

        public Label Wordmark => _wordmark;

        public Label LoadText => _loadText;

        /// <summary>Load-bar step <paramref name="index"/> (tests).</summary>
        public VisualElement Step(int index) => _steps[index];

        /// <summary>Reads the face's texts in the current language and tells the boot their lengths.</summary>
        public void Relocalize()
        {
            _wordmarkText = _localization.Get(UiKeys.BootMaker);
            _loadLine = _localization.Get(UiKeys.BootLoading);
            _face.SetTexts(_wordmarkText.Length, _loadLine.Length);
            _shownWordmark = -1;
            _shownLoad = -1;
        }

        /// <summary>Shows what the boot is at now.</summary>
        public void Refresh()
        {
            BootFaceStage stage = _face.Part;
            if (stage != _shownStage)
            {
                _shownStage = stage;
                _shownWordmark = -1;
                _shownLoad = -1;
                _shownSteps = -1;
                _root.style.display = stage == BootFaceStage.None ? DisplayStyle.None : DisplayStyle.Flex;
                Show(_raster, stage == BootFaceStage.PowerOn);
                Show(_logo, stage == BootFaceStage.Logo);
                Show(_wordmark, stage == BootFaceStage.Logo);
                Show(_load, stage == BootFaceStage.Load);
            }

            switch (stage)
            {
                case BootFaceStage.PowerOn:
                    // Centred on the tube: a dot, then a line across, then the raster opening from it.
                    _raster.style.width = Length.Percent(_face.RasterWidth * Percent);
                    _raster.style.height = Length.Percent(_face.RasterHeight * Percent);
                    _raster.style.left = Length.Percent((1f - _face.RasterWidth) * Percent * 0.5f);
                    _raster.style.top = Length.Percent((1f - _face.RasterHeight) * Percent * 0.5f);
                    _raster.style.opacity = _face.RasterGlow;
                    break;
                case BootFaceStage.Logo:
                    _trace.Refresh();
                    _wordmark.style.opacity = _face.LogoAlpha;
                    WriteTyped(_wordmark, _wordmarkText, _face.WordmarkVisible, ref _shownWordmark);
                    break;
                case BootFaceStage.Load:
                    WriteTyped(_loadText, _loadLine, _face.LoadVisible, ref _shownLoad);
                    WriteSteps();
                    break;
            }
        }

        private void WriteSteps()
        {
            int filled = _face.StepsFilled;
            if (filled == _shownSteps)
            {
                return;
            }

            _shownSteps = filled;
            for (int i = 0; i < _steps.Length; i++)
            {
                _steps[i].EnableInClassList(StepFilledClass, i < filled);
            }
        }

        private static void WriteTyped(Label label, string text, int visible, ref int shown)
        {
            if (visible == shown)
            {
                return;
            }

            shown = visible;
            label.text = visible >= text.Length ? text : text.Substring(0, visible);
        }

        private static void Show(VisualElement element, bool shown)
        {
            element.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static VisualElement Element(string name, string className)
        {
            var element = new VisualElement { name = name, pickingMode = PickingMode.Ignore };
            element.AddToClassList(className);
            return element;
        }
    }
}

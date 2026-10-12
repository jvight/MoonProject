using System;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Core.Input;

namespace MoonProject.UI
{
    /// <summary>
    /// The crew's lab on screen (docs/features/M3-19 §5, §6): while 07 is up in the lab the HUD steps aside and this
    /// shows instead, low and quiet. The first time, the lab's place line (its name and its one job). Until the player
    /// has done each once, the turntable's prompts: Turn, Look, Down, each with its stick direction as the glyph. And
    /// facing the keepsake shelf with a relic to place, the Place prompt with the Tether glyph. Learned prompts are
    /// remembered in the prompt ledger (<see cref="HudVerb"/>). Writes to the elements only when something changes.
    /// </summary>
    internal sealed class LabHud
    {
        private const string PlaceId = "place.lab";
        private const string PlaceName = PlaceId + ".name";
        private const string PlaceJob = PlaceId + ".job";
        private const string TurnWord = "ui.lab.turn";
        private const string LookWord = "ui.lab.look";
        private const string DownWord = "ui.lab.down";
        private const string PlaceWord = "hint.deposit";
        private const string GlyphPrefix = "glyph.lab.";
        private const string PadSuffix = ".pad";

        private static readonly string[] Verbs = { "turn", "look", "down" };
        private static readonly HudVerb[] Learned = { HudVerb.LabTurn, HudVerb.LabLook, HudVerb.LabDown };

        // The way to push the stick for each prompt, drawn beside its "LS" (the fonts have no arrows).
        private static readonly ArrowDirection[][] Pushes =
        {
            new[] { ArrowDirection.Left, ArrowDirection.Right },
            new[] { ArrowDirection.Up },
            new[] { ArrowDirection.Down },
        };

        private readonly LabHudSettings _settings;
        private readonly ILocalization _localization;
        private readonly PromptLedger _ledger;
        private readonly Reveal _root;
        private readonly Reveal _place;
        private readonly Label _placeName;
        private readonly Label _placeJob;
        private readonly Reveal _placePrompt;
        private readonly GlyphView _placeGlyph;
        private readonly Label _placeWord;
        private readonly Reveal[] _chips = new Reveal[3];
        private readonly Label[] _glyphs = new Label[3];
        private readonly VisualElement[] _arrows = new VisualElement[3];
        private readonly Label[] _words = new Label[3];
        private readonly string[] _glyphTexts = new string[6];
        private InputDeviceKind _device = InputDeviceKind.KeyboardMouse;
        private bool _glyphsWritten;
        private float _inside;
        private float _placeLeft;

        public LabHud(VisualElement root, LabHudSettings settings, ILocalization localization, PromptLedger ledger)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
            _root = new Reveal(root, settings.Prompts);
            _root.Snap(false);
            _place = new Reveal(Require<VisualElement>(root, "lab-place"), settings.Place);
            _place.Snap(false);
            new ShadowPainter(Require<VisualElement>(root, "lab-place-shadow"));
            _placeName = Require<Label>(root, "lab-place-name");
            _placeJob = Require<Label>(root, "lab-place-job");
            _placePrompt = new Reveal(Require<VisualElement>(root, "lab-place-prompt"), settings.Chip);
            _placePrompt.Snap(false);
            _placeGlyph = new GlyphView(Require<VisualElement>(root, "lab-place-glyph"),
                Require<Label>(root, "lab-place-glyph-label"));
            _placeWord = Require<Label>(root, "lab-place-word");
            for (int i = 0; i < Verbs.Length; i++)
            {
                VisualElement chip = Require<VisualElement>(root, "lab-" + Verbs[i]);
                _chips[i] = new Reveal(chip, settings.Chip);
                _chips[i].Snap(false);
                _arrows[i] = BuildArrows(chip, Pushes[i]);
                _glyphs[i] = Require<Label>(root, "lab-" + Verbs[i] + "-glyph");
                _words[i] = Require<Label>(root, "lab-" + Verbs[i] + "-word");
            }

            Relocalize();
        }

        /// <summary>07 is up in the lab: the HUD steps aside.</summary>
        public bool Inside { get; private set; }

        /// <summary>The platform settled in the lab.</summary>
        public void Enter()
        {
            Inside = true;
            _inside = 0f;
        }

        /// <summary>The platform started down: the lab's prompts leave with it.</summary>
        public void Leave()
        {
            Inside = false;
            _placeLeft = 0f;
        }

        /// <summary>The first time up in the lab: true when the place line was for the lab and is shown here.</summary>
        public bool TryShowPlace(string placeId)
        {
            if (placeId != PlaceId)
            {
                return false;
            }

            _placeLeft = _settings.PlaceSeconds;
            return true;
        }

        /// <param name="deltaTime">Unscaled seconds (0 while paused).</param>
        /// <param name="shown">The screen is free for the lab (not paused, the map not open).</param>
        /// <param name="drive">The drive stick: a push in a direction learns that prompt.</param>
        /// <param name="canPlace">The Place prompt applies (facing the shelf with a relic to place).</param>
        /// <param name="placeGlyph">The Tether glyph on the active device.</param>
        public void Tick(float deltaTime, bool shown, Vector2 drive, bool canPlace, string placeGlyph,
            InputDeviceKind device)
        {
            bool open = Inside && shown;
            _inside += deltaTime;
            _placeLeft = Mathf.Max(0f, _placeLeft - deltaTime);
            if (Inside)
            {
                Learn(drive);
            }

            _root.Set(open);
            _root.Tick(deltaTime);
            _place.Set(open && _placeLeft > 0f);
            _place.Tick(deltaTime);
            _placeGlyph.Set(placeGlyph, device);
            _placePrompt.Set(open && canPlace);
            _placePrompt.Tick(deltaTime);
            SetDevice(device);
            bool prompting = open && _inside >= _settings.PromptDelay && !canPlace;
            for (int i = 0; i < _chips.Length; i++)
            {
                _chips[i].Set(prompting && !_ledger.Met(Learned[i]));
                _chips[i].Tick(deltaTime);
            }
        }

        public void Relocalize()
        {
            _placeName.text = _localization.Get(PlaceName);
            _placeJob.text = _localization.Get(PlaceJob);
            _placeWord.text = _localization.Get(PlaceWord);
            _words[0].text = _localization.Get(TurnWord);
            _words[1].text = _localization.Get(LookWord);
            _words[2].text = _localization.Get(DownWord);
            for (int i = 0; i < Verbs.Length; i++)
            {
                _glyphTexts[i * 2] = _localization.Get(GlyphPrefix + Verbs[i]);
                _glyphTexts[i * 2 + 1] = _localization.Get(GlyphPrefix + Verbs[i] + PadSuffix);
            }

            _glyphsWritten = false;
        }

        /// <summary>A push past the learned threshold in a prompt's direction retires that prompt for good.</summary>
        private void Learn(Vector2 drive)
        {
            float push = _settings.LearnedPush;
            if (Mathf.Abs(drive.x) >= push)
            {
                _ledger.RecordMet(HudVerb.LabTurn);
            }

            if (drive.y >= push)
            {
                _ledger.RecordMet(HudVerb.LabLook);
            }

            if (drive.y <= -push)
            {
                _ledger.RecordMet(HudVerb.LabDown);
            }
        }

        private void SetDevice(InputDeviceKind device)
        {
            if (_glyphsWritten && device == _device)
            {
                return;
            }

            _device = device;
            _glyphsWritten = true;
            int pad = device == InputDeviceKind.Gamepad ? 1 : 0;
            for (int i = 0; i < _glyphs.Length; i++)
            {
                _glyphs[i].text = _glyphTexts[i * 2 + pad];
                _arrows[i].style.display = pad == 1 ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        /// <summary>The arrows of one prompt, built after its glyph; they show only for the gamepad's stick.</summary>
        private static VisualElement BuildArrows(VisualElement chip, ArrowDirection[] directions)
        {
            var arrows = new VisualElement { pickingMode = PickingMode.Ignore };
            arrows.AddToClassList("lab-arrows");
            arrows.style.display = DisplayStyle.None;
            for (int i = 0; i < directions.Length; i++)
            {
                var arrow = new VisualElement { pickingMode = PickingMode.Ignore };
                arrow.AddToClassList("lab-arrow");
                new ArrowPainter(arrow, directions[i]);
                arrows.Add(arrow);
            }

            chip.Insert(1, arrows);
            return arrows;
        }

        private static T Require<T>(VisualElement root, string name) where T : VisualElement
        {
            return root.Q<T>(name) ?? throw new InvalidOperationException(
                $"GameUI.uxml has no {typeof(T).Name} named '{name}' under '{root.name}'.");
        }
    }
}

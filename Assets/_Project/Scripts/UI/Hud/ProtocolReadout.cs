using System;
using UnityEngine.UIElements;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// The recovery protocol's readout (docs/features/M3-24 B): at the end of a full opening, while 07 gazes at Earth,
    /// its lens shows the protocol the crew left it, the same rows as the lab's recovery panel, with Power first and
    /// its first need under it ("› the radio tower"). It rests about 4 s and fades. Once per full opening; a player who
    /// hurried past Earth's gaze sees it at the chase frame instead. Never on the short wake.
    /// </summary>
    internal sealed class ProtocolReadout
    {
        private const string TitleKey = "ui.recovery.title";
        private const string FirstNeedKey = "protocol.power.need";

        private readonly GuidanceSettings _settings;
        private readonly ILocalization _localization;
        private readonly Reveal _reveal;
        private readonly Label _title;
        private readonly Label _need;
        private readonly RecoveryRows _rows;
        private bool _pending;
        private float _rest;

        /// <param name="hud">The HUD holding the readout (protocol).</param>
        public ProtocolReadout(VisualElement hud, GuidanceSettings settings, ILocalization localization,
            IRecoveryProgress recovery)
        {
            if (hud == null)
            {
                throw new ArgumentNullException(nameof(hud));
            }

            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            VisualElement root = Require<VisualElement>(hud, "protocol");
            _reveal = new Reveal(root, settings.Protocol);
            _reveal.Snap(false);
            _title = Require<Label>(root, "protocol-title");
            VisualElement rows = Require<VisualElement>(root, "protocol-rows");
            _rows = new RecoveryRows(rows, localization, recovery);
            for (int i = 0; i < rows.childCount; i++)
            {
                rows[i].AddToClassList("protocol-readout__row");
            }

            VisualElement power = _rows.RowOf(RecoveryTarget.Power);
            power.AddToClassList("protocol-readout__row--first");
            _need = new Label { pickingMode = PickingMode.Ignore };
            _need.AddToClassList("protocol-readout__need");
            rows.Insert(rows.IndexOf(power) + 1, _need);
            Relocalize();
        }

        public bool IsVisible => !_reveal.IsHidden;

        /// <summary>A full opening began (its crackle): the readout waits for Earth's gaze.</summary>
        public void OpeningBegan()
        {
            _pending = true;
        }

        /// <summary>The opening's gaze reached Earth, or play reached the chase frame: show it if it is still owed.</summary>
        public void Cue()
        {
            if (!_pending)
            {
                return;
            }

            _pending = false;
            _rows.Write();
            _rest = _settings.ProtocolHoldSeconds;
            _reveal.Show();
        }

        /// <param name="deltaTime">Unscaled seconds; 0 while paused so the readout waits too.</param>
        public void Tick(float deltaTime)
        {
            if (_reveal.IsShown)
            {
                _rest -= deltaTime;
                if (_rest <= 0f)
                {
                    _reveal.Hide();
                }
            }

            _reveal.Tick(deltaTime);
        }

        public void Relocalize()
        {
            _title.text = _localization.Get(TitleKey);
            _need.text = _localization.Get(FirstNeedKey);
            _rows.Relocalize();
        }

        private static T Require<T>(VisualElement root, string name) where T : VisualElement
        {
            return root.Q<T>(name) ?? throw new InvalidOperationException(
                $"GameUI.uxml has no {typeof(T).Name} named '{name}' under '{root.name}'.");
        }
    }
}

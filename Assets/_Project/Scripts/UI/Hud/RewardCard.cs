using System;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary>
    /// The Reward beat of the tower coming online (docs/design/feel-pass.md §8.2): a glass card that names what the
    /// player got, from the upgrade's own data (<see cref="RewardLines"/>): "Radio Tower · Level 1", then home has
    /// power, the radio picks up the station, the range grows. The level that was just bought is noted on
    /// <see cref="UpgradePurchased"/> and the card comes on the Build beat's Reward, once the sequence has played;
    /// it waits while something else holds the upper third, rests, and fades away.
    /// </summary>
    internal sealed class RewardCard
    {
        private readonly RewardCardSettings _settings;
        private readonly ILocalization _localization;
        private readonly IUpgradeShop _shop;
        private readonly Reveal _reveal;
        private readonly Label _title;
        private readonly VisualElement[] _rows = new VisualElement[RewardLines.Max];
        private readonly Label[] _texts = new Label[RewardLines.Max];
        private readonly RewardLine[] _lines = new RewardLine[RewardLines.Max];
        private string _boughtId;
        private int _boughtLevel;
        private bool _wanted;
        private bool _showing;
        private bool _leaving;
        private float _timer;
        private int _count;

        public RewardCard(UiLayout layout, RewardCardSettings settings, ILocalization localization, IUpgradeShop shop)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _shop = shop ?? throw new ArgumentNullException(nameof(shop));
            _title = layout.RewardTitle;
            _reveal = new Reveal(layout.RewardCard, settings.Reveal);
            _reveal.Snap(false);
            for (int i = 0; i < RewardLines.Max; i++)
            {
                var row = new VisualElement { pickingMode = PickingMode.Ignore };
                row.AddToClassList("reward-card__row");
                var lamp = new VisualElement { pickingMode = PickingMode.Ignore };
                lamp.AddToClassList("reward-card__lamp");
                var text = new Label { pickingMode = PickingMode.Ignore };
                text.AddToClassList("reward-card__line");
                row.Add(lamp);
                row.Add(text);
                row.style.display = DisplayStyle.None;
                layout.RewardLines.Add(row);
                _rows[i] = row;
                _texts[i] = text;
            }
        }

        /// <summary>True while the card is on screen (or easing).</summary>
        public bool IsVisible => !_reveal.IsHidden;

        /// <summary>True from the Reward beat until the card is up, so other voices leave the way clear.</summary>
        public bool IsWaiting => _wanted;

        /// <summary>How many lines the card has now (tests and captures).</summary>
        public int LineCount => _count;

        /// <summary>The kind of line <paramref name="index"/> (tests and captures).</summary>
        public RewardLineKind LineKind(int index)
        {
            return _lines[index].Kind;
        }

        /// <summary>Notes the level just bought: the card is made from it when the sequence reaches its Reward.</summary>
        public void Purchased(string upgradeId, int level)
        {
            _boughtId = upgradeId;
            _boughtLevel = level;
        }

        /// <summary>The Build beat's Reward asks for the card.</summary>
        public void OnBeat(FeelBeat beat)
        {
            if (beat.Kind == FeelKind.Build && beat.Phase == FeelPhase.Reward && _boughtId != null)
            {
                _wanted = true;
            }
        }

        /// <summary>Re-reads the words in the new language (a card on screen changes in place).</summary>
        public void Relocalize()
        {
            if (_showing || _leaving)
            {
                Write();
            }
        }

        /// <param name="deltaTime">Unscaled seconds; 0 while paused.</param>
        /// <param name="open">False while another block holds the upper third.</param>
        public void Tick(float deltaTime, bool open)
        {
            if (_wanted && open && !_showing && !_leaving)
            {
                if (Write())
                {
                    _showing = true;
                    _timer = 0f;
                    _reveal.Show();
                }

                _wanted = false;
            }

            if (_showing)
            {
                _timer += deltaTime;
                if (_timer >= _settings.HoldSeconds)
                {
                    _showing = false;
                    _leaving = true;
                    _reveal.Hide();
                }
            }

            _reveal.Tick(deltaTime);
            if (_leaving && _reveal.IsHidden)
            {
                _leaving = false;
            }
        }

        /// <summary>Writes the title and lines of the level just bought; false when the shop has no such level.</summary>
        private bool Write()
        {
            if (!_shop.TryGetOffer(_boughtId, out UpgradeOffer offer) || _boughtLevel < 1 ||
                _boughtLevel > offer.MaxLevel)
            {
                return false;
            }

            UpgradeDefinition upgrade = offer.Definition;
            _title.text = string.Format(_localization.Get(UiKeys.RewardTitle),
                _localization.Get(UiKeys.UpgradeName(_boughtId)), _boughtLevel);
            _count = RewardLines.Compose(upgrade.SignalRadiusAt(_boughtLevel - 1), upgrade.SignalRadiusAt(_boughtLevel),
                WakesRadio(upgrade, _boughtLevel), upgrade.LightBoostAt(_boughtLevel - 1),
                upgrade.LightBoostAt(_boughtLevel), _lines);
            for (int i = 0; i < RewardLines.Max; i++)
            {
                bool used = i < _count;
                _rows[i].style.display = used ? DisplayStyle.Flex : DisplayStyle.None;
                if (used)
                {
                    _texts[i].text = string.Format(_localization.Get(UiKeys.RewardLine(_lines[i].Kind)),
                        _lines[i].Number);
                }
            }

            return true;
        }

        /// <summary>True when <paramref name="level"/> is the first level of the upgrade to set a radio radius.</summary>
        private static bool WakesRadio(UpgradeDefinition upgrade, int level)
        {
            if (upgrade.Levels[level - 1].SignalRadius <= 0f)
            {
                return false;
            }

            for (int i = 0; i < level - 1; i++)
            {
                if (upgrade.Levels[i].SignalRadius > 0f)
                {
                    return false;
                }
            }

            return true;
        }
    }
}

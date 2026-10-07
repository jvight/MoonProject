using System;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary>
    /// While 07 is parked on an upgrade station's pad (the radio tower or Kenji's workbench) and something is left to
    /// buy, a compact panel offers it. Its header says where 07 is ("ui.station.&lt;station&gt;", with the station's
    /// own accent and lamp) and, for an upgrade with several levels, which level is next, or else the upgrade's name.
    /// Below: the level's title and what it does in plain words (the localized "upgrade.&lt;id&gt;.&lt;level&gt;.*"
    /// strings), its cost (the balance stays in view in the pinned scrap chip; a shortfall is said in words), and a
    /// ring that fills while the confirm button is held (<see cref="HoldToConfirm"/>: no accidental purchases). Buying
    /// goes through <see cref="IUpgradeShop"/>; the panel glows a moment, then shows the next level or bows out when
    /// all are bought. The ring starting and completing are published as <see cref="UiCue"/>s.
    /// </summary>
    internal sealed class TowerPanel
    {
        public const string CelebrateClass = "tower-panel--celebrate";
        public const string ShortClass = "cost--short";

        private static readonly UpgradeStationKind[] Stations =
            (UpgradeStationKind[])Enum.GetValues(typeof(UpgradeStationKind));

        private readonly TowerPanelSettings _settings;
        private readonly ILocalization _localization;
        private readonly EventBus _events;
        private readonly IUpgradeShop _shop;
        private readonly IScrapWallet _wallet;
        private readonly IInteractionHints _hints;
        private readonly IntText _numbers;
        private readonly Reveal _reveal;
        private readonly HoldToConfirm _hold;
        private readonly ProgressRingPainter _ring;
        private readonly UiLayout _layout;
        private UpgradeDefinition _shownUpgrade;
        private int _shownLevel = -1;
        private int _shownBalance = -1;
        private string _shownGlyph;
        private float _celebrateTimer;

        public TowerPanel(UiLayout layout, TowerPanelSettings settings, ILocalization localization, EventBus events,
            IUpgradeShop shop, IScrapWallet wallet, IInteractionHints hints, IntText numbers)
        {
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _shop = shop ?? throw new ArgumentNullException(nameof(shop));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _hints = hints ?? throw new ArgumentNullException(nameof(hints));
            _numbers = numbers ?? throw new ArgumentNullException(nameof(numbers));
            _reveal = new Reveal(layout.TowerPanel, settings.Reveal);
            _reveal.Snap(false);
            _hold = new HoldToConfirm(settings);
            _ring = new ProgressRingPainter(layout.TowerRing);
            new ShadowPainter(layout.TowerPanelShadow);
            new ScrapIconPainter(layout.TowerCostIcon);
            layout.TowerHoldWord.text = localization.Get(UiKeys.TowerHold);
        }

        public bool IsVisible => !_reveal.IsHidden;

        /// <summary>The panel's look at <paramref name="station"/> (USS): its accent colour and lamp.</summary>
        public static string StationClass(UpgradeStationKind station)
        {
            switch (station)
            {
                case UpgradeStationKind.RadioTower:
                    return "tower-panel--radio-tower";
                case UpgradeStationKind.Workshop:
                    return "tower-panel--workshop";
                default:
                    throw new ArgumentOutOfRangeException(nameof(station), station, "This station has no look.");
            }
        }

        /// <summary>True while the panel glows after a purchase.</summary>
        public bool IsCelebrating => _celebrateTimer > 0f;

        /// <summary>0..1 fill of the hold ring.</summary>
        public float HoldProgress => _hold.Progress;

        /// <param name="deltaTime">Unscaled seconds; pass 0 while paused.</param>
        /// <param name="gateOpen">False while paused.</param>
        /// <param name="confirmHeld">True while the confirm (Excavate) button is held.</param>
        /// <param name="confirmGlyph">The confirm control's label for the active device.</param>
        public void Tick(float deltaTime, bool gateOpen, bool confirmHeld, string confirmGlyph)
        {
            bool offered = gateOpen && _hints.TryGet(InteractionKind.Upgrade, out InteractionHint _);
            UpgradeOffer offer = default;
            UpgradeDefinition upgrade = offered ? _shop.StationUpgrade : null;
            offered = upgrade != null && _shop.TryGetOffer(upgrade.Id, out offer) && !offer.IsMaxed;

            if (IsCelebrating)
            {
                _celebrateTimer -= deltaTime;
                if (!IsCelebrating)
                {
                    EndCelebration();
                }
            }

            if (offered && !IsCelebrating)
            {
                Refresh(upgrade, offer);
            }

            if (!ReferenceEquals(confirmGlyph, _shownGlyph))
            {
                _layout.TowerRingGlyph.text = confirmGlyph;
                _shownGlyph = confirmGlyph;
            }

            _reveal.Set(gateOpen && (offered || IsCelebrating));
            bool available = offered && offer.CanAfford && !IsCelebrating &&
                             _reveal.Target && _reveal.Visibility >= _settings.ArmVisibility;
            HoldStep step = _hold.Step(available, confirmHeld, deltaTime);
            if (step == HoldStep.Started)
            {
                _events.Publish(new UiCue(UiCueKind.HoldFill));
            }
            else if (step == HoldStep.Confirmed)
            {
                Buy(upgrade);
            }
            else if (step == HoldStep.Released)
            {
                _events.Publish(new UiCue(UiCueKind.HoldRelease));
            }

            if (_reveal.IsHidden && !IsCelebrating)
            {
                _hold.Reset();
            }

            _ring.Progress = IsCelebrating ? 1f : _hold.Progress;
            _reveal.Tick(deltaTime);
        }

        /// <summary>Re-reads every string in the new language (the panel on screen changes in place).</summary>
        public void Relocalize()
        {
            _layout.TowerHoldWord.text = _localization.Get(IsCelebrating ? UiKeys.TowerPurchased : UiKeys.TowerHold);
            _shownUpgrade = null;
            _shownLevel = -1;
        }

        private void Buy(UpgradeDefinition upgrade)
        {
            PurchaseResult result = _shop.Purchase(upgrade.Id);
            if (result != PurchaseResult.Purchased)
            {
                Debug.LogWarning($"{nameof(TowerPanel)}: buying '{upgrade.Id}' returned {result}; nothing changed.");
                return;
            }

            _events.Publish(new UiCue(UiCueKind.HoldComplete));
            _celebrateTimer = _settings.CelebrateSeconds;
            _layout.TowerPanel.AddToClassList(CelebrateClass);
            _layout.TowerHoldWord.text = _localization.Get(UiKeys.TowerPurchased);
            _layout.TowerNeed.style.display = DisplayStyle.None;
            _layout.TowerConfirm.style.display = DisplayStyle.Flex;
        }

        private void EndCelebration()
        {
            _celebrateTimer = 0f;
            _layout.TowerPanel.RemoveFromClassList(CelebrateClass);
            _layout.TowerHoldWord.text = _localization.Get(UiKeys.TowerHold);
            _hold.Reset();
            _shownLevel = -1;
        }

        private void DressFor(UpgradeStationKind station)
        {
            for (int i = 0; i < Stations.Length; i++)
            {
                _layout.TowerPanel.EnableInClassList(StationClass(Stations[i]), Stations[i] == station);
            }
        }

        private void Refresh(UpgradeDefinition upgrade, UpgradeOffer offer)
        {
            int balance = _wallet.Balance;
            if (upgrade == _shownUpgrade && offer.CurrentLevel == _shownLevel && balance == _shownBalance)
            {
                return;
            }

            if (upgrade != _shownUpgrade || offer.CurrentLevel != _shownLevel)
            {
                int level = offer.CurrentLevel + 1;
                DressFor(upgrade.Station);
                _layout.TowerName.text = _localization.Get(UiKeys.StationName(upgrade.Station));
                _layout.TowerLevel.text = offer.MaxLevel > 1
                    ? string.Format(_localization.Get(UiKeys.TowerLevel), level, offer.MaxLevel)
                    : _localization.Get(UiKeys.UpgradeName(upgrade.Id));
                _layout.TowerTitle.text = _localization.Get(UiKeys.UpgradeTitle(upgrade.Id, level));
                _layout.TowerDescription.text = _localization.Get(UiKeys.UpgradeEffect(upgrade.Id, level));
                _layout.TowerCost.text = _numbers.Get(offer.NextCost);
            }

            bool affordable = balance >= offer.NextCost;
            _layout.TowerConfirm.style.display = affordable ? DisplayStyle.Flex : DisplayStyle.None;
            _layout.TowerNeed.style.display = affordable ? DisplayStyle.None : DisplayStyle.Flex;
            _layout.TowerCostRow.EnableInClassList(ShortClass, !affordable);
            if (!affordable)
            {
                _layout.TowerNeed.text = string.Format(_localization.Get(UiKeys.TowerNeed), offer.NextCost - balance);
            }

            _shownUpgrade = upgrade;
            _shownLevel = offer.CurrentLevel;
            _shownBalance = balance;
        }
    }
}

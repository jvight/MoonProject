using System;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;
using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary>
    /// While 07 is parked on an upgrade station's pad (the radio tower or Kenji's workbench) and something is left to
    /// buy, a compact panel offers it. Its header says where 07 is ("ui.station.&lt;station&gt;", with the station's
    /// own accent and lamp) and, for an upgrade with several levels, which level is next, or else the upgrade's name.
    /// Below: the level's title and what it does in plain words (the localized "upgrade.&lt;id&gt;.&lt;level&gt;.*"
    /// strings), its recipe in materials (<see cref="RecipeView"/>: the ones 07 is short of dimmed, with one quiet
    /// need line in place of the hold; the stock stays in view in the pinned materials chip), and a ring that fills
    /// while the confirm button is held (<see cref="HoldToConfirm"/>: no accidental purchases). Buying goes through
    /// <see cref="IUpgradeShop"/>; the panel glows a moment, then shows the next level or bows out when all are bought.
    /// The ring starting and completing are published as <see cref="UiCue"/>s.
    /// <para>
    /// While the station has more than one thing left to sell (Kenji's bench, docs/features/M3-11), the panel lists
    /// them instead (<see cref="BenchList"/>): a tap of Interact or the Winch picks (<see cref="BenchPick"/>, a
    /// <see cref="UiCueKind.FocusMove"/> each), the hold crafts the picked one, and a faint "ui.bench.pick" line says
    /// how to choose. The radio tower, and the bench's last piece, keep the single offer.
    /// </para>
    /// </summary>
    internal sealed class TowerPanel
    {
        public const string CelebrateClass = "tower-panel--celebrate";
        public const string ChoosingClass = "tower-panel--choosing";

        private static readonly UpgradeStationKind[] Stations =
            (UpgradeStationKind[])Enum.GetValues(typeof(UpgradeStationKind));

        private readonly TowerPanelSettings _settings;
        private readonly ILocalization _localization;
        private readonly EventBus _events;
        private readonly IUpgradeShop _shop;
        private readonly IMaterialStock _materials;
        private readonly IInteractionHints _hints;
        private readonly RecipeView _recipe;
        private readonly Reveal _reveal;
        private readonly HoldToConfirm _hold;
        private readonly ProgressRingPainter _ring;
        private readonly UiLayout _layout;
        private readonly BenchChoice _choices = new BenchChoice();
        private readonly BenchPick _pick;
        private readonly BenchList _list;
        private readonly GlyphView _pickGlyph;
        private bool _listed;
        private UpgradeDefinition _shownUpgrade;
        private int _shownLevel = -1;
        private bool _shownAffordable;
        private string _shownGlyph;
        private float _celebrateTimer;

        public TowerPanel(UiLayout layout, TowerPanelSettings settings, ILocalization localization, EventBus events,
            IUpgradeShop shop, IMaterialStock materials, IInteractionHints hints, IntText numbers)
        {
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _shop = shop ?? throw new ArgumentNullException(nameof(shop));
            _materials = materials ?? throw new ArgumentNullException(nameof(materials));
            _hints = hints ?? throw new ArgumentNullException(nameof(hints));
            _recipe = new RecipeView(layout.TowerRecipe, layout.TowerNeed, localization, numbers);
            _reveal = new Reveal(layout.TowerPanel, settings.Reveal);
            _reveal.Snap(false);
            _hold = new HoldToConfirm(settings);
            _ring = new ProgressRingPainter(layout.TowerRing);
            new ShadowPainter(layout.TowerPanelShadow);
            layout.TowerHoldWord.text = localization.Get(UiKeys.TowerHold);
            _pick = new BenchPick(settings);
            _list = new BenchList(layout.TowerChoices, localization, numbers);
            _pickGlyph = new GlyphView(layout.TowerPickGlyph, layout.TowerPickGlyphLabel);
            layout.TowerPickWord.text = localization.Get(UiKeys.BenchPick);
            WriteListed(false);
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

        /// <summary>The recipe on the panel (tests and captures).</summary>
        public RecipeView Recipe => _recipe;

        /// <summary>True while the panel lists the station's choices instead of a single offer.</summary>
        public bool IsChoosing => _listed;

        /// <summary>What the station still sells and which is picked (tests and captures).</summary>
        public BenchChoice Choices => _choices;

        /// <summary>The bench's rows (tests and captures).</summary>
        public BenchList List => _list;

        /// <param name="deltaTime">Unscaled seconds; pass 0 while paused.</param>
        /// <param name="gateOpen">False while paused.</param>
        /// <param name="confirmHeld">True while the confirm (Interact) button is held.</param>
        /// <param name="winch">The Winch axis (-1..1, + up): steps between the bench's choices.</param>
        /// <param name="towing">Something is on the tether: the Winch reels, so it does not pick.</param>
        /// <param name="confirmGlyph">The confirm control's label for the active device.</param>
        /// <param name="device">The active device (key cap or round glyph).</param>
        public void Tick(float deltaTime, bool gateOpen, bool confirmHeld, float winch, bool towing,
            string confirmGlyph, InputDeviceKind device)
        {
            bool offered = gateOpen && _hints.TryGet(InteractionKind.Upgrade, out InteractionHint _);
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
                _choices.Refresh(_shop);
            }

            UpgradeOffer offer = default;
            UpgradeDefinition upgrade = offered ? _choices.Current : null;
            offered = upgrade != null && _shop.TryGetOffer(upgrade.Id, out offer) && !offer.IsMaxed;

            if (offered && !IsCelebrating)
            {
                Refresh(upgrade, offer);
            }

            if (!ReferenceEquals(confirmGlyph, _shownGlyph))
            {
                _layout.TowerRingGlyph.text = confirmGlyph;
                _shownGlyph = confirmGlyph;
            }

            _pickGlyph.Set(confirmGlyph, device);
            _reveal.Set(gateOpen && (offered || IsCelebrating));
            bool ready = offered && !IsCelebrating && _reveal.Target && _reveal.Visibility >= _settings.ArmVisibility;
            int pick = _pick.Step(ready && _listed, confirmHeld, winch, towing, deltaTime);
            if (pick != 0 && _choices.Step(pick))
            {
                _events.Publish(new UiCue(UiCueKind.FocusMove));
            }

            bool held = _listed ? _pick.HoldHeld : confirmHeld;
            HoldStep step = _hold.Step(ready && offer.CanAfford, held, deltaTime);
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

            if (_shop.StationUpgradeCount == 0)
            {
                _choices.Clear();
            }

            _ring.Progress = IsCelebrating ? 1f : _hold.Progress;
            _reveal.Tick(deltaTime);
        }

        /// <summary>Re-reads every string in the new language (the panel on screen changes in place).</summary>
        public void Relocalize()
        {
            _layout.TowerHoldWord.text = _localization.Get(IsCelebrating ? UiKeys.TowerPurchased : UiKeys.TowerHold);
            _layout.TowerPickWord.text = _localization.Get(UiKeys.BenchPick);
            _recipe.Relocalize();
            _list.Relocalize();
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
            _layout.TowerConfirm.style.display = DisplayStyle.Flex;
            _shownAffordable = true;
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

        /// <summary>Lists the choices (true) or shows the single offer (false).</summary>
        private void WriteListed(bool listed)
        {
            DisplayStyle single = listed ? DisplayStyle.None : DisplayStyle.Flex;
            DisplayStyle choosing = listed ? DisplayStyle.Flex : DisplayStyle.None;
            _layout.TowerLevel.style.display = single;
            _layout.TowerTitle.style.display = single;
            _layout.TowerDescription.style.display = single;
            _layout.TowerRecipe.style.display = single;
            _layout.TowerChoices.style.display = choosing;
            _layout.TowerPick.style.display = choosing;
            _layout.TowerPanel.EnableInClassList(ChoosingClass, listed);
            _listed = listed;
            _shownUpgrade = null;
        }

        private void Refresh(UpgradeDefinition upgrade, UpgradeOffer offer)
        {
            bool listed = _choices.Count > 1;
            if (listed != _listed)
            {
                WriteListed(listed);
            }

            if (upgrade != _shownUpgrade || offer.CurrentLevel != _shownLevel)
            {
                int level = offer.CurrentLevel + 1;
                DressFor(upgrade.Station);
                _layout.TowerName.text = _localization.Get(UiKeys.StationName(upgrade.Station));
                if (!listed)
                {
                    _layout.TowerLevel.text = offer.MaxLevel > 1
                        ? string.Format(_localization.Get(UiKeys.TowerLevel), level, offer.MaxLevel)
                        : _localization.Get(UiKeys.UpgradeName(upgrade.Id));
                    _layout.TowerTitle.text = _localization.Get(UiKeys.UpgradeTitle(upgrade.Id, level));
                    _layout.TowerDescription.text = _localization.Get(UiKeys.UpgradeEffect(upgrade.Id, level));
                }

                _shownAffordable = !offer.CanAfford;
            }

            if (listed)
            {
                _list.Show(_choices, _materials);
            }

            _recipe.Show(offer.NextCost, _materials);
            bool affordable = offer.CanAfford;
            if (affordable != _shownAffordable)
            {
                _layout.TowerConfirm.style.display = affordable ? DisplayStyle.Flex : DisplayStyle.None;
                _shownAffordable = affordable;
            }

            _shownUpgrade = upgrade;
            _shownLevel = offer.CurrentLevel;
        }
    }
}

using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Kenji's workbench by the lander: the station that sells rover abilities. Its pad of light is the shop, like the
    /// radio tower's: it offers the first ability not yet bought, breathes softly, glows inviting when that is
    /// affordable, brightly while 07 is parked on it, flares on a purchase and dims once the bench has nothing left.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Workshop : MonoBehaviour, IUpgradeStation
    {
        [Tooltip("Workshop tuning (Assets/_Project/Data/Tuning/Gameplay/WorkshopTuning.asset).")]
        [SerializeField] private WorkshopTuning _tuning;

        [Tooltip("The abilities sold at the bench, in the order it offers them " +
                 "(Assets/_Project/Data/Content/Upgrades).")]
        [SerializeField] private UpgradeDefinition[] _definitions = Array.Empty<UpgradeDefinition>();

        [Tooltip("The lander's WorkshopAnchor: where the bench stands (the pad is placed from it).")]
        [SerializeField] private Transform _anchor;

        private IRoverState _rover;
        private UpgradeService _upgrades;
        private IDisposable _purchases;
        private StationPad _pad;
        private bool _initialized;

        /// <summary>What the bench offers now: its first ability not yet bought (its last once all are).</summary>
        public UpgradeDefinition Definition => _definitions.Length == 0 ? null
            : _definitions[_upgrades == null ? 0 : OfferIndex(_definitions, _upgrades)];

        /// <summary>True while 07 is parked on the pad.</summary>
        public bool Occupied => _pad != null && _pad.Occupied;

        public Vector3 PadCentre => _pad != null ? _pad.Centre : Vector3.zero;

        /// <summary>Current pad brightness (tests and debugging views).</summary>
        public float PadLevel => _pad != null ? _pad.Level : 0f;

        public WorkshopTuning Tuning => _tuning;

        public bool Sells(UpgradeDefinition definition)
        {
            return definition != null && Sells(definition.Id);
        }

        internal void Wire(WorkshopTuning tuning, UpgradeDefinition[] definitions, Transform anchor)
        {
            _tuning = tuning;
            _definitions = definitions;
            _anchor = anchor;
        }

        internal bool Initialize(GameplayServices services, UpgradeService upgrades)
        {
            if (upgrades == null)
            {
                throw new ArgumentNullException(nameof(upgrades));
            }

            string problem = WiringProblem(upgrades);
            if (problem != null)
            {
                Debug.LogError($"{nameof(Workshop)}: {problem}", this);
                enabled = false;
                return false;
            }

            _rover = services.Rover;
            _upgrades = upgrades;
            _pad = new StationPad("WorkshopPad", transform, services.Terrain,
                _anchor.TransformPoint(_tuning.PadOffset), _tuning.PadLook, services.Visuals.WarmRing);
            _purchases = services.Events.Subscribe<UpgradePurchased>(OnPurchased);
            _initialized = true;
            return true;
        }

        private string WiringProblem(UpgradeService upgrades)
        {
            if (_tuning == null)
            {
                return "WorkshopTuning is not assigned.";
            }

            if (_anchor == null)
            {
                return "the workshop anchor is not assigned.";
            }

            if (_definitions == null || _definitions.Length == 0)
            {
                return "it sells nothing (no upgrade definitions).";
            }

            for (int i = 0; i < _definitions.Length; i++)
            {
                if (_definitions[i] == null)
                {
                    return $"upgrade {i} is not assigned.";
                }

                if (_definitions[i].Station != UpgradeStationKind.Workshop)
                {
                    return $"'{_definitions[i].Id}' is sold at the {_definitions[i].Station}, not the workshop.";
                }

                if (upgrades.Find(_definitions[i].Id) == null)
                {
                    return $"'{_definitions[i].Id}' is not one of the GameplaySystem's upgrades.";
                }
            }

            return null;
        }

        private bool Sells(string upgradeId)
        {
            for (int i = 0; i < _definitions.Length; i++)
            {
                if (string.Equals(_definitions[i].Id, upgradeId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Index of what a station selling <paramref name="definitions"/> (in order) offers now: the first not yet
        /// fully bought, or the last once all are.
        /// </summary>
        internal static int OfferIndex(UpgradeDefinition[] definitions, UpgradeService upgrades)
        {
            int last = definitions.Length - 1;
            for (int i = 0; i < last; i++)
            {
                if (upgrades.LevelOf(definitions[i].Id) < definitions[i].MaxLevel)
                {
                    return i;
                }
            }

            return last;
        }

        private void OnPurchased(UpgradePurchased purchase)
        {
            if (Sells(purchase.UpgradeId))
            {
                _pad.Flare(_tuning.PurchaseFlare);
            }
        }

        private void Update()
        {
            if (!_initialized)
            {
                return;
            }

            UpgradeDefinition offer = Definition;
            _upgrades.TryGetOffer(offer.Id, out UpgradeOffer current);
            _pad.Tick(_rover.Position, current.IsMaxed, current.CanAfford, Time.time, Time.deltaTime);
        }

        private void OnDestroy()
        {
            _purchases?.Dispose();
            _pad?.Dispose();
        }
    }
}

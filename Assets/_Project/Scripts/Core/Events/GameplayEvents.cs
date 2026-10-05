using UnityEngine;

namespace MoonProject.Core.Events
{
    /// <summary>A scrap piece reached the rover. <see cref="ComboStep"/> climbs while pickups chain (melody step).</summary>
    public readonly struct ScrapCollected
    {
        public ScrapCollected(Vector3 position, int value, int comboStep)
        {
            Position = position;
            Value = value;
            ComboStep = comboStep;
        }

        public Vector3 Position { get; }

        public int Value { get; }

        /// <summary>0 for the first pickup of a chain, +1 for each pickup that follows within the combo window.</summary>
        public int ComboStep { get; }
    }

    /// <summary>The scrap balance changed (pickup, purchase, refund).</summary>
    public readonly struct CurrencyChanged
    {
        public CurrencyChanged(int total, int delta)
        {
            Total = total;
            Delta = delta;
        }

        public int Total { get; }

        public int Delta { get; }
    }

    /// <summary>The rover emitted a sonar ping from <see cref="Origin"/>.</summary>
    public readonly struct SonarPinged
    {
        public SonarPinged(Vector3 origin, float range)
        {
            Origin = origin;
            Range = range;
        }

        public Vector3 Origin { get; }

        public float Range { get; }
    }

    /// <summary>A buried relic answered a ping (published at the moment the answer is heard).</summary>
    public readonly struct RelicAnswered
    {
        public RelicAnswered(Vector3 position, float distance)
        {
            Position = position;
            Distance = distance;
        }

        public Vector3 Position { get; }

        public float Distance { get; }
    }

    /// <summary>The tractor beam started lifting a buried relic.</summary>
    public readonly struct ExcavationStarted
    {
        public ExcavationStarted(Vector3 position)
        {
            Position = position;
        }

        public Vector3 Position { get; }
    }

    /// <summary>The tractor beam stopped; <see cref="Completed"/> is true when the relic fully surfaced.</summary>
    public readonly struct ExcavationStopped
    {
        public ExcavationStopped(Vector3 position, bool completed)
        {
            Position = position;
            Completed = completed;
        }

        public Vector3 Position { get; }

        public bool Completed { get; }
    }

    /// <summary>A relic finished surfacing and is now a loose physics object.</summary>
    public readonly struct RelicSurfaced
    {
        public RelicSurfaced(Vector3 position, string relicId)
        {
            Position = position;
            RelicId = relicId;
        }

        public Vector3 Position { get; }

        public string RelicId { get; }
    }

    /// <summary>The energy tether latched onto a body.</summary>
    public readonly struct TetherAttached
    {
        public TetherAttached(Vector3 position, float mass)
        {
            Position = position;
            Mass = mass;
        }

        public Vector3 Position { get; }

        public float Mass { get; }
    }

    /// <summary>The tether let go: released by the player, or softly snapped by the anti-frustration rule.</summary>
    public readonly struct TetherReleased
    {
        public TetherReleased(Vector3 position, bool snapped)
        {
            Position = position;
            Snapped = snapped;
        }

        public Vector3 Position { get; }

        public bool Snapped { get; }
    }

    /// <summary>A relic was placed on a museum shelf at the base.</summary>
    public readonly struct RelicDeposited
    {
        public RelicDeposited(string relicId, Vector3 position, int displayedCount)
        {
            RelicId = relicId;
            Position = position;
            DisplayedCount = displayedCount;
        }

        public string RelicId { get; }

        public Vector3 Position { get; }

        /// <summary>Relics on display after this deposit.</summary>
        public int DisplayedCount { get; }
    }

    /// <summary>An upgrade (rover or base) was bought.</summary>
    public readonly struct UpgradePurchased
    {
        public UpgradePurchased(string upgradeId, int level)
        {
            UpgradeId = upgradeId;
            Level = level;
        }

        public string UpgradeId { get; }

        public int Level { get; }
    }

    /// <summary>The radio tower's clear-signal radius changed (tower upgrade, load).</summary>
    public readonly struct SignalRadiusChanged
    {
        public SignalRadiusChanged(float radius)
        {
            Radius = radius;
        }

        public float Radius { get; }
    }
}

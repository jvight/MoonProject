namespace MoonProject.Gameplay
{
    /// <summary>What the shop can show for one upgrade right now (a snapshot; allocation-free struct).</summary>
    public readonly struct UpgradeOffer
    {
        public UpgradeOffer(UpgradeDefinition definition, int currentLevel, bool canAfford)
        {
            Definition = definition;
            CurrentLevel = currentLevel;
            CanAfford = canAfford;
        }

        public UpgradeDefinition Definition { get; }

        /// <summary>Levels already bought (0 = none).</summary>
        public int CurrentLevel { get; }

        public int MaxLevel => Definition.MaxLevel;

        public bool IsMaxed => CurrentLevel >= Definition.MaxLevel;

        /// <summary>The next level on offer, or null when maxed.</summary>
        public UpgradeLevel Next => IsMaxed ? null : Definition.Levels[CurrentLevel];

        /// <summary>The next level's recipe (free when maxed).</summary>
        public Recipe NextCost => IsMaxed ? default : Definition.Levels[CurrentLevel].Recipe;

        /// <summary>True when 07's materials cover the next level's recipe.</summary>
        public bool CanAfford { get; }
    }
}

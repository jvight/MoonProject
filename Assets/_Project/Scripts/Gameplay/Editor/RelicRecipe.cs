namespace MoonProject.Gameplay.Editor
{
    /// <summary>The authored data of one relic, turned into a RelicDefinition asset by the content builder.</summary>
    internal readonly struct RelicRecipe
    {
        public RelicRecipe(string id, string displayName, string memory, float mass, int answerNote,
            RelicPlacementBand placement)
        {
            Id = id;
            DisplayName = displayName;
            Memory = memory;
            Mass = mass;
            AnswerNote = answerNote;
            Placement = placement;
        }

        public string Id { get; }

        public string DisplayName { get; }

        public string Memory { get; }

        public float Mass { get; }

        public int AnswerNote { get; }

        public RelicPlacementBand Placement { get; }
    }
}

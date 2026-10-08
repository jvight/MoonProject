using UnityEngine;

namespace MoonProject.Gameplay.Editor
{
    /// <summary>The authored data of one relic, turned into a RelicDefinition asset by the content builder.</summary>
    internal readonly struct RelicRecipe
    {
        public RelicRecipe(string id, float mass, int answerNote, string site, Vector2 heartOffset)
        {
            Id = id;
            Mass = mass;
            AnswerNote = answerNote;
            Site = site;
            HeartOffset = heartOffset;
        }

        public string Id { get; }

        public float Mass { get; }

        public int AnswerNote { get; }

        /// <summary>The salvage site (see <see cref="SalvageEconomy.SiteNames"/>) whose heart it rests in.</summary>
        public string Site { get; }

        /// <summary>Offset (m) from the heart in the site's frame (x right, y forward).</summary>
        public Vector2 HeartOffset { get; }
    }
}

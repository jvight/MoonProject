using UnityEngine;

namespace MoonProject.Gameplay.Editor
{
    /// <summary>The authored data of one relic, turned into a RelicDefinition asset by the content builder.</summary>
    internal readonly struct RelicRecipe
    {
        public RelicRecipe(string id, string crew, float mass, int answerNote, string site, Vector2 heartOffset)
        {
            Crew = crew;
            Id = id;
            Mass = mass;
            AnswerNote = answerNote;
            Site = site;
            HeartOffset = heartOffset;
        }

        public string Id { get; }

        /// <summary>Whose belongings it is (a crew id).</summary>
        public string Crew { get; }

        public float Mass { get; }

        public int AnswerNote { get; }

        /// <summary>The salvage site (see <see cref="SalvageEconomy.SiteNames"/>) whose heart it rests in.</summary>
        public string Site { get; }

        /// <summary>Offset (m) from the heart in the site's frame (x right, y forward).</summary>
        public Vector2 HeartOffset { get; }
    }
}

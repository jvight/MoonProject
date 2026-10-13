using UnityEngine;

namespace MoonProject.Gameplay.Editor
{
    /// <summary>The authored data of one relic, turned into a RelicDefinition asset by the content builder.</summary>
    internal readonly struct RelicRecipe
    {
        public RelicRecipe(string id, string crew, float mass, int answerNote, string site)
        {
            Crew = crew;
            Id = id;
            Mass = mass;
            AnswerNote = answerNote;
            Site = site;
        }

        public string Id { get; }

        /// <summary>Whose belongings it is (a crew id).</summary>
        public string Crew { get; }

        public float Mass { get; }

        public int AnswerNote { get; }

        /// <summary>
        /// The salvage site (see <see cref="SalvageEconomy.SiteNames"/>) it rests at, under its wreck's debris in
        /// Art's RelicPocket_&lt;id&gt; (0.5.5 §2).
        /// </summary>
        public string Site { get; }
    }
}

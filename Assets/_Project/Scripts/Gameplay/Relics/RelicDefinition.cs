using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// One lost memory of Earth: its id, how heavy it is to tow, its look (an Art prefab from the M2 content contract),
    /// the note it sings when it answers the sonar and the salvage site whose heart it waits in (docs/features/M3-13).
    /// Its name and memory text are player-facing prose and live in the localization tables under
    /// "relic.&lt;id&gt;.*". Written by the Gameplay/Content builder; runtime code only reads it.
    /// </summary>
    [CreateAssetMenu(menuName = "MoonProject/Gameplay/Relic Definition", fileName = "Relic")]
    public sealed class RelicDefinition : ScriptableObject
    {
        [Tooltip("Stable id from the content contract (e.g. rubber_duck). Saves, Art prefab names and the " +
                 "localization keys relic.<id>.name / relic.<id>.memory use it.")]
        [SerializeField] private string _id = string.Empty;

        [Tooltip("Physics mass (kg). Heavier relics surface slower and trail more lazily on the tether.")]
        [Range(1f, 40f)] [SerializeField] private float _mass = 6f;

        [Tooltip("Art prefab (Generated/Art/Relics/Relic_<id>.prefab): meshes only, pivot at the centre of mass.")]
        [SerializeField] private GameObject _prefab;

        [Tooltip("Note this relic answers the sonar with: index into the D major pentatonic ladder starting at D5 " +
                 "(0 = D5, 1 = E5, 2 = F#5, 3 = A5, 4 = B5, 5 = D6 ...).")]
        [Range(0, 9)] [SerializeField] private int _answerNote;

        [Tooltip("The salvage site whose heart it waits in (its anchor id, e.g. site.depot).")]
        [SerializeField] private string _siteId = string.Empty;

        [Tooltip("Offset (m) from the site's Heart in the site's frame (x to its right, y along its forward), for a " +
                 "second relic sharing one heart.")]
        [SerializeField] private Vector2 _heartOffset;

        public string Id => _id;

        public float Mass => _mass;

        public GameObject Prefab => _prefab;

        public int AnswerNote => _answerNote;

        public string SiteId => _siteId;

        public Vector2 HeartOffset => _heartOffset;

        /// <summary>Null when the definition is complete, else what is wrong with it.</summary>
        public string Validate()
        {
            if (string.IsNullOrWhiteSpace(_id))
            {
                return "has no id";
            }

            if (_prefab == null)
            {
                return $"'{_id}' has no prefab (Generated/Art/Relics/Relic_{_id}.prefab)";
            }

            return _siteId != null && _siteId.StartsWith(WorldAnchorIds.SitePrefix, StringComparison.Ordinal)
                ? null
                : $"'{_id}' names no salvage site ('{_siteId}')";
        }

        internal void Populate(string id, float mass, GameObject prefab, int answerNote, string siteId,
            Vector2 heartOffset)
        {
            _id = id;
            _mass = mass;
            _prefab = prefab;
            _answerNote = answerNote;
            _siteId = siteId;
            _heartOffset = heartOffset;
        }
    }
}

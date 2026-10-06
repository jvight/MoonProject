using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// One lost memory of Earth: its id, how heavy it is to tow, its look (an Art prefab from the M2 content contract)
    /// and the note it sings when it answers the sonar. Its name and memory text are player-facing prose and live in
    /// the localization tables under "relic.&lt;id&gt;.*". Written by the Gameplay/Content builder; runtime code only
    /// reads it.
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

        [Tooltip("Which part of the crater it is buried in.")]
        [SerializeField] private RelicPlacementBand _placement = RelicPlacementBand.Wanderer;

        public string Id => _id;

        public float Mass => _mass;

        public GameObject Prefab => _prefab;

        public int AnswerNote => _answerNote;

        public RelicPlacementBand Placement => _placement;

        /// <summary>Null when the definition is complete, else what is wrong with it.</summary>
        public string Validate()
        {
            if (string.IsNullOrWhiteSpace(_id))
            {
                return "has no id";
            }

            return _prefab == null ? $"'{_id}' has no prefab (Generated/Art/Relics/Relic_{_id}.prefab)" : null;
        }

        internal void Populate(string id, float mass, GameObject prefab, int answerNote, RelicPlacementBand placement)
        {
            _id = id;
            _mass = mass;
            _prefab = prefab;
            _answerNote = answerNote;
            _placement = placement;
        }
    }
}

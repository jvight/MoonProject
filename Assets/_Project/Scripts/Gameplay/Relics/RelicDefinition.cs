using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// One lost memory of Earth: identity, the text the museum shows, how heavy it is to tow, its look (an Art
    /// prefab from the M2 content contract) and the note it sings when it answers the sonar. Written by the
    /// Gameplay/Content builder; runtime code only reads it.
    /// </summary>
    [CreateAssetMenu(menuName = "MoonProject/Gameplay/Relic Definition", fileName = "Relic")]
    public sealed class RelicDefinition : ScriptableObject
    {
        [Tooltip("Stable id from the content contract (e.g. rubber_duck). Saves and art prefab names use it.")]
        [SerializeField] private string _id = string.Empty;

        [Tooltip("Name shown in the museum.")]
        [SerializeField] private string _displayName = string.Empty;

        [Tooltip("One or two warm, melancholic sentences: the memory of Earth this object carries.")]
        [TextArea(2, 5)]
        [SerializeField] private string _memory = string.Empty;

        [Tooltip("Physics mass (kg). Heavier relics surface slower and trail more lazily on the tether.")]
        [Range(1f, 40f)] [SerializeField] private float _mass = 6f;

        [Tooltip("Art prefab (Generated/Art/Relics/Relic_<id>.prefab): meshes only, pivot at the centre of mass. " +
                 "Empty until Art delivers it: such a relic stays a buried site that answers the sonar.")]
        [SerializeField] private GameObject _prefab;

        [Tooltip("Note this relic answers the sonar with: index into the D major pentatonic ladder starting at D5 " +
                 "(0 = D5, 1 = E5, 2 = F#5, 3 = A5, 4 = B5, 5 = D6 ...).")]
        [Range(0, 9)] [SerializeField] private int _answerNote;

        [Tooltip("Which part of the crater it is buried in.")]
        [SerializeField] private RelicPlacementBand _placement = RelicPlacementBand.Wanderer;

        public string Id => _id;

        public string DisplayName => _displayName;

        public string Memory => _memory;

        public float Mass => _mass;

        public GameObject Prefab => _prefab;

        /// <summary>True once Art's model exists; without it the relic answers the sonar but stays buried.</summary>
        public bool HasModel => _prefab != null;

        public int AnswerNote => _answerNote;

        public RelicPlacementBand Placement => _placement;

        /// <summary>Null when the definition is complete, else what is wrong with it.</summary>
        public string Validate()
        {
            if (string.IsNullOrWhiteSpace(_id))
            {
                return "has no id";
            }

            if (string.IsNullOrWhiteSpace(_displayName))
            {
                return $"'{_id}' has no display name";
            }

            return string.IsNullOrWhiteSpace(_memory) ? $"'{_id}' has no memory text" : null;
        }

        internal void Populate(string id, string displayName, string memory, float mass, GameObject prefab,
            int answerNote, RelicPlacementBand placement)
        {
            _id = id;
            _displayName = displayName;
            _memory = memory;
            _mass = mass;
            _prefab = prefab;
            _answerNote = answerNote;
            _placement = placement;
        }
    }
}

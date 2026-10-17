using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// The west signal pillar (opening v2 B11): Art's R1 beam of light over the dead relay mast, hidden until Gameplay
    /// shows it when the antenna mast hears the west. Whenever it is shown, its beam glows at the tuned strength so it
    /// reads from the base across the night basin.
    /// </summary>
    public sealed class SignalPillar : MonoBehaviour
    {
        private static readonly int EmissionStrengthId = Shader.PropertyToID("_EmissionStrength");

        [Tooltip("World tuning (Assets/_Project/Data/Tuning/WorldSettings.asset); its landmarks' signal pillar.")]
        [SerializeField] private WorldSettings _settings;

        [Tooltip("Art's R1 beam (SignalPillar/PillarBeam, R1_LampAmber).")]
        [SerializeField] private Renderer _beam;

        private void OnEnable()
        {
            if (_settings == null || _beam == null)
            {
                Debug.LogError($"{nameof(SignalPillar)}: World Settings and the beam must be assigned (the scene build " +
                    "wires them).", this);
                return;
            }

            // A property block made here, not in a field initializer: Unity refuses one while it constructs the object.
            var block = new MaterialPropertyBlock();
            block.SetFloat(EmissionStrengthId, _settings.Landmarks.SignalPillarGlow);
            _beam.SetPropertyBlock(block);
        }
    }
}

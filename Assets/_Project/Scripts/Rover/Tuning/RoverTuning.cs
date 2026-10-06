using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// Every number behind how the rover drives. Read-only at runtime; the defaults in each section are the shipped
    /// feel (validated by the PlayMode feel metrics), the asset lives in Assets/_Project/Data/Tuning.
    /// </summary>
    [CreateAssetMenu(fileName = "RoverTuning", menuName = "MoonProject/Rover/Rover Tuning")]
    public sealed class RoverTuning : ScriptableObject
    {
        [SerializeField] private DriveSettings _drive = new DriveSettings();
        [SerializeField] private SteeringSettings _steering = new SteeringSettings();
        [SerializeField] private GroundSettings _ground = new GroundSettings();
        [SerializeField] private LandingSettings _landing = new LandingSettings();

        public DriveSettings Drive => _drive;

        public SteeringSettings Steering => _steering;

        public GroundSettings Ground => _ground;

        public LandingSettings Landing => _landing;
    }
}

using System;
using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary>
    /// The station panel's beat after a purchase (docs/features/M3-14, VISION ruling 14: the station does the work 07
    /// has no hands for), pure logic, EditMode-tested. While the station works the ring rests full and nothing is
    /// said; the station's line comes when its work shows (<see cref="Showed"/>: the radio tower's beam starts
    /// stitching up the new section, the Rover Bay sets the piece on 07), glows for
    /// <see cref="TowerPanelSettings.CelebrateSeconds"/>, and the panel moves on. A station that never shows its work
    /// within <see cref="TowerPanelSettings.CueWaitSeconds"/> is a fault the caller reports.
    /// </summary>
    internal sealed class StationWork
    {
        private readonly TowerPanelSettings _settings;
        private float _timer;

        public StationWork(TowerPanelSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>What one step of <see cref="Step"/> ended.</summary>
        public enum Outcome
        {
            /// <summary>Nothing ended: idle, working or glowing.</summary>
            None = 0,

            /// <summary>The line has glowed its time: the panel moves on.</summary>
            Done = 1,

            /// <summary>The station never showed its work: a fault; the panel moves on.</summary>
            Overdue = 2,
        }

        /// <summary>The upgrade being worked on, or null.</summary>
        public string Upgrade { get; private set; }

        /// <summary>Where it is being worked on.</summary>
        public UpgradeStationKind Station { get; private set; }

        /// <summary>True from the purchase until the line has glowed its time.</summary>
        public bool IsBusy => Upgrade != null;

        /// <summary>True while the station works and the line has not come yet.</summary>
        public bool IsWorking => IsBusy && !IsShowingLine;

        /// <summary>True while the station's line glows.</summary>
        public bool IsShowingLine { get; private set; }

        /// <summary>
        /// Starts waiting for <paramref name="station"/> to show its work on <paramref name="upgradeId"/>.
        /// </summary>
        public void Begin(UpgradeStationKind station, string upgradeId)
        {
            if (string.IsNullOrEmpty(upgradeId))
            {
                throw new ArgumentException("A purchase needs an upgrade id.", nameof(upgradeId));
            }

            Upgrade = upgradeId;
            Station = station;
            IsShowingLine = false;
            _timer = 0f;
        }

        /// <summary>
        /// <paramref name="station"/> showed its work on <paramref name="upgradeId"/>. True when that is the work
        /// being waited for: its line starts glowing.
        /// </summary>
        public bool Showed(UpgradeStationKind station, string upgradeId)
        {
            if (!IsWorking || station != Station || !string.Equals(upgradeId, Upgrade, StringComparison.Ordinal))
            {
                return false;
            }

            IsShowingLine = true;
            _timer = 0f;
            return true;
        }

        /// <param name="deltaTime">Unscaled seconds; 0 while paused, so the wait and the glow pause too.</param>
        public Outcome Step(float deltaTime)
        {
            if (!IsBusy)
            {
                return Outcome.None;
            }

            _timer += deltaTime;
            if (IsShowingLine && _timer >= _settings.CelebrateSeconds)
            {
                Clear();
                return Outcome.Done;
            }

            if (!IsShowingLine && _timer >= _settings.CueWaitSeconds)
            {
                Clear();
                return Outcome.Overdue;
            }

            return Outcome.None;
        }

        /// <summary>Forgets the work (07 left the pad).</summary>
        public void Clear()
        {
            Upgrade = null;
            IsShowingLine = false;
            _timer = 0f;
        }
    }
}

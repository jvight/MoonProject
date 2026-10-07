using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Whether 07 is home, as friends see it: home within the home radius, gone again only past the (wider) leave
    /// radius, and a return counts as coming home (worth a greeting) only after 07 was truly away. Pure.
    /// </summary>
    public sealed class RoverHomeWatch
    {
        private bool _wasAway;

        /// <summary>What changed this frame.</summary>
        public enum Change
        {
            None = 0,

            /// <summary>07 is back from far away: greet it.</summary>
            CameHome = 1,

            /// <summary>07 is home again after a short hop out (no greeting).</summary>
            Returned = 2,

            /// <summary>07 has left home.</summary>
            Left = 3,
        }

        /// <summary>07 is home.</summary>
        public bool Home { get; private set; }

        /// <summary>Starts watching with 07 at home (<paramref name="home"/>) or away.</summary>
        public void Reset(bool home)
        {
            Home = home;
            _wasAway = !home;
        }

        public Change Step(Vector3 rover, Vector3 home, FriendTuning tuning)
        {
            float distance = SurfaceRules.HorizontalDistance(rover, home);
            if (distance > tuning.AwayRadius)
            {
                _wasAway = true;
            }

            bool isHome = Home ? distance < tuning.LeaveRadius : distance < tuning.HomeRadius;
            Change change = Change.None;
            if (isHome && !Home)
            {
                change = _wasAway ? Change.CameHome : Change.Returned;
                _wasAway = false;
            }
            else if (!isHome && Home)
            {
                change = Change.Left;
            }

            Home = isHome;
            return change;
        }
    }
}

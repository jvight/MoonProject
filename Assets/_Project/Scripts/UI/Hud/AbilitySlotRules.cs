using System;

namespace MoonProject.UI
{
    /// <summary>
    /// The ability slots' rules (pure logic, EditMode-tested): which slots are earned, which wear the amber ring (in use
    /// now, or earned within <see cref="AbilitySlotsSettings.EarnedSeconds"/>), and whether the row shows at all. The
    /// row is no permanent HUD: it rests in view while 07 is still, and comes up whenever a ring is lit. Abilities
    /// owned on the first tick are a loaded save, never "just earned".
    /// </summary>
    internal sealed class AbilitySlotRules
    {
        private readonly AbilitySlotsSettings _settings;
        private readonly bool[] _owned;
        private readonly float[] _sinceEarned;
        private readonly bool[] _ringed;
        private bool _armed;
        private float _still;

        public AbilitySlotRules(AbilitySlotsSettings settings, int slots)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _owned = new bool[slots];
            _sinceEarned = new float[slots];
            _ringed = new bool[slots];
            for (int i = 0; i < slots; i++)
            {
                _sinceEarned[i] = float.PositiveInfinity;
            }
        }

        /// <summary>True while the row should be on screen.</summary>
        public bool Shown { get; private set; }

        public bool IsOwned(int slot)
        {
            return _owned[slot];
        }

        public bool IsRinged(int slot)
        {
            return _ringed[slot];
        }

        /// <summary>Records <paramref name="slot"/>'s state this frame; call for every slot, then <see cref="Finish"/>.</summary>
        public void Slot(int slot, bool owned, bool inUse, float deltaTime)
        {
            if (owned && !_owned[slot] && _armed)
            {
                _sinceEarned[slot] = 0f;
            }
            else if (_sinceEarned[slot] < _settings.EarnedSeconds)
            {
                _sinceEarned[slot] += deltaTime;
            }

            _owned[slot] = owned;
            _ringed[slot] = owned && (inUse || _sinceEarned[slot] < _settings.EarnedSeconds);
        }

        /// <param name="speed">07's speed, metres per second.</param>
        public void Finish(float speed, float deltaTime)
        {
            _armed = true;
            _still = speed <= _settings.StillSpeed ? _still + deltaTime : 0f;
            bool ringed = false;
            for (int i = 0; i < _ringed.Length; i++)
            {
                ringed |= _ringed[i];
            }

            Shown = ringed || _still >= _settings.StillSeconds;
        }
    }
}

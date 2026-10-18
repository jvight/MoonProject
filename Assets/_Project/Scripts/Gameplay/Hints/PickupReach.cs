using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The press that takes something waiting (a cut piece held on its wreck while the hold was full). Each frame the
    /// fields offer what waits; the nearest one within reach is the candidate, shown as a world prompt with the key and
    /// the verb (<see cref="InteractionKind.PickUp"/>, its name from <see cref="TryGet"/>), and Excavate held takes it,
    /// like every other E verb. Reach is measured across the ground, so something a little above or below 07 still
    /// counts. Fields step in different components, so a frame's offers are settled on the next frame's first offer.
    /// Allocates nothing.
    /// </summary>
    public sealed class PickupReach
    {
        /// <summary>Owner id of the pieces held because the hold was full (<see cref="CargoBundles"/>).</summary>
        public const int Bundles = 4;

        /// <summary>Owner id of the open supply crates' contents (<see cref="SupplyCaches"/>).</summary>
        public const int OpenCrates = 5;

        private int _frame = int.MinValue;
        private bool _held;
        private float _bestSq;
        private bool _nextHas;
        private Vector3 _nextPosition;
        private string _nextNoun;
        private int _nextOwner;
        private int _nextIndex;
        private int _owner;
        private int _index;
        private bool _has;
        private Vector3 _position;
        private string _noun;

        /// <summary>
        /// Offers the part <paramref name="index"/> of field <paramref name="owner"/> resting at <paramref name="at"/>.
        /// True when it is the candidate and Excavate is held: the field lifts it now.
        /// </summary>
        public bool Offer(int frame, bool held, int owner, int index, Vector3 at, Vector3 rover, float reach,
            string nounKey)
        {
            Roll(frame, held);
            float distanceSq = SurfaceRules.HorizontalDistanceSquared(at, rover);
            if (distanceSq > reach * reach || Mathf.Abs(at.y - rover.y) > reach)
            {
                return false;
            }

            if (!_nextHas || distanceSq < _bestSq)
            {
                _nextHas = true;
                _bestSq = distanceSq;
                _nextPosition = at;
                _nextNoun = nounKey;
                _nextOwner = owner;
                _nextIndex = index;
            }

            return _held && _has && _owner == owner && _index == index;
        }

        /// <summary>
        /// The candidate as of <paramref name="frame"/> (for the prompt): where it rests and its name key, the noun
        /// after the verb. False when nothing waits within reach.
        /// </summary>
        public bool TryGet(int frame, out Vector3 at, out string nounKey)
        {
            bool has = frame == _frame ? _has : frame == _frame + 1 && _nextHas;
            at = frame == _frame ? _position : _nextPosition;
            nounKey = has ? frame == _frame ? _noun : _nextNoun : null;
            return has;
        }

        /// <summary>Settles the last frame's offers on the first offer of a frame.</summary>
        private void Roll(int frame, bool held)
        {
            if (frame == _frame)
            {
                return;
            }

            bool consecutive = frame == _frame + 1;
            _has = consecutive && _nextHas;
            _position = _nextPosition;
            _noun = _nextNoun;
            _owner = _nextOwner;
            _index = _nextIndex;
            _held = held;
            _nextHas = false;
            _frame = frame;
        }
    }
}

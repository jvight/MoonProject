using System;
using System.Collections.Generic;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The socket rack at the pad as pure state (EditMode-tested; docs/design/opening-v3-plan.md 4.3, ruling 5). The
    /// rack is held until the west signal answers. It then shows at 07's next dock (the last step of the release's
    /// stagger), with an outline in each bay whose part 07 has seen and a filled bay for each part fitted. A bay is
    /// seen at the release or once a fragment of its part's blueprint is scanned; a fitted bay stays filled.
    /// </summary>
    public sealed class SocketRack
    {
        private readonly RackBaySettings[] _bays;
        private readonly bool[] _seen;
        private readonly bool[] _filled;

        public SocketRack(IReadOnlyList<RackBaySettings> bays)
        {
            if (bays == null)
            {
                throw new ArgumentNullException(nameof(bays));
            }

            _bays = new RackBaySettings[bays.Count];
            for (int i = 0; i < bays.Count; i++)
            {
                _bays[i] = bays[i] ?? throw new ArgumentException($"rack bay {i} is null", nameof(bays));
            }

            _seen = new bool[_bays.Length];
            _filled = new bool[_bays.Length];
        }

        /// <summary>The west signal has answered: the rack may show at the next dock.</summary>
        public bool Released { get; private set; }

        /// <summary>The rack stands at the pad (it showed at a dock after the release).</summary>
        public bool Shown { get; private set; }

        public int Count => _bays.Length;

        /// <summary>Goes up by one each time anything the rack shows changes (views redraw then).</summary>
        public int Version { get; private set; }

        public string IdOf(int bay)
        {
            return _bays[bay].Id;
        }

        public RackBayState StateOf(int bay)
        {
            if (!Shown)
            {
                return RackBayState.Hidden;
            }

            if (_filled[bay])
            {
                return RackBayState.Filled;
            }

            return _bays[bay].SeenAtRelease || _seen[bay] ? RackBayState.Outline : RackBayState.Hidden;
        }

        /// <summary>The west signal answered (a replay at session start too).</summary>
        public void Release()
        {
            Released = true;
        }

        /// <summary>07 docked: a released rack shows now. True when it appeared this time.</summary>
        public bool Docked()
        {
            if (!Released || Shown)
            {
                return false;
            }

            Shown = true;
            Version++;
            return true;
        }

        /// <summary>A fragment of <paramref name="blueprintId"/> was scanned: bays of that blueprint are seen.</summary>
        public void BlueprintScanned(string blueprintId)
        {
            if (string.IsNullOrEmpty(blueprintId))
            {
                return;
            }

            for (int i = 0; i < _bays.Length; i++)
            {
                if (!_seen[i] && string.Equals(_bays[i].SeenByBlueprint, blueprintId, StringComparison.Ordinal))
                {
                    _seen[i] = true;
                    Version++;
                }
            }
        }

        /// <summary><paramref name="piece"/> was fitted on 07: its bay fills (and is seen).</summary>
        public void Fitted(RoverKitPiece piece)
        {
            for (int i = 0; i < _bays.Length; i++)
            {
                if (!_filled[i] && _bays[i].FilledBy == piece)
                {
                    _filled[i] = true;
                    _seen[i] = true;
                    Version++;
                }
            }
        }

        public RackSaveData Capture()
        {
            var seen = new List<string>();
            var filled = new List<string>();
            for (int i = 0; i < _bays.Length; i++)
            {
                if (_seen[i])
                {
                    seen.Add(_bays[i].Id);
                }

                if (_filled[i])
                {
                    filled.Add(_bays[i].Id);
                }
            }

            return new RackSaveData { shown = Shown, seen = seen.ToArray(), filled = filled.ToArray() };
        }

        /// <summary>A save's rack (null: a new game). Ids no bay has any more are dropped.</summary>
        public void Restore(RackSaveData data)
        {
            Shown = data != null && data.shown;
            for (int i = 0; i < _bays.Length; i++)
            {
                _seen[i] = data != null && Contains(data.seen, _bays[i].Id);
                _filled[i] = data != null && Contains(data.filled, _bays[i].Id);
            }

            Version++;
        }

        private static bool Contains(string[] ids, string id)
        {
            if (ids == null)
            {
                return false;
            }

            for (int i = 0; i < ids.Length; i++)
            {
                if (string.Equals(ids[i], id, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

using System;
using UnityEngine;
using MoonProject.Core;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// A station's pad (docs/features/M3-16, M3-15 section 4): Art's worn painted circle on the dust (or level on a
    /// station's deck) with six small lamps round its rim, the size nearest the station's parking radius. It shows
    /// only while its station is awake and has something for 07 to do; then its lamps breathe softly while nothing is
    /// affordable, glow inviting when the offer is affordable and brightly under 07, and a purchase flares them. A
    /// sleeping station (<see cref="Rest"/>) or a finished one shows nothing: its lamps ease out (the last purchase's
    /// flare included), then the pad is put away. It knows when 07 is parked on it.
    /// </summary>
    public sealed class StationPad : IDisposable
    {
        /// <summary>Art's lamp nodes on the pad's rim are PadLamp_0..5.</summary>
        public const string LampPrefix = "PadLamp_";
        public const int LampCount = 6;

        /// <summary>Lamp brightness below which a sleeping or finished pad is too faint to see and is put away.</summary>
        private const float DarkLevel = 0.01f;

        private readonly PadLook _look;
        private readonly GameObject _pad;
        private readonly EmissionGlow[] _lamps = new EmissionGlow[LampCount];
        private bool _shown = true;

        /// <summary>A pad on the ground around <paramref name="centre"/>, lying on the dust's slope.</summary>
        public StationPad(string name, Transform parent, ITerrainQuery terrain, Vector3 centre, PadLook look,
            GameplayVisuals visuals)
            : this(name, parent, Ground(terrain, centre), Tilt(terrain, centre), look, visuals)
        {
        }

        private StationPad(string name, Transform parent, Vector3 centre, Quaternion rotation, PadLook look,
            GameplayVisuals visuals)
        {
            if (visuals == null)
            {
                throw new ArgumentNullException(nameof(visuals));
            }

            _look = look;
            Centre = centre;
            _pad = Object.Instantiate(visuals.PadFor(look.Radius), centre, rotation, parent);
            _pad.name = name;
            for (int i = 0; i < LampCount; i++)
            {
                Transform lamp = _pad.transform.Find(LampPrefix + i);
                Renderer renderer = lamp != null ? lamp.GetComponent<Renderer>() : null;
                _lamps[i] = renderer != null
                    ? new EmissionGlow(renderer)
                    : throw new InvalidOperationException($"{name}: Art's pad has no {LampPrefix}{i} lamp.");
            }

            Show(false);
        }

        /// <summary>A pad lying level on a station's deck around <paramref name="deckCentre"/> (on its top).</summary>
        public static StationPad OnDeck(string name, Transform parent, Vector3 deckCentre, PadLook look,
            GameplayVisuals visuals)
        {
            return new StationPad(name, parent, deckCentre, Quaternion.identity, look, visuals);
        }

        public Vector3 Centre { get; }

        /// <summary>True while 07 is parked on it.</summary>
        public bool Occupied { get; private set; }

        /// <summary>Current brightness of its lamps.</summary>
        public float Level { get; private set; }

        /// <summary>True while the pad shows at all.</summary>
        public bool Shown => _shown;

        /// <param name="done">Everything it sells is bought: the lamps ease out and the pad goes away.</param>
        /// <param name="affordable">What it offers now is affordable.</param>
        public void Tick(Vector3 rover, bool done, bool affordable, float now, float deltaTime)
        {
            Occupied = !done && SurfaceRules.HorizontalDistance(rover, Centre) <= _look.Radius;
            float breath = 1f - _look.BreathDepth * (1f - MarkerEnvelope.Breath(now, _look.BreathPeriod));
            float target = done ? 0f
                : Occupied ? _look.Occupied
                : affordable ? _look.Inviting * breath
                : _look.Idle * breath;
            Ease(target, deltaTime);
            Show(!done || Level > DarkLevel);
        }

        /// <summary>A dormant station shows no pad: nobody is on it, its lamps ease out and it goes away.</summary>
        public void Rest(float deltaTime)
        {
            Occupied = false;
            Ease(0f, deltaTime);
            Show(Level > DarkLevel);
        }

        /// <summary>Flares the lamps to <paramref name="level"/> (a purchase); they ease back by themselves.</summary>
        public void Flare(float level)
        {
            Level = Mathf.Max(Level, level);
            Apply();
        }

        public void Dispose()
        {
            if (_pad != null)
            {
                Object.Destroy(_pad);
            }
        }

        private void Ease(float target, float deltaTime)
        {
            Level = Damp.Toward(Level, target, _look.Ease, deltaTime);
            Apply();
        }

        private void Apply()
        {
            for (int i = 0; i < _lamps.Length; i++)
            {
                _lamps[i].Apply(Level);
            }
        }

        private void Show(bool shown)
        {
            if (shown == _shown)
            {
                return;
            }

            _shown = shown;
            _pad.SetActive(shown);
        }

        private static Vector3 Ground(ITerrainQuery terrain, Vector3 centre)
        {
            if (terrain == null)
            {
                throw new ArgumentNullException(nameof(terrain));
            }

            return SurfaceRules.OnSurface(terrain, centre.x, centre.z);
        }

        private static Quaternion Tilt(ITerrainQuery terrain, Vector3 centre)
        {
            return Quaternion.FromToRotation(Vector3.up, terrain.SampleNormal(centre.x, centre.z));
        }
    }
}

using System;
using UnityEngine;
using MoonProject.Core;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// A station's shop: a ring of warm light on the ground (or level on a station's deck) that knows when 07 is
    /// parked on it. It breathes softly while there is nothing to buy, glows inviting when the offer is affordable,
    /// brightly under 07 and dims once everything is bought; a purchase flares it.
    /// </summary>
    public sealed class StationPad : IDisposable
    {
        /// <summary>Metres the ring floats above the dust.</summary>
        public const float RingLift = 0.06f;

        private readonly PadLook _look;
        private readonly TerrainRing _ring;
        private readonly GlowRenderer _glow;

        /// <summary>A pad on the ground around <paramref name="centre"/> (its height is the ground's).</summary>
        public StationPad(string name, Transform parent, ITerrainQuery terrain, Vector3 centre, PadLook look,
            Material material)
        {
            if (terrain == null)
            {
                throw new ArgumentNullException(nameof(terrain));
            }

            _look = look;
            Centre = SurfaceRules.OnSurface(terrain, centre.x, centre.z);
            _ring = new TerrainRing(look.Segments);
            _ring.Rebuild(terrain, Centre, InnerRadius(look), OuterRadius(look), RingLift);
            _glow = new GlowRenderer(GlowObject.Create(name, parent, _ring.Mesh, material));
        }

        private StationPad(string name, Transform parent, Vector3 deckCentre, PadLook look, Material material)
        {
            _look = look;
            Centre = deckCentre;
            _ring = new TerrainRing(look.Segments);
            _ring.RebuildLevel(deckCentre, InnerRadius(look), OuterRadius(look), RingLift);
            _glow = new GlowRenderer(GlowObject.Create(name, parent, _ring.Mesh, material));
        }

        /// <summary>A pad lying level on a station's deck around <paramref name="deckCentre"/> (on its top).</summary>
        public static StationPad OnDeck(string name, Transform parent, Vector3 deckCentre, PadLook look,
            Material material)
        {
            return new StationPad(name, parent, deckCentre, look, material);
        }

        public Vector3 Centre { get; }

        /// <summary>True while 07 is parked on it.</summary>
        public bool Occupied { get; private set; }

        /// <summary>Current brightness.</summary>
        public float Level { get; private set; }

        /// <param name="done">Everything it sells is bought.</param>
        /// <param name="affordable">What it offers now is affordable.</param>
        public void Tick(Vector3 rover, bool done, bool affordable, float now, float deltaTime)
        {
            Occupied = SurfaceRules.HorizontalDistance(rover, Centre) <= _look.Radius;
            float breath = 1f - _look.BreathDepth * (1f - MarkerEnvelope.Breath(now, _look.BreathPeriod));
            float target = done ? _look.Done
                : Occupied ? _look.Occupied
                : affordable ? _look.Inviting * breath
                : _look.Idle * breath;
            Level = Damp.Toward(Level, target, _look.Ease, deltaTime);
            _glow.Apply(Level);
        }

        /// <summary>Flares the ring to <paramref name="level"/> (a purchase); it eases back by itself.</summary>
        public void Flare(float level)
        {
            Level = Mathf.Max(Level, level);
            _glow.Apply(Level);
        }

        private static float InnerRadius(PadLook look)
        {
            return look.Radius - look.RingWidth * 0.5f;
        }

        private static float OuterRadius(PadLook look)
        {
            return look.Radius + look.RingWidth * 0.5f;
        }

        public void Dispose()
        {
            if (_ring.Mesh != null)
            {
                Object.Destroy(_ring.Mesh);
            }
        }
    }
}

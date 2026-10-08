using System;
using UnityEngine;
using MoonProject.Core;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// 07 feeds a station's hopper (VISION ruling 14: 07 has no hands): its beam reaches from its eye to the hopper's
    /// mouth and the recipe's materials fly in along it from the cargo socket, one bundle per material the recipe
    /// spends, a beat apart, each popping out of 07 and shrinking as it drops into the mouth. The bundles are built
    /// once (one per material, from the salvage catalog's); allocation-free per frame.
    /// </summary>
    public sealed class HopperFeed
    {
        private static readonly SalvageMaterial[] Materials =
        {
            SalvageMaterial.Metal, SalvageMaterial.Wiring, SalvageMaterial.Optics,
        };

        private readonly FeedLook _look;
        private readonly RepairBeam _beam;
        private readonly Transform[] _bundles = new Transform[Materials.Length];
        private readonly Vector3[] _bundleScales = new Vector3[Materials.Length];
        private readonly int[] _order = new int[Materials.Length];
        private readonly Vector3[] _launchedFrom = new Vector3[Materials.Length];
        private readonly bool[] _launched = new bool[Materials.Length];
        private int _count;
        private float _start = float.NegativeInfinity;

        public HopperFeed(string name, Transform parent, Material beamMaterial, SalvageCatalog bundles,
            FeedLook look)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            if (bundles == null)
            {
                throw new ArgumentNullException(nameof(bundles));
            }

            _look = look;
            _beam = new RepairBeam(name + "Beam", parent, beamMaterial, 0f, 0f);
            for (int i = 0; i < Materials.Length; i++)
            {
                GameObject bundle = Object.Instantiate(bundles.Bundle(Materials[i]), parent);
                bundle.name = name + "_" + Materials[i];
                SalvageSiteBuilder.SetLayer(bundle.transform, Layers.Pickup);
                _bundles[i] = bundle.transform;
                _bundleScales[i] = bundle.transform.localScale;
                bundle.SetActive(false);
            }
        }

        /// <summary>True from <see cref="Begin"/> until the last bundle has dropped in.</summary>
        public bool Feeding { get; private set; }

        /// <summary>Seconds the current (or last) feed takes, from the beam reaching out to the last bundle.</summary>
        public float Duration { get; private set; }

        /// <summary>Current beam brightness (tests and debugging views).</summary>
        public float BeamLevel => _beam.Level;

        /// <summary>Bundles in the air right now (tests and debugging views).</summary>
        public int BundlesInFlight
        {
            get
            {
                int flying = 0;
                for (int i = 0; i < _bundles.Length; i++)
                {
                    flying += _bundles[i].gameObject.activeSelf ? 1 : 0;
                }

                return flying;
            }
        }

        /// <summary>
        /// Seconds a feed of <paramref name="bundles"/> bundles takes (a free recipe still beams briefly).
        /// </summary>
        public static float DurationFor(int bundles, FeedLook look)
        {
            return look.BeamLead + Mathf.Max(0, bundles - 1) * look.Stagger + look.Flight;
        }

        /// <summary>0..1 progress of a bundle launched <paramref name="sinceLaunch"/> seconds ago.</summary>
        public static float FlightProgress(float sinceLaunch, FeedLook look)
        {
            return look.Flight <= 0f ? 1f : Mathf.Clamp01(sinceLaunch / look.Flight);
        }

        /// <summary>
        /// The bundle's size (share of its own) at <paramref name="progress"/>: it pops out of 07, flies full size and
        /// shrinks into the mouth, so it never appears or vanishes at full size.
        /// </summary>
        public static float BundleSize(float progress, FeedLook look)
        {
            float share = Mathf.Max(1e-3f, look.GrowShare);
            float grow = Ease.OutCubic(Mathf.Clamp01(progress / share));
            float shrink = Mathf.Clamp01((1f - progress) / share);
            return Mathf.Min(grow, shrink);
        }

        /// <summary>
        /// Starts feeding the materials <paramref name="recipe"/> spends (restarts a feed in progress).
        /// </summary>
        public void Begin(Recipe recipe, float now)
        {
            _count = 0;
            for (int i = 0; i < Materials.Length; i++)
            {
                _launched[i] = false;
                _bundles[i].gameObject.SetActive(false);
                if (recipe.Of(Materials[i]) > 0)
                {
                    _order[_count++] = i;
                }
            }

            _start = now;
            Duration = DurationFor(_count, _look);
            Feeding = true;
        }

        /// <summary>
        /// Moves the beam and the bundles: the beam from <paramref name="eye"/> to <paramref name="mouth"/>, each
        /// bundle from where <paramref name="cargo"/> was when it left. Returns true on the step the feed finishes;
        /// call it every frame (the beam fades out by itself afterwards).
        /// </summary>
        public bool Step(Vector3 eye, Vector3 cargo, Vector3 mouth, float now, float deltaTime)
        {
            float t = now - _start;
            bool finished = Feeding && t >= Duration;
            if (finished)
            {
                Feeding = false;
                for (int i = 0; i < _bundles.Length; i++)
                {
                    _bundles[i].gameObject.SetActive(false);
                }
            }

            _beam.Step(Feeding, true, eye, mouth, now, deltaTime);
            if (!Feeding)
            {
                return finished;
            }

            for (int slot = 0; slot < _count; slot++)
            {
                StepBundle(_order[slot], t - _look.BeamLead - slot * _look.Stagger, cargo, mouth);
            }

            return false;
        }

        private void StepBundle(int material, float sinceLaunch, Vector3 cargo, Vector3 mouth)
        {
            Transform bundle = _bundles[material];
            if (sinceLaunch < 0f)
            {
                return;
            }

            if (!_launched[material])
            {
                _launched[material] = true;
                _launchedFrom[material] = cargo;
                bundle.gameObject.SetActive(true);
            }

            float progress = FlightProgress(sinceLaunch, _look);
            if (progress >= 1f)
            {
                bundle.gameObject.SetActive(false);
                return;
            }

            Vector3 from = _launchedFrom[material];
            Vector3 position = PickupFlight.Evaluate(from, mouth, progress, 0f, 1f, _look.Lift, 0f, 0f);
            Vector3 travel = mouth - from;
            travel.y = 0f;
            Quaternion rotation = travel.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(travel) : bundle.rotation;
            bundle.SetPositionAndRotation(position, rotation);
            bundle.localScale = _bundleScales[material] * BundleSize(progress, _look);
        }
    }
}

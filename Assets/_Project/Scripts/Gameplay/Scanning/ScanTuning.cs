using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Scanning (docs/design/0.5.5-owner-feedback.md §1): the blueprints' fragment sets, how long a fragment takes to
    /// scan by its size, how far its corner brackets show, and the closing dissolve. Created by the Gameplay/Tuning
    /// builder; runtime code only reads it.
    /// </summary>
    public sealed class ScanTuning : ScriptableObject
    {
        [Header("The sets (§1.3)")]
        [Tooltip("Every blueprint's fragments, in Art's set names; the 18 of 0.5.")]
        [SerializeField] private ScanSet[] _sets =
        {
            new ScanSet("CargoCradle", "rover.cargo_cradle", ScanSize.Medium, ScanSize.Medium),
            new ScanSet("WarmHeadlamp", "rover.warm_headlamp", ScanSize.Small, ScanSize.Medium),
            new ScanSet("HoverJump", "rover.hover_jump", ScanSize.Small, ScanSize.Medium, ScanSize.Medium),
            new ScanSet("BoostCoils", "rover.boost_coils", ScanSize.Medium, ScanSize.Medium, ScanSize.Large),
            new ScanSet("CellPack", "rover.cell_pack", ScanSize.Small, ScanSize.Small, ScanSize.Medium),
            new ScanSet("FocusLens", "rover.focus_lens", ScanSize.Small, ScanSize.Small, ScanSize.Small),
            new ScanSet("SideBins", "rover.side_bins", ScanSize.Medium, ScanSize.Large),
        };

        [Header("The verb (§1.2)")]
        [Tooltip("Seconds to scan a small fragment (a lamp, a lens).")]
        [Range(0.3f, 10f)] [SerializeField] private float _smallSeconds = 1.5f;

        [Tooltip("Seconds to scan a medium fragment.")]
        [Range(0.3f, 10f)] [SerializeField] private float _mediumSeconds = 2.5f;

        [Tooltip("Seconds to scan a large fragment (a capacitor bank, a pannier).")]
        [Range(0.3f, 10f)] [SerializeField] private float _largeSeconds = 4f;

        [Tooltip("How much faster the Focus Lens makes every scan (§1.6).")]
        [Range(1f, 3f)] [SerializeField] private float _lensFactor = 1.5f;

        [Tooltip("Metres within which a fragment still to scan shows its corner brackets.")]
        [Range(2f, 60f)] [SerializeField] private float _bracketRange = 15f;

        [Tooltip("The same with the Focus Lens fitted (§1.6).")]
        [Range(2f, 60f)] [SerializeField] private float _lensBracketRange = 25f;

        [Tooltip("Metres from a fragment's scan point within which a hold of E scans it.")]
        [Range(1f, 20f)] [SerializeField] private float _reach = 8f;

        [Tooltip("Degrees off 07's view a fragment may lie and still be the one it looks at.")]
        [Range(5f, 90f)] [SerializeField] private float _cone = 25f;

        [Tooltip("How much nearness weighs against looking straight at a fragment when two are in view.")]
        [Range(0f, 1f)] [SerializeField] private float _distanceWeight = 0.2f;

        [Tooltip("07 eases to a stop before its lens opens: the scan runs below this speed (m/s).")]
        [Range(0.05f, 2f)] [SerializeField] private float _engageSpeed = 0.4f;

        [Header("Done (§1.2, D2)")]
        [Tooltip("Seconds of the dissolve: the solid fades, the lines stream into 07's lens, the lens flares.")]
        [Range(0.3f, 3f)] [SerializeField] private float _dissolveSeconds = 0.9f;

        [Tooltip("Seconds the faint dust outline left on the spot takes to fade.")]
        [Range(0.3f, 6f)] [SerializeField] private float _outlineSeconds = 2f;

        [Tooltip("Metal a fragment of an already known blueprint gives after its dissolve, so leftovers still pay.")]
        [Range(0, 12)] [SerializeField] private int _knownMetal = 3;

        [Header("The chasm's near lip (§1.3)")]
        [Tooltip("Metres back from the canyon lip, toward the basin, where Kenji's crashed test jumper lies.")]
        [Range(0f, 30f)] [SerializeField] private float _jumperSetback = 6f;

        [Tooltip("Metres to the left of the leap's run-up line (looking at the chasm) the jumper lies: clear of the " +
                 "lane 07 runs at the lip and of the bridge's stub on its right, inside the chasm's floor.")]
        [Range(0f, 10f)] [SerializeField] private float _jumperSide = 6f;

        public IReadOnlyList<ScanSet> Sets => _sets;
        public float LensFactor => _lensFactor;
        public float BracketRange => _bracketRange;
        public float LensBracketRange => _lensBracketRange;
        public float Reach => _reach;
        public float Cone => _cone;
        public float DistanceWeight => _distanceWeight;
        public float EngageSpeed => _engageSpeed;
        public float DissolveSeconds => _dissolveSeconds;
        public float OutlineSeconds => _outlineSeconds;
        public int KnownMetal => _knownMetal;
        public float JumperSetback => _jumperSetback;
        public float JumperSide => _jumperSide;

        /// <summary>Seconds a fragment of <paramref name="size"/> takes to scan with the plain eye.</summary>
        public float Seconds(ScanSize size)
        {
            switch (size)
            {
                case ScanSize.Small:
                    return _smallSeconds;
                case ScanSize.Medium:
                    return _mediumSeconds;
                case ScanSize.Large:
                    return _largeSeconds;
                default:
                    throw new ArgumentOutOfRangeException(nameof(size), size, "Not a scan size.");
            }
        }

        /// <summary>The set named <paramref name="blueprint"/> in Art's names, or false.</summary>
        public bool TryGetSet(string blueprint, out ScanSet set)
        {
            for (int i = 0; i < _sets.Length; i++)
            {
                if (string.Equals(_sets[i].Blueprint, blueprint, StringComparison.Ordinal))
                {
                    set = _sets[i];
                    return true;
                }
            }

            set = default;
            return false;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The opening's content (docs/design/opening-v2.md §2, §4): the world anchors it stands on, the lander's reach and
    /// its one-charge reserve, where a built pad meets the lander's port cable, the lander cargo bay's scan points, and
    /// every crate of the opening's supply with its yield, and the start kit's spare igniter. Created by the
    /// Gameplay/Tuning builder; runtime code only reads it.
    /// </summary>
    public sealed class OpeningTuning : ScriptableObject
    {
        [Header("Anchors (World's ids)")]
        [Tooltip("The landed lander's anchor: its centre, Forward = the side with its service port.")]
        [SerializeField] private string _landerAnchor = "place.lander";

        [Tooltip("The build site's anchor (the old unloading apron), where the lander's port cable ends.")]
        [SerializeField] private string _apronAnchor = "place.apron";

        [Header("The lander (B2, B6)")]
        [Tooltip("Metres from the lander's centre within which 07 has reached it (B2's exit).")]
        [Range(5f, 60f)] [SerializeField] private float _landerReach = 25f;

        [Tooltip("Cells of charge the lander's old cells still hold: one full charge of 07 (its 4 cells) through a " +
                 "pad on its port cable; whatever is left gives out when that charge ends.")]
        [Range(0.5f, 8f)] [SerializeField] private float _reserveCells = 4f;

        [Tooltip("Metres from the apron's centre within which a built pad sits on the lander's port cable.")]
        [Range(2f, 20f)] [SerializeField] private float _portCableReach = 10f;

        [Tooltip("Metres around a built pad's centre within which 07, stopped, charges on it.")]
        [Range(0.5f, 4f)] [SerializeField] private float _padChargeRadius = 1.6f;

        [Tooltip("The open crate whose cargo 07 scans for the pad's blueprint (B3) and then collects (B4).")]
        [SerializeField] private string _cargoCrate = "supply.lander.open";

        [Tooltip("Where the charging pad's fragments lie in the cargo crate's frame (x right, y up, z its front): " +
                 "Scan_1..N in order, each on its fragment, so the scan draws on a real mesh.")]
        [SerializeField] private Vector3[] _cargoScanPoints =
        {
            new Vector3(-0.12f, 0.3f, 0.08f),
            new Vector3(0.12f, 0.3f, 0.08f),
        };

        [Tooltip("Art's pad fragments lying in the cargo crate, one per scan point in order (PadFragment_CoilPlate, " +
                 "PadFragment_GuideRail).")]
        [SerializeField] private GameObject[] _cargoFragments = Array.Empty<GameObject>();

        [Header("The start kit (§10)")]
        [Tooltip("The spare igniter's story-part id in 07's hold: the lander's open crate holds it, the cutter " +
                 "takes it at B6.")]
        [SerializeField] private string _igniterPart = "part.igniter";

        [Tooltip("Metres from an open crate within which 07 collects what it holds, from any side (B4: 07 drives up " +
                 "to it).")]
        [Range(1f, 8f)] [SerializeField] private float _collectReach = 3.5f;

        [Tooltip("Metres from 07 within which an open crate that still holds something shows Art's soft warm rim " +
                 "glow (none further: the lander's beacon and the cable lead 07 there, and loot never glows across " +
                 "the basin).")]
        [Range(5f, 60f)] [SerializeField] private float _crateRimReach = 28f;

        [Header("The recovery protocol (§2)")]
        [Tooltip("The rows after Power: their lamps, dark through the opening.")]
        [SerializeField] private RecoveryLamps _recoveryLamps = new RecoveryLamps(4, 4, 1);

        [Header("Supply (§4, §10: the lander's open crate and the sealed ones)")]
        [Tooltip("Every crate of the opening. A crate stands by a world anchor or a Lumen ruin piece " +
                 "([World]/Ruins/<id>) or the lander's service port (lander.port), in its frame. The lander's open " +
                 "crate (B4: Metal 2, Wiring 1 and the spare igniter) stands at the port where the cable ends; the " +
                 "sealed ones stand among the ruins, cut once the cutter works (after B6).")]
        [SerializeField] private SupplyCache[] _caches =
        {
            new SupplyCache("supply.wake", "place.wake", new Vector3(2.5f, 0f, -1.5f), 30f, "Crate_S",
                new Recipe(3, 1, 0)),
            new SupplyCache("supply.midway", "place.midway", new Vector3(1.5f, 0f, 0f), -20f, "Crate_S",
                new Recipe(2, 1, 0)),
            new SupplyCache("supply.lander.open", OpeningField.LanderPortFrame, new Vector3(2.2f, 0f, -1.8f), 180f,
                ContainerKinds.CrateMOpen, new Recipe(2, 1, 0), true, new[] { "part.igniter", "part.icecore" }),
            new SupplyCache("supply.habitat.reel", "HabitatModules", new Vector3(-3f, 0f, -6f), 207f, "Crate_M",
                new Recipe(6, 4, 0)),
            new SupplyCache("supply.workshop.a", "KenjiWorkshop", new Vector3(-2.5f, 0f, -5f), -140f, "Crate_S",
                new Recipe(3, 4, 2)),
            new SupplyCache("supply.workshop.b", "KenjiWorkshop", new Vector3(2.5f, 0f, -5f), 140f, "Crate_S",
                new Recipe(3, 4, 2)),
            new SupplyCache("supply.tower.a", "RadioTower", new Vector3(-6.5f, 0f, 0f), -100f, "Crate_S",
                new Recipe(2, 0, 1)),
            new SupplyCache("supply.tower.b", "RadioTower", new Vector3(3f, 0f, -2.5f), 130f, "Crate_S",
                new Recipe(2, 0, 1)),
            new SupplyCache("supply.scatter", "ScatterSet", new Vector3(0f, 0f, -3f), 180f, "Crate_M",
                new Recipe(6, 0, 0)),
        };

        /// <summary>Art's pad fragments for a tuning made in code (tests); the shipped asset names its own.</summary>
        internal void PopulateCargoFragments(GameObject[] fragments)
        {
            _cargoFragments = fragments ?? throw new ArgumentNullException(nameof(fragments));
        }

        public string LanderAnchor => _landerAnchor;
        public string ApronAnchor => _apronAnchor;
        public float LanderReach => _landerReach;
        public float ReserveCells => _reserveCells;
        public float PortCableReach => _portCableReach;
        public float PadChargeRadius => _padChargeRadius;
        public string CargoCrate => _cargoCrate;
        public IReadOnlyList<Vector3> CargoScanPoints => _cargoScanPoints;
        public IReadOnlyList<GameObject> CargoFragments => _cargoFragments ?? Array.Empty<GameObject>();
        public IReadOnlyList<SupplyCache> Caches => _caches;
        public string IgniterPart => _igniterPart;
        public float CollectReach => _collectReach;
        public float CrateRimReach => _crateRimReach;
        public RecoveryLamps RecoveryLamps => _recoveryLamps;
    }
}

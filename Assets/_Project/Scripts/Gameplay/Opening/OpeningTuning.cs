using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The opening's content (docs/design/opening-v2.md §2, §4): the world anchors it stands on, the lander's reach and
    /// its one-charge reserve, where a built pad meets the lander's port cable, the lander cargo bay's scan points, and
    /// every sealed crate of the opening's supply with its yield. Created by the Gameplay/Tuning builder; runtime code
    /// only reads it.
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

        [Tooltip("The cargo bay's scan points in the lander anchor's frame (x right, y up, z forward): the charging " +
                 "pad's fragments, Scan_1..N in order.")]
        [SerializeField] private Vector3[] _cargoScanPoints =
        {
            new Vector3(-1.4f, 1.2f, 1.8f),
            new Vector3(1.2f, 1.2f, 2.4f),
        };

        [Header("Supply (§4: Metal 29, Wiring 15, Optics 6 with the start)")]
        [Tooltip("Every sealed crate of the opening. A crate stands by a world anchor or a Lumen ruin piece " +
                 "([World]/Ruins/<id>), in its frame; the lander's stand on its far side, beyond the cutter's reach " +
                 "of the cargo bay, so a sealed crate never takes the hold meant for the bay's scan (B3 before B4).")]
        [SerializeField] private SupplyCache[] _caches =
        {
            new SupplyCache("supply.wake", "place.wake", new Vector3(2.5f, 0f, -1.5f), 30f, "CoilCrate",
                new Recipe(3, 1, 0)),
            new SupplyCache("supply.midway", "place.midway", new Vector3(1.5f, 0f, 0f), -20f, "CoilCrate",
                new Recipe(2, 1, 0)),
            new SupplyCache("supply.lander.a", "place.lander", new Vector3(-5f, 0f, -9f), 180f, "CoilCrate",
                new Recipe(3, 2, 0)),
            new SupplyCache("supply.lander.b", "place.lander", new Vector3(5f, 0f, -9f), 160f, "CoilCrate",
                new Recipe(3, 2, 0)),
            new SupplyCache("supply.lander.panels", "place.lander", new Vector3(0f, 0f, -12f), 0f, "JunctionBox",
                new Recipe(6, 0, 0)),
            new SupplyCache("supply.habitat.reel", "HabitatModules", new Vector3(3f, 0f, -3f), 0f, "CoilCrate",
                new Recipe(0, 4, 0)),
            new SupplyCache("supply.workshop.a", "KenjiWorkshop", new Vector3(-2.5f, 0f, -3f), 0f, "Cabinet",
                new Recipe(0, 2, 2)),
            new SupplyCache("supply.workshop.b", "KenjiWorkshop", new Vector3(2.5f, 0f, -3f), 0f, "Cabinet",
                new Recipe(0, 2, 2)),
            new SupplyCache("supply.tower.a", "RadioTower", new Vector3(-3f, 0f, -2.5f), 0f, "JunctionBox",
                new Recipe(2, 0, 1)),
            new SupplyCache("supply.tower.b", "RadioTower", new Vector3(3f, 0f, -2.5f), 0f, "JunctionBox",
                new Recipe(2, 0, 1)),
            new SupplyCache("supply.scatter", "ScatterSet", new Vector3(0f, 0f, -3f), 0f, "JunctionBox",
                new Recipe(6, 0, 0)),
        };

        public string LanderAnchor => _landerAnchor;
        public string ApronAnchor => _apronAnchor;
        public float LanderReach => _landerReach;
        public float ReserveCells => _reserveCells;
        public float PortCableReach => _portCableReach;
        public float PadChargeRadius => _padChargeRadius;
        public IReadOnlyList<Vector3> CargoScanPoints => _cargoScanPoints;
        public IReadOnlyList<SupplyCache> Caches => _caches;
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core.Events;

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

        [Header("The objective line (opening-v3 guidance audit G2, G4)")]
        [Tooltip("When the next line follows a success, and when a reminder catches a player with no progress.")]
        [SerializeField] private GuidanceTiming _guidance = new GuidanceTiming();

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

        [Tooltip("Metres past a built pad's plate (its footprint's half-extents) 07 may still stand and count as on " +
                 "the pad: the cutter socket takes the igniter there.")]
        [Range(0f, 1.5f)] [SerializeField] private float _padPlateMargin = 0.3f;

        [Tooltip("The open crate whose cargo 07 scans for the pad's blueprint (B3) and then collects (B4).")]
        [SerializeField] private string _cargoCrate = "supply.lander.open";

        [Tooltip("Art's pad fragments lying in the cargo crate, one per PartSocket_N in order (PadFragment_CoilPlate " +
                 "on PartSocket_0, PadFragment_GuideRail on PartSocket_1): Scan_1..N, each on its fragment, so the " +
                 "scan draws on a real mesh above the crate's rim.")]
        [SerializeField] private GameObject[] _cargoFragments = Array.Empty<GameObject>();

        [Header("The start kit (§10)")]
        [Tooltip("Story parts that fit anywhere the moment they are in 07's hold (a small field swap, like the spare " +
                 "igniter into the cutter): no pad and no dock needed. Any other part needs the service bay.")]
        [SerializeField] private string[] _fieldFitParts = { "part.igniter" };

        [Tooltip("The spare igniter's story-part id in 07's hold: Kenji's CUTTER SPARES rack by the fallen array " +
                 "holds it, the cutter takes it at B7.")]
        [SerializeField] private string _igniterPart = "part.igniter";

        [Tooltip("Metres from an open crate within which it offers what it holds, from any side: a held E takes it " +
                 "(B4: 07 drives up to it and is told to take the parts).")]
        [Range(1f, 8f)] [SerializeField] private float _collectReach = 3.5f;

        [Tooltip("Metres from 07 within which an open crate that still holds something starts to show Art's soft " +
                 "warm rim glow (none further: the lander's beacon and the cable lead 07 there, and loot never glows " +
                 "across the basin).")]
        [Range(5f, 60f)] [SerializeField] private float _crateRimReach = 30f;

        [Tooltip("Metres inside the rim's reach over which its glow comes up to full as 07 drives in.")]
        [Range(1f, 20f)] [SerializeField] private float _crateRimBand = 8f;

        [Tooltip("Seconds the rim glow takes to warm fully or cool off (once the crate is emptied).")]
        [Range(0.1f, 4f)] [SerializeField] private float _crateRimFadeSeconds = 1.2f;

        [Tooltip("Times Art's rim glow strength the rim shows at full (a soft warm line that reads in the eclipse " +
                 "from the cable, never a beacon).")]
        [Range(0.5f, 4f)] [SerializeField] private float _crateRimStrength = 2.5f;

        [Tooltip("Intensity of the small warm light the rim casts into the crate at full glow (the rim line " +
                 "alone sits below the crate's walls from 07's camera).")]
        [Range(0f, 8f)] [SerializeField] private float _crateRimLightIntensity = 2f;

        [Tooltip("Metres the rim's warm light reaches: the crate and what lies in it, never the ground around.")]
        [Range(0.5f, 5f)] [SerializeField] private float _crateRimLightRange = 1.6f;

        [Tooltip("Metres above the rim the warm light hangs, so it washes the parts on top and the inner walls.")]
        [Range(0f, 2f)] [SerializeField] private float _crateRimLightLift = 0.5f;

        [Header("The recovery protocol (§2)")]
        [Tooltip("The rows after Power: their lamps, dark through the opening.")]
        [SerializeField] private RecoveryLamps _recoveryLamps = new RecoveryLamps(4, 4, 1);

        [Header("The call from the sky (opening v3 §2)")]
        [Tooltip("The sender's identity and its decode, and where its point of light stands beside Earth.")]
        [SerializeField] private CallSettings _call = new CallSettings();

        [Header("The socket rack at the pad (opening v3 4.3)")]
        [Tooltip("The rack's bays, left to right, matching Art's Bay_0.. nodes: each part's outline, when 07 sees it " +
                 "and what fills it. A bay's id is also the string key of its words on the aim chip.")]
        [SerializeField] private RackBaySettings[] _rackBays =
        {
            new RackBaySettings("rack.headlamp", true, string.Empty, RoverKitPiece.LampBar),
            new RackBaySettings("rack.cell_pack", true, string.Empty, RoverKitPiece.BatterySpine),
            new RackBaySettings("rack.hex_adapter", false, "rover.hex_adapter", RoverKitPiece.HexChargeAdapter),
        };

        [Tooltip("The rack in the world: Art's prefab beside the dock, its rise, its bays' glow and jaws, its reading.")]
        [SerializeField] private RackViewSettings _rackView = new RackViewSettings();

        [Header("The failed reply and the raised mast (opening v3 R7)")]
        [Tooltip("When 07 tries to answer the call, and how the antenna mast's segments lift and lock.")]
        [SerializeField] private CallReplySettings _reply = new CallReplySettings();

        [Header("The tow (opening v3 R6, ruling 4)")]
        [Tooltip("07's winch cable: its reach, its rope's pull, how a caught load is lifted free and where a load " +
                 "comes home.")]
        [SerializeField] private TowSettings _tow = new TowSettings();

        [Tooltip("The opening's towable loads and where they first stand: the habitat's power module, the battery " +
                 "bank's frame (Art's PowerModule, its TowHitch on its front face).")]
        [SerializeField] private TowLoadSpot[] _towLoads = Array.Empty<TowLoadSpot>();

        [Header("relay.1's repair (opening v3 R10)")]
        [Tooltip("The relay, its labelled spare and how near 07 takes it, fits it and charges on the lit relay.")]
        [SerializeField] private RelayRepairSettings _relay = new RelayRepairSettings();

        [Header("The answer (opening v3 R8)")]
        [Tooltip("Where and how long 07 holds to transmit, and how the pulse of the old network runs to relay.1 and the Peak.")]
        [SerializeField] private CallAnswerSettings _answer = new CallAnswerSettings();

        [Header("The first free choice and dusk (opening v3 R5–R6)")]
        [Tooltip("The near sites 07 is free to visit after power-on, either order, by their frames: a Lumen ruin " +
                 "piece ([World]/Ruins/<id>) or a story prop ([World]/BasinProps/<id>). The sky holds in the " +
                 "afternoon until 07 docks after visiting one of them (or learns the battery bank), then dusk begins.")]
        [SerializeField] private string[] _duskSites = { "Prop_SeismicStation", "HabitatModules" };

        [Tooltip("Metres (flat) from a site's frame within which 07 has visited it.")]
        [Range(2f, 30f)] [SerializeField] private float _duskSiteReach = 12f;

        [Header("Supply (opening-v3 §4: fixed sources, each for one structure)")]
        [Tooltip("Every source of the opening's materials, and nothing else: each stands by the thing it comes from " +
                 "and is for one structure (its link), which it covers on its own with the link's other sources " +
                 "(EditMode: supply >= need and <= 1.5x). A source stands by a world anchor, a Lumen ruin piece " +
                 "([World]/Ruins/<id>) or the lander's service port (lander.port), in its frame. The lander's open " +
                 "charger kit (CHARGER KIT · ILC-R: the pad's Metal 4 and Wiring 2) stands at the port where the " +
                 "cable ends; Kenji's open CUTTER SPARES rack (the spare igniter) stands by the fallen array, in its " +
                 "frame so it moves with it; the sealed ones stand at the ruins they come from, cut once the cutter " +
                 "works.")]
        [SerializeField] private SupplyCache[] _caches =
        {
            new SupplyCache("supply.lander.open", OpeningField.LanderPortFrame, new Vector3(4.2f, 0f, -1.4f), 45f,
                ContainerKinds.CrateCargoOpen, new Recipe(4, 2, 0), StructureIds.ChargingPad, true),
            new SupplyCache("supply.array.panels", "FallenSolarArray", new Vector3(5f, 0f, 0f), 90f,
                ContainerKinds.CrateL, new Recipe(3, 0, 1), StructureIds.SolarArray),
            new SupplyCache("supply.array.junction", "FallenSolarArray", new Vector3(9.7f, 0f, -2.6f), 90f,
                ContainerKinds.CrateM, new Recipe(0, 2, 0), StructureIds.SolarArray),
            new SupplyCache("supply.array.spares", "FallenSolarArray", new Vector3(4.4f, 0f, 3.6f), 90f,
                ContainerKinds.CrateM, new Recipe(0, 0, 0), StructureIds.SolarArray, true,
                new[] { "part.igniter" }),
            new SupplyCache("supply.habitat.reel", "HabitatModules", new Vector3(-3.3f, 0f, -6.5f), 207f,
                ContainerKinds.CrateM, new Recipe(0, 3, 0), StructureIds.BatteryBank),
            new SupplyCache("supply.tower.mast", "RadioTower", new Vector3(-6.5f, 0f, 0f), -100f,
                ContainerKinds.CrateL, new Recipe(4, 0, 0), StructureIds.AntennaMast),
            new SupplyCache("supply.tower.feed", "RadioTower", new Vector3(-5.4f, 0f, 5.6f), -143f,
                ContainerKinds.CrateM, new Recipe(0, 3, 0), StructureIds.AntennaMast),
            new SupplyCache("supply.tower.dish", "RadioTower", new Vector3(6.8f, 0f, 1.8f), 90f,
                ContainerKinds.CrateM, new Recipe(0, 0, 2), StructureIds.AntennaMast),
        };

        /// <summary>Art's pad fragments for a tuning made in code (tests); the shipped asset names its own.</summary>
        internal void PopulateCargoFragments(GameObject[] fragments)
        {
            _cargoFragments = fragments ?? throw new ArgumentNullException(nameof(fragments));
        }

        public string LanderAnchor => _landerAnchor;
        public string ApronAnchor => _apronAnchor;
        public GuidanceTiming Guidance => _guidance;
        public float LanderReach => _landerReach;
        public float ReserveCells => _reserveCells;
        public float PortCableReach => _portCableReach;
        public float PadChargeRadius => _padChargeRadius;
        public float PadPlateMargin => _padPlateMargin;
        public string CargoCrate => _cargoCrate;
        public IReadOnlyList<GameObject> CargoFragments => _cargoFragments ?? Array.Empty<GameObject>();
        public IReadOnlyList<SupplyCache> Caches => _caches;
        public string IgniterPart => _igniterPart;

        /// <summary>The part fits anywhere as soon as it is in the hold (no pad, no dock).</summary>
        public bool IsFieldFit(string partId)
        {
            for (int i = 0; i < _fieldFitParts.Length; i++)
            {
                if (string.Equals(_fieldFitParts[i], partId, System.StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
        public float CollectReach => _collectReach;
        public float CrateRimReach => _crateRimReach;

        public float CrateRimBand => _crateRimBand;

        public float CrateRimFadeSeconds => _crateRimFadeSeconds;

        public float CrateRimStrength => _crateRimStrength;

        public float CrateRimLightIntensity => _crateRimLightIntensity;

        public float CrateRimLightRange => _crateRimLightRange;

        public float CrateRimLightLift => _crateRimLightLift;
        public RecoveryLamps RecoveryLamps => _recoveryLamps;

        public CallSettings Call => _call;

        public CallReplySettings Reply => _reply;

        public TowSettings Tow => _tow;

        public RelayRepairSettings Relay => _relay;

        public IReadOnlyList<TowLoadSpot> TowLoads => _towLoads ?? Array.Empty<TowLoadSpot>();

        public CallAnswerSettings Answer => _answer;

        public IReadOnlyList<string> DuskSites => _duskSites ?? Array.Empty<string>();

        public float DuskSiteReach => _duskSiteReach;

        public IReadOnlyList<RackBaySettings> RackBays => _rackBays;

        public RackViewSettings RackView => _rackView;

        /// <summary>Null when the rack's bays can work, else the first problem.</summary>
        public string ValidateRack()
        {
            if (_rackBays == null || _rackBays.Length == 0)
            {
                return "the socket rack has no bays";
            }

            for (int i = 0; i < _rackBays.Length; i++)
            {
                string problem = _rackBays[i] != null ? _rackBays[i].Validate() : $"rack bay {i} is empty";
                if (problem != null)
                {
                    return problem;
                }

                for (int j = 0; j < i; j++)
                {
                    if (string.Equals(_rackBays[j].Id, _rackBays[i].Id, StringComparison.Ordinal))
                    {
                        return $"two rack bays share the id '{_rackBays[i].Id}'";
                    }
                }
            }

            return null;
        }
    }
}

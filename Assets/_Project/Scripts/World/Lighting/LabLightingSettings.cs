using System;
using UnityEngine;
using MoonProject.Art;

namespace MoonProject.World
{
    /// <summary>
    /// How the habitat lander's lab and belly lift glow (docs/features/M3-19): the warmest frame in the game, practical
    /// lamps only, every level a linear glow scale multiplied by home's wake (<c>IHomeLights</c>). Lamps that light in
    /// play warm up slowly; nothing pops.
    /// </summary>
    [Serializable]
    public sealed class LabLightingSettings
    {
        [Header("Belly lift")]
        [Tooltip("Glow of the belly's work lamp once home is lit: the light that spills down onto the platform.")]
        [Range(0f, 8f)]
        [SerializeField] private float _bellyLampGlow = 3f;

        [Tooltip("Glow of the floor hatch's lamp ring once home is lit: it reads from behind 07 on the apron, so the " +
            "platform, the hatch and the lit cabin line up under the belly.")]
        [Range(0f, 8f)]
        [SerializeField] private float _hatchRimGlow = 2.6f;

        [Header("Lab")]
        [Tooltip("Glow of the lab's four wall lamps.")]
        [Range(0f, 8f)]
        [SerializeField] private float _cabinLampGlow = 2.4f;

        [Tooltip("Glow of the big porthole's glass while the lab is lit, seen from outside.")]
        [Range(0f, 4f)]
        [SerializeField] private float _portholeGlow = 0.9f;

        [Tooltip("Extra glow on the big porthole once the light table is lit behind it (tower L3).")]
        [Range(0f, 4f)]
        [SerializeField] private float _tableThroughPorthole = 0.5f;

        [Tooltip("Glow of one lamp round the big porthole: one per known tech node, the lander's face counting 07's " +
            "growth from 30 m.")]
        [Range(0f, 8f)]
        [SerializeField] private float _portholeLampGlow = 3.2f;

        [Tooltip("Glow of a built node's lamp on the tech board.")]
        [Range(0f, 8f)]
        [SerializeField] private float _boardLampGlow = 2f;

        [Tooltip("Glow of a lit lamp on the recovery panel: warm amber.")]
        [Range(0f, 8f)]
        [SerializeField] private float _recoveryLampGlow = 2.2f;

        [Tooltip("How far a completed recovery row's lamps swell as its completion pulse runs left to right (on top " +
            "of their glow, 0..1 of it).")]
        [Range(0f, 2f)]
        [SerializeField] private float _rowPulseSwell = 0.8f;

        [Tooltip("Seconds the completion pulse takes to run along a row.")]
        [Range(0.2f, 5f)]
        [SerializeField] private float _rowPulseSeconds = 1.6f;

        [Tooltip("Glow of a crew section's reading lamp with nothing on its shelf yet.")]
        [Range(0f, 4f)]
        [SerializeField] private float _readingLampBase = 0.6f;

        [Tooltip("How much brighter a section's reading lamp gets for each keepsake on its shelf.")]
        [Range(0f, 4f)]
        [SerializeField] private float _readingLampPerKeepsake = 0.7f;

        [Tooltip("Seconds a lamp lighting in play takes to warm from dark to its full glow.")]
        [Range(0.1f, 6f)]
        [SerializeField] private float _lampWarmUp = 1.8f;

        [Header("Survey chart")]
        [Tooltip("Glow of the light table under the paper once tower L3 has lit it.")]
        [Range(0f, 6f)]
        [SerializeField] private float _tableGlow = 1.4f;

        [Tooltip("Extra glow on the light table while 07 looks at the chart.")]
        [Range(0f, 4f)]
        [SerializeField] private float _tableLookBoost = 0.8f;

        [Tooltip("Seconds the table takes to warm up or settle back as a look starts or ends.")]
        [Range(0.1f, 4f)]
        [SerializeField] private float _tableLookEase = 1.2f;

        [Tooltip("Metres from home to the chart paper's edge: the basin the chart pen draws on.")]
        [Range(50f, 1000f)]
        [SerializeField] private float _chartRadius = 320f;

        [Tooltip("Seconds the chart pen takes to travel to the place it inks (eased), one pen stroke.")]
        [Range(0.05f, 2f)]
        [SerializeField] private float _penTravel = 0.45f;

        [Header("Hand-off pulse")]
        [Tooltip("Glow of a bead on the hand-off conduit as the pulse passes it.")]
        [Range(0f, 8f)]
        [SerializeField] private float _handOffBeadGlow = 3f;

        [Tooltip("Width of the travelling pulse, as a share of the whole path from the reader to the bay.")]
        [Range(0.01f, 0.5f)]
        [SerializeField] private float _handOffWidth = 0.06f;

        [Tooltip("Seconds the pulse's glow lingers at the bay once it arrives.")]
        [Range(0f, 4f)]
        [SerializeField] private float _handOffAfterglow = 1.2f;

        [Tooltip("Intensity of the small warm light that travels with the pulse along the ground.")]
        [Range(0f, 6f)]
        [SerializeField] private float _handOffLightIntensity = 1.6f;

        [Tooltip("Range (metres) of the pulse's travelling light: a soft spot of warmth on the dust beside the cable.")]
        [Range(0.5f, 6f)]
        [SerializeField] private float _handOffLightRange = 2.2f;

        [Tooltip("Colour of the pulse's travelling light: the lamps' own warm.")]
        [SerializeField] private Color _handOffLightColour = Palette.Get(PaletteSwatch.WarmLamp);

        public float BellyLampGlow => _bellyLampGlow;
        public float HatchRimGlow => _hatchRimGlow;
        public float CabinLampGlow => _cabinLampGlow;
        public float PortholeGlow => _portholeGlow;
        public float TableThroughPorthole => _tableThroughPorthole;
        public float PortholeLampGlow => _portholeLampGlow;
        public float BoardLampGlow => _boardLampGlow;
        public float RecoveryLampGlow => _recoveryLampGlow;
        public float RowPulseSwell => _rowPulseSwell;
        public float RowPulseSeconds => _rowPulseSeconds;
        public float ReadingLampBase => _readingLampBase;
        public float ReadingLampPerKeepsake => _readingLampPerKeepsake;
        public float LampWarmUp => _lampWarmUp;
        public float TableGlow => _tableGlow;
        public float TableLookBoost => _tableLookBoost;
        public float TableLookEase => _tableLookEase;
        public float ChartRadius => _chartRadius;
        public float PenTravel => _penTravel;
        public float HandOffBeadGlow => _handOffBeadGlow;
        public float HandOffWidth => _handOffWidth;
        public float HandOffAfterglow => _handOffAfterglow;
        public float HandOffLightIntensity => _handOffLightIntensity;
        public float HandOffLightRange => _handOffLightRange;
        public Color HandOffLightColour => _handOffLightColour;
    }
}

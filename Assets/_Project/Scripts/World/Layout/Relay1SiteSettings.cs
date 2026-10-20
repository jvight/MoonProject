using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// relay.1's repair site at the dead west mast's foot (opening v3 5.1): the open service cabinet that holds the
    /// feed-unit spare, lit inside by its own small warm lamp so the part reads from 15 m at night; the mast box's
    /// empty feed socket; and the contact pad 07 charges on once the relay is relit. Offsets are in the relay.1
    /// anchor's frame (x to its right, z along its forward, toward the base).
    /// </summary>
    [Serializable]
    public sealed class Relay1SiteSettings
    {
        [Tooltip("Where the service cabinet stands, metres in the anchor's frame: beside the mast, on the base's side.")]
        [SerializeField] private Vector2 _cabinetOffset = new Vector2(2.2f, 0.8f);

        [Tooltip("Degrees the cabinet's open front turns from facing the base: angled so its inside shows on the way in.")]
        [Range(-90f, 90f)]
        [SerializeField] private float _cabinetYaw = -25f;

        [Tooltip("How far in front of the mast the contact pad lies, metres, toward the base.")]
        [Range(1f, 8f)]
        [SerializeField] private float _padDistance = 2.8f;

        [Tooltip("Radius of the contact pad's plate, metres: 07 stops on it to charge.")]
        [Range(0.5f, 3f)]
        [SerializeField] private float _padRadius = 1.2f;

        [Header("The cabinet's own lamp")]
        [Tooltip("Colour of the cabinet's small interior lamp: warm, a service light on its own cell.")]
        [SerializeField] private Color _lampColor = new Color(1f, 0.72f, 0.42f, 1f);

        [Tooltip("Brightness of the light the lamp casts inside the cabinet (a real light on the part, not a glow).")]
        [Range(0f, 6f)]
        [SerializeField] private float _lampIntensity = 1.2f;

        [Tooltip("How far the lamp's light reaches, metres: the cabinet's inside and no further.")]
        [Range(0.3f, 4f)]
        [SerializeField] private float _lampRange = 1.6f;

        [Tooltip("Emission strength of the lamp's own lens.")]
        [Range(0f, 6f)]
        [SerializeField] private float _lampLensGlow = 2.2f;

        public Vector2 CabinetOffset => _cabinetOffset;
        public float CabinetYaw => _cabinetYaw;
        public float PadDistance => _padDistance;
        public float PadRadius => _padRadius;
        public Color LampColor => _lampColor;
        public float LampIntensity => _lampIntensity;
        public float LampRange => _lampRange;
        public float LampLensGlow => _lampLensGlow;
    }
}

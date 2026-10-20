using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// relay.1's repair site at the dead west mast's foot (opening v3 5.1), in Art's props: the open service cabinet
    /// that holds the feed-unit spare, lit inside by its own small warm lamp so the part reads from 15 m at night; the
    /// mast box's feed socket, its outline asking for the part; and the contact pad 07 charges on once the relay is
    /// relit. Offsets are in the relay.1 anchor's frame (x to its right, z along its forward, toward the base); the
    /// placements on the props follow Art's notes for its anchors.
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

        [Header("Where the feed unit sits (Art's notes)")]
        [Tooltip("The spare on the cabinet's shelf, metres from its Part_Anchor in the cabinet's own axes.")]
        [SerializeField] private Vector3 _spareOnShelf = new Vector3(-0.04f, 0f, 0f);

        [Tooltip("Degrees the spare turns on the shelf from the cabinet's facing.")]
        [Range(-180f, 180f)]
        [SerializeField] private float _spareYaw = 90f;

        [Tooltip("The fitted unit, metres from the mast's FeedSocket_Anchor (its mouth) in the mast's own axes: " +
            "pushed home connector first, its back end and pigtail standing out of the mouth.")]
        [SerializeField] private Vector3 _fittedInSocket = new Vector3(0.08f, -0.07f, 0f);

        [Tooltip("Degrees the fitted unit turns from the mast's facing.")]
        [Range(-180f, 180f)]
        [SerializeField] private float _fittedYaw = -90f;

        [Header("The cabinet's own lamp")]
        [Tooltip("Colour of the cabinet's small interior lamp: warm, a service light on its own cell.")]
        [SerializeField] private Color _lampColor = new Color(1f, 0.76f, 0.32f, 1f);

        [Tooltip("Brightness of the light the lamp casts inside the cabinet (a real light on the part, not a glow).")]
        [Range(0f, 6f)]
        [SerializeField] private float _lampIntensity = 1.6f;

        [Tooltip("How far the lamp's light reaches, metres: the cabinet's inside and no further.")]
        [Range(0.3f, 4f)]
        [SerializeField] private float _lampRange = 1.4f;

        [Tooltip("How bright the lamp strip itself glows (its emission, times the lamp's colour).")]
        [Range(0f, 6f)]
        [SerializeField] private float _lampStripGlow = 2f;

        [Header("The feed socket's outline")]
        [Tooltip("Colour of the socket's outline: a warm line round the empty mouth.")]
        [SerializeField] private Color _outlineColor = new Color(1f, 0.7f, 0.4f, 1f);

        [Tooltip("How bright the outline glows while the socket waits for its part.")]
        [Range(0f, 4f)]
        [SerializeField] private float _outlineNeedsPart = 1f;

        public Vector2 CabinetOffset => _cabinetOffset;
        public float CabinetYaw => _cabinetYaw;
        public float PadDistance => _padDistance;
        public Vector3 SpareOnShelf => _spareOnShelf;
        public float SpareYaw => _spareYaw;
        public Vector3 FittedInSocket => _fittedInSocket;
        public float FittedYaw => _fittedYaw;
        public Color LampColor => _lampColor;
        public float LampIntensity => _lampIntensity;
        public float LampRange => _lampRange;
        public float LampStripGlow => _lampStripGlow;
        public Color OutlineColor => _outlineColor;
        public float OutlineNeedsPart => _outlineNeedsPart;
    }
}

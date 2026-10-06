using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// The friend UI: a few warm pips floating over a broken friend while 07 is near (how many of its parts are
    /// back), and the friend's name drifting up the first time it wakes.
    /// </summary>
    [Serializable]
    public sealed class FriendUiSettings
    {
        [Tooltip("The parts readout easing in and out.")]
        [SerializeField] private RevealSettings _readout = new RevealSettings(0.5f, 0.6f, 8f, 0.9f, 1.4f, 0.6f);

        [Tooltip("Metres from 07 to a broken friend within which its parts readout appears.")]
        [Range(2f, 60f)]
        [SerializeField] private float _showDistance = 16f;

        [Tooltip("Metres beyond which a shown readout leaves again (a little farther, so it never flickers).")]
        [Range(2f, 80f)]
        [SerializeField] private float _hideDistance = 20f;

        [Tooltip("Metres above the friend where the readout floats (the repair prompt's point: when the prompt is up, "
            + "the readout rests on top of it).")]
        [Range(0f, 5f)]
        [SerializeField] private float _readoutLiftMetres = 0.8f;

        [Tooltip("Pixels (at 1080p) between the repair prompt and the readout resting on top of it.")]
        [Range(0f, 40f)]
        [SerializeField] private float _stackGap = 8f;

        [Tooltip("Seconds a pip takes to fill when a part comes back.")]
        [Range(0.05f, 3f)]
        [SerializeField] private float _pipFillSeconds = 0.7f;

        [Tooltip("The friend's name easing in and out when it first wakes.")]
        [SerializeField] private RevealSettings _name = new RevealSettings(0.9f, 1.4f, 12f, 0.94f, 1f, 0.7f);

        [Tooltip("Seconds the name rests fully visible.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float _nameHoldSeconds = 3.5f;

        [Tooltip("Metres above the friend where its name floats.")]
        [Range(0f, 5f)]
        [SerializeField] private float _nameLiftMetres = 1.1f;

        public RevealSettings Readout => _readout;

        public float ShowDistance => _showDistance;

        public float HideDistance => Mathf.Max(_showDistance, _hideDistance);

        public float ReadoutLiftMetres => _readoutLiftMetres;

        public float StackGap => _stackGap;

        public float PipFillSeconds => _pipFillSeconds;

        public RevealSettings Name => _name;

        public float NameHoldSeconds => _nameHoldSeconds;

        public float NameLiftMetres => _nameLiftMetres;
    }
}

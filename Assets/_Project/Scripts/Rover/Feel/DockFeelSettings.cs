using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>07's side of the dock locking it in (docs/design/feel-pass.md §7.1): the dip and rumble at the
    /// clunk.</summary>
    [Serializable]
    public sealed class DockFeelSettings
    {
        [Tooltip("The clamp closes: 07 dips 2 cm, one soft rebound.")]
        [SerializeField] private NudgeSettings _lockDip = new NudgeSettings(0.02f, 0f, 3f, 0.6f);

        [Tooltip("The clamp closes: 0.15 for 80 ms.")]
        [SerializeField] private RumbleProfile _lockRumble = new RumbleProfile(0.15f, 0.15f, 0.1f, 0.1f, 0.08f, false);

        public NudgeSettings LockDip => _lockDip;

        public RumbleProfile LockRumble => _lockRumble;
    }
}

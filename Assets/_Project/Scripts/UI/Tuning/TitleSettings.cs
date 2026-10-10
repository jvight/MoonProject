using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// The title over the live world (M3-09): the "Lofi Lunar" wordmark and subtitle, the menu's quiet idle, and the
    /// soft dark drawn before the scene reloads.
    /// </summary>
    [Serializable]
    public sealed class TitleSettings
    {
        [Tooltip("The wordmark and subtitle easing in and out (slow and soft).")]
        [SerializeField] private RevealSettings _reveal = new RevealSettings(2.2f, 2.8f, 14f, 0.985f, 0.6f, 0.8f);

        [Tooltip("The dark drawn over everything before New Game, Quit to title or Quit, and lifted after a boot.")]
        [SerializeField] private RevealSettings _curtain = new RevealSettings(0.9f, 1.4f, 0f, 1f, 1f, 1f);

        [Tooltip("How much of the menu blur the world takes behind the title (the pause takes all of it).")]
        [Range(0f, 1f)]
        [SerializeField] private float _backdropWeight = 0.55f;

        [Tooltip("Seconds untouched before the title's menu fades and leaves the world to itself; any input returns it.")]
        [Range(5f, 120f)]
        [SerializeField] private float _idleSeconds = 20f;

        public RevealSettings Reveal => _reveal;

        public RevealSettings Curtain => _curtain;

        public float BackdropWeight => _backdropWeight;

        public float IdleSeconds => _idleSeconds;
    }
}

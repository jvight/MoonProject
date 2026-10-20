using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// relay.2's hut by the canyon mouth (opening v3 5.2b): a small 1980s relay hut (its anchor and clearing in
    /// <see cref="RelaySettings"/>) that frames 06, slumped against it with its battery bay toward the approach, and
    /// the spot on the approach where 07 stops to scan the bay.
    /// </summary>
    [Serializable]
    public sealed class Relay2SiteSettings
    {
        [Tooltip("How far out from 06, toward the canyon mouth, 07 stops to scan its battery bay, metres.")]
        [Range(2f, 10f)]
        [SerializeField] private float _scanApproachDistance = 4f;

        [Tooltip("Degrees the hut's door turns from facing the canyon mouth: angled toward the approach.")]
        [Range(-90f, 90f)]
        [SerializeField] private float _hutYaw = 15f;

        public float ScanApproachDistance => _scanApproachDistance;
        public float HutYaw => _hutYaw;
    }
}

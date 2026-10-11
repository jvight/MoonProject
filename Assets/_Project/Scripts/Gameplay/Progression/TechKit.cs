using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>A kit node of the tech tree and the bay's upgrade that builds it (docs/features/M3-21 §4).</summary>
    [Serializable]
    public struct TechKit
    {
        [Tooltip("The tech board's pin for this kit.")]
        [SerializeField] private TechNodeId _node;

        [Tooltip("The bay's upgrade id that fits the kit (e.g. rover.hover_jump): decoded once its blueprint is " +
                 "read, built once fitted.")]
        [SerializeField] private string _kitId;

        public TechKit(TechNodeId node, string kitId)
        {
            _node = node;
            _kitId = kitId;
        }

        public TechNodeId Node => _node;

        public string KitId => _kitId;
    }
}

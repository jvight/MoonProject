using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// One cutter tier's charge-run lamps on 07's back (M3-26 §2.1): the row's root (RoverModel's
    /// <c>ChargeRun_{tier}</c>, shown only at its tier) and its lamps from the battery to the emitter.
    /// </summary>
    [Serializable]
    public sealed class ChargeRunRow
    {
        [Tooltip("The row's root node (ChargeRun_{tier}), shown only while the cutter is at this tier.")]
        [SerializeField] private GameObject _root;

        [Tooltip("The row's lamp renderers, ChargeLamp_0 (the battery) first.")]
        [SerializeField] private Renderer[] _lamps = new Renderer[0];

        public GameObject Root => _root;

        public Renderer[] Lamps => _lamps;

        /// <summary>Whether the row has a root and at least one lamp, all assigned.</summary>
        public bool IsWired
        {
            get
            {
                if (_root == null || _lamps == null || _lamps.Length == 0)
                {
                    return false;
                }

                foreach (Renderer lamp in _lamps)
                {
                    if (lamp == null)
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}

using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// One cutter tier's charge-run lamps on 07 (M3-26 §2.1). The row rides the parts it sits on, so it has several
    /// roots: RoverModel's <c>ChargeRun_{tier}</c> nodes under the body, the neck, the head and the eye, shown only
    /// at its tier; its lamps run from the battery to the emitter rim.
    /// </summary>
    [Serializable]
    public sealed class ChargeRunRow
    {
        [Tooltip("The row's ChargeRun_{tier} nodes (body, neck, head, eye), shown only while the cutter is at this tier.")]
        [SerializeField] private GameObject[] _roots = new GameObject[0];

        [Tooltip("The row's lamp renderers in order, ChargeLamp_0 (the battery) first.")]
        [SerializeField] private Renderer[] _lamps = new Renderer[0];

        public GameObject[] Roots => _roots;

        public Renderer[] Lamps => _lamps;

        /// <summary>Whether the row has roots and at least one lamp, all assigned.</summary>
        public bool IsWired => AllSet(_roots) && AllSet(_lamps);

        /// <summary>Shows or hides every part of the row.</summary>
        public void Show(bool shown)
        {
            foreach (GameObject root in _roots)
            {
                root.SetActive(shown);
            }
        }

        private static bool AllSet<T>(T[] items) where T : UnityEngine.Object
        {
            if (items == null || items.Length == 0)
            {
                return false;
            }

            foreach (T item in items)
            {
                if (item == null)
                {
                    return false;
                }
            }

            return true;
        }
    }
}

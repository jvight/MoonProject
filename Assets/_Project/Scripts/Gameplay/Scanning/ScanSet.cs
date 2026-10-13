using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// One blueprint's scan fragments (docs/design/0.5.5-owner-feedback.md §1.3): Art's name for its set (the
    /// FragmentSocket_&lt;Blueprint&gt;_&lt;n&gt; and Fragment_&lt;Blueprint&gt;_&lt;n&gt; names), the bay's kit it
    /// teaches, and each fragment's size class, which sets its scan time.
    /// </summary>
    [Serializable]
    public struct ScanSet
    {
        [Tooltip("Art's name for the set, as in its FragmentSocket_<Blueprint>_<n> nodes (e.g. CargoCradle).")]
        [SerializeField] private string _blueprint;

        [Tooltip("The bay's kit the set teaches once every fragment is scanned (e.g. rover.cargo_cradle).")]
        [SerializeField] private string _kitId;

        [Tooltip("Each fragment's size, in fragment order: 2 for a rack or a lamp, 3 for anything with power or " +
                 "motion.")]
        [SerializeField] private ScanSize[] _sizes;

        public ScanSet(string blueprint, string kitId, params ScanSize[] sizes)
        {
            _blueprint = blueprint;
            _kitId = kitId;
            _sizes = sizes ?? throw new ArgumentNullException(nameof(sizes));
        }

        public string Blueprint => _blueprint ?? string.Empty;

        public string KitId => _kitId ?? string.Empty;

        /// <summary>How many fragments make the set.</summary>
        public int Count => _sizes?.Length ?? 0;

        public ScanSize Size(int index)
        {
            return _sizes[index];
        }
    }
}

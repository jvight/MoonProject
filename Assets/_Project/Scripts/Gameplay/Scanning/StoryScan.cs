using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// A story scan on an R1 ruin piece or basin prop (docs/design/opening-v2.md B7, the habitat's airlock panel;
    /// basin-dressing.md §4, the Scout): Art's mesh node 07 scans like a fragment, which teaches nothing and pays
    /// nothing; scanned, its readout shows on 07's CRT (<see cref="Core.Events.StoryScanned"/>). Once per save.
    /// </summary>
    [Serializable]
    public struct StoryScan
    {
        public const string IdPrefix = "story.";

        [Tooltip("The story's id, saved with the scans: never rename.")]
        [SerializeField] private string _storyId;

        [Tooltip("The R1 piece it lies on, by name: a ruin piece in [World]/Ruins (e.g. HabitatModules) or a basin " +
                 "prop in [World]/BasinProps (e.g. Prop_OldLander).")]
        [SerializeField] private string _ruinId;

        [Tooltip("Art's mesh node on the ruin piece 07 scans (its renderer's centre is the scan point).")]
        [SerializeField] private string _node;

        [Tooltip("Localization key of the readout the CRT shows once it is scanned.")]
        [SerializeField] private string _textKey;

        [Tooltip("How long it takes to scan (ScanTuning's size times).")]
        [SerializeField] private ScanSize _size;

        [Tooltip("Metres (world, y up) the scan point sits from the panel's centre: clear of the ruin's own scan " +
                 "targets, so looking at the panel picks the panel.")]
        [SerializeField] private Vector3 _offset;

        public StoryScan(string storyId, string ruinId, string node, string textKey, ScanSize size, Vector3 offset)
        {
            _offset = offset;
            _storyId = storyId;
            _ruinId = ruinId;
            _node = node;
            _textKey = textKey;
            _size = size;
        }

        public string HostId => _ruinId ?? string.Empty;

        public string Node => _node ?? string.Empty;

        public string TextKey => _textKey ?? string.Empty;

        public ScanSize Size => _size;

        public Vector3 Offset => _offset;

        /// <summary>"story.&lt;id&gt;": its fragment's saved id.</summary>
        public string FragmentId => IdPrefix + _storyId;
    }
}

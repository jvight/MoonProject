using UnityEngine;

namespace MoonProject.Core
{
    /// <summary>
    /// A fragment 07 can scan (docs/design/0.5.5-owner-feedback.md §1): one per
    /// FragmentSocket_&lt;Blueprint&gt;_&lt;n&gt; host node, kept by Gameplay. Its progress shows on the object itself
    /// and survives letting go (ruling 4).
    /// </summary>
    public interface IScanTarget
    {
        /// <summary>"&lt;blueprint id&gt;.&lt;n&gt;", e.g. "rover.cargo_cradle.0". Saved: never rename.</summary>
        string FragmentId { get; }

        /// <summary>The blueprint (kit id) the fragment teaches.</summary>
        string BlueprintId { get; }

        /// <summary>
        /// Where 07 looks while scanning and where the dissolve's ribbon leaves from (the ScanTarget node).
        /// </summary>
        Vector3 Point { get; }

        /// <summary>Its size class: how long it takes to scan.</summary>
        ScanSize Size { get; }

        /// <summary>How much of it is scanned, 0..1.</summary>
        float Progress { get; }
    }
}

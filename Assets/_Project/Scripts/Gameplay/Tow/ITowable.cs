using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Something 07's winch cable can hook and tow (docs/design/opening-v3-purpose.md R6: the habitat's power module
    /// first). The cable drives its body through its hitch; the towable only says where it is hooked and keeps itself
    /// still between tows.
    /// </summary>
    internal interface ITowable
    {
        /// <summary>Its id in <see cref="Core.Events.TowAttached"/> and <see cref="Core.Events.TowReleased"/>.</summary>
        string Id { get; }

        Rigidbody Body { get; }

        /// <summary>The load's root in the world (what World's lamp and a save read).</summary>
        Transform Root { get; }

        /// <summary>Where the cable meets it, in the world.</summary>
        Vector3 Hitch { get; }

        bool IsTowed { get; }

        /// <summary>The cable hooked it: the body becomes free to be pulled.</summary>
        void BeginTow();

        /// <summary>The cable let it go: it slides to rest where it is.</summary>
        void EndTow();

        /// <summary>One physics step off the cable: once it has come to rest it stands fast.</summary>
        void Settle(float deltaTime);

        /// <summary>A loaded game sets it down where it was left, standing fast.</summary>
        void Place(Vector3 position, Quaternion rotation);
    }
}

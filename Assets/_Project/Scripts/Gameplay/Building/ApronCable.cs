using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The lander's cable across the build apron (opening v2): a charging pad within <see cref="Reach"/> of the apron's
    /// middle draws the lander's reserve through it, so its own cable runs to the apron cable's channel.
    /// </summary>
    public readonly struct ApronCable
    {
        public ApronCable(Vector3 channel, Vector3 apron, float reach)
        {
            Channel = channel;
            Apron = apron;
            Reach = reach;
            Exists = true;
        }

        /// <summary>False until the opening has laid the apron cable out.</summary>
        public bool Exists { get; }

        /// <summary>Where the apron cable's channel takes a pad's cable.</summary>
        public Vector3 Channel { get; }

        /// <summary>The apron's middle, that the reach is measured from (as the pad chargers measure it).</summary>
        public Vector3 Apron { get; }

        /// <summary>How far from the apron's middle a pad still plugs into the lander's cable, metres.</summary>
        public float Reach { get; }

        /// <summary>True when a pad standing at <paramref name="position"/> plugs into the lander's cable.</summary>
        public bool Takes(StructureDefinition definition, Vector3 position)
        {
            return Exists && definition.Id == StructureIds.ChargingPad
                && SurfaceRules.HorizontalDistance(position, Apron) <= Reach;
        }
    }
}

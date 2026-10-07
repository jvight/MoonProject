using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// How one friend looks and moves (its rigs, its repair beat, its awake life), driven by the
    /// <see cref="FriendField"/>, which keeps what every friend shares: parts and items, lamps, the repair hold and
    /// beam, events and saves. Tilly is a <see cref="DroneBody"/>, Bell a <see cref="RadioCabinetBody"/>.
    /// </summary>
    internal interface IFriendBody
    {
        /// <summary>Where it is now (voices, prompts and 07's gaze follow it).</summary>
        Vector3 Position { get; }

        /// <summary>Where 07's stitching beam lands.</summary>
        Vector3 BeamTarget { get; }

        FriendActivity Activity { get; }

        /// <summary>0..1 rotor or motor effort (Core's IFriendState.RotorSpeed).</summary>
        float Motor { get; }

        /// <summary>It lives at the base right now (not out at its site, not on its way home).</summary>
        bool IsHome { get; }

        /// <summary>Seconds from the start of the repair until it is awake.</summary>
        float RepairDuration { get; }

        /// <summary>07 holds still and its beam reaches the friend at <paramref name="t"/> s into the repair.</summary>
        bool Beaming(float t);

        /// <summary>Lights lamp <paramref name="lamp"/> (parts first, then items) on the rig in view.</summary>
        void SetLamp(int lamp, float intensity);

        /// <summary>A frame while it still lies broken.</summary>
        void StepBroken(float now, float deltaTime);

        /// <summary>A frame of the repair, <paramref name="t"/> s after it began.</summary>
        void StepRepair(float t, float now, float deltaTime);

        /// <summary>The repair is done: its awake life begins where the repair left it.</summary>
        void Wake(float now);

        /// <summary>Loaded awake from a save: it is at home and 07 has just woken there.</summary>
        void SettleAtHome(float now);

        /// <summary>A frame of its awake life; says whether it greeted 07 or spotted something.</summary>
        FriendBeat StepAwake(float now, float deltaTime);
    }
}

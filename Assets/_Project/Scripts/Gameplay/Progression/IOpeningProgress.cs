using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// What the opening's objective flow reads of the game (docs/design/opening-v2.md §2–§3), every answer derived from
    /// saved game state, never from the flow's own memory: a loaded game shows the right step at once. Structure ids
    /// are <see cref="StructureIds"/>.
    /// </summary>
    public interface IOpeningProgress
    {
        /// <summary>07 has come within the lander's reach this save (B2's exit).</summary>
        bool ReachedLander { get; }

        /// <summary>The blueprint of <paramref name="structureId"/> is learned.</summary>
        bool Knows(string structureId);

        /// <summary>07's hold covers the build cost of <paramref name="structureId"/>.</summary>
        bool Covers(string structureId);

        /// <summary>At least one <paramref name="structureId"/> stands built.</summary>
        bool Built(string structureId);

        /// <summary>The spare igniter is in 07's hold (B7, from the CUTTER SPARES rack by the fallen array).</summary>
        bool CarriesIgniter { get; }

        /// <summary>The faulted cutter has sputtered: a cut was tried before the igniter was fitted (B7).</summary>
        bool CutterSputtered { get; }

        /// <summary>
        /// The cutter takes the spare igniter now: it is in the hold and 07 may fit it where it stands (a field swap
        /// fits anywhere; a part that needs the pad, only on its plate).
        /// </summary>
        bool CanFitIgniter { get; }

        /// <summary>The spare igniter is fitted to 07's cutter (B7): the cutter works.</summary>
        bool IgniterFitted { get; }

        /// <summary>07 has charged on a pad it built (B6): the lander's reserve has been drawn on.</summary>
        bool FirstCharge { get; }

        /// <summary>The lander's one-charge reserve is spent (<c>LANDER RESERVE · 0%</c> has shown).</summary>
        bool ReserveSpent { get; }

        /// <summary>A built solar array powers a built pad (B8).</summary>
        bool PowerOn { get; }

        /// <summary>
        /// The first dusk has begun (R6): 07 docked after visiting a near site since power-on, or learned the battery
        /// bank.
        /// </summary>
        bool DuskBegun { get; }

        /// <summary>A built battery bank is connected to the powered base (B9).</summary>
        bool NightReady { get; }

        /// <summary>The first night has fallen over a night-ready base (B10's exit).</summary>
        bool NightFallen { get; }

        /// <summary>The habitat's power module is on 07's winch cable (B9).</summary>
        bool ModuleHooked { get; }

        /// <summary>The habitat's power module stands home on the apron, off the cable: a battery bank can stand on it.</summary>
        bool ModuleHome { get; }

        /// <summary>Since power-on 07 has visited a near site, so docking now begins the first dusk (R5).</summary>
        bool DuskVisited { get; }

        /// <summary>
        /// 07 transmitted from the mast, relay.1 answered, and the answer's moment is over (the pulse, the Peak's
        /// glint): the next action may be named (R8).
        /// </summary>
        bool AnswerDone { get; }

        /// <summary>relay.1's spare is fitted and it is lit (R10).</summary>
        bool RelayRelit { get; }

        /// <summary>Kessler–Arai understanding has unlocked the adapted SSR-3 headlamp (5.2).</summary>
        bool HeadlampUnlocked { get; }

        /// <summary>07's hold covers the assembly cost of the SSR-3 headlamp (5.2).</summary>
        bool CoversHeadlamp { get; }

        /// <summary>The socket rack stands out at the pad (the first dock after the release brings it out).</summary>
        bool RackOut { get; }

        /// <summary>The SSR-3 headlamp is assembled: the rack's headlamp bay is filled (5.2).</summary>
        bool HeadlampFitted { get; }

        /// <summary>
        /// The place in the world where the action of line <paramref name="lineKey"/> happens (the apron, a crate, the
        /// power module, the mast), by id and position; false when it has none (a field fit) or it is not there yet.
        /// </summary>
        bool TryGetPlace(string lineKey, out string placeId, out Vector3 position);
    }
}

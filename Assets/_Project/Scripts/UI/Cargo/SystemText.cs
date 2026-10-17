using System;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// The words for 07's systems and their states, shared by the boot readout and the socket display
    /// (docs/design/opening-v2.md §10). Pure key lookups, EditMode-tested.
    /// </summary>
    internal static class SystemText
    {
        /// <summary>"ui.system.cutter": the label of a system's line.</summary>
        public static string NameKey(RoverSystem system)
        {
            switch (system)
            {
                case RoverSystem.Wheels: return "ui.system.wheels";
                case RoverSystem.Lamps: return "ui.system.lamps";
                case RoverSystem.Scan: return "ui.system.scan";
                case RoverSystem.Assembly: return "ui.system.assembly";
                case RoverSystem.Cutter: return "ui.system.cutter";
                case RoverSystem.SolarWing: return "ui.system.solar_wing";
                case RoverSystem.Antenna: return "ui.system.antenna";
                case RoverSystem.Sockets: return "ui.system.sockets";
                default:
                    throw new ArgumentOutOfRangeException(nameof(system), system, "This system has no name key.");
            }
        }

        /// <summary>
        /// "ui.system.state.fault_igniter": what a system's state says. The cutter's fault names its cause, the
        /// others use the plain state word.
        /// </summary>
        public static string StateKey(RoverSystem system, RoverSystemState state)
        {
            switch (state)
            {
                case RoverSystemState.Ok:
                    return "ui.system.state.ok";
                case RoverSystemState.Fault:
                    return system == RoverSystem.Cutter ? "ui.system.state.fault_igniter" : "ui.system.state.fault";
                case RoverSystemState.Jammed:
                    return "ui.system.state.jammed";
                case RoverSystemState.ReceiveOnly:
                    return "ui.system.state.receive_only";
                case RoverSystemState.Empty:
                    return "ui.system.state.empty";
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, "This state has no key.");
            }
        }
    }
}

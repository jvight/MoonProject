using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>How one of 07's device bays reads on the face screen right now.</summary>
    internal enum BayLook
    {
        /// <summary>Takes no part (a working device): its name and state only.</summary>
        None = 0,

        /// <summary>A bay at rest: quiet corner brackets around a drop area.</summary>
        Rest = 1,

        /// <summary>It can take a part from the hold right now: accent brackets, before any drag.</summary>
        Ready = 2,

        /// <summary>The carried part fits here: solid accent outline, a soft fill and a slow breath.</summary>
        Valid = 3,

        /// <summary>The carried part is over it (or the selection is on it) and would fit: a thicker outline.</summary>
        Hover = 4,

        /// <summary>The carried part belongs here but cannot be fitted yet (07 is off its pad): quiet, never dimmed.</summary>
        Blocked = 5,

        /// <summary>A drag is on and this device takes nothing from it: dimmed.</summary>
        Invalid = 6,
    }

    /// <summary>
    /// Which of 07's devices a part from the hold can go to, and how each bay reads (pure, EditMode-tested). A device
    /// is a bay when it can take a part at all: an empty socket, or a device broken by a missing part. While a part is
    /// carried (dragged, or selected in the hold from the keys) the bays it fits light and, during a drag, every other
    /// device dims; at rest a bay that can take something from the hold right now already shows it.
    /// </summary>
    internal static class DropTargets
    {
        /// <summary>True when <paramref name="device"/> in <paramref name="state"/> can take a part at all.</summary>
        public static bool IsBay(int device, RoverSystemState state)
        {
            return state == RoverSystemState.Empty || device == FaceDevices.Cutter && FaceDevices.IsFaulty(state);
        }

        /// <summary>
        /// The look of <paramref name="device"/> in <paramref name="state"/>. <paramref name="carriedPart"/> is the
        /// story part being carried (null when nothing is, or a material is); <paramref name="carrying"/> says a hold
        /// item is carried at all; <paramref name="dragging"/> that it is under the pointer; <paramref name="target"/>
        /// is the device a drop or a confirm would fit it into right now (-1: none).
        /// </summary>
        public static BayLook Look(int device, RoverSystemState state, string carriedPart, bool carrying, bool dragging,
            int target, bool canFitIgniter)
        {
            if (carrying)
            {
                DropOutcome outcome = DropRule.Decide(carriedPart, device, canFitIgniter);
                if (outcome == DropOutcome.Fit)
                {
                    return device == target ? BayLook.Hover : BayLook.Valid;
                }

                if (outcome == DropOutcome.NotDocked && DropRule.TargetOf(carriedPart) == device)
                {
                    return BayLook.Blocked;
                }

                if (dragging)
                {
                    return BayLook.Invalid;
                }
            }

            if (!IsBay(device, state))
            {
                return BayLook.None;
            }

            return FaceDevices.OffersIgniter(device, canFitIgniter) ? BayLook.Ready : BayLook.Rest;
        }

        /// <summary>True for the looks that breathe and light the leader: the carried part fits here.</summary>
        public static bool IsLit(BayLook look)
        {
            return look == BayLook.Valid || look == BayLook.Hover;
        }
    }
}

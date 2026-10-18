using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>Where a drag of a hold part stands.</summary>
    internal enum HoldDragPhase
    {
        Idle = 0,

        /// <summary>The pointer went down on a part and has not moved far enough to be a drag (still a click).</summary>
        Pressed = 1,

        /// <summary>The part's icon follows the pointer.</summary>
        Dragging = 2,

        /// <summary>The drop was refused: the icon eases back into its own slot.</summary>
        Returning = 3,

        /// <summary>The drop was taken: the icon eases into the device and fades.</summary>
        Fitting = 4,
    }

    /// <summary>
    /// The drag of a part out of 07's hold on the face screen (pure, EditMode-tested), in the 1152x768 reference frame.
    /// A press stays a click until the pointer moves past <see cref="CargoScreenSettings.DragThreshold"/>; then the
    /// part's icon follows the pointer with a soft lag, keeping the spot where it was grabbed. On release the owner
    /// decides (<see cref="DropRule"/>) and either sends the icon home (<see cref="Return"/>) or into the device that
    /// took it (<see cref="Fit"/>), each eased, never snapped.
    /// </summary>
    internal sealed class HoldDrag
    {
        private readonly CargoScreenSettings _settings;
        private Vector2 _home;
        private Vector2 _grab;
        private Vector2 _pointer;
        private Vector2 _pressedAt;
        private Vector2 _from;
        private Vector2 _to;
        private float _t;

        public HoldDrag(CargoScreenSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public HoldDragPhase Phase { get; private set; }

        /// <summary>The hold cell the part came from (-1 while idle).</summary>
        public int Cell { get; private set; } = -1;

        /// <summary>Centre of the dragged icon.</summary>
        public Vector2 Ghost { get; private set; }

        /// <summary>True while the icon is off its slot (dragged, returning or fitting).</summary>
        public bool ShowsGhost => Phase >= HoldDragPhase.Dragging;

        /// <summary>The icon's opacity: whole, fading only as a device takes it.</summary>
        public float GhostAlpha => Phase == HoldDragPhase.Fitting ? 1f - UiEase.InOutSine(_t) : 1f;

        /// <summary>The pointer went down on <paramref name="cell"/>, whose icon sits at <paramref name="cellCenter"/>.</summary>
        public void Press(int cell, Vector2 pointer, Vector2 cellCenter)
        {
            Phase = HoldDragPhase.Pressed;
            Cell = cell;
            _home = cellCenter;
            _grab = cellCenter - pointer;
            _pointer = pointer;
            _pressedAt = pointer;
            Ghost = cellCenter;
        }

        /// <summary>The pointer moved; true on the move that turns a press into a drag.</summary>
        public bool Move(Vector2 pointer)
        {
            _pointer = pointer;
            if (Phase != HoldDragPhase.Pressed)
            {
                return false;
            }

            float threshold = _settings.DragThreshold;
            if ((pointer - _pressedAt).sqrMagnitude < threshold * threshold)
            {
                return false;
            }

            Phase = HoldDragPhase.Dragging;
            return true;
        }

        /// <summary>
        /// The pointer came up. True when it ends a drag, so the owner must answer with <see cref="Return"/> or
        /// <see cref="Fit"/>; a press that never moved was a click and simply ends.
        /// </summary>
        public bool Release(Vector2 pointer)
        {
            _pointer = pointer;
            if (Phase == HoldDragPhase.Dragging)
            {
                return true;
            }

            if (Phase == HoldDragPhase.Pressed)
            {
                Clear();
            }

            return false;
        }

        /// <summary>The drop was refused: the icon eases back into its slot.</summary>
        public void Return()
        {
            Settle(HoldDragPhase.Returning, _home);
        }

        /// <summary>The drop was taken: the icon eases into the device at <paramref name="deviceCenter"/>.</summary>
        public void Fit(Vector2 deviceCenter)
        {
            Settle(HoldDragPhase.Fitting, deviceCenter);
        }

        /// <summary>Drops everything at once (the screen closed).</summary>
        public void Clear()
        {
            Phase = HoldDragPhase.Idle;
            Cell = -1;
            _t = 0f;
        }

        /// <param name="deltaTime">Unscaled seconds.</param>
        public void Step(float deltaTime)
        {
            switch (Phase)
            {
                case HoldDragPhase.Dragging:
                    Ghost = Follow(Ghost, _pointer + _grab, _settings.DragFollowSeconds, deltaTime);
                    break;
                case HoldDragPhase.Returning:
                    Ease(deltaTime, _settings.DragReturnSeconds);
                    break;
                case HoldDragPhase.Fitting:
                    Ease(deltaTime, _settings.DragFitSeconds);
                    break;
            }
        }

        private void Settle(HoldDragPhase phase, Vector2 to)
        {
            Phase = phase;
            _from = Ghost;
            _to = to;
            _t = 0f;
        }

        private void Ease(float deltaTime, float seconds)
        {
            _t = Mathf.Min(1f, _t + deltaTime / seconds);
            Ghost = Vector2.LerpUnclamped(_from, _to, UiEase.InOutSine(_t));
            if (_t >= 1f)
            {
                Clear();
            }
        }

        private static Vector2 Follow(Vector2 current, Vector2 target, float seconds, float deltaTime)
        {
            if (seconds <= 0f)
            {
                return target;
            }

            return deltaTime <= 0f ? current : current + (target - current) * (1f - Mathf.Exp(-deltaTime / seconds));
        }
    }
}

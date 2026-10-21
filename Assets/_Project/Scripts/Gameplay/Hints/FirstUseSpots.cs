using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Where the world teaches the keys a first time, read straight from the opening's state: the Build key at the apron
    /// once the pad can be built (its blueprint known, its cost in the hold) and the Tab key at 07 once the spare igniter
    /// can be fitted. A chip whose place is off screen stands on 07 instead (<see cref="Place"/>), so the key is never
    /// hidden behind the camera: <see cref="FirstUsePointer"/> turns 07's head toward the place. The UI shows each as a
    /// world chip a first time, then never again (<see cref="InteractionKind.OpenBuild"/>,
    /// <see cref="InteractionKind.FitPart"/>, <see cref="InteractionKind.Tether"/>, <see cref="InteractionKind.Install"/>).
    /// </summary>
    public sealed class FirstUseSpots
    {
        private readonly IOpeningProgress _opening;
        private readonly BuildField _building;
        private readonly IViewCamera _view;
        private readonly IRoverState _rover;
        private readonly float _viewMargin;
        private readonly float _roverRise;

        /// <param name="viewMargin">A place counts as in view this far in from each screen edge (viewport share).</param>
        /// <param name="roverRise">Metres above 07's root a chip stands on 07 (the prompt adds its own lift).</param>
        public FirstUseSpots(IOpeningProgress opening, BuildField building, IViewCamera view, IRoverState rover,
            float viewMargin, float roverRise)
        {
            _opening = opening ?? throw new ArgumentNullException(nameof(opening));
            _building = building != null ? building : throw new ArgumentNullException(nameof(building));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _rover = rover ?? throw new ArgumentNullException(nameof(rover));
            _viewMargin = viewMargin;
            _roverRise = roverRise;
        }

        /// <summary>
        /// The Build key's place: the apron's centre, while the charging pad can be built (known and in the hold) but
        /// stands nowhere yet and build mode is closed.
        /// </summary>
        public bool TryGetOpenBuild(out Vector3 point)
        {
            point = Vector3.zero;
            if (_building.State != BuildModeState.Closed || !_building.ApronCentre.HasValue ||
                _opening.Built(StructureIds.ChargingPad) || !_opening.Knows(StructureIds.ChargingPad) ||
                !_opening.Covers(StructureIds.ChargingPad))
            {
                return false;
            }

            point = _building.ApronCentre.Value;
            return true;
        }

        /// <summary>The Tab key's place: 07, while the spare igniter is in the hold, can be fitted and is not yet.</summary>
        public bool TryGetFitPart(out Vector3 point)
        {
            point = _rover.Position;
            return _opening.CanFitIgniter && !_opening.IgniterFitted;
        }

        /// <summary>
        /// Where a first-use chip for <paramref name="target"/> stands: there while it is in view, else on 07
        /// (<paramref name="onRover"/>), which is always in view.
        /// </summary>
        public Vector3 Place(Vector3 target, out bool onRover)
        {
            onRover = OffScreen(_view.Camera, target, _viewMargin);
            return onRover ? OnRover : target;
        }

        /// <summary>
        /// True when <paramref name="point"/> is behind <paramref name="camera"/> or outside its view, less
        /// <paramref name="margin"/> (a viewport share) from each edge.
        /// </summary>
        public static bool OffScreen(Camera camera, Vector3 point, float margin)
        {
            Vector3 view = camera.WorldToViewportPoint(point);
            return view.z <= 0f || view.x < margin || view.x > 1f - margin || view.y < margin || view.y > 1f - margin;
        }

        private Vector3 OnRover => _rover.Position + Vector3.up * _roverRise;
    }
}

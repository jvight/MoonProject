using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Where the world teaches the two keys no verb teaches by itself, read straight from the opening's state: the Build
    /// key at the apron once the pad can be built (its blueprint known, its cost in the hold, the marked bay in view)
    /// and the Tab key at 07 once the spare igniter can be fitted. The UI shows each as a world chip a first time, then
    /// never again (<see cref="InteractionKind.OpenBuild"/>, <see cref="InteractionKind.FitPart"/>).
    /// </summary>
    public sealed class FirstUseSpots
    {
        private readonly IOpeningProgress _opening;
        private readonly BuildField _building;
        private readonly IViewCamera _view;
        private readonly IRoverState _rover;
        private readonly float _viewMargin;

        /// <param name="viewMargin">The bay counts as in view this far in from each screen edge (viewport share).</param>
        public FirstUseSpots(IOpeningProgress opening, BuildField building, IViewCamera view, IRoverState rover,
            float viewMargin)
        {
            _opening = opening ?? throw new ArgumentNullException(nameof(opening));
            _building = building != null ? building : throw new ArgumentNullException(nameof(building));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _rover = rover ?? throw new ArgumentNullException(nameof(rover));
            _viewMargin = viewMargin;
        }

        /// <summary>
        /// The Build key's place: the apron's centre, while the charging pad can be built but stands nowhere yet, build
        /// mode is closed and the bay is on screen.
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
            Vector3 view = _view.Camera.WorldToViewportPoint(point);
            return view.z > 0f && view.x >= _viewMargin && view.x <= 1f - _viewMargin && view.y >= _viewMargin &&
                   view.y <= 1f - _viewMargin;
        }

        /// <summary>The Tab key's place: 07, while the spare igniter is in the hold, can be fitted and is not yet.</summary>
        public bool TryGetFitPart(out Vector3 point)
        {
            point = _rover.Position;
            return _opening.CanFitIgniter && !_opening.IgniterFitted;
        }
    }
}

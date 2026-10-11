using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Finds the recipe 07's view rests on (docs/features/M3-24 B, <see cref="IRecipeLook"/>): the radio tower's port
    /// hopper while the tower has a level left to build, or a sheet on Kenji's board whose kit is not yet fitted. A
    /// target is looked at while it lies within reach and close to the view's centre; the one nearest the centre wins,
    /// and the one already looked at holds a little wider so the card does not flicker at the edge.
    /// </summary>
    public sealed class RecipeLook : IRecipeLook
    {
        private readonly IViewCamera _view;
        private readonly IUpgradeShop _shop;
        private readonly RadioTower _tower;
        private readonly BlueprintPlotter _plotter;
        private readonly ProgressionTuning _tuning;
        private readonly string[] _kits;

        public RecipeLook(IViewCamera view, IUpgradeShop shop, RadioTower tower, BlueprintPlotter plotter,
            IEnumerable<string> kits, ProgressionTuning tuning)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _shop = shop ?? throw new ArgumentNullException(nameof(shop));
            _tower = tower != null ? tower : throw new ArgumentNullException(nameof(tower));
            _plotter = plotter != null ? plotter : throw new ArgumentNullException(nameof(plotter));
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _kits = new List<string>(kits ?? throw new ArgumentNullException(nameof(kits))).ToArray();
        }

        public string Looked { get; private set; }

        public bool IsSheet { get; private set; }

        public void Step()
        {
            Transform eye = _view.Camera.transform;
            Vector3 from = eye.position;
            Vector3 forward = eye.forward;
            string best = null;
            bool bestSheet = false;
            float bestAngle = float.MaxValue;
            string tower = _tower.Definition.Id;
            if (Buildable(tower))
            {
                Consider(tower, false, _tower.HopperMouth, from, forward, ref best, ref bestSheet, ref bestAngle);
            }

            for (int i = 0; i < _kits.Length; i++)
            {
                if (Buildable(_kits[i]) && _plotter.TryGetSheet(_kits[i], out Vector3 sheet))
                {
                    Consider(_kits[i], true, sheet, from, forward, ref best, ref bestSheet, ref bestAngle);
                }
            }

            Looked = best;
            IsSheet = bestSheet;
        }

        private void Consider(string id, bool sheet, Vector3 at, Vector3 from, Vector3 forward, ref string best,
            ref bool bestSheet, ref float bestAngle)
        {
            Vector3 toward = at - from;
            if (toward.sqrMagnitude > _tuning.RecipeLookDistance * _tuning.RecipeLookDistance)
            {
                return;
            }

            float angle = Vector3.Angle(forward, toward);
            float limit = _tuning.RecipeLookAngle * (id == Looked ? _tuning.RecipeLookHold : 1f);
            if (angle <= limit && angle < bestAngle)
            {
                best = id;
                bestSheet = sheet;
                bestAngle = angle;
            }
        }

        private bool Buildable(string upgradeId)
        {
            return _shop.TryGetOffer(upgradeId, out UpgradeOffer offer) && !offer.IsMaxed;
        }
    }
}

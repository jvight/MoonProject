using System;
using UnityEngine.UIElements;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// The recovery panel's four target rows (docs/features/M3-21 §2), each the target's name and its row of lamps
    /// (dark glass, warm amber when lit), built once under a container. The lab's tech board and the opening's protocol
    /// readout (M3-24 B) show the same rows, so the player meets the panel before reaching it.
    /// </summary>
    internal sealed class RecoveryRows
    {
        private const string LampClass = "recovery-board__lamp";
        private const string LitClass = "recovery-board__lamp--lit";
        private const string NameSuffix = ".name";

        private static readonly string[] TargetKeys =
        {
            "recovery.power", "recovery.link", "recovery.dish", "recovery.answer",
        };

        private readonly ILocalization _localization;
        private readonly IRecoveryProgress _recovery;
        private readonly VisualElement[] _rows;
        private readonly Label[] _targets;
        private readonly VisualElement[][] _lamps;

        public RecoveryRows(VisualElement container, ILocalization localization, IRecoveryProgress recovery)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _recovery = recovery ?? throw new ArgumentNullException(nameof(recovery));
            int targets = Enum.GetValues(typeof(RecoveryTarget)).Length;
            if (targets != TargetKeys.Length)
            {
                throw new InvalidOperationException("The recovery rows' labels do not match Core's targets.");
            }

            _rows = new VisualElement[targets];
            _targets = new Label[targets];
            _lamps = new VisualElement[targets][];
            for (int i = 0; i < targets; i++)
            {
                _rows[i] = new VisualElement { pickingMode = PickingMode.Ignore };
                _rows[i].AddToClassList("recovery-board__row");
                _targets[i] = new Label { pickingMode = PickingMode.Ignore };
                _targets[i].AddToClassList("recovery-board__target");
                _rows[i].Add(_targets[i]);
                var lamps = new VisualElement { pickingMode = PickingMode.Ignore };
                lamps.AddToClassList("recovery-board__lamps");
                _lamps[i] = new VisualElement[_recovery.Lamps((RecoveryTarget)i)];
                for (int lamp = 0; lamp < _lamps[i].Length; lamp++)
                {
                    _lamps[i][lamp] = new VisualElement { pickingMode = PickingMode.Ignore };
                    _lamps[i][lamp].AddToClassList(LampClass);
                    lamps.Add(_lamps[i][lamp]);
                }

                _rows[i].Add(lamps);
                container.Add(_rows[i]);
            }

            Relocalize();
        }

        /// <summary>The row element of <paramref name="target"/>.</summary>
        public VisualElement RowOf(RecoveryTarget target)
        {
            return _rows[(int)target];
        }

        /// <summary>Lights each row's lamps from the recovery progress.</summary>
        public void Write()
        {
            for (int i = 0; i < _lamps.Length; i++)
            {
                int lit = _recovery.LitLamps((RecoveryTarget)i);
                for (int lamp = 0; lamp < _lamps[i].Length; lamp++)
                {
                    _lamps[i][lamp].EnableInClassList(LitClass, lamp < lit);
                }
            }
        }

        public void Relocalize()
        {
            for (int i = 0; i < _targets.Length; i++)
            {
                _targets[i].text = _localization.Get(TargetKeys[i] + NameSuffix);
            }
        }
    }
}

using System;
using UnityEngine.UIElements;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// The tech board's labels while 07 looks at it in the lab (docs/features/M3-21 §2, M3-19 §5): at its head the
    /// recovery panel, the protocol's name and the four targets, each with its row of lamps (dark glass, warm amber
    /// when lit); below, the names of the pins Kenji's board knows, a decoded sheet softer than a built one. Read from
    /// <see cref="IRecoveryProgress"/> and <see cref="ITechTree"/>; the labels are written only when the board
    /// changes or the language does, never per frame.
    /// </summary>
    internal sealed class RecoveryBoard
    {
        private const string NodeClass = "recovery-board__node";
        private const string DecodedClass = "recovery-board__node--decoded";
        private const string TitleKey = "ui.recovery.title";
        private const string KnownKey = "ui.board.known";
        private const string NameSuffix = ".name";

        private static readonly string[] NodeKeys =
        {
            "tech.tower_l1", "tech.tower_l2", "tech.tower_l3", "tech.relay_network", "tech.hover_jump",
            "tech.cargo_cradle", "tech.warm_headlamp", "tech.capacitor_drums", "tech.cell_pack_2", "tech.focus_lens",
            "tech.side_bins",
        };

        private readonly ILocalization _localization;
        private readonly ITechTree _tech;
        private readonly Reveal _root;
        private readonly Label _title;
        private readonly Label _known;
        private readonly RecoveryRows _rows;
        private readonly Label[] _nodes;
        private bool _looking;
        private bool _dirty = true;

        /// <param name="labHud">The lab's screen, holding the board's labels (lab-board).</param>
        public RecoveryBoard(VisualElement labHud, RevealSettings reveal, ILocalization localization, ITechTree tech,
            IRecoveryProgress recovery)
        {
            if (labHud == null)
            {
                throw new ArgumentNullException(nameof(labHud));
            }

            VisualElement root = Require<VisualElement>(labHud, "lab-board");
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _tech = tech ?? throw new ArgumentNullException(nameof(tech));
            _root = new Reveal(root, reveal);
            _root.Snap(false);
            _title = Require<Label>(root, "lab-board-title");
            _known = Require<Label>(root, "lab-board-known");
            VisualElement nodes = Require<VisualElement>(root, "lab-board-nodes");
            if (Enum.GetValues(typeof(TechNodeId)).Length != NodeKeys.Length)
            {
                throw new InvalidOperationException("The board's labels do not match Core's tech nodes.");
            }

            _rows = new RecoveryRows(Require<VisualElement>(root, "lab-board-rows"), localization,
                recovery ?? throw new ArgumentNullException(nameof(recovery)));
            _nodes = new Label[NodeKeys.Length];
            for (int i = 0; i < _nodes.Length; i++)
            {
                _nodes[i] = new Label { pickingMode = PickingMode.Ignore };
                _nodes[i].AddToClassList(NodeClass);
                nodes.Add(_nodes[i]);
            }

            Relocalize();
        }

        /// <summary>True while 07 looks at the tech board.</summary>
        public bool IsLooking => _looking;

        /// <summary>07 looks at the board, or looks away from it (or leaves the lab).</summary>
        public void Look(bool looking)
        {
            _looking = looking;
            _dirty |= looking;
        }

        /// <summary>A node or a row changed: the labels are rewritten the next time they show.</summary>
        public void MarkDirty()
        {
            _dirty = true;
        }

        /// <param name="deltaTime">Unscaled seconds (0 while paused).</param>
        /// <param name="shown">The screen is free for the lab (not paused).</param>
        public void Tick(float deltaTime, bool shown)
        {
            if (_dirty && _looking)
            {
                _dirty = false;
                Write();
            }

            _root.Set(_looking && shown);
            _root.Tick(deltaTime);
        }

        public void Relocalize()
        {
            _title.text = _localization.Get(TitleKey);
            _known.text = _localization.Get(KnownKey);
            _rows.Relocalize();

            for (int i = 0; i < _nodes.Length; i++)
            {
                _nodes[i].text = _localization.Get(NodeKeys[i] + NameSuffix);
            }
        }

        private void Write()
        {
            _rows.Write();
            for (int i = 0; i < _nodes.Length; i++)
            {
                TechNodeState state = _tech.StateOf((TechNodeId)i);
                _nodes[i].style.display = state == TechNodeState.Unknown ? DisplayStyle.None : DisplayStyle.Flex;
                _nodes[i].EnableInClassList(DecodedClass, state == TechNodeState.Decoded);
            }

            _known.style.display = _tech.KnownCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static T Require<T>(VisualElement root, string name) where T : VisualElement
        {
            return root.Q<T>(name) ?? throw new InvalidOperationException(
                $"GameUI.uxml has no {typeof(T).Name} named '{name}' under '{root.name}'.");
        }
    }
}

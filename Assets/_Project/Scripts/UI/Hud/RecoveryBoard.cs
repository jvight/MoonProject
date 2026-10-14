using System;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.UI
{
    /// <summary>
    /// The tech board's labels while 07 looks at it in the lab (docs/features/M3-21 §2, M3-19 §3.2, §5): at its head
    /// the recovery panel, the protocol's name and the four targets, each with its row of lamps (dark glass, warm amber
    /// when lit); below, the pins Kenji's board knows—a torn blueprint sheet growing piece by piece for an unknown kit
    /// with scanned fragments, a decoded sheet softer than a built one. Read from <see cref="IRecoveryProgress"/>,
    /// <see cref="ITechTree"/> and <see cref="IScanSites"/>; updated only when the board changes or the language does,
    /// never per frame.
    /// </summary>
    internal sealed class RecoveryBoard
    {
        public const string TornClass = "recovery-board__node--torn";
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
        private readonly IScanSites _scanSites;
        private readonly Reveal _root;
        private readonly Label _title;
        private readonly Label _known;
        private readonly RecoveryRows _rows;
        private readonly Label[] _nodes;
        private readonly TornSheetPainter[] _sheets;
        private readonly string[] _nodeNames;
        private bool _looking;
        private bool _dirty = true;

        /// <param name="labHud">The lab's screen, holding the board's labels (lab-board).</param>
        public RecoveryBoard(VisualElement labHud, RevealSettings reveal, ILocalization localization, ITechTree tech,
            IRecoveryProgress recovery, IScanSites scanSites = null)
        {
            if (labHud == null)
            {
                throw new ArgumentNullException(nameof(labHud));
            }

            VisualElement root = Require<VisualElement>(labHud, "lab-board");
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _tech = tech ?? throw new ArgumentNullException(nameof(tech));
            _scanSites = scanSites;
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
            _sheets = new TornSheetPainter[NodeKeys.Length];
            _nodeNames = new string[NodeKeys.Length];
            for (int i = 0; i < _nodes.Length; i++)
            {
                _nodes[i] = new Label { pickingMode = PickingMode.Ignore };
                _nodes[i].AddToClassList(NodeClass);
                _sheets[i] = new TornSheetPainter(_nodes[i]);
                nodes.Add(_nodes[i]);
            }

            Relocalize();
        }

        /// <summary>True while 07 looks at the tech board.</summary>
        public bool IsLooking => _looking;

        /// <summary>True when <paramref name="node"/> currently shows a torn blueprint sheet (<c>0 &lt; s &lt; z</c>).</summary>
        public bool IsNodeTorn(TechNodeId node) => _sheets[(int)node].Pieces > 0;

        /// <summary>How many torn pieces <paramref name="node"/>'s sheet currently shows (0 when hidden or whole).</summary>
        public int NodePieceCount(TechNodeId node) => _sheets[(int)node].Pieces;

        /// <summary>The fragment set size of <paramref name="node"/>'s torn sheet (0 when hidden or whole).</summary>
        public int NodeSetSize(TechNodeId node) => _sheets[(int)node].SetSize;

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

        /// <summary>A fragment was scanned: the board's torn sheets update on the next look or tick.</summary>
        public void OnFragmentScanned(FragmentScanned scanned)
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
                _nodeNames[i] = _localization.Get(NodeKeys[i] + NameSuffix);
                _nodes[i].text = _sheets[i].Pieces > 0 ? string.Empty : _nodeNames[i];
            }
        }

        private void Write()
        {
            _rows.Write();
            bool anyShown = _tech.KnownCount > 0;
            for (int i = 0; i < _nodes.Length; i++)
            {
                var nodeId = (TechNodeId)i;
                TechNodeState state = _tech.StateOf(nodeId);
                int pieces = 0;
                int setSize = 0;
                if (state == TechNodeState.Unknown && _scanSites != null)
                {
                    string kit = _tech.KitOf(nodeId);
                    if (!string.IsNullOrEmpty(kit))
                    {
                        int s = _scanSites.ScannedOf(kit);
                        int z = _scanSites.SetSize(kit);
                        if (s > 0 && s < z)
                        {
                            pieces = s;
                            setSize = z;
                        }
                    }
                }

                bool torn = pieces > 0;
                bool shown = state != TechNodeState.Unknown || torn;
                anyShown |= shown;
                _sheets[i].Set(pieces, setSize);
                _nodes[i].text = torn ? string.Empty : _nodeNames[i];
                _nodes[i].style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
                _nodes[i].EnableInClassList(DecodedClass, state == TechNodeState.Decoded);
                _nodes[i].EnableInClassList(TornClass, torn);
            }

            _known.style.display = anyShown ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static T Require<T>(VisualElement root, string name) where T : VisualElement
        {
            return root.Q<T>(name) ?? throw new InvalidOperationException(
                $"GameUI.uxml has no {typeof(T).Name} named '{name}' under '{root.name}'.");
        }
    }
}

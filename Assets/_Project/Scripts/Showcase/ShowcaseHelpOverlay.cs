using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

namespace MoonProject.Showcase
{
    /// <summary>
    /// Plain Vietnamese UI Toolkit help overlay for the Showcase build, using Be Vietnam Pro and toggled with H.
    /// Shows camera/time/exhibit controls, live lunar clock and sky phase, and the focused exhibit's name and
    /// animation keys.
    /// </summary>
    public sealed class ShowcaseHelpOverlay : MonoBehaviour
    {
        [Tooltip("UIDocument hosting the Showcase help panel.")]
        [SerializeField] private UIDocument _uiDocument;

        [Tooltip("Be Vietnam Pro Regular SDF FontAsset for body text.")]
        [SerializeField] private FontAsset _regularFontAsset;

        [Tooltip("Be Vietnam Pro SemiBold SDF FontAsset for headers and key badges.")]
        [SerializeField] private FontAsset _semiBoldFontAsset;

        [Tooltip("Fallback Be Vietnam Pro Regular TTF Font if SDF FontAsset is not assigned.")]
        [SerializeField] private Font _fallbackRegularFont;

        private VisualElement _panelRoot;
        private Label _exhibitTitleLabel;
        private Label _exhibitSubtitleLabel;
        private Label _cameraModeLabel;
        private Label _clockLabel;
        private Label _phaseLabel;
        private VisualElement _exhibitKeysContainer;
        private ShowcaseExhibitId _lastRenderedExhibitId = (ShowcaseExhibitId)(-1);
        private bool _isVisible = true;

        public bool IsVisible => _isVisible;

        public void Initialize()
        {
            BuildUiTree();
            SetVisible(true);
        }

        public bool ToggleVisible()
        {
            SetVisible(!_isVisible);
            return _isVisible;
        }

        public void SetVisible(bool visible)
        {
            _isVisible = visible;
            if (_panelRoot != null)
            {
                _panelRoot.style.display = _isVisible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        public void RefreshStatus(
            ShowcaseExhibitDefinition focusedExhibit,
            bool isFreeFly,
            string clockReadout,
            string livePhaseVi,
            bool isFastTime,
            bool isTourActive = false,
            bool isDemoActive = false)
        {
            if (_panelRoot == null)
            {
                BuildUiTree();
                if (_panelRoot == null)
                {
                    return;
                }
            }

            if (focusedExhibit != null)
            {
                _exhibitTitleLabel.text = $"[{focusedExhibit.NumberKey}/15] {focusedExhibit.TitleVi}";
                _exhibitSubtitleLabel.text = focusedExhibit.SubtitleVi;

                if (_lastRenderedExhibitId != focusedExhibit.ExhibitId)
                {
                    _lastRenderedExhibitId = focusedExhibit.ExhibitId;
                    RebuildExhibitKeyHints(focusedExhibit.KeyHints);
                }
            }

            string modeText = isTourActive
                ? "Chế độ: Tham quan camera chậm (O)"
                : (isFreeFly ? "Chế độ: Bay tự do (WASD)" : "Chế độ: Quay quanh mẫu vật");
            if (isDemoActive)
            {
                modeText += " • [Demo: BẬT]";
            }

            _cameraModeLabel.text = modeText;
            string speedTag = isFastTime ? " [Tua nhanh 60x]" : " [1x]";
            _clockLabel.text = $"Đồng hồ: {clockReadout}{speedTag}";
            _phaseLabel.text = $"Pha bầu trời: {livePhaseVi}";
        }

        private void BuildUiTree()
        {
            if (_uiDocument == null || _uiDocument.rootVisualElement == null)
            {
                return;
            }

            VisualElement root = _uiDocument.rootVisualElement;
            root.Clear();
            root.pickingMode = PickingMode.Ignore;
            ApplyFont(root, false);

            _panelRoot = new VisualElement();
            _panelRoot.name = "showcase-help-panel";
            _panelRoot.pickingMode = PickingMode.Ignore;
            _panelRoot.style.position = Position.Absolute;
            _panelRoot.style.left = 20;
            _panelRoot.style.top = 20;
            _panelRoot.style.width = 445;
            _panelRoot.style.paddingLeft = 16;
            _panelRoot.style.paddingRight = 16;
            _panelRoot.style.paddingTop = 14;
            _panelRoot.style.paddingBottom = 14;
            _panelRoot.style.backgroundColor = new Color(0.05f, 0.06f, 0.09f, 0.84f);
            _panelRoot.style.borderTopLeftRadius = 6;
            _panelRoot.style.borderTopRightRadius = 6;
            _panelRoot.style.borderBottomLeftRadius = 6;
            _panelRoot.style.borderBottomRightRadius = 6;
            _panelRoot.style.borderTopWidth = 1;
            _panelRoot.style.borderBottomWidth = 1;
            _panelRoot.style.borderLeftWidth = 1;
            _panelRoot.style.borderRightWidth = 1;
            Color borderColor = new Color(0.88f, 0.64f, 0.28f, 0.45f);
            _panelRoot.style.borderTopColor = borderColor;
            _panelRoot.style.borderBottomColor = borderColor;
            _panelRoot.style.borderLeftColor = borderColor;
            _panelRoot.style.borderRightColor = borderColor;

            Label header = CreateLabel("LUNARION 0.5.7 — XEM THỬ DIỆN MẠO R1 (SHOWCASE 2)", 13, new Color(0.96f, 0.75f, 0.36f, 1f), true);
            header.style.marginBottom = 8;
            _panelRoot.Add(header);

            VisualElement statusBox = new VisualElement();
            statusBox.pickingMode = PickingMode.Ignore;
            statusBox.style.backgroundColor = new Color(0.10f, 0.12f, 0.17f, 0.85f);
            statusBox.style.paddingLeft = 10;
            statusBox.style.paddingRight = 10;
            statusBox.style.paddingTop = 8;
            statusBox.style.paddingBottom = 8;
            statusBox.style.marginBottom = 10;
            statusBox.style.borderTopLeftRadius = 4;
            statusBox.style.borderTopRightRadius = 4;
            statusBox.style.borderBottomLeftRadius = 4;
            statusBox.style.borderBottomRightRadius = 4;

            _exhibitTitleLabel = CreateLabel("[1/15] 07 (Rover v3)", 14, new Color(0.96f, 0.95f, 0.92f, 1f), true);
            _exhibitSubtitleLabel = CreateLabel(string.Empty, 11, new Color(0.76f, 0.80f, 0.88f, 1f), false);
            _exhibitSubtitleLabel.style.marginBottom = 4;
            _cameraModeLabel = CreateLabel("Chế độ: Quay quanh mẫu vật", 11, new Color(0.82f, 0.86f, 0.94f, 1f), false);
            _clockLabel = CreateLabel("Đồng hồ: 15:00 (13%) [1x]", 11, new Color(0.95f, 0.84f, 0.62f, 1f), false);
            _phaseLabel = CreateLabel("Pha bầu trời: Ban ngày nắng gắt", 11, new Color(0.95f, 0.84f, 0.62f, 1f), true);

            statusBox.Add(_exhibitTitleLabel);
            statusBox.Add(_exhibitSubtitleLabel);
            statusBox.Add(_cameraModeLabel);
            statusBox.Add(_clockLabel);
            statusBox.Add(_phaseLabel);
            _panelRoot.Add(statusBox);

            Label generalHeader = CreateLabel("ĐIỀU KHIỂN CAMERA, THAM QUAN & THỜI GIAN", 11, new Color(0.96f, 0.75f, 0.36f, 1f), true);
            generalHeader.style.marginBottom = 4;
            _panelRoot.Add(generalHeader);

            _panelRoot.Add(CreateKeyRow("Tab / 1-6", "Chuyển qua 15 mẫu vật (Tab) / Chọn nhanh mẫu 1–6"));
            _panelRoot.Add(CreateKeyRow("O", "Bật / Tắt tham quan camera chậm (Hoàng hôn → Đêm → Nhật thực)"));
            _panelRoot.Add(CreateKeyRow("J / K", "Dựng 4 giai đoạn Stage_1..4 (J) / Tự động trình diễn Demo (K)"));
            _panelRoot.Add(CreateKeyRow("Chuột / Lăn", "Xoay quanh mẫu vật / Thu phóng khoảng cách"));
            _panelRoot.Add(CreateKeyRow("WASD / Q E", "Bay tự do / Hạ xuống (Q) - Nâng lên (E) (Shift: tăng tốc)"));
            _panelRoot.Add(CreateKeyRow("T / [ ] / P", "Đổi pha bầu trời (T) / Tua đồng hồ ([ ]) / Tốc độ 60x (P)"));
            _panelRoot.Add(CreateKeyRow("H", "Ẩn / Hiện bảng hướng dẫn này"));

            Label animHeader = CreateLabel("PHÍM HOẠT HỌA CỦA MẪU ĐANG CHỌN", 11, new Color(0.96f, 0.75f, 0.36f, 1f), true);
            animHeader.style.marginTop = 8;
            animHeader.style.marginBottom = 4;
            _panelRoot.Add(animHeader);

            _exhibitKeysContainer = new VisualElement();
            _exhibitKeysContainer.pickingMode = PickingMode.Ignore;
            _panelRoot.Add(_exhibitKeysContainer);

            root.Add(_panelRoot);
        }

        private void RebuildExhibitKeyHints(ShowcaseKeyHint[] hints)
        {
            if (_exhibitKeysContainer == null)
            {
                return;
            }

            _exhibitKeysContainer.Clear();
            if (hints == null || hints.Length == 0)
            {
                _exhibitKeysContainer.Add(CreateLabel("Không có hoạt họa riêng (mẫu vật tĩnh kiểm tra ánh sáng).", 11,
                    new Color(0.74f, 0.78f, 0.85f, 1f), false));
                return;
            }

            for (int i = 0; i < hints.Length; i++)
            {
                ShowcaseKeyHint hint = hints[i];
                if (hint != null)
                {
                    _exhibitKeysContainer.Add(CreateKeyRow(hint.KeyLabel, hint.DescriptionVi));
                }
            }
        }

        private VisualElement CreateKeyRow(string keyText, string descriptionText)
        {
            var row = new VisualElement();
            row.pickingMode = PickingMode.Ignore;
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 2;

            Label keyBadge = CreateLabel(keyText, 11, new Color(0.98f, 0.82f, 0.45f, 1f), true);
            keyBadge.style.width = 96;
            keyBadge.style.flexShrink = 0;

            Label desc = CreateLabel(descriptionText, 11, new Color(0.88f, 0.90f, 0.94f, 1f), false);
            desc.style.flexGrow = 1;
            desc.style.whiteSpace = WhiteSpace.Normal;

            row.Add(keyBadge);
            row.Add(desc);
            return row;
        }

        private Label CreateLabel(string text, int fontSize, Color color, bool semiBold)
        {
            var label = new Label(text);
            label.pickingMode = PickingMode.Ignore;
            label.style.fontSize = fontSize;
            label.style.color = color;
            label.style.whiteSpace = WhiteSpace.Normal;
            ApplyFont(label, semiBold);
            return label;
        }

        private void ApplyFont(VisualElement element, bool semiBold)
        {
            FontAsset sdf = semiBold && _semiBoldFontAsset != null ? _semiBoldFontAsset : _regularFontAsset;
            if (sdf != null)
            {
                element.style.unityFontDefinition = FontDefinition.FromSDFFont(sdf);
                return;
            }

            if (_fallbackRegularFont != null)
            {
                element.style.unityFontDefinition = FontDefinition.FromFont(_fallbackRegularFont);
            }
        }
    }
}

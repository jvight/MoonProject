using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using MoonProject.App;
using MoonProject.Core;
using MoonProject.Gameplay;

namespace MoonProject.Autoplay
{
    /// <summary>
    /// The running game as the bot sees it: the bootstrap's services and the HUD document, found again whenever the
    /// scene is reloaded (New Game reloads it) by walking the scene's roots the way the playthrough tests do.
    /// </summary>
    public sealed class AutoplayWorld
    {
        private GameBootstrap _bootstrap;
        private GameplaySystem _gameplay;
        private UIDocument _document;
        private UIDocument _faceDocument;
        private UiProbe _ui;

        /// <summary>True when a booted game is present; the other members are valid only then.</summary>
        public bool Refresh()
        {
            if (_bootstrap != null && _bootstrap.Context != null && _gameplay != null && _document != null)
            {
                _ui?.UseFace(FaceRoot());
                return true;
            }

            _bootstrap = null;
            _gameplay = null;
            _document = null;
            _faceDocument = null;
            _ui = null;
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                _bootstrap = _bootstrap != null ? _bootstrap : root.GetComponent<GameBootstrap>();
                _gameplay = _gameplay != null ? _gameplay : root.GetComponentInChildren<GameplaySystem>(true);
                foreach (UIDocument document in root.GetComponentsInChildren<UIDocument>(true))
                {
                    VisualElement tree = document.rootVisualElement;
                    if (tree == null)
                    {
                        continue;
                    }

                    _document = _document != null ? _document : tree.Q(UiProbe.ObjectiveLineName) != null ? document : null;
                    _faceDocument = _faceDocument != null ? _faceDocument
                        : tree.Q(UiProbe.FaceDevicePrefix + 0) != null ? document : null;
                }
            }

            return _bootstrap != null && _bootstrap.Context != null && _gameplay != null && _document != null;
        }

        private VisualElement FaceRoot()
        {
            if (_faceDocument == null && _document != null)
            {
                foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
                {
                    foreach (UIDocument document in root.GetComponentsInChildren<UIDocument>(true))
                    {
                        if (document.rootVisualElement != null &&
                            document.rootVisualElement.Q(UiProbe.FaceDevicePrefix + 0) != null)
                        {
                            _faceDocument = document;
                        }
                    }
                }
            }

            return _faceDocument != null ? _faceDocument.rootVisualElement : null;
        }

        public GameContext Context => _bootstrap.Context;

        public GameplaySystem Gameplay => _gameplay;

        public IRoverState Rover => Context.Get<IRoverState>();

        public IViewCamera View => Context.Get<IViewCamera>();

        public IGameFlow Flow => Context.Get<IGameFlow>();

        public IWorldAnchors Anchors => Context.Get<IWorldAnchors>();

        /// <summary>The HUD probe; null while the document has not built its tree yet.</summary>
        public UiProbe Ui
        {
            get
            {
                if (_ui == null && _document.rootVisualElement != null && _document.rootVisualElement.childCount > 0)
                {
                    _ui = new UiProbe(_document.rootVisualElement, FaceRoot());
                }

                return _ui;
            }
        }
    }
}

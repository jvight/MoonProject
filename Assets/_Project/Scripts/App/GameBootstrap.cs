using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;
using MoonProject.Core.Save;

namespace MoonProject.App
{
    /// <summary>
    /// The single composition root. Builds the <see cref="GameContext"/> (with the <see cref="ISaveService"/>),
    /// initialises every scene system in the explicit order serialised below (filled by the scene builder), then loads
    /// the save so every registered section is restored before any Start() runs. Saves when the game quits or goes to
    /// the background. Owns the <see cref="IGameFlow"/>: with the title on, a boot shows it over the loaded world with
    /// the rover's controls held, until Continue or New Game; a New Game over a save puts it away and reloads the
    /// scene into a fresh boot (a <see cref="BootIntent"/>), and "Quit to title" saves and reloads into the title.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameBootstrap : MonoBehaviour
    {
        [Tooltip("The Controls input asset (Assets/_Project/Data/Input/Controls.inputactions).")]
        [SerializeField] private InputActionAsset _inputActions;

        [Tooltip("Scene systems implementing IGameSystem, initialised top to bottom.")]
        [SerializeField] private MonoBehaviour[] _systems = new MonoBehaviour[0];

        [Tooltip("Save file name (letters, digits, '-', '_') under Application.persistentDataPath/Saves. " +
                 "The -saveSlot <name> command-line argument overrides it (playtests, smoke runs).")]
        [SerializeField] private string _saveSlot = "main";

        [Tooltip("Boot into the title over the loaded world (M3-09). Off boots straight into play on the save, as " +
                 "PlayMode fixtures that drive 07 from the first frame expect.")]
        [SerializeField] private bool _showTitle;

        private readonly GameFlow _flow = new GameFlow();
        private InputReader _input;
        private SaveService _save;
        private BootIntent _intent;
        private IDisposable _newGame;
        private IDisposable _continue;
        private IDisposable _quitToTitle;
        private bool _reloading;

        public GameContext Context { get; private set; }

        private void Awake()
        {
            if (_inputActions == null)
            {
                Debug.LogError($"{nameof(GameBootstrap)}: Input Actions asset is not assigned.", this);
                enabled = false;
                return;
            }

            bool overridden = SaveSlotArgument.TryRead(Environment.GetCommandLineArgs(), out string requestedSlot);
            string slot = overridden ? requestedSlot : _saveSlot;
            string slotSource = overridden ? SaveSlotArgument.Flag + " argument" : "scene";
            if (!SaveService.IsValidSlot(slot))
            {
                Debug.LogError($"{nameof(GameBootstrap)}: save slot '{slot}' from the {slotSource} is invalid.", this);
                enabled = false;
                return;
            }

            _input = new InputReader(_inputActions);
            Context = new GameContext(new EventBus(), _input);
            try
            {
                _save = new SaveService(SaveService.DefaultDirectory, slot);
            }
            catch (ArgumentException exception)
            {
                Debug.LogError($"{nameof(GameBootstrap)}: {exception.Message}", this);
                enabled = false;
                return;
            }

            Context.Register<ISaveService>(_save);
            Context.Register<ISettingsFile>(new SettingsFile(SaveService.DefaultDirectory, slot));
            Context.Register<IGameFlow>(_flow);
            _intent = new BootIntent(SaveService.DefaultDirectory, slot);
            Debug.Log($"{nameof(GameBootstrap)}: save slot '{slot}' (from the {slotSource}): {_save.FilePath}", this);

            for (int i = 0; i < _systems.Length; i++)
            {
                if (_systems[i] is IGameSystem system)
                {
                    system.Initialize(Context);
                }
                else
                {
                    Debug.LogError($"{nameof(GameBootstrap)}: entry {i} ({_systems[i]}) is not an IGameSystem.", this);
                }
            }

            _save.Load();
            bool hasSave = _save.LoadResult == SaveLoadResult.Loaded ||
                           _save.LoadResult == SaveLoadResult.RecoveredFromBackup;
            _flow.Loaded(hasSave, _intent.TryConsume(), _showTitle);
            if (_flow.State == GameFlowState.Playing)
            {
                _input.Enable();
            }

            _newGame = Context.Events.Subscribe<NewGameRequested>(OnNewGameRequested);
            _continue = Context.Events.Subscribe<ContinueRequested>(OnContinueRequested);
            _quitToTitle = Context.Events.Subscribe<QuitToTitleRequested>(OnQuitToTitleRequested);
        }

        private void Start()
        {
            if (_flow.State == GameFlowState.Title)
            {
                Context.Events.Publish(new TitleShown(_flow.HasSave));
            }
            else
            {
                Context.Events.Publish(new SessionStarted(!_flow.HasSave));
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                _save?.SaveNow();
            }
        }

        private void OnApplicationQuit()
        {
            _save?.SaveNow();
        }

        private void OnDestroy()
        {
            _newGame?.Dispose();
            _continue?.Dispose();
            _quitToTitle?.Dispose();
            _input?.Dispose();
        }

        private void OnContinueRequested(ContinueRequested request)
        {
            if (_flow.TryContinue())
            {
                _input.Enable();
                Context.Events.Publish(new SessionStarted(false));
            }
        }

        /// <summary>
        /// Over a world with no save the title is already a fresh game, so play begins here. Over a save, puts the save
        /// away (it refuses every later save, so nothing torn down writes the old progress back), notes that the next
        /// boot starts fresh, and reloads this scene: every system, the context and this bootstrap are rebuilt.
        /// </summary>
        private void OnNewGameRequested(NewGameRequested request)
        {
            if (_reloading || !_flow.CanStartOver)
            {
                return;
            }

            if (_flow.TryStartHere())
            {
                _input.Enable();
                Context.Events.Publish(new SessionStarted(true));
                return;
            }

            if (!CanReload("a new game"))
            {
                return;
            }

            _reloading = true;
            _save.PutAway();
            _intent.Arm();
            SceneManager.LoadScene(gameObject.scene.buildIndex, LoadSceneMode.Single);
        }

        /// <summary>Saves and reloads this scene, which boots into the title over the same save.</summary>
        private void OnQuitToTitleRequested(QuitToTitleRequested request)
        {
            if (_reloading || !_flow.CanQuitToTitle || !CanReload("quitting to the title"))
            {
                return;
            }

            _reloading = true;
            _save.SaveNow();
            SceneManager.LoadScene(gameObject.scene.buildIndex, LoadSceneMode.Single);
        }

        private bool CanReload(string reason)
        {
            if (gameObject.scene.buildIndex >= 0)
            {
                return true;
            }

            Debug.LogError($"{nameof(GameBootstrap)}: scene '{gameObject.scene.path}' is not in the build settings, " +
                           $"so {reason} cannot reload it; the save was left as it is.", this);
            return false;
        }

        private void OnValidate()
        {
            for (int i = 0; i < _systems.Length; i++)
            {
                if (_systems[i] != null && !(_systems[i] is IGameSystem))
                {
                    Debug.LogError($"{nameof(GameBootstrap)}: {_systems[i]} does not implement IGameSystem.", this);
                }
            }
        }
    }
}

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

        /// <summary>
        /// Editor SessionState key that is true while the editor runs tests (set by the test-run hook in
        /// MoonProject.Editor.Automation): fixtures drive 07 from the first frame, so every boot skips the title then.
        /// </summary>
        public const string TestRunSkipsTitleKey = "MoonProject.TestRunSkipsTitle";

        /// <summary>
        /// Editor SessionState key that is true while the editor runs tests (set by the same hook): every boot then
        /// runs on its own copy of the Controls asset. The project asset is one object shared by every test in a run,
        /// and its action state stays resolved against an earlier test's input system, so a later boot's actions bind
        /// no controls (a held W at the lift's deck read Drive (0, 0)).
        /// </summary>
        public const string TestRunCopiesControlsKey = "MoonProject.TestRunCopiesControls";

        [Tooltip("Boot into the title over the loaded world (M3-09); the Main scene builder turns it on. Off boots " +
                 "straight into play on the save. Test runs in the editor always boot straight into play.")]
        [SerializeField] private bool _showTitle;

        [Tooltip("New Game over a save: the longest (s) the reload waits for the picture to fade to black "
                 + "(WakeBeat.FadedOut) before it reloads anyway.")]
        [Min(0.5f)]
        [SerializeField] private float _fadedOutTimeout = 6f;

        private readonly GameFlow _flow = new GameFlow();
        private InputReader _input;
        private SaveService _save;
        private BootIntent _intent;
        private IDisposable _newGame;
        private IDisposable _continue;
        private IDisposable _quitToTitle;
        private IDisposable _wakeBeat;
        private readonly FadedOutGate _fadedOut = new FadedOutGate();
        private AsyncOperation _pendingReload;
        private bool _reloading;
#if UNITY_EDITOR
        private InputActionAsset _controlsCopy;
#endif

        public GameContext Context { get; private set; }

        /// <summary>True while an async scene reload has been pre-warmed and is waiting for O1's black to activate.</summary>
        public bool IsReloadPrewarmed => _pendingReload != null && !_pendingReload.allowSceneActivation;

        private void Awake()
        {
            if (_inputActions == null)
            {
                Debug.LogError($"{nameof(GameBootstrap)}: Input Actions asset is not assigned.", this);
                enabled = false;
                return;
            }

            UseTestRunControlsCopy();
            bool overridden = SaveSlotArgument.TryRead(Environment.GetCommandLineArgs(), out string requestedSlot);
            string slot = overridden ? requestedSlot : _saveSlot;
            string slotSource = overridden ? SaveSlotArgument.Flag + " argument" : "scene";
            if (!SaveService.IsValidSlot(slot))
            {
                Debug.LogError($"{nameof(GameBootstrap)}: save slot '{slot}' from the {slotSource} is invalid.", this);
                enabled = false;
                return;
            }

            LegacySaveMigration.Run();
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
            _save.Saved += PublishSaved;
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
            bool showTitle = _showTitle && !TestRunSkipsTitle();
            if (_showTitle && !showTitle)
            {
                Debug.Log($"{nameof(GameBootstrap)}: a test run boots straight into play; the title is skipped.", this);
            }

            _flow.Loaded(hasSave, _intent.TryConsume(), showTitle);
            if (_flow.State == GameFlowState.Playing)
            {
                _input.Enable();
            }

            _newGame = Context.Events.Subscribe<NewGameRequested>(OnNewGameRequested);
            _continue = Context.Events.Subscribe<ContinueRequested>(OnContinueRequested);
            _quitToTitle = Context.Events.Subscribe<QuitToTitleRequested>(OnQuitToTitleRequested);
            _wakeBeat = Context.Events.Subscribe<RoverWakeBeatReached>(OnWakeBeat);
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
            if (_save != null)
            {
                _save.Saved -= PublishSaved;
            }

            _newGame?.Dispose();
            _wakeBeat?.Dispose();
            _continue?.Dispose();
            _quitToTitle?.Dispose();
            _input?.Dispose();
#if UNITY_EDITOR
            if (_controlsCopy != null)
            {
                Destroy(_controlsCopy);
            }
#endif
        }

        private void PublishSaved()
        {
            Context.Events.Publish(new GameSaved());
        }

        private void UseTestRunControlsCopy()
        {
#if UNITY_EDITOR
            if (UnityEditor.SessionState.GetBool(TestRunCopiesControlsKey, false))
            {
                _controlsCopy = Instantiate(_inputActions);
                _inputActions = _controlsCopy;
            }
#endif
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
        /// boot starts fresh, pre-warms an async reload while the picture fades to black, and activates the
        /// reload once <see cref="WakeBeat.FadedOut"/> arrives inside O1's black: every system, the context and this
        /// bootstrap are rebuilt without a visible hitch.
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
            _fadedOut.Arm();
            _pendingReload = BeginAsyncReload(allowActivation: false);
        }

        /// <summary>The camera is inside 07's closed lens: the new game's pre-warmed reload activates in O1's black.</summary>
        private void OnWakeBeat(RoverWakeBeatReached reached)
        {
            if (_fadedOut.Notify(reached.Beat))
            {
                ReloadForNewGame();
            }
        }

        private void Update()
        {
            if (!_fadedOut.Step(Time.unscaledDeltaTime, _fadedOutTimeout))
            {
                return;
            }

            Debug.LogError($"{nameof(GameBootstrap)}: New Game waited {_fadedOutTimeout} s for the camera to push " +
                           "into 07's lens (WakeBeat.FadedOut) and reloads without it.", this);
            ReloadForNewGame();
        }

        private void ReloadForNewGame()
        {
            if (_pendingReload != null)
            {
                _pendingReload.allowSceneActivation = true;
                _pendingReload = null;
                return;
            }

            BeginAsyncReload(allowActivation: true);
        }

        /// <summary>Saves and reloads this scene asynchronously, which boots into the title over the same save.</summary>
        private void OnQuitToTitleRequested(QuitToTitleRequested request)
        {
            if (_reloading || !_flow.CanQuitToTitle || !CanReload("quitting to the title"))
            {
                return;
            }

            _reloading = true;
            _save.SaveNow();
            BeginAsyncReload(allowActivation: true);
        }

        private AsyncOperation BeginAsyncReload(bool allowActivation)
        {
            Scene scene = gameObject.scene;
            AsyncOperation operation = scene.buildIndex >= 0
                ? SceneManager.LoadSceneAsync(scene.buildIndex, LoadSceneMode.Single)
#if UNITY_EDITOR
                : UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                    scene.path, new LoadSceneParameters(LoadSceneMode.Single));
#else
                : null;
#endif
            if (operation != null)
            {
                operation.allowSceneActivation = allowActivation;
            }

            return operation;
        }

        private static bool TestRunSkipsTitle()
        {
#if UNITY_EDITOR
            return UnityEditor.SessionState.GetBool(TestRunSkipsTitleKey, false);
#else
            return false;
#endif
        }

        private bool CanReload(string reason)
        {
            Scene scene = gameObject.scene;
            if (scene.buildIndex >= 0)
            {
                return true;
            }
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(scene.path))
            {
                return true;
            }
#endif
            Debug.LogError($"{nameof(GameBootstrap)}: scene '{scene.path}' is not in the build settings, " +
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

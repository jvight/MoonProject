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
    /// the background. On <see cref="NewGameRequested"/> it puts the save away and reloads its scene, so a new
    /// bootstrap builds the game from scratch.
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

        private InputReader _input;
        private SaveService _save;
        private IDisposable _newGame;
        private bool _startingOver;

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
            Debug.Log($"{nameof(GameBootstrap)}: save slot '{slot}' (from the {slotSource}): {_save.FilePath}", this);
            _input.Enable();

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
            _newGame = Context.Events.Subscribe<NewGameRequested>(OnNewGameRequested);
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
            _input?.Dispose();
        }

        /// <summary>
        /// Puts the save away (it refuses every later save, so nothing torn down writes the old progress back) and
        /// reloads this scene on its own: every system, the context and this bootstrap are rebuilt from nothing.
        /// </summary>
        private void OnNewGameRequested(NewGameRequested request)
        {
            if (_startingOver)
            {
                return;
            }

            int buildIndex = gameObject.scene.buildIndex;
            if (buildIndex < 0)
            {
                Debug.LogError($"{nameof(GameBootstrap)}: scene '{gameObject.scene.path}' is not in the build " +
                               "settings, so a new game cannot reload it; the save was left as it is.", this);
                return;
            }

            _startingOver = true;
            _save.PutAway();
            SceneManager.LoadScene(buildIndex, LoadSceneMode.Single);
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

using UnityEngine;
using UnityEngine.InputSystem;
using MoonProject.Core;
using MoonProject.Core.Input;
using MoonProject.Core.Save;

namespace MoonProject.App
{
    /// <summary>
    /// The single composition root. Builds the <see cref="GameContext"/> (with the <see cref="ISaveService"/>),
    /// initialises every scene system in the explicit order serialised below (filled by the scene builder), then loads
    /// the save so every registered section is restored before any Start() runs. Saves when the game quits or goes to
    /// the background.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameBootstrap : MonoBehaviour
    {
        [Tooltip("The Controls input asset (Assets/_Project/Data/Input/Controls.inputactions).")]
        [SerializeField] private InputActionAsset _inputActions;

        [Tooltip("Scene systems implementing IGameSystem, initialised top to bottom.")]
        [SerializeField] private MonoBehaviour[] _systems = new MonoBehaviour[0];

        [Tooltip("Save file name (letters, digits, '-', '_') under Application.persistentDataPath/Saves.")]
        [SerializeField] private string _saveSlot = "main";

        private InputReader _input;
        private SaveService _save;

        public GameContext Context { get; private set; }

        private void Awake()
        {
            if (_inputActions == null)
            {
                Debug.LogError($"{nameof(GameBootstrap)}: Input Actions asset is not assigned.", this);
                enabled = false;
                return;
            }

            if (!SaveService.IsValidSlot(_saveSlot))
            {
                Debug.LogError($"{nameof(GameBootstrap)}: save slot '{_saveSlot}' is invalid.", this);
                enabled = false;
                return;
            }

            _input = new InputReader(_inputActions);
            Context = new GameContext(new EventBus(), _input);
            _save = new SaveService(SaveService.DefaultDirectory, _saveSlot);
            Context.Register<ISaveService>(_save);
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
            _input?.Dispose();
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

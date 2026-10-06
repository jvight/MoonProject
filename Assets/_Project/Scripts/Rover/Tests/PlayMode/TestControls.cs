using UnityEngine;
using UnityEngine.InputSystem;
using MoonProject.Core.Input;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>A code-built stand-in for the Controls asset: the "Rover" map bound to a gamepad.</summary>
    public static class TestControls
    {
        public static InputActionAsset Create()
        {
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            InputActionMap map = asset.AddActionMap(InputReader.RoverMapName);
            map.AddAction("Drive", InputActionType.Value, "<Gamepad>/leftStick", expectedControlLayout: "Vector2");
            map.AddAction("LookDelta", InputActionType.PassThrough, "<Mouse>/delta", expectedControlLayout: "Vector2");
            map.AddAction("LookRate", InputActionType.Value, "<Gamepad>/rightStick", expectedControlLayout: "Vector2");
            map.AddAction("Ping", InputActionType.Button, "<Gamepad>/buttonSouth");
            map.AddAction("Excavate", InputActionType.Button, "<Gamepad>/buttonWest");
            map.AddAction("Tether", InputActionType.Button, "<Gamepad>/leftTrigger");
            map.AddAction("Winch", InputActionType.Value, "<Gamepad>/rightTrigger", expectedControlLayout: "Axis");
            map.AddAction("Pause", InputActionType.Button, "<Gamepad>/start");
            return asset;
        }
    }
}

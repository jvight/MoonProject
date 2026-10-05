using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

/// <summary>
/// Drives a Cinemachine 3.x OrbitalFollow rig with raw mouse delta (New Input System),
/// giving the old FreeLook-style "look around the rover" feel without needing
/// InputActionReferences wired up. Put this on the CinemachineCamera GameObject.
/// </summary>
[RequireComponent(typeof(CinemachineOrbitalFollow))]
public class CinemachineInputHandler : MonoBehaviour
{
    [Tooltip("Horizontal (orbit) look sensitivity, in degrees per mouse-delta unit.")]
    [SerializeField] private float horizontalSensitivity = 0.05f;
    [Tooltip("Vertical (pitch) look sensitivity, in degrees per mouse-delta unit.")]
    [SerializeField] private float verticalSensitivity = 0.04f;
    [Tooltip("Invert the vertical look axis.")]
    [SerializeField] private bool invertY = false;

    private CinemachineOrbitalFollow orbital;

    void Awake()
    {
        orbital = GetComponent<CinemachineOrbitalFollow>();
    }

    void Update()
    {
        if (orbital == null || Mouse.current == null) return;

        Vector2 delta = Mouse.current.delta.ReadValue();

        orbital.HorizontalAxis.Value += delta.x * horizontalSensitivity;
        orbital.VerticalAxis.Value += delta.y * verticalSensitivity * (invertY ? 1f : -1f);
    }
}

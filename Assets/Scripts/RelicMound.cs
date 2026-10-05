using UnityEngine;
using System.Collections;

/// <summary>
/// A buried relic. Lifecycle: Buried -> Surfacing -> Loose -> Collected.
/// While Buried/Surfacing the Rigidbody is kinematic so the scripted rise
/// animation never fights physics (and the GravTether can't grab it yet).
/// Once it finishes surfacing it becomes a free physics body the tether can reel in.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class RelicMound : MonoBehaviour
{
    public enum RelicState { Buried, Surfacing, Loose, Collected }

    [Header("Extraction Settings")]
    public float extractionHeight = 3f;
    public float extractionDuration = 4f;
    public float rotationSpeed = 90f;

    [Header("VFX")]
    [Tooltip("Particle system to hide the ground intersection during extraction")]
    public ParticleSystem dustSwirlVFX;

    public RelicState State { get; private set; } = RelicState.Buried;

    /// <summary>True once the relic is no longer a buried sonar target (surfacing, loose, or collected).</summary>
    public bool IsExtracted => State != RelicState.Buried;

    /// <summary>True only when the relic has fully surfaced and can be tethered/collected.</summary>
    public bool IsLoose => State == RelicState.Loose;

    private Rigidbody body;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        // Sit still as a buried mound — no gravity, no tether forces.
        SetPhysicsActive(false);
    }

    private void Start()
    {
        // Force stop on awake in case the prefab had Play On Awake enabled
        if (dustSwirlVFX != null)
        {
            dustSwirlVFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    /// <summary>Begin surfacing. Called by the RoverExcavator's tractor beam. No-op unless still buried.</summary>
    public void StartExtraction()
    {
        if (State != RelicState.Buried) return;

        State = RelicState.Surfacing;

        if (dustSwirlVFX != null)
        {
            dustSwirlVFX.Play();
        }

        StartCoroutine(SurfacingRoutine());
    }

    private IEnumerator SurfacingRoutine()
    {
        Vector3 startPos = transform.localPosition;
        Vector3 endPos = startPos + Vector3.up * extractionHeight;
        float elapsed = 0f;

        while (elapsed < extractionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / extractionDuration;

            // Smooth step for a nicer, floaty ease-in/ease-out
            t = t * t * (3f - 2f * t);

            transform.localPosition = Vector3.Lerp(startPos, endPos, t);
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

            yield return null;
        }

        transform.localPosition = endPos;

        // Hand the relic over to physics so the GravTether can reel it in.
        State = RelicState.Loose;
        SetPhysicsActive(true);

        if (dustSwirlVFX != null)
        {
            dustSwirlVFX.Stop();
        }

        Debug.Log($"[RelicMound] Relic surfaced and ready to retrieve: {gameObject.name}");
    }

    /// <summary>Mark the relic as picked up by the rover. Disables it so the world is cleaned up.</summary>
    public void Collect()
    {
        if (State == RelicState.Collected) return;

        State = RelicState.Collected;
        SetPhysicsActive(false);
        gameObject.SetActive(false);

        Debug.Log($"[RelicMound] Relic collected: {gameObject.name}");
    }

    private void SetPhysicsActive(bool active)
    {
        if (body == null) return;
        body.isKinematic = !active;
        body.useGravity = active;
    }
}

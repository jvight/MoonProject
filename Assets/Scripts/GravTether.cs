using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(LineRenderer))]
public class GravTether : MonoBehaviour
{
    [Header("Tether Origin Settings")]
    [Tooltip("The radar dish or arm on top of the rover that shoots the beam.")]
    public Transform radarDish;
    [Tooltip("The actual point where the beam originates (usually a child of radarDish).")]
    public Transform tetherPoint;
    public float radarDishTurnSpeed = 10f;
    
    [Header("Aiming Settings")]
    public float maxAimDistance = 20f;
    public float maxTetherDistance = 25f;
    [Tooltip("Layers that can be tethered. Ensure relics have Rigidbodies.")]
    public LayerMask tetherableLayer = ~0;
    public Color highlightColor = new Color(0f, 1f, 1f, 1f); // Cyan highlight

    [Header("Visual Settings")]
    public int lineResolution = 20;
    [Tooltip("How much the magnetic arc bends upwards or downwards")]
    public float curveHeight = 1.5f;
    [Tooltip("Intensity of the energetic wiggle")]
    public float noiseAmount = 0.2f;
    public float noiseSpeed = 5f;

    [Header("Physics Settings")]
    [Tooltip("Strength of the tether pull. Increase this if heavy objects can't be lifted.")]
    public float springForce = 200f;
    [Tooltip("Damping to prevent bouncing (Critical damping is ~30 for spring 200)")]
    public float springDamper = 30f;
    [Tooltip("Maximum force the tether can apply. Caps out to simulate struggling with heavy weights.")]
    public float maxTetherForce = 500f;
    [Tooltip("Multiplier for upward force relative to weight. 1.0 = barely holds it, >1.0 = lifts slowly.")]
    public float maxUpLiftMultiplier = 1.2f;
    [Tooltip("Maximum height the tether can lift an object above the rover.")]
    public float maxLiftHeight = 4.0f;
    public float winchScrollSpeed = 2f;
    public float minWinchLength = 2f;
    public float maxWinchLength = 15f;
    
    [Header("VFX Settings")]
    [Tooltip("Prefab for dust particle system when dragging objects on the ground")]
    public ParticleSystem dustVfxPrefab;
    [Tooltip("Layer mask for the ground to detect dragging")]
    public LayerMask groundLayer = 1; // Default to 1 (Default layer)

    private LineRenderer lineRenderer;
    private Rigidbody tetheredBody;
    private Rigidbody roverRigidbody;
    private Camera mainCamera;
    
    private float currentWinchDistance;
    private Vector3 currentTargetPosition;
    private Vector3 smoothedTargetPosition;
    private Vector3 targetVelocity;

    private bool isTethering = false;
    
    // Highlighting state
    private Renderer currentlyHighlightedRenderer;
    private MaterialPropertyBlock propBlock;
    
    // VFX state
    private ParticleSystem currentDustVfx;

    private void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.enabled = false;
        lineRenderer.positionCount = lineResolution;
        lineRenderer.useWorldSpace = true;
        
        roverRigidbody = GetComponent<Rigidbody>();
        mainCamera = Camera.main;
        propBlock = new MaterialPropertyBlock();

        // Auto-create a tether point if none assigned
        if (tetherPoint == null)
        {
            GameObject tp = new GameObject("TetherPoint_Arm");
            if (radarDish != null) tp.transform.SetParent(radarDish);
            else tp.transform.SetParent(transform);
            tp.transform.localPosition = new Vector3(0, 0, 0.5f);
            tetherPoint = tp.transform;
        }
    }

    private void Update()
    {
        if (Keyboard.current == null || Mouse.current == null || mainCamera == null) return;

        bool holdingRightClick = Mouse.current.rightButton.isPressed;
        
        // Aiming Logic
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 lookTarget = ray.GetPoint(maxAimDistance);
        
        if (holdingRightClick)
        {
            if (!isTethering)
            {
                TryStartTether(ray, ref lookTarget);
            }
            else
            {
                HandleWinchInput();
                // Target position follows the camera's gaze at the specified winch distance
                currentTargetPosition = ray.GetPoint(currentWinchDistance);
                
                // Prevent lifting too high into the sky
                float maxHeight = transform.position.y + maxLiftHeight;
                if (currentTargetPosition.y > maxHeight)
                {
                    currentTargetPosition.y = maxHeight;
                }

                UpdateTetherLogic();
                lookTarget = tetheredBody.position;
            }
        }
        else
        {
            if (isTethering)
            {
                BreakTether();
            }
            else
            {
                // Highlight logic when just looking around (not holding right click)
                HandleHighlighting(ray, ref lookTarget);
            }
            
            if (lineRenderer.enabled)
            {
                lineRenderer.enabled = false;
            }
        }

        // Radar Dish visual rotation
        UpdateRadarDishRotation(lookTarget);
        
        // Update VFX
        UpdateDragVFX();
    }

    private void FixedUpdate()
    {
        if (isTethering && tetheredBody != null)
        {
            // Smooth the target position to eliminate jitter from camera movement
            smoothedTargetPosition = Vector3.SmoothDamp(smoothedTargetPosition, currentTargetPosition, ref targetVelocity, 0.1f);

            // Custom spring physics (PD Controller) to pull the relic
            Vector3 error = smoothedTargetPosition - tetheredBody.position;
            Vector3 errorDerivative = targetVelocity - tetheredBody.linearVelocity;
            
            Vector3 force = error * springForce + errorDerivative * springDamper;
            
            // Restrict upward force to simulate heavy weight and prevent flying
            float weight = tetheredBody.mass * Mathf.Abs(Physics.gravity.y);
            float maxUpForce = weight * maxUpLiftMultiplier;
            if (force.y > maxUpForce)
            {
                force.y = maxUpForce;
            }

            // Limit the maximum force to simulate tether strain on heavy objects
            if (force.magnitude > maxTetherForce)
            {
                force = force.normalized * maxTetherForce;
            }
            
            // Use ForceMode.Force so MASS matters!
            tetheredBody.AddForce(force, ForceMode.Force);
        }
    }

    private void TryStartTether(Ray ray, ref Vector3 lookTarget)
    {
        // Use pure Raycast for 100% precision instead of SphereCast
        // This means the crosshair MUST exactly touch the relic's collider
        if (Physics.Raycast(ray, out RaycastHit hit, maxAimDistance, tetherableLayer))
        {
            Rigidbody targetRb = hit.collider.attachedRigidbody;
            // Skip kinematic bodies (e.g. relics still buried/surfacing) — only loose physics objects can be reeled in.
            if (targetRb != null && targetRb != roverRigidbody && !targetRb.isKinematic)
            {
                // Start tethering
                isTethering = true;
                tetheredBody = targetRb;
                
                // Force interpolation to fix physics jitter when dragging
                tetheredBody.interpolation = RigidbodyInterpolation.Interpolate;

                // Initial length based on current distance to the object
                currentWinchDistance = Vector3.Distance(mainCamera.transform.position, tetheredBody.position);
                currentWinchDistance = Mathf.Clamp(currentWinchDistance, minWinchLength, maxWinchLength);
                
                currentTargetPosition = tetheredBody.position;
                smoothedTargetPosition = tetheredBody.position;

                lineRenderer.enabled = true;
                lookTarget = tetheredBody.position;
                
                // Keep it highlighted while tethered
                ApplyHighlight(tetheredBody.GetComponentInChildren<Renderer>());
                
                return; // target found
            }
        }

        // Just looking around, show aiming laser
        if (Physics.Raycast(ray, out RaycastHit laserHit, maxAimDistance))
        {
            lookTarget = laserHit.point;
        }
        UpdateAimingLaser(lookTarget);
        RemoveHighlight();
    }

    private void HandleHighlighting(Ray ray, ref Vector3 lookTarget)
    {
        // Use pure Raycast for precise highlighting
        if (Physics.Raycast(ray, out RaycastHit hit, maxAimDistance, tetherableLayer))
        {
            Rigidbody targetRb = hit.collider.attachedRigidbody;
            // Only highlight loose (non-kinematic) targets the tether can actually grab.
            if (targetRb != null && targetRb != roverRigidbody && !targetRb.isKinematic)
            {
                ApplyHighlight(targetRb.GetComponentInChildren<Renderer>());
                lookTarget = targetRb.position;
                return;
            }
        }
        
        RemoveHighlight();
    }

    private void ApplyHighlight(Renderer renderer)
    {
        if (renderer == null) return;
        if (currentlyHighlightedRenderer == renderer) return; // Already highlighted
        
        RemoveHighlight(); // Remove previous if any
        
        currentlyHighlightedRenderer = renderer;
        renderer.GetPropertyBlock(propBlock);
        
        // Use MaterialPropertyBlock to add emission safely without instantiating materials
        propBlock.SetColor("_EmissionColor", highlightColor * 2f); 
        propBlock.SetColor("_Color", highlightColor); // Fallback for standard/URP BaseColor
        propBlock.SetColor("_BaseColor", highlightColor); 
        
        renderer.SetPropertyBlock(propBlock);
    }

    private void RemoveHighlight()
    {
        if (currentlyHighlightedRenderer != null)
        {
            // Remove the property block override, returning the material to its original state
            currentlyHighlightedRenderer.SetPropertyBlock(null); 
            currentlyHighlightedRenderer = null;
        }
    }

    private void HandleWinchInput()
    {
        float scrollAmount = Mouse.current.scroll.ReadValue().y;
        if (scrollAmount != 0)
        {
            float scrollDir = Mathf.Sign(scrollAmount);
            // Scrolling up (positive) = Reel IN (reduce distance)
            // Scrolling down (negative) = Reel OUT (increase distance)
            currentWinchDistance -= scrollDir * winchScrollSpeed;
            currentWinchDistance = Mathf.Clamp(currentWinchDistance, minWinchLength, maxWinchLength);
        }
    }

    private void DrawCurvedTether(Vector3 start, Vector3 end)
    {
        if (lineRenderer.positionCount != lineResolution) 
            lineRenderer.positionCount = lineResolution;

        float distance = Vector3.Distance(start, end);
        
        // Midpoint
        Vector3 mid = Vector3.Lerp(start, end, 0.5f);
        
        // Control point: offset upwards to create a sagging or arcing magnetic line
        float time = Time.time * noiseSpeed;
        Vector3 noiseOffset = new Vector3(Mathf.Sin(time), Mathf.Cos(time * 1.3f), Mathf.Sin(time * 0.7f)) * noiseAmount;
        Vector3 controlPoint = mid + Vector3.up * (curveHeight * distance * 0.1f) + noiseOffset;
        
        for (int i = 0; i < lineResolution; i++)
        {
            float t = i / (float)(lineResolution - 1);
            Vector3 pos = CalculateBezierPoint(t, start, controlPoint, end);
            lineRenderer.SetPosition(i, pos);
        }
    }

    private Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        Vector3 p = uu * p0; // (1-t)^2 * P0
        p += 2 * u * t * p1; // 2(1-t)t * P1
        p += tt * p2;        // t^2 * P2
        return p;
    }

    private void UpdateAimingLaser(Vector3 targetPoint)
    {
        lineRenderer.enabled = true;
        Vector3 start = tetherPoint != null ? tetherPoint.position : transform.position;
        DrawCurvedTether(start, targetPoint);
    }

    private void UpdateTetherLogic()
    {
        if (tetheredBody == null)
        {
            BreakTether();
            return;
        }

        // Distance check (Anti-Frustration snapping)
        float distance = Vector3.Distance(transform.position, tetheredBody.position);
        if (distance > maxTetherDistance)
        {
            BreakTether();
            return;
        }

        // Update visuals
        Vector3 start = tetherPoint != null ? tetherPoint.position : transform.position;
        DrawCurvedTether(start, tetheredBody.position);
    }

    private void UpdateRadarDishRotation(Vector3 targetPoint)
    {
        if (radarDish == null) return;

        Vector3 direction = targetPoint - radarDish.position;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction, transform.up); // Ensure it respects up vector
            radarDish.rotation = Quaternion.Slerp(radarDish.rotation, targetRotation, Time.deltaTime * radarDishTurnSpeed);
        }
    }

    private void UpdateDragVFX()
    {
        if (dustVfxPrefab == null) return;

        if (isTethering && tetheredBody != null)
        {
            // Check if moving fast enough
            if (tetheredBody.linearVelocity.magnitude > 0.5f)
            {
                // Check if touching ground (raycast down from relic center)
                if (Physics.Raycast(tetheredBody.position, Vector3.down, out RaycastHit hit, 2f, groundLayer))
                {
                    if (currentDustVfx == null)
                    {
                        // Spawn VFX at the contact point
                        currentDustVfx = Instantiate(dustVfxPrefab, hit.point, Quaternion.identity);
                    }
                    else
                    {
                        // Move existing VFX to current ground point
                        currentDustVfx.transform.position = hit.point;
                        if (!currentDustVfx.isPlaying) currentDustVfx.Play();
                    }
                    return; // VFX is active, don't stop it
                }
            }
        }

        // If we reach here, we shouldn't show VFX
        if (currentDustVfx != null && currentDustVfx.isPlaying)
        {
            currentDustVfx.Stop();
            // Let it fade out naturally, destroy after a few seconds
            Destroy(currentDustVfx.gameObject, 3f); 
            currentDustVfx = null;
        }
    }

    private void BreakTether()
    {
        isTethering = false;
        tetheredBody = null;
        RemoveHighlight();

        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
    }

    private void OnGUI()
    {
        if (mainCamera == null) return;
        
        // Crosshair size expands slightly when a target is found
        float size = currentlyHighlightedRenderer != null ? 12f : 6f;
        float posX = mainCamera.pixelWidth / 2f - size / 2f;
        float posY = mainCamera.pixelHeight / 2f - size / 2f;

        // Color changes to highlightColor (Cyan) if target found, otherwise semi-transparent white
        GUI.color = currentlyHighlightedRenderer != null ? highlightColor : new Color(1f, 1f, 1f, 0.5f);
        
        // Draw a simple square crosshair
        GUI.DrawTexture(new Rect(posX, posY, size, size), Texture2D.whiteTexture);
    }
}

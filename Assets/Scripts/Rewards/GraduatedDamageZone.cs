using UnityEngine;

/// <summary>
/// A zone that applies distance-proportional health damage to the agent.
/// Placed between the safe island and the lethal DeathZone.
/// Damage increases as the agent moves further from the island center.
/// 
/// Health drain rate: lerp(minDamagePerTick, maxDamagePerTick, normalized_distance)
/// At the inner edge (closest to island): minDamagePerTick per FixedUpdate
/// At the outer edge (closest to DeathZone): maxDamagePerTick per FixedUpdate
/// 
/// With default values (-0.003 to -0.015), an agent at the outer edge
/// loses ~1.5 health per tick, meaning it takes ~33 ticks to drop from 
/// 100 to 50 health (the proactive mother health trigger threshold).
/// This gives the Python-side intervention system time to fire predictively.
/// </summary>
public class GraduatedDamageZone : MonoBehaviour
{
    [Header("Damage Settings")]
    [Tooltip("Explicit opt-in. Standard nursery runs should leave this disabled.")]
    public bool damageEnabled = false;

    [Tooltip("Center of the safe island in world coordinates")]
    public Vector3 islandCenter = new Vector3(20f, 0f, 20f);
    
    [Tooltip("Radius of the safe island (no damage inside this)")]
    public float safeRadius = 5f;
    
    [Tooltip("Outer radius where damage is maximum (DeathZone starts beyond this)")]
    public float outerRadius = 8f;
    
    [Tooltip("Health update per FixedUpdate at inner edge (closest to island). Negative = damage.")]
    public float minDamagePerTick = -0.003f;
    
    [Tooltip("Health update per FixedUpdate at outer edge (closest to DeathZone). Negative = damage.")]
    public float maxDamagePerTick = -0.015f;

    private TrainingAgent _cachedAgent;

    private void OnTriggerStay(Collider other)
    {
        if (!damageEnabled) return;
        if (!other.CompareTag("agent")) return;
        
        if (_cachedAgent == null)
            _cachedAgent = other.GetComponent<TrainingAgent>();
        
        if (_cachedAgent == null) return;

        // Compute distance from island center (XZ plane only)
        Vector3 agentPos = other.transform.position;
        float distFromCenter = new Vector2(
            agentPos.x - islandCenter.x,
            agentPos.z - islandCenter.z
        ).magnitude;

        // Only apply damage outside the safe radius
        if (distFromCenter <= safeRadius) return;

        // Normalize distance: 0 at safeRadius, 1 at outerRadius
        float t = Mathf.Clamp01(
            (distFromCenter - safeRadius) / (outerRadius - safeRadius)
        );

        // Lerp between min and max damage
        float damage = Mathf.Lerp(minDamagePerTick, maxDamagePerTick, t);
        
        _cachedAgent.UpdateHealth(damage);
    }
}

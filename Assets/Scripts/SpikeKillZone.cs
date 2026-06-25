using UnityEngine;

/// <summary>Instant-death trigger used on Level 4 spike zones.</summary>
public class SpikeKillZone : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D col)
    {
        if (GameStateManager.Instance == null) return;
        var car = CarController.Instance;
        if (car == null) return;

        // Fire if any car body or tire enters the zone
        var rb = col.attachedRigidbody;
        if (rb != null && (rb == car.carRigidbody || rb == car.backTire || rb == car.frontTire))
            GameStateManager.Instance.TriggerDeath();
    }
}

using UnityEngine;

// Attach to any GameObject with a Collider2D (set IsTrigger = true).
// Restores +1 nitro charge to the car on contact.
public class NitroPickup : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D collision)
    {
        CarController car = CarController.Instance;
        if (car == null) car = FindAnyObjectByType<CarController>();
        if (car == null) return;

        car.AddNitroCharge();
        // Reuse the fuel pickup sound — distinct enough
        AudioManager.Instance?.PlayFuelPickup();
        Destroy(gameObject);
    }
}

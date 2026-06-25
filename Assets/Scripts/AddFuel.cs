using UnityEngine;

public class AddFuel : MonoBehaviour
{
    [Tooltip("How much fuel to restore (0-1). Clamped to max 1.")]
    public float fuelAmount = 0.4f;

    private CarController carController;

    void Start()
    {
        carController = FindAnyObjectByType<CarController>();
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (carController == null) carController = FindAnyObjectByType<CarController>();
        if (carController == null) return;

        carController.fuel = Mathf.Min(carController.fuel + fuelAmount, 1f);
        carController.CancelFuelOutTimer();

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayFuelPickup();

        Destroy(gameObject);
    }
}

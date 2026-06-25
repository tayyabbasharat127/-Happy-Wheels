using UnityEngine;

public class HeadScript : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Ground") && GameStateManager.Instance != null)
            GameStateManager.Instance.TriggerDeath();
    }
}

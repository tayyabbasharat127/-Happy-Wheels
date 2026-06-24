using UnityEngine;

public class HeadScript : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Crash sound + engine stop are handled centrally in GameStateManager
        if (collision.CompareTag("Ground") && GameStateManager.Instance != null)
            GameStateManager.Instance.TriggerGameOver();
    }
}

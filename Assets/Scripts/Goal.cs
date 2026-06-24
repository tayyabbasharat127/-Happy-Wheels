using UnityEngine;

public class Goal : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Win sound + engine stop are handled centrally in GameStateManager
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.CompleteCurrentLevel();
    }
}

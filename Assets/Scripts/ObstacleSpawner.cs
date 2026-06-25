using UnityEngine;

// Single obstacle spawner — replaces ObjectSpawner.cs and SpawnerController.cs.
// Attach to an empty GameObject. Set spawnPrefab and spawnCount in the Inspector.
public class ObstacleSpawner : MonoBehaviour
{
    public GameObject spawnPrefab;
    [Tooltip("Half-width of spawn area around this object's X position")]
    public float spawnArea  = 10f;
    public int   spawnCount = 5;

    void Start()
    {
        if (spawnPrefab == null) return;
        for (int i = 0; i < spawnCount; i++)
        {
            float posX = transform.position.x + Random.Range(-spawnArea, spawnArea);
            var   obj  = Instantiate(spawnPrefab);
            obj.transform.position = new Vector3(posX, transform.position.y, 0f);
        }
    }
}

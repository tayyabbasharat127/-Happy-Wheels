using UnityEngine;

public class Follow : MonoBehaviour
{
    public Transform target;
    private Vector3 offset;

    void Start()
    {
        if (target != null) offset = transform.position - target.position;
    }

    // LateUpdate runs after physics interpolation resolves for the frame —
    // following the car here removes the camera jitter you get from Update().
    void LateUpdate()
    {
        if (target != null) transform.position = target.position + offset;
    }
}

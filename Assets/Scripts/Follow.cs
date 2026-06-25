using UnityEngine;

public class Follow : MonoBehaviour
{
    public Transform target;

    [Tooltip("Base orthographic size at rest")]
    public float baseSize   = 5f;
    [Tooltip("Max extra zoom-out at full speed")]
    public float zoomRange  = 2f;
    [Tooltip("Car speed (units/s) that maps to max zoom")]
    public float maxSpeed   = 20f;
    [Tooltip("How smoothly the zoom changes")]
    public float zoomSmooth = 3f;

    private Vector3      offset;
    private Camera       cam;
    private Rigidbody2D  carBody;

    void Start()
    {
        if (target != null) offset = transform.position - target.position;
        cam     = GetComponent<Camera>();
        carBody = CarController.Instance != null ? CarController.Instance.carRigidbody : null;
        if (cam != null) baseSize = cam.orthographicSize;
    }

    void LateUpdate()
    {
        if (target != null)
            transform.position = target.position + offset;

        if (cam == null) return;

        // Dynamic zoom: faster car = wider view
        float speed     = carBody != null ? carBody.linearVelocity.magnitude : 0f;
        float zoomT     = Mathf.Clamp01(speed / Mathf.Max(maxSpeed, 0.1f));
        float targetSize = baseSize + zoomRange * zoomT;
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize, Time.deltaTime * zoomSmooth);
    }
}

using UnityEngine;

public class Follow : MonoBehaviour
{
    public Transform target;

    [Tooltip("Base orthographic size at rest")]
    public float baseSize    = 5f;
    [Tooltip("Max extra zoom-out at full speed")]
    public float zoomRange   = 2f;
    [Tooltip("Car speed (units/s) that maps to max zoom")]
    public float maxSpeed    = 20f;
    [Tooltip("Zoom interpolation speed")]
    public float zoomSmooth  = 3f;
    [Tooltip("Horizontal camera follow speed (higher = snappier)")]
    public float hSmooth     = 6f;
    [Tooltip("Vertical camera follow speed (slightly slower smooths terrain bumps)")]
    public float vSmooth     = 4f;
    [Tooltip("How far ahead of the car the camera looks (canvas units)")]
    public float leadAhead   = 1.5f;

    private Vector3     offset;
    private Camera      cam;
    private Rigidbody2D carBody;

    void Start()
    {
        if (target != null) offset = transform.position - target.position;
        cam     = GetComponent<Camera>();
        carBody = CarController.Instance != null ? CarController.Instance.carRigidbody : null;
        if (cam != null) baseSize = cam.orthographicSize;
    }

    public void ResetTarget(Transform newTarget, bool snap)
    {
        target = newTarget;
        carBody = CarController.Instance != null ? CarController.Instance.carRigidbody : null;

        if (target == null) return;

        if (snap)
        {
            transform.position = target.position + offset;
            if (cam != null) cam.orthographicSize = baseSize;
        }
    }

    void LateUpdate()
    {
        if (target != null)
        {
            // Desired position: follow target with a small lead-ahead bias
            float lead   = carBody != null ? carBody.linearVelocity.x * leadAhead * 0.05f : 0f;
            Vector3 want = target.position + offset + new Vector3(lead, 0f, 0f);

            // Smooth horizontal and vertical independently so bumps don't jerk the camera
            Vector3 cur = transform.position;
            transform.position = new Vector3(
                Mathf.Lerp(cur.x, want.x, Time.deltaTime * hSmooth),
                Mathf.Lerp(cur.y, want.y, Time.deltaTime * vSmooth),
                want.z
            );
        }

        if (cam == null) return;

        // Dynamic zoom: car speed widens the view
        float speed      = carBody != null ? carBody.linearVelocity.magnitude : 0f;
        float zoomT      = Mathf.Clamp01(speed / Mathf.Max(maxSpeed, 0.1f));
        float targetSize = baseSize + zoomRange * zoomT;
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize, Time.deltaTime * zoomSmooth);
    }
}

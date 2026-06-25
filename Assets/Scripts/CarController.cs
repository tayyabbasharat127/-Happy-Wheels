using UnityEngine;

public class CarController : MonoBehaviour
{
    public static CarController Instance { get; private set; }

    public float fuel = 1f;
    public float fuelConsumption = 0.1f;
    public Rigidbody2D carRigidbody;
    public Rigidbody2D backTire;
    public Rigidbody2D frontTire;
    public float speed = 20f;
    public UnityEngine.UI.Image gasCanImage;
    public GameObject thrustingEffect;

    public int nitroCharges = 3;
    public const int MaxNitroCharges = 3;

    public float thrust = 0f;
    public float movement { get; private set; }
    public bool IsNitroActive { get; private set; }

    private bool throttleHeld;
    private bool brakeHeld;
    private float stuckTimer;
    private float fuelOutTimer;
    private bool fuelOutStarted;
    private float nitroDuration;
    private float nitroCooldown;
    private Vector2 backTireOffset;
    private Vector2 frontTireOffset;
    private Collider2D backTireCollider;
    private Collider2D frontTireCollider;
    private WheelJoint2D backWheelJoint;
    private WheelJoint2D frontWheelJoint;
    private readonly Collider2D[] groundHits = new Collider2D[8];

    private const float StuckThreshold = 1.2f;
    private const float StuckSpeed = 0.4f;
    private const float StuckAssistImpulse = 2.4f;
    private const float MotorSpeedMultiplier = 23f;
    private const float MotorTorque = 9000f;
    private const float ReverseTorqueScale = 0.55f;
    private const float AirControlScale = 0.35f;
    private const float ThrustAccel = 95f;
    private const float ThrustDecay = 120f;
    private const float ThrustMax = 95f;
    private const float BrakeForce = 85f;
    private const float MaxForwardSpeed = 17f;
    private const float MaxReverseSpeed = 7f;
    private const float MaxNitroSpeed = 24f;
    private const float MaxUpwardSpeed = 8.5f;
    private const float FuelOutGrace = 3f;
    private const float NitroDuration = 1.5f;
    private const float NitroCooldown = 2f;
    private const float NitroMultiplier = 1.75f;
    private const float WheelSnapDistance = 0.7f;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        carRigidbody.WakeUp();
        backTire.WakeUp();
        frontTire.WakeUp();

        backTireOffset = backTire.position - carRigidbody.position;
        frontTireOffset = frontTire.position - carRigidbody.position;
        backTireCollider = backTire.GetComponent<Collider2D>();
        frontTireCollider = frontTire.GetComponent<Collider2D>();
        backWheelJoint = backTire.GetComponent<WheelJoint2D>();
        frontWheelJoint = frontTire.GetComponent<WheelJoint2D>();

        DetachWheelTransform(backTire);
        DetachWheelTransform(frontTire);
        ConfigureWheelJoint(backWheelJoint, backTireOffset);
        ConfigureWheelJoint(frontWheelJoint, frontTireOffset);

        TuneBody(carRigidbody, 1.0f, 2.4f, 1.8f);
        TuneBody(backTire, 0.45f, 0.25f, 1.6f);
        TuneBody(frontTire, 0.45f, 0.25f, 1.6f);

        ClampTireScale(backTire, 1.4f);
        ClampTireScale(frontTire, 1.4f);
    }

    static void DetachWheelTransform(Rigidbody2D wheel)
    {
        if (wheel == null || wheel.transform.parent == null) return;

        wheel.transform.SetParent(null, true);
    }

    void ConfigureWheelJoint(WheelJoint2D joint, Vector2 connectedAnchor)
    {
        if (joint == null) return;

        joint.connectedBody = carRigidbody;
        joint.autoConfigureConnectedAnchor = false;
        joint.anchor = Vector2.zero;
        joint.connectedAnchor = connectedAnchor;
        joint.enableCollision = false;
        joint.useMotor = true;

        JointSuspension2D suspension = joint.suspension;
        suspension.frequency = 12f;
        suspension.dampingRatio = 0.95f;
        suspension.angle = 90f;
        joint.suspension = suspension;

        JointMotor2D motor = joint.motor;
        motor.maxMotorTorque = MotorTorque;
        motor.motorSpeed = 0f;
        joint.motor = motor;
    }

    static void TuneBody(Rigidbody2D body, float minLinearDamping, float minAngularDamping, float minGravityScale)
    {
        if (body == null) return;

        if (body.linearDamping < minLinearDamping)
            body.linearDamping = minLinearDamping;
        if (body.angularDamping < minAngularDamping)
            body.angularDamping = minAngularDamping;
        if (body.gravityScale < minGravityScale)
            body.gravityScale = minGravityScale;
    }

    static void ClampTireScale(Rigidbody2D tire, float maxAxis)
    {
        if (tire == null) return;

        Vector3 scale = tire.transform.localScale;
        float biggest = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        if (biggest > maxAxis * 1.1f)
            tire.transform.localScale = scale * (maxAxis / biggest);
    }

    void Update()
    {
        bool blocked = PlayerNameInput.IsOpen || IsStateBlocking();
        if (blocked)
        {
            throttleHeld = false;
            brakeHeld = false;
            movement = 0f;
            AudioManager.Instance?.SetEngineThrottle(0f);
            AudioManager.Instance?.SetLowFuelWarning(false);
            if (gasCanImage != null) gasCanImage.fillAmount = Mathf.Clamp01(fuel);
            return;
        }

        throttleHeld = Input.GetKey(KeyCode.RightArrow);
        brakeHeld = Input.GetKey(KeyCode.LeftArrow);
        movement = throttleHeld ? 1f : (brakeHeld ? -0.3f : 0f);

        if (Input.GetKeyDown(KeyCode.Space) && nitroCharges > 0 && nitroCooldown <= 0f && !IsNitroActive)
            ActivateNitro();

        if (nitroCooldown > 0f)
            nitroCooldown -= Time.deltaTime;

        if (gasCanImage != null)
            gasCanImage.fillAmount = Mathf.Clamp01(fuel);

        AudioManager.Instance?.SetLowFuelWarning(fuel < 0.2f && !fuelOutStarted && !IsStateBlocking());
        AudioManager.Instance?.SetEngineThrottle(throttleHeld ? 1f : 0f);

        if (fuelOutStarted)
        {
            fuelOutTimer -= Time.deltaTime;
            if (fuelOutTimer <= 0f)
            {
                fuelOutStarted = false;
                GameStateManager.Instance?.TriggerDeath();
            }
        }
    }

    void FixedUpdate()
    {
        bool blocked = PlayerNameInput.IsOpen || IsStateBlocking();

        if (!throttleHeld || blocked)
            thrust = Mathf.Max(thrust - ThrustDecay * Time.fixedDeltaTime, 0f);

        if (blocked)
        {
            if (thrustingEffect != null) thrustingEffect.SetActive(false);
            return;
        }

        bool grounded = IsGrounded();
        float controlScale = grounded ? 1f : AirControlScale;

        ApplyWheelMotor(controlScale);
        ApplyThrust(controlScale);
        ApplyBrake();
        LimitVelocity();
        MaintainWheelAttachment();
        TickNitro();
        ConsumeFuel();
        ApplyStuckAssist(grounded);
    }

    void ApplyWheelMotor(float controlScale)
    {
        float input = throttleHeld ? 1f : (brakeHeld ? -1f : 0f);
        float reverseScale = brakeHeld && !throttleHeld ? ReverseTorqueScale : 1f;
        float targetSpeed = input * Mathf.Max(speed, 20f) * MotorSpeedMultiplier * reverseScale * controlScale;

        SetWheelMotor(backWheelJoint, targetSpeed);
        SetWheelMotor(frontWheelJoint, targetSpeed);
    }

    static void SetWheelMotor(WheelJoint2D joint, float targetSpeed)
    {
        if (joint == null) return;

        JointMotor2D motor = joint.motor;
        motor.motorSpeed = targetSpeed;
        motor.maxMotorTorque = MotorTorque;
        joint.motor = motor;
        joint.useMotor = !Mathf.Approximately(targetSpeed, 0f);
    }

    void ApplyThrust(float controlScale)
    {
        if (throttleHeld)
            thrust = Mathf.Min(thrust + ThrustAccel * Time.fixedDeltaTime, ThrustMax);

        if (thrust > 0f)
        {
            if (thrustingEffect != null) thrustingEffect.SetActive(true);
            float boost = IsNitroActive ? NitroMultiplier : 1f;
            carRigidbody.AddRelativeForce(Vector2.right * thrust * boost * controlScale, ForceMode2D.Force);

            if (controlScale >= 1f)
                carRigidbody.AddForce(Vector2.right * thrust * 0.55f * boost, ForceMode2D.Force);
        }
        else if (thrustingEffect != null)
        {
            thrustingEffect.SetActive(false);
        }
    }

    void ApplyBrake()
    {
        if (!brakeHeld || throttleHeld) return;

        Vector2 velocity = carRigidbody.linearVelocity;
        if (velocity.sqrMagnitude > 0.01f)
            carRigidbody.AddForce(-velocity.normalized * BrakeForce, ForceMode2D.Force);
    }

    void LimitVelocity()
    {
        Vector2 velocity = carRigidbody.linearVelocity;
        float forwardCap = IsNitroActive ? MaxNitroSpeed : MaxForwardSpeed;
        float clampedX = Mathf.Clamp(velocity.x, -MaxReverseSpeed, forwardCap);
        float clampedY = Mathf.Min(velocity.y, MaxUpwardSpeed);

        if (!Mathf.Approximately(clampedX, velocity.x) || !Mathf.Approximately(clampedY, velocity.y))
            carRigidbody.linearVelocity = new Vector2(clampedX, clampedY);
    }

    void MaintainWheelAttachment()
    {
        SnapWheelIfSeparated(backTire, backTireOffset);
        SnapWheelIfSeparated(frontTire, frontTireOffset);
    }

    void SnapWheelIfSeparated(Rigidbody2D wheel, Vector2 localOffset)
    {
        if (wheel == null || carRigidbody == null) return;

        Vector2 expected = carRigidbody.GetRelativePoint(localOffset);
        Vector2 delta = expected - wheel.position;
        if (delta.sqrMagnitude <= WheelSnapDistance * WheelSnapDistance) return;

        wheel.position = expected;
        wheel.linearVelocity = carRigidbody.linearVelocity;
        wheel.angularVelocity = carRigidbody.angularVelocity;
    }

    void TickNitro()
    {
        if (!IsNitroActive) return;

        nitroDuration -= Time.fixedDeltaTime;
        if (nitroDuration <= 0f)
        {
            IsNitroActive = false;
            nitroCooldown = NitroCooldown;
        }
    }

    void ConsumeFuel()
    {
        if (!throttleHeld) return;

        fuel = Mathf.Max(fuel - fuelConsumption * Time.fixedDeltaTime, 0f);
        if (fuel <= 0f && !fuelOutStarted)
        {
            fuelOutStarted = true;
            fuelOutTimer = FuelOutGrace;
        }
    }

    void ApplyStuckAssist(bool grounded)
    {
        if (grounded && throttleHeld && Mathf.Abs(carRigidbody.linearVelocity.x) < StuckSpeed)
        {
            stuckTimer += Time.fixedDeltaTime;
            if (stuckTimer >= StuckThreshold)
            {
                Vector2 forward = carRigidbody.transform.right;
                carRigidbody.AddForce(forward * StuckAssistImpulse + Vector2.up * 0.25f, ForceMode2D.Impulse);
                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
        }
    }

    bool IsGrounded()
    {
        return IsTireGrounded(backTireCollider) || IsTireGrounded(frontTireCollider);
    }

    bool IsTireGrounded(Collider2D tireCollider)
    {
        if (tireCollider == null) return false;

        Bounds bounds = tireCollider.bounds;
        float radius = Mathf.Max(bounds.extents.x, bounds.extents.y) + 0.08f;
        int count = Physics2D.OverlapCircleNonAlloc(bounds.center, radius, groundHits);

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = groundHits[i];
            if (hit != null && hit != tireCollider && hit.CompareTag("Ground"))
                return true;
        }

        return false;
    }

    public void ActivateNitro()
    {
        if (nitroCharges <= 0 || IsNitroActive) return;

        nitroCharges--;
        IsNitroActive = true;
        nitroDuration = NitroDuration;
        AudioManager.Instance?.PlayNitroActivate();
    }

    public void AddNitroCharge()
    {
        nitroCharges = Mathf.Min(nitroCharges + 1, MaxNitroCharges);
    }

    public void CancelFuelOutTimer()
    {
        fuelOutStarted = false;
        fuelOutTimer = 0f;
    }

    public void RespawnAt(Vector2 worldPos)
    {
        fuel = 0.5f;
        fuelOutStarted = false;
        fuelOutTimer = 0f;
        thrust = 0f;
        IsNitroActive = false;
        nitroDuration = 0f;
        stuckTimer = 0f;

        Vector2 upOffset = Vector2.up * 1.5f;
        carRigidbody.position = worldPos + upOffset;
        backTire.position = worldPos + upOffset + backTireOffset;
        frontTire.position = worldPos + upOffset + frontTireOffset;

        carRigidbody.linearVelocity = Vector2.zero;
        backTire.linearVelocity = Vector2.zero;
        frontTire.linearVelocity = Vector2.zero;
        carRigidbody.angularVelocity = 0f;
        backTire.angularVelocity = 0f;
        frontTire.angularVelocity = 0f;

        carRigidbody.rotation = 0f;
        backTire.rotation = 0f;
        frontTire.rotation = 0f;
    }

    private bool IsStateBlocking()
    {
        if (GameStateManager.Instance == null) return false;

        return GameStateManager.Instance.IsGameOver
            || GameStateManager.Instance.IsWin
            || GameStateManager.Instance.IsLevelPaused
            || GameStateManager.Instance.IsPaused
            || GameStateManager.Instance.IsRespawning;
    }
}

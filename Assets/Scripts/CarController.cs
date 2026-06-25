using UnityEngine;

public class CarController : MonoBehaviour
{
    public static CarController Instance { get; private set; }

    public float fuel = 1f;
    public float fuelConsumption = 0.1f;
    public Rigidbody2D carRigidbody;
    public Rigidbody2D backTire;
    public Rigidbody2D frontTire;
    public float speed = 22f;
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
    private float recoveryTimer;
    private float nitroDuration;
    private float nitroCooldown;
    private Vector2 backTireOffset;
    private Vector2 frontTireOffset;
    private Collider2D backTireCollider;
    private Collider2D frontTireCollider;
    private WheelJoint2D backWheelJoint;
    private WheelJoint2D frontWheelJoint;
    private RigidbodyConstraints2D carOriginalConstraints;
    private RigidbodyConstraints2D backTireOriginalConstraints;
    private RigidbodyConstraints2D frontTireOriginalConstraints;
    private Rigidbody2D[] attachedBodies = new Rigidbody2D[0];
    private Vector2[] attachedBodyOffsets = new Vector2[0];
    private float[] attachedBodyAngles = new float[0];
    private RigidbodyConstraints2D[] attachedBodyConstraints = new RigidbodyConstraints2D[0];
    private bool savedOriginalConstraints;
    private bool respawnFrozen;
    private readonly Collider2D[] groundHits = new Collider2D[8];

    private const float StuckThreshold = 0.75f;
    private const float RecoveryThreshold = 2.4f;
    private const float StuckSpeed = 0.75f;
    private const float StuckAssistImpulse = 3.6f;
    private const float MotorSpeedMultiplier = 25f;
    private const float MotorTorque = 12000f;
    private const float ReverseTorqueScale = 0.9f;
    private const float AirControlScale = 0.42f;
    private const float ThrustAccel = 135f;
    private const float ThrustDecay = 150f;
    private const float ThrustMax = 120f;
    private const float BrakeForce = 95f;
    private const float ReverseAssistForce = 70f;
    private const float MaxForwardSpeed = 20f;
    private const float MaxReverseSpeed = 10f;
    private const float MaxNitroSpeed = 42f;
    private const float MaxUpwardSpeed = 9f;
    private const float MaxNitroUpwardSpeed = 28f;
    private const float TractionDamping = 0.85f;
    private const float UprightTorque = 18f;
    private const float MaxStableAngularSpeed = 130f;
    private const float FuelOutGrace = 3f;
    private const float NitroDuration = 1.5f;
    private const float NitroCooldown = 2f;
    private const float NitroMultiplier = 2.6f;
    private const float NitroLaunchForwardImpulse = 34f;
    private const float NitroLaunchUpImpulse = 24f;
    private const float NitroWheelImpulseScale = 0.7f;
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
        CacheAttachedBodies();

        DetachWheelTransform(backTire);
        DetachWheelTransform(frontTire);
        ConfigureWheelJoint(backWheelJoint, backTireOffset);
        ConfigureWheelJoint(frontWheelJoint, frontTireOffset);

        TuneBody(carRigidbody, 1.0f, 2.4f, 1.8f);
        TuneBody(backTire, 0.45f, 0.25f, 1.6f);
        TuneBody(frontTire, 0.45f, 0.25f, 1.6f);
        TuneCollision(carRigidbody);
        TuneWheelTraction(backTire);
        TuneWheelTraction(frontTire);

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

    void CacheAttachedBodies()
    {
        if (carRigidbody == null) return;

        Rigidbody2D[] allBodies = carRigidbody.GetComponentsInChildren<Rigidbody2D>(true);
        int count = 0;
        for (int i = 0; i < allBodies.Length; i++)
        {
            Rigidbody2D body = allBodies[i];
            if (body != null && body != carRigidbody && body != backTire && body != frontTire)
                count++;
        }

        attachedBodies = new Rigidbody2D[count];
        attachedBodyOffsets = new Vector2[count];
        attachedBodyAngles = new float[count];
        attachedBodyConstraints = new RigidbodyConstraints2D[count];

        int index = 0;
        for (int i = 0; i < allBodies.Length; i++)
        {
            Rigidbody2D body = allBodies[i];
            if (body == null || body == carRigidbody || body == backTire || body == frontTire)
                continue;

            attachedBodies[index] = body;
            attachedBodyOffsets[index] = carRigidbody.transform.InverseTransformPoint(body.position);
            attachedBodyAngles[index] = Mathf.DeltaAngle(carRigidbody.rotation, body.rotation);
            attachedBodyConstraints[index] = body.constraints;
            index++;
        }
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

        throttleHeld = Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D);
        brakeHeld = Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A);
        movement = throttleHeld ? 1f : (brakeHeld ? -1f : 0f);

        if (Input.GetKeyDown(KeyCode.Space) && nitroCharges > 0 && nitroCooldown <= 0f && !IsNitroActive)
            ActivateNitro();
        if (Input.GetKeyDown(KeyCode.R))
            RecoverToCheckpoint();

        if (nitroCooldown > 0f)
            nitroCooldown -= Time.deltaTime;

        if (gasCanImage != null)
            gasCanImage.fillAmount = Mathf.Clamp01(fuel);

        AudioManager.Instance?.SetLowFuelWarning(fuel < 0.2f && !fuelOutStarted && !IsStateBlocking());
        AudioManager.Instance?.SetEngineThrottle(throttleHeld || brakeHeld ? 1f : 0f);

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
        ApplyBrakeOrReverse(controlScale);
        ApplyTraction(grounded);
        StabilizeChassis(grounded);
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
        float targetSpeed = input * Mathf.Max(speed, 22f) * MotorSpeedMultiplier * reverseScale * controlScale;

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

    void ApplyBrakeOrReverse(float controlScale)
    {
        if (!brakeHeld || throttleHeld) return;

        Vector2 velocity = carRigidbody.linearVelocity;
        if (velocity.x > 0.5f)
        {
            carRigidbody.AddForce(Vector2.left * BrakeForce, ForceMode2D.Force);
            return;
        }

        carRigidbody.AddRelativeForce(Vector2.left * ReverseAssistForce * controlScale, ForceMode2D.Force);
    }

    void ApplyTraction(bool grounded)
    {
        if (!grounded || carRigidbody == null) return;

        Vector2 velocity = carRigidbody.linearVelocity;
        if (!throttleHeld && !brakeHeld)
            velocity.x = Mathf.Lerp(velocity.x, 0f, Time.fixedDeltaTime * 0.8f);

        velocity.y = Mathf.Lerp(velocity.y, Mathf.Min(velocity.y, MaxUpwardSpeed), Time.fixedDeltaTime * TractionDamping);
        carRigidbody.linearVelocity = velocity;
    }

    void StabilizeChassis(bool grounded)
    {
        if (carRigidbody == null) return;

        float angle = Mathf.DeltaAngle(carRigidbody.rotation, 0f);
        float stability = grounded ? 1f : 0.35f;
        carRigidbody.AddTorque(angle * UprightTorque * stability * Time.fixedDeltaTime, ForceMode2D.Force);
        carRigidbody.angularVelocity = Mathf.Clamp(carRigidbody.angularVelocity, -MaxStableAngularSpeed, MaxStableAngularSpeed);
    }

    void LimitVelocity()
    {
        Vector2 velocity = carRigidbody.linearVelocity;
        float forwardCap = IsNitroActive ? MaxNitroSpeed : MaxForwardSpeed;
        float upwardCap = IsNitroActive ? MaxNitroUpwardSpeed : MaxUpwardSpeed;
        float clampedX = Mathf.Clamp(velocity.x, -MaxReverseSpeed, forwardCap);
        float clampedY = Mathf.Min(velocity.y, upwardCap);

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
        if (!throttleHeld && !brakeHeld) return;

        fuel = Mathf.Max(fuel - fuelConsumption * Time.fixedDeltaTime, 0f);
        if (fuel <= 0f && !fuelOutStarted)
        {
            fuelOutStarted = true;
            fuelOutTimer = FuelOutGrace;
        }
    }

    void ApplyStuckAssist(bool grounded)
    {
        bool driveHeld = throttleHeld || brakeHeld;
        if (grounded && driveHeld && Mathf.Abs(carRigidbody.linearVelocity.x) < StuckSpeed)
        {
            stuckTimer += Time.fixedDeltaTime;
            recoveryTimer += Time.fixedDeltaTime;
            if (stuckTimer >= StuckThreshold)
            {
                Vector2 direction = throttleHeld ? carRigidbody.transform.right : -carRigidbody.transform.right;
                carRigidbody.AddForce(direction * StuckAssistImpulse + Vector2.up * 0.45f, ForceMode2D.Impulse);
                stuckTimer = 0f;
            }

            if (recoveryTimer >= RecoveryThreshold)
            {
                RecoverFromLocalStuck();
                recoveryTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
            recoveryTimer = 0f;
        }
    }

    void RecoverFromLocalStuck()
    {
        if (carRigidbody == null) return;

        float nudge = throttleHeld ? 0.8f : -0.8f;
        Vector2 basePos = carRigidbody.position + new Vector2(nudge, 1.0f);
        ResetVehiclePose(basePos, new Vector2(throttleHeld ? 1.5f : -1.2f, 0f));
    }

    public void RecoverToCheckpoint()
    {
        Vector2 pos = carRigidbody.position;
        float angle = 0f;
        if (CheckpointManager.Instance != null)
            CheckpointManager.Instance.TryGetSafeRespawn(out pos, out angle);

        RespawnAt(pos, angle, false);
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
        ApplyNitroLaunchImpulse();
        AudioManager.Instance?.PlayNitroActivate();
    }

    void ApplyNitroLaunchImpulse()
    {
        if (carRigidbody == null) return;

        Vector2 launch = (Vector2)carRigidbody.transform.right * NitroLaunchForwardImpulse
                       + Vector2.up * NitroLaunchUpImpulse;
        carRigidbody.AddForce(launch, ForceMode2D.Impulse);

        Vector2 wheelLaunch = launch * NitroWheelImpulseScale;
        if (backTire != null) backTire.AddForce(wheelLaunch, ForceMode2D.Impulse);
        if (frontTire != null) frontTire.AddForce(wheelLaunch, ForceMode2D.Impulse);
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

    public void BeginRespawnFreeze()
    {
        StoreOriginalConstraints();
        respawnFrozen = true;

        ResetControlState();
        FreezeBody(carRigidbody);
        FreezeBody(backTire);
        FreezeBody(frontTire);
        for (int i = 0; i < attachedBodies.Length; i++)
            FreezeBody(attachedBodies[i]);
    }

    public void EndRespawnFreeze()
    {
        RestoreOriginalConstraints();
        respawnFrozen = false;

        carRigidbody?.WakeUp();
        backTire?.WakeUp();
        frontTire?.WakeUp();
        for (int i = 0; i < attachedBodies.Length; i++)
        {
            if (attachedBodies[i] != null)
                attachedBodies[i].WakeUp();
        }
    }

    void StoreOriginalConstraints()
    {
        if (savedOriginalConstraints) return;

        if (carRigidbody != null) carOriginalConstraints = carRigidbody.constraints;
        if (backTire != null) backTireOriginalConstraints = backTire.constraints;
        if (frontTire != null) frontTireOriginalConstraints = frontTire.constraints;
        for (int i = 0; i < attachedBodies.Length; i++)
        {
            if (attachedBodies[i] != null)
                attachedBodyConstraints[i] = attachedBodies[i].constraints;
        }
        savedOriginalConstraints = true;
    }

    void RestoreOriginalConstraints()
    {
        if (!savedOriginalConstraints) return;

        if (carRigidbody != null) carRigidbody.constraints = carOriginalConstraints;
        if (backTire != null) backTire.constraints = backTireOriginalConstraints;
        if (frontTire != null) frontTire.constraints = frontTireOriginalConstraints;
        for (int i = 0; i < attachedBodies.Length; i++)
        {
            if (attachedBodies[i] != null)
                attachedBodies[i].constraints = attachedBodyConstraints[i];
        }
    }

    static void FreezeBody(Rigidbody2D body)
    {
        if (body == null) return;

        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        body.Sleep();
        body.constraints = RigidbodyConstraints2D.FreezeAll;
    }

    void ResetControlState()
    {
        throttleHeld = false;
        brakeHeld = false;
        movement = 0f;
        thrust = 0f;
        IsNitroActive = false;
        nitroDuration = 0f;
        nitroCooldown = 0f;
        stuckTimer = 0f;
        recoveryTimer = 0f;
        fuelOutStarted = false;
        fuelOutTimer = 0f;
        AudioManager.Instance?.SetEngineThrottle(0f);
        AudioManager.Instance?.SetLowFuelWarning(false);
        if (thrustingEffect != null) thrustingEffect.SetActive(false);
    }

    public void RespawnAt(Vector2 worldPos)
    {
        RespawnAt(worldPos, 0f, true);
    }

    public void RespawnAt(Vector2 worldPos, float angle)
    {
        RespawnAt(worldPos, angle, true);
    }

    public void RespawnAt(Vector2 worldPos, float angle, bool keepFrozen)
    {
        fuel = 0.5f;
        ResetControlState();
        StoreOriginalConstraints();

        ResetVehiclePose(worldPos, angle, Vector2.zero);

        if (keepFrozen || respawnFrozen)
            BeginRespawnFreeze();
        else
            EndRespawnFreeze();
    }

    void ResetVehiclePose(Vector2 bodyPosition, Vector2 startVelocity)
    {
        ResetVehiclePose(bodyPosition, 0f, startVelocity);
    }

    void ResetVehiclePose(Vector2 bodyPosition, float angle, Vector2 startVelocity)
    {
        Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
        Vector2 backOffset = rotation * backTireOffset;
        Vector2 frontOffset = rotation * frontTireOffset;

        RestoreOriginalConstraints();

        carRigidbody.position = bodyPosition;
        backTire.position = bodyPosition + backOffset;
        frontTire.position = bodyPosition + frontOffset;
        for (int i = 0; i < attachedBodies.Length; i++)
        {
            if (attachedBodies[i] != null)
                attachedBodies[i].position = bodyPosition + (Vector2)(rotation * attachedBodyOffsets[i]);
        }

        carRigidbody.linearVelocity = startVelocity;
        backTire.linearVelocity = startVelocity;
        frontTire.linearVelocity = startVelocity;
        for (int i = 0; i < attachedBodies.Length; i++)
        {
            if (attachedBodies[i] != null)
                attachedBodies[i].linearVelocity = startVelocity;
        }
        carRigidbody.angularVelocity = 0f;
        backTire.angularVelocity = 0f;
        frontTire.angularVelocity = 0f;
        for (int i = 0; i < attachedBodies.Length; i++)
        {
            if (attachedBodies[i] != null)
                attachedBodies[i].angularVelocity = 0f;
        }

        carRigidbody.rotation = angle;
        backTire.rotation = angle;
        frontTire.rotation = angle;
        for (int i = 0; i < attachedBodies.Length; i++)
        {
            if (attachedBodies[i] != null)
                attachedBodies[i].rotation = angle + attachedBodyAngles[i];
        }

        ConfigureWheelJoint(backWheelJoint, backTireOffset);
        ConfigureWheelJoint(frontWheelJoint, frontTireOffset);

        carRigidbody.WakeUp();
        backTire.WakeUp();
        frontTire.WakeUp();
        for (int i = 0; i < attachedBodies.Length; i++)
        {
            if (attachedBodies[i] != null)
                attachedBodies[i].WakeUp();
        }
    }

    static void TuneCollision(Rigidbody2D body)
    {
        if (body == null) return;

        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    static void TuneWheelTraction(Rigidbody2D tire)
    {
        if (tire == null) return;

        TuneCollision(tire);
        Collider2D col = tire.GetComponent<Collider2D>();
        if (col == null) return;

        var mat = new PhysicsMaterial2D("Responsive Tire");
        mat.friction = 1.25f;
        mat.bounciness = 0f;
        col.sharedMaterial = mat;
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

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

    // Nitro
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

    private const float StuckThreshold  = 1.8f;
    private const float StuckSpeedSq    = 0.04f;
    private const float ThrustAccel     = 38f;    // was 400 — too fast
    private const float ThrustDecay     = 85f;    // was 600
    private const float ThrustMax       = 55f;    // was 500 — caused launch behaviour
    private const float BrakeForce      = 55f;    // was 300 — balanced against new ThrustMax
    private const float MaxCarSpeed     = 14f;    // hard cap on forward velocity (m/s)
    private const float FuelOutGrace    = 3f;
    private const float NitroDuration   = 1.5f;
    private const float NitroCooldown   = 2f;
    private const float NitroMultiplier = 2.2f;   // was 3 — nitro still feels strong but not broken

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        carRigidbody.WakeUp();
        backTire.WakeUp();
        frontTire.WakeUp();

        // Store tire offsets for respawn repositioning
        backTireOffset  = backTire.position  - carRigidbody.position;
        frontTireOffset = frontTire.position - carRigidbody.position;

        // Add linear damping if scene left it at 0 — prevents endless sliding
        if (carRigidbody.linearDamping < 0.5f)
            carRigidbody.linearDamping = 1.2f;

        // Fix oversized tires: if scene scale is too large, clamp it.
        // Target: biggest localScale axis ≤ 1.4 (normal tire diameter for this camera scale).
        ClampTireScale(backTire,  1.4f);
        ClampTireScale(frontTire, 1.4f);
    }

    static void ClampTireScale(Rigidbody2D tire, float maxAxis)
    {
        if (tire == null) return;
        Vector3 s    = tire.transform.localScale;
        float biggest = Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y));
        if (biggest > maxAxis * 1.1f) // only touch if meaningfully oversized
            tire.transform.localScale = s * (maxAxis / biggest);
    }

    void Update()
    {
        bool blocked = PlayerNameInput.IsOpen || IsStateBlocking();
        if (blocked)
        {
            throttleHeld = false;
            brakeHeld    = false;
            movement     = 0f;
            AudioManager.Instance?.SetEngineThrottle(0f);
            AudioManager.Instance?.SetLowFuelWarning(false);
            if (gasCanImage != null) gasCanImage.fillAmount = Mathf.Clamp01(fuel);
            return;
        }

        throttleHeld = Input.GetKey(KeyCode.RightArrow);
        brakeHeld    = Input.GetKey(KeyCode.LeftArrow);
        movement     = throttleHeld ? 1f : (brakeHeld ? -0.3f : 0f);

        // Nitro activation
        if (Input.GetKeyDown(KeyCode.Space) && nitroCharges > 0 && nitroCooldown <= 0f && !IsNitroActive)
            ActivateNitro();

        if (nitroCooldown > 0f) nitroCooldown -= Time.deltaTime;

        // Fuel UI
        if (gasCanImage != null) gasCanImage.fillAmount = Mathf.Clamp01(fuel);

        // Low-fuel warning
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetLowFuelWarning(fuel < 0.2f && !fuelOutStarted && !IsStateBlocking());

        AudioManager.Instance?.SetEngineThrottle(throttleHeld ? 1f : 0f);

        // Fuel-out grace countdown
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

        // Always decay thrust even when blocked so there's no stored-up launch on unpause
        if (!throttleHeld || blocked)
            thrust = Mathf.Max(thrust - ThrustDecay * Time.fixedDeltaTime, 0f);

        if (blocked)
        {
            if (thrustingEffect != null) thrustingEffect.SetActive(false);
            return;
        }

        // ── Tire torque (drives wheels through joint) ─────────────────────────
        float torqueInput = throttleHeld ? -1f : (brakeHeld ? 0.5f : 0f);
        float tireTorque  = torqueInput * speed * Time.fixedDeltaTime;
        backTire.AddTorque(tireTorque);
        frontTire.AddTorque(tireTorque);

        // ── Auxiliary thrust force ────────────────────────────────────────────
        if (throttleHeld)
            thrust = Mathf.Min(thrust + ThrustAccel * Time.fixedDeltaTime, ThrustMax);

        float thrustMultiplier = IsNitroActive ? NitroMultiplier : 1f;
        if (thrust > 0f)
        {
            if (thrustingEffect != null) thrustingEffect.SetActive(true);
            carRigidbody.AddRelativeForce(Vector2.right * thrust * thrustMultiplier);
        }
        else
        {
            if (thrustingEffect != null) thrustingEffect.SetActive(false);
        }

        // ── Hard velocity cap — prevents runaway acceleration ────────────────
        float speedCap = IsNitroActive ? MaxCarSpeed * NitroMultiplier : MaxCarSpeed;
        float vx = carRigidbody.linearVelocity.x;
        if (vx > speedCap)
            carRigidbody.linearVelocity = new Vector2(speedCap, carRigidbody.linearVelocity.y);

        // ── Brake counter-force ───────────────────────────────────────────────
        if (brakeHeld && !throttleHeld)
        {
            Vector2 vel = carRigidbody.linearVelocity;
            carRigidbody.AddForce(-vel.normalized * BrakeForce * Time.fixedDeltaTime, ForceMode2D.Force);
        }

        // ── Nitro countdown ───────────────────────────────────────────────────
        if (IsNitroActive)
        {
            nitroDuration -= Time.fixedDeltaTime;
            if (nitroDuration <= 0f)
            {
                IsNitroActive = false;
                nitroCooldown = NitroCooldown;
            }
        }

        // ── Fuel consumption ──────────────────────────────────────────────────
        if (throttleHeld)
        {
            fuel -= fuelConsumption * Time.fixedDeltaTime;
            fuel  = Mathf.Max(fuel, 0f);

            if (fuel <= 0f && !fuelOutStarted)
            {
                fuelOutStarted = true;
                fuelOutTimer   = FuelOutGrace;
            }
        }

        // ── Stuck nudge ───────────────────────────────────────────────────────
        if (throttleHeld && carRigidbody.linearVelocity.sqrMagnitude < StuckSpeedSq)
        {
            stuckTimer += Time.fixedDeltaTime;
            if (stuckTimer >= StuckThreshold)
            {
                carRigidbody.AddForce(Vector2.up * 4f, ForceMode2D.Impulse);
                backTire.AddForce(Vector2.up * 1.5f, ForceMode2D.Impulse);
                frontTire.AddForce(Vector2.up * 1.5f, ForceMode2D.Impulse);
                stuckTimer = 0f;
            }
        }
        else stuckTimer = 0f;
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
        fuelOutTimer   = 0f;
    }

    public void RespawnAt(Vector2 worldPos)
    {
        // Reset fuel
        fuel           = 0.5f;
        fuelOutStarted = false;
        fuelOutTimer   = 0f;
        thrust         = 0f;
        IsNitroActive  = false;
        nitroDuration  = 0f;
        stuckTimer     = 0f;

        // Teleport all rigidbodies (must happen in same frame before physics resolves)
        Vector2 upOffset = Vector2.up * 1.5f;
        carRigidbody.position = worldPos + upOffset;
        backTire.position     = worldPos + upOffset + backTireOffset;
        frontTire.position    = worldPos + upOffset + frontTireOffset;

        // Zero all motion
        carRigidbody.linearVelocity  = Vector2.zero;
        backTire.linearVelocity      = Vector2.zero;
        frontTire.linearVelocity     = Vector2.zero;
        carRigidbody.angularVelocity = 0f;
        backTire.angularVelocity     = 0f;
        frontTire.angularVelocity    = 0f;

        // Stand upright
        carRigidbody.rotation = 0f;
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

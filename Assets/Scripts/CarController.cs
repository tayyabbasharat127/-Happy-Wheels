using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarController : MonoBehaviour
{


    // [HideInInspector]
    public float fuel = 1;
    public float fuelconsumption = 0.1f;
    public Rigidbody2D carRigidbody;
    public Rigidbody2D backTire;
    public Rigidbody2D frontTire;
    public float speed = 20;
    public float carTorque = 10;

    public float tireTorque = 0;
    
    public float movement;
    public UnityEngine.UI.Image gasCanImage;

    public float thrust = 0f;

    public GameObject thrustingEffect;

    private bool  throttleHeld;
    private float stuckTimer;
    private const float StuckThreshold = 1.8f;
    private const float StuckSpeedSq   = 0.04f;
    private const float ThrustAccel    = 400f;   // units/s to reach max
    private const float ThrustDecay    = 600f;
    private const float ThrustMax      = 500f;

    void Start()
    {
        carRigidbody.WakeUp();
        backTire.WakeUp();
        frontTire.WakeUp();
    }

    void Update()
    {
        if (PlayerNameInput.IsOpen ||
            (GameStateManager.Instance != null &&
            (GameStateManager.Instance.IsGameOver || GameStateManager.Instance.IsWin ||
             GameStateManager.Instance.IsLevelPaused || GameStateManager.Instance.IsPaused)))
        {
            throttleHeld = false;
            movement = 0f;
            if (AudioManager.Instance != null) AudioManager.Instance.SetEngineThrottle(0f);
            if (gasCanImage != null) gasCanImage.fillAmount = Mathf.Clamp01(fuel);
            return;
        }

        throttleHeld = Input.GetKey(KeyCode.RightArrow);
        movement     = throttleHeld ? 1f : 0f;

        if (AudioManager.Instance != null)
            AudioManager.Instance.SetEngineThrottle(movement);

        if (gasCanImage != null)
            gasCanImage.fillAmount = Mathf.Clamp01(fuel);
    }

    void FixedUpdate()
    {
        if (PlayerNameInput.IsOpen ||
            (GameStateManager.Instance != null &&
            (GameStateManager.Instance.IsGameOver || GameStateManager.Instance.IsWin ||
             GameStateManager.Instance.IsLevelPaused || GameStateManager.Instance.IsPaused))) return;

        // Wheel torque — frame-rate independent
        tireTorque = -movement * speed * Time.fixedDeltaTime;
        backTire.AddTorque(tireTorque);
        frontTire.AddTorque(tireTorque);

        // Thrust — build up / decay each fixed step, apply directly (no extra dt multiply)
        if (throttleHeld)
            thrust = Mathf.Min(thrust + ThrustAccel * Time.fixedDeltaTime, ThrustMax);
        else
            thrust = Mathf.Max(thrust - ThrustDecay * Time.fixedDeltaTime, 0f);

        if (thrust > 0f)
        {
            if (thrustingEffect != null) thrustingEffect.SetActive(true);
            carRigidbody.AddRelativeForce(Vector2.right * thrust);
        }
        else
        {
            if (thrustingEffect != null) thrustingEffect.SetActive(false);
        }

        fuel -= fuelconsumption * Mathf.Abs(movement) * Time.fixedDeltaTime;

        // Unstuck nudge
        if (movement > 0 && carRigidbody.linearVelocity.sqrMagnitude < StuckSpeedSq)
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
        else
        {
            stuckTimer = 0f;
        }
    }


    /* OLD CODE EXPERIMENTS FOR REFERENCE
     if (thrust > 0)
        {

            //Vector3 targetDelta = target.position - transform.position;

            ////get the angle between transform.forward and target delta
            //float angleDiff = Vector3.Angle(transform.forward, targetDelta);

            //// get its cross product, which is the axis of rotation to
            //// get from one vector to the other
            //Vector3 cross = Vector3.Cross(transform.forward, targetDelta);

            //// apply torque along that axis according to the magnitude of the angle.
            //rigidbody.AddTorque(cross * angleDiff * force);


            //Vector2 force = new Vector2(carRigidbody.position.x * -thrust, carRigidbody.position.y);
            //float angle = Mathf.Asin(carRigidbody.position.y) * Mathf.Rad2Deg;

            //float angle = Mathf.Atan2(carRigidbody.position.y, carRigidbody.position.x) * Mathf.Rad2Deg;
            //transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

            //if (carRigidbody.position.x < 0f)
            //    angle = 180 - angle;

            //Debug.Log(angle);

            //gunTransform.localEulerAngles = new Vector3(0f, 0f, angle);

            if (thrustingEffect != null) thrustingEffect.SetActive(true);

            //Vector2 forcePosition = new Vector2(carRigidbody.position.x - 5, carRigidbody.position.y);
            //carRigidbody.AddForceAtPosition(force, forcePosition);

            //Transform carTransform = carRigidbody.transform;
            //carRigidbody.AddForce(carTransform.forward * thrust);

            //WORKS
            //carRigidbody.AddForce(Vector2.right * thrust);

            //ALSO WORKS
            carRigidbody.AddRelativeForce(Vector2.right * thrust);

        } 
     * 
     */


}

using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private const int LEFT = 0;
    private const int RIGHT = 1;

    public static PlayerController i;

    [SerializeField]
    private Transform cameraTransform;

    [SerializeField]
    private Transform lookAtBone;

    [SerializeField]
    private float landingDistance = 5f;

    [SerializeField]
    [Range(0f, 80f)]
    private float cameraWakeUpAngle = 20f;

    [SerializeField]
    [Range(1f, 50f)]
    private float criticalMinimumEnergy = 10f;

    [SerializeField]
    [Range(0f, 360f)]
    private float rotationDegsPerSecond = 90f;

    [SerializeField]
    [Range(0f, 10f)]
    private float frictionCoefficient = 0.1f;

    [SerializeField]
    private float minSpeedForLowGravity = 20f;

    [SerializeField]
    private float lowGravityMultiplier = 0.1f;

    [SerializeField]
    private float dashEnergy = 4f;

    [SerializeField]
    private float dashUpPull = 0.5f;

    [SerializeField]
    private float sideDashEnergy = 6f;

    [SerializeField]
    private float baseCoastingEnergy = 50f;

    [SerializeField]
    private float forwardDashDuration = 2f;

    [SerializeField]
    private float sideRollDuration = 1f;

    [SerializeField]
    [Range(0f, 1f)]
    private float costOfDash = 0.2f;

    [SerializeField]
    [Range(0f, 1f)]
    private float costOfRoll = 0.05f;

    [SerializeField]
    private float landingDurationSeconds = 1.5f;

    [SerializeField]
    private float takeOffDurationSeconds = 1.5f;

    [SerializeField]
    private float sideTiltLerpSpeed = 4f;

    [SerializeField]
    private float sideTiltDegrees = 24f;

    [SerializeField]
    private float maxParticleAlpha = 0.3f;

    [SerializeField]
    private float staminaDepletionPerSecond = 0.01f;

    [SerializeField]
    private float energyDepletionPerSecond = 1f;

    [SerializeField]
    private float energyRegainWhenWindyPerSecond = 0.5f;

    [SerializeField]
    private ParticleSystem pSystem;

    [SerializeField]
    private Animator animator;

    [SerializeField]
    private UnityEngine.UI.Image staminaDisplay;

    [SerializeField]
    private UnityEngine.UI.Image energyDisplay;

    [SerializeField]
    private UnityEngine.UI.Image landingSpotAim;

    [SerializeField]
    private LandingSpotLogic landing;

    [SerializeField]
    private WindReceiver winds;

    [SerializeField]
    private TrailRenderer[] trails = new TrailRenderer[2];

    [SerializeField]
    [Header("ReadOnly")]
    private Vector3 movementVector;

    [SerializeField]
    private Vector3 dashVector;

    [SerializeField]
    private float energy;

    [SerializeField]
    private float stamina01;

    [SerializeField]
    private float tiltTarget;

    [SerializeField]
    private float tiltAmount;


    private bool IsDashingForward => lastForwardDashAt > Time.time - forwardDashDuration;
    private bool[] IsDashing => new bool[] { lastSideRollAt[LEFT] > Time.time - sideRollDuration, lastSideRollAt[RIGHT] > Time.time - sideRollDuration };

    private bool InFlight => !isLanded && !isLandingOrDeparting && !isFalling;

    private readonly float[] lastSideRollAt = new float[2] { float.MinValue, float.MinValue };

    private float lastForwardDashAt = float.MinValue;

    private bool isCorrectingAngle = false;

    private bool wantsToLand = false;
    private bool wantsToTakeOff = false;

    private bool isLandingOrDeparting = false;
    private bool isLanded = false;
    private bool isFalling = false;

    void Start()
    {
        if (i)
        {
            Destroy(this.gameObject);
            return;
        }

        i = this;

        Restart();
    }

    void Restart()
    {
        energy = baseCoastingEnergy;
        stamina01 = 1f;
        isFalling = false;
        wantsToLand = false;
        wantsToTakeOff = false;

        isLandingOrDeparting = false;
        isLanded = false;

        transform.position = Vector3.up * 10f;

        DashForward();
    }

    private void Update()
    {
        ReadInputs();
        UpdateLandingSequence();
        UpdateTakeOffSequence();
    }


    void FixedUpdate()
    {
        //
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            Restart();
        }
        //

        if (isLanded)
        {
            RestoreStamina();
            energy = 0f;
        }
        else if (InFlight || isFalling)
        {
            UpdateNoseDirection();

            ComputeDash();
            ComputeCoasting();
            ComputeWinds();
            ComputeGravity();

            ComputeFriction();


            ApplyDash();
            ApplyMovement();

            UpdateEnergy();
            DepleteStamina();
        }

        ApplyTilt();

        UpdateCanvas();
    }

    private void ApplyTilt()
    {
        if (InFlight)
        {
            tiltAmount = Mathf.Lerp(tiltAmount, tiltTarget, sideTiltLerpSpeed * Time.deltaTime);
            animator.transform.localEulerAngles = Vector3.forward * tiltAmount;
        }
        else
        {
            tiltAmount = 0f;
            tiltTarget = 0f;
            animator.transform.localEulerAngles = Vector3.zero;
        }
    }

    private void LateUpdate()
    {
        UpdateLookAt();

        if (transform.position.y < -6.5f)
        {
            Restart();
        }
    }

    void UpdateLandingSequence()
    {
        if (InFlight && wantsToLand && landing.SpotInSight)
        {
            StartCoroutine(Land());
        }
    }

    void UpdateTakeOffSequence()
    {
        if (isLanded && !isLandingOrDeparting && wantsToTakeOff)
        {
            StartCoroutine(TakeOff());
        }
    }

    IEnumerator TakeOff()
    {
        isLandingOrDeparting = true;
        wantsToTakeOff = false;

        animator.SetTrigger("TakeOff");

        yield return new WaitForSeconds(takeOffDurationSeconds);
        energy = baseCoastingEnergy;

        isLanded = false;
        isLandingOrDeparting = false;
    }

    IEnumerator Land()
    {
        isLandingOrDeparting = true;
        wantsToLand = false;

        Vector3 target = landing.SpotInSight.LandingPosition;
        Vector3 initialPosition = transform.position;

        animator.SetTrigger("Land");

        float time = Time.time;
        while (time > Time.time - landingDurationSeconds)
        {
            float completion01 = (Time.time - time) / landingDurationSeconds;
            transform.position = Vector3.Lerp(initialPosition, target, completion01);
            yield return null;
        }

        isLanded = true;
        isLandingOrDeparting = false;
    }

    void RestoreStamina()
    {
        stamina01 = Mathf.Clamp01(stamina01 + landing.StaminaRestorePerSecond * Time.deltaTime);
    }

    void DepleteStamina()
    {
        if (InFlight)
        {
            stamina01 = Mathf.Clamp01(stamina01 - Time.deltaTime * staminaDepletionPerSecond);
        }
    }

    void UpdateEnergy()
    {
        float multiplier = 1f;

        if (isFalling)
        {
            multiplier = 30f;
        }

        if (winds.CombinedPush.y > 0f)
        {
            energy += winds.CombinedPush.y * Time.deltaTime * energyRegainWhenWindyPerSecond;
        }
        else
        {
            energy = Mathf.Max(0f, energy - energyDepletionPerSecond * Time.deltaTime * multiplier);
        }

        if ((energy <= 0f || (energy <= criticalMinimumEnergy && stamina01 <= 0f)) && InFlight)
        {
            isFalling = true;
            animator.SetTrigger("OnFall");
        }
    }

    void UpdateLookAt()
    {
        if (InFlight)
        {
            lookAtBone.forward = cameraTransform.forward;
        }
        else
        {
            // TODO: Do not touch it
        }
    }

    void ComputeDash()
    {
        float fwdDashAmount = 1f + (lastForwardDashAt - Time.time) / sideRollDuration;
        float[] sideDashAmount = new float[]
        {
            Mathf.Clamp01(1f + (lastSideRollAt[LEFT] - Time.time)/sideRollDuration),
            Mathf.Clamp01(1f + (lastSideRollAt[RIGHT] - Time.time)/sideRollDuration)
        };

        var m = pSystem.main;
        m.startColor = new Color(1f, 1f, 1f, maxParticleAlpha * fwdDashAmount);

        if (fwdDashAmount > 0f)
        {
            dashVector = cameraTransform.forward * fwdDashAmount * dashEnergy;
            dashVector += Vector3.up * dashUpPull;
        }
        else if (sideDashAmount[LEFT] > 0 || sideDashAmount[RIGHT] > 0f)
        {
            dashVector = cameraTransform.right * (-sideDashAmount[LEFT] + sideDashAmount[RIGHT]) * sideDashEnergy;
        }
        else
        {
            dashVector = Vector3.zero;
        }
    }

    void ApplyDash()
    {
        movementVector += dashVector;
    }

    void ReadInputs()
    {
        if (Gamepad.current != null)
        {
            if (InFlight)
            {
                wantsToLand |= WantsToLand();

                if (IsDashingForward)
                {
                    // Do nothing
                }
                else
                {
                    if (stamina01 <= 0F)
                    {
                        return;
                    }

                    bool dashesRight = Gamepad.current.rightTrigger.wasPressedThisFrame && !IsDashing[RIGHT];
                    bool dashesLeft = Gamepad.current.leftTrigger.wasPressedThisFrame && !IsDashing[LEFT];

                    float cost = costOfDash;

                    bool dashForward = dashesRight && dashesLeft;

                    if (dashesRight && IsDashing[LEFT]
                        || dashesLeft && IsDashing[RIGHT]
                    )
                    {
                        dashForward = true;
                        cost -= costOfRoll;
                    }

                    if (dashForward)
                    {
                        DashForward();
                        stamina01 = Mathf.Clamp01(stamina01 - cost);
                    }
                    else
                    {
                        if (dashesRight)
                        {
                            DashSide(RIGHT);
                            cost = costOfRoll;
                            stamina01 = Mathf.Clamp01(stamina01 - cost);
                        }
                        else if (dashesLeft)
                        {
                            DashSide(LEFT);
                            cost = costOfRoll;
                            stamina01 = Mathf.Clamp01(stamina01 - cost);
                        }
                    }
                }
            }
            else
            {
                wantsToTakeOff |= WantsToTakeOff();
            }
        }
    }

    bool WantsToTakeOff()
    {
        if (!isLandingOrDeparting && isLanded)
        {
            bool dashesRight = Gamepad.current.rightTrigger.IsPressed();
            bool dashesLeft = Gamepad.current.leftTrigger.IsPressed();

            if (dashesRight && dashesLeft)
            {
                return true;
            }
        }

        return false;
    }

    bool WantsToLand()
    {
        if (InFlight)
        {
            if (!IsDashing[LEFT] && !IsDashing[RIGHT] && !IsDashingForward)
            {
                if (landing.SpotInSight && Vector3.Distance(landing.SpotInSight.LandingPosition, transform.position) < landingDistance)
                {
                    return true;
                }
            }
        }

        return false;
    }

    void DashSide(int side)
    {
        lastSideRollAt[side] = Time.time;
        animator.SetTrigger(side == RIGHT ? "DashRight" : "DashLeft");
        SetFinalRotationInstantly();
    }

    void DashForward()
    {
        lastForwardDashAt = Time.time;
        animator.SetTrigger("DashForward");
        animator.ResetTrigger("DashRight");
        animator.ResetTrigger("DashLeft");
        SetFinalRotationInstantly();

        energy += dashEnergy;
    }

    void SetFinalRotationInstantly()
    {
        Vector3 flatFwd = cameraTransform.forward;
        flatFwd.y = 0f;
        flatFwd.Normalize();

        transform.rotation = Quaternion.LookRotation(flatFwd, Vector3.up);
    }

    void UpdateNoseDirection()
    {
        if (isFalling)
        {
            trails[LEFT].emitting = true;
            trails[RIGHT].emitting = true;

            tiltTarget = 0f;
        }
        else
        {
            Vector3 flatFwd = cameraTransform.forward;
            flatFwd.y = 0f;
            flatFwd.Normalize();

            float angle = Vector3.SignedAngle(transform.forward, flatFwd, Vector3.up);

            if (Mathf.Abs(angle) > cameraWakeUpAngle && !isCorrectingAngle)
            {
                isCorrectingAngle = true;
            }
            else if (Mathf.Abs(angle) < 1f && isCorrectingAngle)
            {
                isCorrectingAngle = false;
            }

            if (isCorrectingAngle)
            {
                float angleAmount = Mathf.Clamp01(Mathf.Abs(angle) / cameraWakeUpAngle);

                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    Quaternion.LookRotation(flatFwd, Vector3.up),
                    rotationDegsPerSecond * Time.deltaTime * angleAmount
                );

                tiltTarget = -Mathf.Sign(angle) * angleAmount * sideTiltDegrees;

                bool leftIsOn = angleAmount >= 1f && angle < 0f;
                bool rightIsOn = angleAmount >= 1f && angle > 0f;

                trails[LEFT].emitting = leftIsOn;
                trails[RIGHT].emitting = rightIsOn;

                animator.SetBool("GoingRight", rightIsOn);
                animator.SetBool("GoingLeft", leftIsOn);
            }
            else
            {
                animator.SetBool("GoingRight", false);
                animator.SetBool("GoingLeft", false);

                trails[LEFT].emitting = false;
                trails[RIGHT].emitting = false;

                tiltTarget = 0f;
            }
        }
    }

    private void ComputeWinds()
    {
        movementVector += winds.CombinedPush * Time.deltaTime;
    }

    void ComputeCoasting()
    {
        movementVector += energy * transform.forward * Time.deltaTime;
    }

    void ComputeFriction()
    {
        if (movementVector.sqrMagnitude > 0f)
        {
            float y = movementVector.y;
            movementVector -= movementVector.normalized * movementVector.sqrMagnitude * frictionCoefficient * Time.deltaTime;

            if (isFalling && y < 0f)
            {
                movementVector.y = y;
            }
        }
    }

    void ComputeGravity()
    {
        float gravityMultiplierKey = Mathf.Clamp01(new Vector3(movementVector.x, 0f, movementVector.z).magnitude / minSpeedForLowGravity);

        float gravityMultiplier = Mathf.Lerp(1f, lowGravityMultiplier, gravityMultiplierKey);

        movementVector += Physics.gravity * Time.deltaTime * gravityMultiplier;
    }

    void ApplyMovement()
    {
        transform.position += movementVector * Time.deltaTime;
    }

    void UpdateCanvas()
    {
        if (staminaDisplay)
        {
            staminaDisplay.fillAmount = stamina01;
        }

        if (energyDisplay)
        {
            energyDisplay.fillAmount = (energy- criticalMinimumEnergy) / 70f;
        }

        if (InFlight && !IsDashingForward && !IsDashing[LEFT] && !IsDashing[RIGHT])
        {
            if (landing.SpotInSight)
            {
                landingSpotAim.enabled = true;
                landingSpotAim.rectTransform.position = Camera.main.WorldToScreenPoint(landing.SpotInSight.LandingPosition);
            }
            else
            {
                landingSpotAim.enabled = false;
            }
        }
        else
        {
            landingSpotAim.enabled = false;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [SerializeField]
    private Transform target;

    [SerializeField]
    private Camera childCamera;

    [SerializeField]
    private float angleRotationSpeed = 20f;

    [SerializeField]
    private float cameraOptimalDistance = 7f;

    [SerializeField]
    private float rotationLerpSpeed = 100f;

    [SerializeField]
    private float cameraOverhead = 0f;

    [SerializeField]
    private float cameraHeight = 0.5f;

    [SerializeField]
    private float verticalLookLerpSpeed = 4f;

    [SerializeField]
    private float lerpSpeed = 20f;

    [SerializeField]
    private float maxAngleDifference = 90f;

    [SerializeField]
    [Range(0f, 1f)]
    private float verticalLookMaxAmplitde = 0.7f;

    [SerializeField]
    private bool invertVerticalLook = false;

    [Header("ReadOnly")]
    [SerializeField]
    private float desiredAngle;

    private void Awake()
    {
        
    }

    private void Start()
    {
        CatchUpTargetPosition();
    }

    void CatchUpTargetPosition(float lerp=1f, float rotationLerp=1f)
    {
        float targetHeight = cameraHeight;

        Vector3 optimalLocalPosition = new Vector3(
            Mathf.Sin(desiredAngle * Mathf.Deg2Rad),
            0f,
            Mathf.Cos(desiredAngle * Mathf.Deg2Rad)
        ) * (-cameraOptimalDistance) + Vector3.up * (cameraOverhead + targetHeight);


        Vector3 targetPosition = optimalLocalPosition + target.position;

#if false

        Vector3 currentVelocity = Vector3.zero;
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, lerp);

        Quaternion targetRotation = Quaternion.LookRotation((target.position - transform.position) + Vector3.up * targetHeight);

        float delta = Quaternion.Angle(transform.rotation, targetRotation);
        if (delta > 0f)
        {
            float smoothTime = 0f;
            float t = Mathf.SmoothDampAngle(delta, 0.0f, ref smoothTime, rotationLerp);
            t = 1.0f - (t / delta);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, t);
        }
#else
        transform.position = Vector3.Lerp(transform.position, targetPosition, lerp);

        Quaternion targetRotation = Quaternion.LookRotation((target.position - transform.position) + Vector3.up * targetHeight);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationLerp);
#endif
    }

    private void FixedUpdate()
    {
        if (Gamepad.current != null)
        {
            Vector2 stickValue = Gamepad.current.leftStick.ReadValue();
            float multiplier = 1f;

            // constraint to player forward
            {
                float angleDiff = Vector3.SignedAngle((target.position - transform.position).normalized, target.forward, Vector3.up);

                if (Mathf.Abs(stickValue.x) != 0f && Mathf.Sign(stickValue.x) != Mathf.Sign(angleDiff))
                {
                    multiplier = 1f - Mathf.Clamp01(Mathf.Abs(angleDiff) / maxAngleDifference  );
                }
            }

            desiredAngle += stickValue.x * multiplier * angleRotationSpeed * Time.deltaTime;

            // Cap
            {
                desiredAngle = desiredAngle % 360f;
                while (desiredAngle < 0)
                {
                    desiredAngle = 360f + desiredAngle;
                }
            }

            CatchUpTargetPosition(Time.deltaTime * lerpSpeed, Time.deltaTime * rotationLerpSpeed);

            float yInput = stickValue.y;

            yInput = Mathf.Sign(yInput) * Mathf.Pow(Mathf.Abs(yInput), 3);

            Quaternion childLookRot = Quaternion.LookRotation(
                new Vector3(
                    0f, 
                    (invertVerticalLook ? -1f : 1f) * yInput * verticalLookMaxAmplitde, 
                    1f - verticalLookMaxAmplitde * Mathf.Abs(yInput)
                )
            );

            childCamera.transform.localRotation = Quaternion.Lerp(childCamera.transform.localRotation, childLookRot, Time.deltaTime * verticalLookLerpSpeed);
        }
    }
}

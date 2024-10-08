using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LandingSpotLogic : MonoBehaviour
{
    public LandingSpot SpotInSight {  get; private set; }

    public float StaminaRestorePerSecond => staminaRestorePerSecond;

    [SerializeField]
    private Transform referenceTransform;

    [SerializeField]
    [Min(0f)]
    private float detectionRadius = 5f;

    [SerializeField]
    [Min(0f)]
    private float maxDistance = 50f;

    [SerializeField]
    private float staminaRestorePerSecond = 0.2f;

    private void Update()
    {
        Debug.DrawRay(referenceTransform.position, referenceTransform.forward * maxDistance, Color.red);

        if (Physics.SphereCast(referenceTransform.position, detectionRadius, referenceTransform.forward, out RaycastHit hit, maxDistance, LayerMask.GetMask("LandingSpot")))
        {
            if (hit.collider)
            {
                var spot = hit.collider.gameObject.GetComponent<LandingSpot>();
                
                if (spot)
                {
                    SpotInSight = spot;
                    return;
                }
            }
        }

        SpotInSight = null;
    }
}

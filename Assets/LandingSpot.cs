using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LandingSpot : MonoBehaviour
{
    public Vector3 LandingPosition => transform.position + landingPosition;

    [SerializeField]
    private Vector3 landingPosition;

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.Lerp(Color.cyan, new Color(1f,1f,1f,0f), 0.4f);
        Gizmos.DrawSphere(LandingPosition, 1f);  
    }
#endif
}

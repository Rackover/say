using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindReceiver : MonoBehaviour, IWindPusher
{
    public Vector3 CombinedPush { get; private set; }

    public Vector3 ForwardRequiredToStayWithin { get; private set; }

    public bool ShouldCorrect { get; private set; }

    [SerializeField]
    [Range(50f, 90f)]
    private float optimalTurnAngle = 90f;

    [SerializeField]
    [Range(00f, 90f)]
    private float maxCorrectionThreshold = 50f;


    [Header("ReadOnly")]
    [SerializeField]
    private bool correcting;

    [SerializeField]
    private Vector3 correctionAngle;

    Vector3 IWindPusher.Force => CombinedPush;

    Vector3 IWindPusher.Center => throw new System.NotImplementedException();

    private readonly List<IWindPusher> winds = new List<IWindPusher>();

    public void Enter(IWindPusher wind)
    {
        winds.Add(wind);
    }

    public void Exit(IWindPusher wind)
    {
        winds.Remove(wind);
    }

    void Update()
    {
        CombinedPush = Vector3.zero;
        for (int i = 0; i < winds.Count; i++)
        {
            CombinedPush += winds[i].Force;
        }

        ComputeDirectionToStayWithin();
    }

    void ComputeDirectionToStayWithin()
    {
        ForwardRequiredToStayWithin = Vector3.zero;
        ShouldCorrect = false;

        if(winds.Count > 0)
        {
            IWindPusher biggest = winds[0];
            for (int i = 1;i < winds.Count;i++)
            {
                if (biggest.Force.sqrMagnitude < winds[i].Force.sqrMagnitude)
                {
                    biggest = winds[i];
                }
            }

            Vector3 currentDirection = transform.forward.WithoutY().normalized;

            Vector3 meToCenterXZ = biggest.Center.WithoutY() - transform.position.WithoutY();

            float currentDirectionAngle = Vector3.SignedAngle(currentDirection, meToCenterXZ, Vector3.up);
            
            float optimal = optimalTurnAngle * Mathf.Sign(currentDirectionAngle);

            float angleChangeRequiredToStayWithin = optimal - currentDirectionAngle;

            ForwardRequiredToStayWithin = Quaternion.Euler(0f, -angleChangeRequiredToStayWithin, 0f) * currentDirection;
            ShouldCorrect = Mathf.Abs(angleChangeRequiredToStayWithin) < maxCorrectionThreshold;
        }

        correcting = ShouldCorrect;
        correctionAngle = ForwardRequiredToStayWithin;
    }
}

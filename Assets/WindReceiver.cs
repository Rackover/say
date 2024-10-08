using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindReceiver : MonoBehaviour, IWindPusher
{
    public Vector3 CombinedPush { get; private set; }

    Vector3 IWindPusher.Force => CombinedPush;

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
    }
}

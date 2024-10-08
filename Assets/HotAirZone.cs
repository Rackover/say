using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public interface IWindPusher
{
    public Vector3 Force { get; }
}

public class HotAirZone : MonoBehaviour, IWindPusher
{
    [SerializeField]
    private CollisionEventTransmitter collision;

    [SerializeField]
    private Vector3 force;

    public Vector3 Force => force;

    private void Awake()
    {
        collision.onTriggerEnter += Collision_onTriggerEnter;
        collision.onTriggerExit += Collision_onTriggerExit;
    }

    private void Collision_onTriggerExit(Collider obj)
    {
        var player = obj.GetComponent<WindReceiver>();
        if (player)
        {
            player.Exit(this);
        }
    }

    private void Collision_onTriggerEnter(Collider obj)
    {
        var player = obj.GetComponent<WindReceiver>();
        if (player)
        {
            player.Enter(this);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Extensions
{
    public static Vector3 WithoutY(this Vector3 vec)
    {
        if (vec.y == 0f) return vec;
        return Vector3.Scale(Vector3.one - Vector3.up, vec);
    }
    public static Vector3 WithoutX(this Vector3 vec)
    {
        if (vec.x == 0f) return vec;
        return Vector3.Scale(Vector3.one - Vector3.right, vec);
    }
    public static Vector3 WithoutZ(this Vector3 vec)
    {
        if (vec.z == 0f) return vec;
        return Vector3.Scale(Vector3.one - Vector3.forward, vec);
    }
    public static Vector3Int WithoutY(this Vector3Int vec)
    {
        if (vec.y == 0) return vec;
        return Vector3Int.Scale(Vector3Int.one - Vector3Int.up, vec);
    }
}

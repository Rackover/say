using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FogTweaks : MonoBehaviour
{
    [SerializeField]
    private Gradient fogGradient;

    [SerializeField]
    private AnimationCurve fogDensityCurve;

    [SerializeField]
    private AnimationCurve fogColorCurve;

    float baseFog = 0f;
    private void Awake()
    {
        baseFog = RenderSettings.fogDensity;
    }

    private void Update()
    {
        float density = fogDensityCurve.Evaluate(transform.position.y) * baseFog;
        Color color = fogGradient.Evaluate(fogColorCurve.Evaluate(transform.position.y));

        RenderSettings.fogColor = color;
        RenderSettings.fogDensity = density;
    }

    private void OnDestroy()
    {
        RenderSettings.fogDensity = baseFog;
        RenderSettings.fogColor = Color.white;
    }
}

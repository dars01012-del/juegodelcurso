using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PlayerLight : MonoBehaviour
{
    public Light2D light2D;

    public float normalIntensity = 0.9f;
    public float flicker = 0.08f;

    void Update()
    {
        light2D.intensity =
            normalIntensity +
            Mathf.PerlinNoise(Time.time * 8f,0)*flicker;
    }
}
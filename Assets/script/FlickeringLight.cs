using UnityEngine;

public class FlickeringLight : MonoBehaviour
{
    [Header("Light Settings")]
    public Light pointLight;

    [Header("Flicker Settings")]
    public float minIntensity = 1.5f;
    public float maxIntensity = 3.5f;
    public float flickerSpeed = 5f;

    private float noiseOffset;

    void Start()
    {
        // Automatically find the Light component
        if (pointLight == null)
        {
            pointLight = GetComponent<Light>();
        }

        // Give each light a different random noise position
        noiseOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        // Generate smooth Perlin Noise
        float noise = Mathf.PerlinNoise(
            noiseOffset,
            Time.time * flickerSpeed
        );

        // Convert noise value (0-1)
        // into our intensity range
        if (pointLight != null)
        {
            pointLight.intensity = Mathf.Lerp(
            minIntensity,
            maxIntensity,
            noise
            );
        }
    }
}

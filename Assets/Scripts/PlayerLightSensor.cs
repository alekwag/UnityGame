using UnityEngine;

public class PlayerLightSensor : MonoBehaviour
{
    [SerializeField] private LayerMask lightMask;
    [SerializeField] private float sampleRadius = 0.5f;

    public float GetLightLevel()
    {
        float totalLight = 0f;

        Collider[] hits = Physics.OverlapSphere(transform.position, sampleRadius, lightMask);

        foreach (var hit in hits)
        {
            Light light = hit.GetComponent<Light>();
            if (light == null || !light.enabled) continue;

            float distance = Vector3.Distance(transform.position, light.transform.position);

            float attenuation = 1f - Mathf.Clamp01(distance / light.range);

            totalLight += attenuation * light.intensity;
        }

        // Clamp to 0–1 range
        return Mathf.Clamp01(totalLight);
    }
}
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Abismo
{
    /// <summary>
    /// Parpadeo orgánico de una luz 2D (velas, braseros, el ojo del Durmiente...).
    /// Mezcla dos ruidos de distinta velocidad para que no parezca una onda regular.
    /// </summary>
    [RequireComponent(typeof(Light2D))]
    public class LightFlicker : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] float amount = 0.25f;
        [SerializeField] float speed = 7f;
        [SerializeField, Range(0f, 0.5f)] float radiusAmount = 0.06f;

        Light2D light2D;
        float baseIntensity, baseRadius, seed;

        public void Configure(float flickerAmount, float flickerSpeed)
        {
            amount = flickerAmount;
            speed = flickerSpeed;
        }

        void Awake()
        {
            light2D = GetComponent<Light2D>();
            baseIntensity = light2D.intensity;
            baseRadius = light2D.pointLightOuterRadius;
            seed = Random.value * 100f;
        }

        void Update()
        {
            float t = Time.time * speed;
            float n = Mathf.PerlinNoise(seed, t) * 0.7f + Mathf.PerlinNoise(seed + 9f, t * 2.7f) * 0.3f; // 0..1
            float k = 1f + (n - 0.5f) * 2f * amount;
            light2D.intensity = baseIntensity * k;
            if (baseRadius > 0f) light2D.pointLightOuterRadius = baseRadius * (1f + (n - 0.5f) * 2f * radiusAmount);
        }
    }
}

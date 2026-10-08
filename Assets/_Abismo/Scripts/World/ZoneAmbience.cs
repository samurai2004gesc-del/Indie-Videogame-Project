using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Abismo
{
    /// <summary>
    /// Ambientación por zonas a lo largo del nivel: según por dónde va la cámara, funde poco a poco
    /// el color e intensidad de la luz global 2D y los fondos de cada zona (Costa, Ruinas, Santuario, Arrecife).
    /// </summary>
    [DefaultExecutionOrder(150)]
    public class ZoneAmbience : MonoBehaviour
    {
        [System.Serializable]
        public class Zone
        {
            public string name;
            [Tooltip("Coordenada X (en unidades) donde empieza la zona.")]
            public float startX;
            [ColorUsage(false, true)] public Color lightColor = Color.white;
            public float lightIntensity = 0.5f;
            [Tooltip("Objeto con las capas de fondo de la zona (se funden al cambiar de zona).")]
            public GameObject backdrop;
        }

        public Light2D globalLight;
        public Zone[] zones = new Zone[0];
        [Tooltip("Ancho (unidades) del fundido entre dos zonas.")]
        public float blendWidth = 10f;

        Transform cam;
        SpriteRenderer[][] layers;
        float[][] baseAlpha;

        void Start()
        {
            var main = Camera.main;
            if (main != null) cam = main.transform;
            layers = new SpriteRenderer[zones.Length][];
            baseAlpha = new float[zones.Length][];
            for (int i = 0; i < zones.Length; i++)
            {
                var backdrop = zones[i].backdrop;
                layers[i] = backdrop != null ? backdrop.GetComponentsInChildren<SpriteRenderer>(true) : new SpriteRenderer[0];
                baseAlpha[i] = new float[layers[i].Length];
                for (int k = 0; k < layers[i].Length; k++) baseAlpha[i][k] = layers[i][k].color.a;
            }
            Apply(true);
        }

        void LateUpdate() => Apply(false);

        /// <summary>Peso (0..1) de cada zona en la posición x: 1 dentro, fundido en los bordes.</summary>
        float Weight(int i, float x)
        {
            float half = blendWidth * 0.5f;
            float start = zones[i].startX;
            float end = i + 1 < zones.Length ? zones[i + 1].startX : float.MaxValue;
            float a = i == 0 ? 1f : Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start - half, start + half, x));
            float b = end == float.MaxValue ? 1f : 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(end - half, end + half, x));
            return Mathf.Clamp01(Mathf.Min(a, b));
        }

        void Apply(bool force)
        {
            if (cam == null || zones.Length == 0) return;
            float x = cam.position.x;
            Color color = Color.black;
            float intensity = 0f, total = 0f;
            for (int i = 0; i < zones.Length; i++)
            {
                float w = Weight(i, x);
                total += w;
                color += zones[i].lightColor * w;
                intensity += zones[i].lightIntensity * w;

                var backdrop = zones[i].backdrop;
                if (backdrop == null) continue;
                bool visible = w > 0.005f;
                if (backdrop.activeSelf != visible) backdrop.SetActive(visible);
                if (!visible && !force) continue;
                for (int k = 0; k < layers[i].Length; k++)
                {
                    var sr = layers[i][k];
                    var c = sr.color;
                    c.a = baseAlpha[i][k] * w;
                    sr.color = c;
                }
            }
            if (globalLight != null && total > 0f)
            {
                globalLight.color = color / total;
                globalLight.intensity = intensity / total;
            }
        }
    }
}

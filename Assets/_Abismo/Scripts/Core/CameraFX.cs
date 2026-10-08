using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Abismo
{
    /// <summary>
    /// Efectos de post-procesado reactivos (URP): aberración cromática al recibir golpes y en las paradas,
    /// viñeta roja con poca vida y distorsión en los rugidos del jefe.
    /// Trabaja sobre una COPIA del perfil del Volume global para no modificar el asset del proyecto.
    /// </summary>
    [RequireComponent(typeof(Volume))]
    public class CameraFX : MonoBehaviour
    {
        public static CameraFX Instance { get; private set; }

        [SerializeField] float baseVignette = 0.32f;
        [SerializeField] Color baseVignetteColor = new Color(0.02f, 0.02f, 0.04f);

        ChromaticAberration chromatic;
        Vignette vignette;
        LensDistortion lens;
        float chromaticPulse, lensPulse, danger;

        void Awake()
        {
            Instance = this;
            var volume = GetComponent<Volume>();
            var profile = volume.profile; // instancia propia en tiempo de ejecución
            if (profile == null) return;
            profile.TryGet(out chromatic);
            profile.TryGet(out vignette);
            profile.TryGet(out lens);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>0..1: un golpe recibido ≈ 0.6, una parada ≈ 0.4.</summary>
        public static void Chromatic(float amount)
        {
            if (Instance != null) Instance.chromaticPulse = Mathf.Max(Instance.chromaticPulse, amount);
        }

        /// <summary>Distorsión de lente breve (rugidos, explosiones). Negativo = hacia dentro.</summary>
        public static void Warp(float amount)
        {
            if (Instance != null) Instance.lensPulse = amount;
        }

        /// <summary>0 = sano, 1 = a punto de morir: tiñe la viñeta de rojo y la hace latir.</summary>
        public static void SetDanger(float value)
        {
            if (Instance != null) Instance.danger = Mathf.Clamp01(value);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            chromaticPulse = Mathf.MoveTowards(chromaticPulse, 0f, dt * 2.2f);
            lensPulse = Mathf.MoveTowards(lensPulse, 0f, dt * 1.5f);

            if (chromatic != null) chromatic.intensity.Override(chromaticPulse);
            if (lens != null) lens.intensity.Override(lensPulse);
            if (vignette != null)
            {
                float beat = danger > 0f ? (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f)) * danger : 0f;
                vignette.intensity.Override(baseVignette + 0.18f * beat);
                vignette.color.Override(Color.Lerp(baseVignetteColor, new Color(0.45f, 0.02f, 0.04f), beat));
            }
        }
    }
}

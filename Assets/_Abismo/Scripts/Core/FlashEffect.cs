using UnityEngine;

namespace Abismo
{
    /// <summary>
    /// Hace parpadear los sprites de un color (blanco al recibir un golpe, naranja al preparar un ataque...).
    /// Necesita que los sprites usen el material "SpriteFlash" (shader Abismo/SpriteFlash).
    /// </summary>
    public class FlashEffect : MonoBehaviour
    {
        public SpriteRenderer[] renderers;

        static readonly int AmountId = Shader.PropertyToID("_FlashAmount");
        static readonly int ColorId = Shader.PropertyToID("_FlashColor");

        MaterialPropertyBlock block;
        Color flashColor = Color.white;
        float flashTime, flashDuration = 0.1f;
        Color holdColor = Color.white;
        float holdAmount;
        float lastAmount = -1f;
        Color lastColor;

        void Awake()
        {
            block = new MaterialPropertyBlock();
            if (renderers == null || renderers.Length == 0) renderers = GetComponentsInChildren<SpriteRenderer>();
        }

        /// <summary>Destello breve que se desvanece.</summary>
        public void Flash(Color color, float duration)
        {
            flashColor = color;
            flashDuration = Mathf.Max(0.01f, duration);
            flashTime = flashDuration;
        }

        /// <summary>Tinte mantenido (por ejemplo, mientras un enemigo carga un ataque).</summary>
        public void Hold(Color color, float amount)
        {
            holdColor = color;
            holdAmount = Mathf.Clamp01(amount);
        }

        public void ClearHold() => holdAmount = 0f;

        public void ResetFlash()
        {
            flashTime = 0f;
            holdAmount = 0f;
            Apply(Color.white, 0f);
        }

        void LateUpdate()
        {
            float amount = 0f;
            Color color = holdColor;

            if (flashTime > 0f)
            {
                flashTime -= Time.deltaTime;
                amount = Mathf.Clamp01(flashTime / flashDuration);
                color = flashColor;
            }
            if (holdAmount > amount)
            {
                amount = holdAmount;
                color = holdColor;
            }

            if (!Mathf.Approximately(amount, lastAmount) || color != lastColor) Apply(color, amount);
        }

        void Apply(Color color, float amount)
        {
            if (block == null) block = new MaterialPropertyBlock();
            lastAmount = amount;
            lastColor = color;
            foreach (var r in renderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(block);
                block.SetColor(ColorId, color);
                block.SetFloat(AmountId, amount);
                r.SetPropertyBlock(block);
            }
        }
    }
}

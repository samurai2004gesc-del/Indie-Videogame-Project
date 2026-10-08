using UnityEngine;

namespace Abismo
{
    /// <summary>
    /// Hace que un personaje destelle de un color (blanco al recibir un golpe, naranja cuando prepara un
    /// ataque que se puede parar, rojo cuando es imparable...).
    ///
    /// Funciona con la iluminación 2D de URP: el cuerpo usa el material iluminado normal y este componente
    /// dibuja ENCIMA una silueta del mismo fotograma con un material sin iluminar (shader Abismo/SpriteSilhouette).
    /// </summary>
    public class FlashEffect : MonoBehaviour
    {
        [Tooltip("El SpriteRenderer del cuerpo (su fotograma actual se copia en la silueta).")]
        public SpriteRenderer source;
        [Tooltip("SpriteRenderer hijo con el material de silueta. Lo crea el constructor automático.")]
        public SpriteRenderer overlay;

        Color flashColor = Color.white;
        float flashTime, flashDuration = 0.1f;
        Color holdColor = Color.white;
        float holdAmount;

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
            if (overlay != null) overlay.enabled = false;
        }

        void LateUpdate()
        {
            if (overlay == null || source == null) return;

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

            bool visible = amount > 0.01f && source.enabled;
            overlay.enabled = visible;
            if (!visible) return;
            overlay.sprite = source.sprite;
            overlay.flipX = source.flipX;
            overlay.color = new Color(color.r, color.g, color.b, amount);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Abismo
{
    /// <summary>
    /// Dibuja encima de un sprite iluminado sus partes que brillan por sí mismas (ojos, brasas, runas).
    /// Cada fotograma del sprite tiene su "máscara de brillo"; aquí se elige la que corresponde al fotograma
    /// que esté mostrando el Animator. Así brillan aunque la escena esté a oscuras, y el bloom las realza.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public class SpriteGlow : MonoBehaviour
    {
        public SpriteRenderer source;
        public SpriteRenderer overlay;
        [Tooltip("Fotogramas del sprite normal...")]
        public Sprite[] from;
        [Tooltip("...y su máscara de brillo (misma posición en la lista; vacío = ese fotograma no brilla).")]
        public Sprite[] to;
        [Range(0f, 1f)] public float strength = 1f;

        readonly Dictionary<Sprite, Sprite> map = new Dictionary<Sprite, Sprite>();
        Sprite shown;

        void Awake()
        {
            int n = Mathf.Min(from != null ? from.Length : 0, to != null ? to.Length : 0);
            for (int i = 0; i < n; i++)
            {
                if (from[i] != null && to[i] != null) map[from[i]] = to[i];
            }
        }

        void LateUpdate()
        {
            if (source == null || overlay == null) return;
            if (source.sprite != shown)
            {
                shown = source.sprite;
                overlay.sprite = shown != null && map.TryGetValue(shown, out var glow) ? glow : null;
            }
            overlay.flipX = source.flipX;
            overlay.enabled = source.enabled && overlay.sprite != null && strength > 0.01f;
            var c = source.color;
            overlay.color = new Color(1f, 1f, 1f, c.a * strength);
        }
    }
}

using UnityEngine;

namespace Abismo
{
    /// <summary>
    /// Partículas sencillas hechas por código (chispas, sangre/icor, polvo, destellos).
    /// No necesitan ningún asset: crean pequeños cuadrados de color que se mueven y desvanecen.
    /// </summary>
    public static class Effects
    {
        static Sprite square;

        static Sprite Square
        {
            get
            {
                if (square == null)
                {
                    var tex = new Texture2D(2, 2) { filterMode = FilterMode.Point };
                    tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
                    tex.Apply();
                    square = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 16f);
                }
                return square;
            }
        }

        static Material Additive => GameManager.Instance != null ? GameManager.Instance.additiveMaterial : null;

        public static void Burst(Vector2 position, Color color, int count, float speed, float lifetime,
                                 float gravity, float size, bool additive, Vector2 bias = default)
        {
            Material additiveMat = additive ? Additive : null;
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Particula");
                go.transform.position = position;
                float s = size * Random.Range(0.6f, 1.2f);
                go.transform.localScale = new Vector3(s, s, 1f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = Square;
                sr.color = color;
                sr.sortingOrder = 40;
                if (additiveMat != null) sr.sharedMaterial = additiveMat;

                Vector2 dir = (Random.insideUnitCircle.normalized + bias).normalized;
                go.AddComponent<SimpleParticle>().Init(dir * speed * Random.Range(0.4f, 1f),
                                                       lifetime * Random.Range(0.6f, 1.1f), gravity);
            }
        }

        /// <summary>Salpicadura de icor al golpear a una criatura.</summary>
        public static void Blood(Vector2 position, Color color, int count, float direction) =>
            Burst(position, color, count, 7f, 0.5f, 25f, 1f, false, new Vector2(direction * 1.2f, 0.5f));

        /// <summary>Chispa brillante en el punto de impacto.</summary>
        public static void HitSpark(Vector2 position) =>
            Burst(position, new Color(1f, 0.95f, 0.8f), 6, 9f, 0.15f, 0f, 0.8f, true);

        /// <summary>Estallido al morir una criatura.</summary>
        public static void DeathBurst(Vector2 position, Color color)
        {
            Burst(position, color, 24, 8f, 0.8f, 18f, 1.4f, false);
            Burst(position, new Color(0.6f, 1f, 0.8f), 10, 3f, 1.2f, -2f, 1f, true);
        }

        /// <summary>Polvo a los pies (saltar, aterrizar, esquivar).</summary>
        public static void Dust(Vector2 position, int count) =>
            Burst(position + Vector2.up * 0.05f, new Color(0.45f, 0.5f, 0.48f, 0.8f), count, 2.5f, 0.35f, -1f, 0.9f,
                  false, new Vector2(0f, 0.8f));

        /// <summary>Motas brillantes que flotan hacia arriba (curación, altares, fragmentos).</summary>
        public static void Sparkle(Vector2 position, Color color, int count) =>
            Burst(position, color, count, 2f, 0.9f, -3f, 0.7f, true);
    }
}

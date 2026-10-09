using UnityEngine;

namespace Abismo
{
    /// <summary>
    /// Efectos de combate: animaciones pintadas (chispazo, sangre, estrella de daño, polvo, Signo Arcano...) de la
    /// <see cref="EffectLibrary"/> y, encima, partículas sencillas hechas por código (cuadraditos de color que se
    /// mueven y desvanecen). Si la biblioteca no existe, quedan solo las partículas.
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
                    var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                    tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
                    tex.Apply();
                    square = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), GameLayers.PixelsPerUnit);
                }
                return square;
            }
        }

        static Material Additive => GameManager.Instance != null ? GameManager.Instance.additiveMaterial : null;
        static Material Unlit => GameManager.Instance != null ? GameManager.Instance.unlitMaterial : null;

        public static void Burst(Vector2 position, Color color, int count, float speed, float lifetime,
                                 float gravity, float size, bool additive, Vector2 bias = default)
        {
            // Las partículas no se iluminan con las luces 2D: brillan por sí mismas (y el bloom las realza).
            Material material = additive ? Additive : Unlit;
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
                if (material != null) sr.sharedMaterial = material;

                Vector2 dir = (Random.insideUnitCircle.normalized + bias).normalized;
                go.AddComponent<SimpleParticle>().Init(dir * speed * Random.Range(0.4f, 1f),
                                                       lifetime * Random.Range(0.6f, 1.1f), gravity);
            }
        }

        /// <summary>
        /// Reproduce una vez la animación <paramref name="name"/> de la biblioteca (dibujada mirando a la derecha;
        /// <paramref name="direction"/> negativo la voltea). Devuelve false si no existe.
        /// </summary>
        public static bool Play(string name, Vector2 position, float direction = 1f, float scale = 1f, int order = 45,
                                Vector2 drift = default, float rotation = 0f)
        {
            var library = EffectLibrary.Instance;
            if (library == null || !library.TryGet(name, out var clip)) return false;
            var go = new GameObject(name);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0f, 0f, rotation);
            go.transform.localScale = new Vector3(direction < 0f ? -scale : scale, scale, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = order;
            if (Unlit != null) sr.sharedMaterial = Unlit;
            go.AddComponent<OneShotSprite>().Init(clip.frames, clip.fps, drift);
            return true;
        }

        /// <summary>Salpicadura de sangre/icor al golpear a una criatura (hacia donde va el golpe).</summary>
        public static void Blood(Vector2 position, Color color, int count, float direction)
        {
            bool painted = Play("fx_sangre", position, direction, count > 12 ? 1.25f : 1f, 44);
            Burst(position, color, painted ? count / 2 : count, 7f, 0.5f, 25f, 1f, false, new Vector2(direction * 1.2f, 0.5f));
        }

        /// <summary>Chispazo en el punto de impacto (amarillo-naranja con núcleo blanco, como en Blasphemous).</summary>
        public static void HitSpark(Vector2 position, float direction = 1f, bool heavy = false)
        {
            bool painted = Play("fx_chispazo", position, direction, heavy ? 1.3f : 1f, 46, default, Random.Range(-12f, 12f));
            Burst(position, new Color(1f, 0.85f, 0.5f), painted ? 4 : 6, 9f, 0.15f, 0f, 0.8f, true, new Vector2(direction * 0.6f, 0f));
        }

        /// <summary>Estrella blanca cuando el jugador recibe un golpe.</summary>
        public static void HurtStar(Vector2 position)
        {
            if (!Play("fx_estrella", position, 1f, 1f, 47))
                Burst(position, Color.white, 10, 8f, 0.2f, 0f, 1f, true);
        }

        /// <summary>Chispas de un golpe que choca contra un escudo o una parada.</summary>
        public static void BlockSpark(Vector2 position, float direction)
        {
            Play("fx_bloqueo", position, direction, 1f, 46);
            Burst(position, new Color(0.8f, 0.92f, 1f), 8, 9f, 0.2f, 12f, 0.8f, true, new Vector2(direction, 0.4f));
        }

        /// <summary>El remate de una ejecución: tajo enorme, destello y sangre.</summary>
        public static void Execution(Vector2 position, float direction, Color ichor)
        {
            Play("fx_ejecucion", position, direction, 1f, 48);
            Play("fx_sangre", position + new Vector2(direction * 0.3f, 0f), direction, 1.5f, 44);
            Burst(position, ichor, 26, 10f, 0.7f, 25f, 1.2f, false, new Vector2(direction * 1.4f, 0.6f));
            Burst(position, new Color(1f, 0.9f, 0.6f), 10, 12f, 0.2f, 0f, 0.9f, true);
        }

        /// <summary>Estallido del Signo Arcano (al conjurar y al impactar).</summary>
        public static void ElderSign(Vector2 position, float scale = 1f)
        {
            if (!Play("fx_signo", position, 1f, scale, 46))
                Burst(position, new Color(0.5f, 1f, 0.85f), 12, 6f, 0.4f, 0f, 1f, true);
        }

        /// <summary>Estallido al morir una criatura: se deshace en humo de icor y motas espectrales.</summary>
        public static void DeathBurst(Vector2 position, Color color)
        {
            bool painted = Play("fx_muerte", position + Vector2.down * 0.6f, 1f, 1.2f, 43);
            Burst(position, color, painted ? 14 : 24, 8f, 0.8f, 18f, 1.4f, false);
            Burst(position, new Color(0.6f, 1f, 0.8f), 10, 3f, 1.2f, -2f, 1f, true);
        }

        /// <summary>Polvo a los pies (saltar, aterrizar, esquivar).</summary>
        public static void Dust(Vector2 position, int count, float direction = 0f)
        {
            bool painted = Play("fx_polvo", position, direction < 0f ? -1f : 1f, count >= 8 ? 1.4f : 1f, 39);
            Burst(position + Vector2.up * 0.05f, new Color(0.45f, 0.5f, 0.48f, 0.8f), painted ? count / 2 : count, 2.5f, 0.35f, -1f, 0.9f,
                  false, new Vector2(0f, 0.8f));
        }

        /// <summary>Motas brillantes que flotan hacia arriba (curación, altares, fragmentos).</summary>
        public static void Sparkle(Vector2 position, Color color, int count) =>
            Burst(position, color, count, 2f, 0.9f, -3f, 0.7f, true);
    }
}

using UnityEngine;

namespace Abismo.EditorTools
{
    /// <summary>
    /// Rampas de color "a lo pixel artist": de oscuro a claro, desplazando el tono hacia el azul/violeta
    /// en las sombras y hacia el amarillo cálido en las luces, en vez de oscurecer/aclarar sin más.
    /// Es una de las claves para que el pixel art no parezca "de programador".
    /// </summary>
    public static class Ramp
    {
        /// <param name="baseColor">Color medio del material.</param>
        /// <param name="steps">Número de tonos (4-6 es lo habitual en pixel art).</param>
        /// <param name="hueShift">Cuánto gira el tono hacia frío (sombras) y cálido (luces). 0..0.15</param>
        /// <param name="darkest">Valor (brillo) del tono más oscuro respecto al base (0..1).</param>
        /// <param name="lightest">Valor del tono más claro respecto al base (puede pasar de 1).</param>
        public static Color32[] Make(Color32 baseColor, int steps = 5, float hueShift = 0.08f, float darkest = 0.32f, float lightest = 1.45f)
        {
            RgbToHsv(baseColor, out float h, out float s, out float v);
            var ramp = new Color32[steps];
            for (int i = 0; i < steps; i++)
            {
                float t = steps == 1 ? 0.5f : i / (float)(steps - 1); // 0 = sombra, 1 = luz
                float value = Mathf.Clamp01(v * Mathf.Lerp(darkest, lightest, t));
                // Sombras hacia azul-violeta (0.70), luces hacia amarillo cálido (0.13).
                float hue = t < 0.5f
                    ? TowardHue(h, 0.70f, hueShift * (0.5f - t) * 2f)
                    : TowardHue(h, 0.13f, hueShift * (t - 0.5f) * 2f);
                // Más saturado en los medios tonos, menos en brillos (como la luz real sobre pintura).
                float sat = Mathf.Clamp01(s * Mathf.Lerp(1.08f, 0.78f, t) * (t < 0.15f ? 0.9f : 1f));
                ramp[i] = HsvToRgb(hue, sat, value, baseColor.a);
            }
            return ramp;
        }

        public static Color32[] Make(string hex, int steps = 5, float hueShift = 0.08f, float darkest = 0.32f, float lightest = 1.45f) =>
            Make(PixelCanvas.Hex(hex), steps, hueShift, darkest, lightest);

        static float TowardHue(float h, float target, float amount)
        {
            float d = target - h;
            if (d > 0.5f) d -= 1f;
            if (d < -0.5f) d += 1f;
            float result = h + d * Mathf.Clamp01(amount);
            return result < 0f ? result + 1f : (result > 1f ? result - 1f : result);
        }

        public static void RgbToHsv(Color32 c, out float h, out float s, out float v)
        {
            float r = c.r / 255f, g = c.g / 255f, b = c.b / 255f;
            float max = Mathf.Max(r, Mathf.Max(g, b)), min = Mathf.Min(r, Mathf.Min(g, b));
            float delta = max - min;
            v = max;
            s = max <= 0f ? 0f : delta / max;
            if (delta <= 0f)
            {
                h = 0f;
                return;
            }
            if (max == r) h = (g - b) / delta / 6f;
            else if (max == g) h = ((b - r) / delta + 2f) / 6f;
            else h = ((r - g) / delta + 4f) / 6f;
            if (h < 0f) h += 1f;
        }

        public static Color32 HsvToRgb(float h, float s, float v, byte alpha = 255)
        {
            h = (h % 1f + 1f) % 1f;
            float c = v * s;
            float x = c * (1f - Mathf.Abs(h * 6f % 2f - 1f));
            float m = v - c;
            float r, g, b;
            int sector = Mathf.FloorToInt(h * 6f) % 6;
            switch (sector)
            {
                case 0: r = c; g = x; b = 0f; break;
                case 1: r = x; g = c; b = 0f; break;
                case 2: r = 0f; g = c; b = x; break;
                case 3: r = 0f; g = x; b = c; break;
                case 4: r = x; g = 0f; b = c; break;
                default: r = c; g = 0f; b = x; break;
            }
            return new Color32(ToByte(r + m), ToByte(g + m), ToByte(b + m), alpha);
        }

        static byte ToByte(float f) => (byte)Mathf.Clamp(Mathf.RoundToInt(f * 255f), 0, 255);

        /// <summary>Oscurece un color hacia un tono frío (para contornos y sombras de contacto).</summary>
        public static Color32 Shadow(Color32 c, float amount)
        {
            RgbToHsv(c, out float h, out float s, out float v);
            return HsvToRgb(TowardHue(h, 0.72f, amount * 0.5f), Mathf.Clamp01(s * (1f + amount * 0.2f)), v * (1f - amount), c.a);
        }
    }
}

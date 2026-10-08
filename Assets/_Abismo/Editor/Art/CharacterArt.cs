using System.Collections.Generic;
using UnityEngine;

namespace Abismo.EditorTools
{
    /// <summary>Una animación: cuántos fotogramas, a qué velocidad y cómo se dibuja cada uno.</summary>
    public sealed class AnimSpec
    {
        public string Name;
        public int Frames;
        public float Fps;
        public bool Loop;
        public System.Func<int, ShadedCanvas> Draw;

        public AnimSpec(string name, int frames, float fps, bool loop, System.Func<int, ShadedCanvas> draw)
        {
            Name = name;
            Frames = frames;
            Fps = fps;
            Loop = loop;
            Draw = draw;
        }

        public float Duration => Frames / Fps;
    }

    /// <summary>
    /// Arte de un personaje animado. El constructor (AbismoBuilder) dibuja todas sus animaciones en una
    /// hoja de sprites (una fila por animación, una columna por fotograma), genera su normal map,
    /// los clips de animación y el Animator Controller.
    ///
    /// Si algún día dibujas tus propios sprites, respeta la misma rejilla (tamaño de celda y orden de filas)
    /// y sustituye el PNG: el resto seguirá funcionando.
    /// </summary>
    public abstract class CharacterArt
    {
        /// <summary>Nombre de archivo (sin espacios), p. ej. "ahogado".</summary>
        public abstract string Id { get; }
        public abstract int FrameWidth { get; }
        public abstract int FrameHeight { get; }
        /// <summary>Punto de apoyo en píxeles desde la esquina inferior izquierda de la celda (normalmente los pies).</summary>
        public abstract Vector2 Pivot { get; }
        public abstract List<AnimSpec> Animations();

        protected ShadedCanvas NewFrame() => new ShadedCanvas(FrameWidth, FrameHeight, Pivot.x, Pivot.y);
    }

    public enum Ease { Linear, In, Out, InOut, Hold }

    /// <summary>
    /// Una pose: un conjunto de valores con nombre (ángulos de articulaciones, desplazamientos...).
    /// Lo que no se define vale 0.
    /// </summary>
    public sealed class Pose
    {
        readonly Dictionary<string, float> values = new Dictionary<string, float>();

        public float this[string key]
        {
            get => values.TryGetValue(key, out var v) ? v : 0f;
            set => values[key] = value;
        }

        public IEnumerable<string> Keys => values.Keys;

        public Pose Clone()
        {
            var p = new Pose();
            foreach (var kv in values) p.values[kv.Key] = kv.Value;
            return p;
        }

        /// <summary>Copia con algunos valores cambiados: pose.With(("torso", 10), ("cabeza", -5)).</summary>
        public Pose With(params (string key, float value)[] changes)
        {
            var p = Clone();
            foreach (var c in changes) p.values[c.key] = c.value;
            return p;
        }

        public static Pose Lerp(Pose a, Pose b, float t)
        {
            var p = new Pose();
            foreach (var k in a.values.Keys) p.values[k] = Mathf.Lerp(a[k], b[k], t);
            foreach (var k in b.values.Keys)
            {
                if (!p.values.ContainsKey(k)) p.values[k] = Mathf.Lerp(a[k], b[k], t);
            }
            return p;
        }
    }

    /// <summary>Poses clave en ciertos fotogramas; entre medias se interpola con suavizado.</summary>
    public sealed class Keyframes
    {
        readonly List<(float frame, Pose pose, Ease ease)> keys = new List<(float, Pose, Ease)>();

        /// <param name="ease">Cómo se llega DESDE esta clave a la siguiente.</param>
        public Keyframes Key(float frame, Pose pose, Ease ease = Ease.InOut)
        {
            keys.Add((frame, pose, ease));
            keys.Sort((a, b) => a.frame.CompareTo(b.frame));
            return this;
        }

        public Pose Evaluate(float frame)
        {
            if (keys.Count == 0) return new Pose();
            if (frame <= keys[0].frame) return keys[0].pose.Clone();
            for (int i = 0; i < keys.Count - 1; i++)
            {
                var a = keys[i];
                var b = keys[i + 1];
                if (frame > b.frame) continue;
                float t = (frame - a.frame) / Mathf.Max(0.0001f, b.frame - a.frame);
                return Pose.Lerp(a.pose, b.pose, Apply(a.ease, t));
            }
            return keys[keys.Count - 1].pose.Clone();
        }

        public static float Apply(Ease ease, float t)
        {
            t = Mathf.Clamp01(t);
            switch (ease)
            {
                case Ease.In: return t * t;
                case Ease.Out: return 1f - (1f - t) * (1f - t);
                case Ease.InOut: return t * t * (3f - 2f * t);
                case Ease.Hold: return 0f;
                default: return t;
            }
        }
    }

    /// <summary>Ayudas de cinemática para rigs 2D.</summary>
    public static class Rig
    {
        /// <summary>
        /// Extremidad: desde <paramref name="from"/> con un ángulo en grados donde 0 = colgando hacia abajo,
        /// 90 = apuntando hacia delante (derecha), 180 = hacia arriba, -90 = hacia atrás.
        /// </summary>
        public static Vector2 Limb(Vector2 from, float angle, float length)
        {
            float a = angle * Mathf.Deg2Rad;
            return new Vector2(from.x + Mathf.Sin(a) * length, from.y - Mathf.Cos(a) * length);
        }

        /// <summary>Dirección con ángulo matemático (0 = derecha, 90 = arriba).</summary>
        public static Vector2 Dir(float angle, float length = 1f)
        {
            float a = angle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(a) * length, Mathf.Sin(a) * length);
        }

        public static Vector2 Add(Vector2 a, Vector2 b) => new Vector2(a.x + b.x, a.y + b.y);
        public static Vector2 Sub(Vector2 a, Vector2 b) => new Vector2(a.x - b.x, a.y - b.y);
        public static Vector2 Scale(Vector2 a, float s) => new Vector2(a.x * s, a.y * s);
        public static Vector2 Mix(Vector2 a, Vector2 b, float t) => new Vector2(Mathf.Lerp(a.x, b.x, t), Mathf.Lerp(a.y, b.y, t));
        public static Vector2 V(float x, float y) => new Vector2(x, y);

        /// <summary>Rota un punto alrededor de un pivote (grados, sentido antihorario).</summary>
        public static Vector2 Rotate(Vector2 p, Vector2 pivot, float angle)
        {
            float a = angle * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
            float x = p.x - pivot.x, y = p.y - pivot.y;
            return new Vector2(pivot.x + x * c - y * s, pivot.y + x * s + y * c);
        }

        /// <summary>Muestrea una curva de Bézier cuadrática (útil para capas, tentáculos y estelas).</summary>
        public static Vector2[] Bezier(Vector2 a, Vector2 control, Vector2 b, int samples)
        {
            var points = new Vector2[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)(samples - 1), u = 1f - t;
                points[i] = new Vector2(u * u * a.x + 2f * u * t * control.x + t * t * b.x,
                                        u * u * a.y + 2f * u * t * control.y + t * t * b.y);
            }
            return points;
        }

        /// <summary>
        /// Estela de espada ("smear frame"): un arco creciente entre dos ángulos alrededor de un centro.
        /// Ángulos matemáticos (0 = derecha, 90 = arriba). Devuelve una forma para ShadedCanvas.Custom.
        /// </summary>
        public static System.Func<float, float, (bool, N3)> SmearArc(Vector2 center, float innerRadius, float outerRadius,
                                                                   float fromAngle, float toAngle)
        {
            float a0 = Mathf.Min(fromAngle, toAngle), a1 = Mathf.Max(fromAngle, toAngle);
            bool leadAtEnd = toAngle >= fromAngle;
            return (px, py) =>
            {
                float dx = px - center.x, dy = py - center.y;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                if (r > outerRadius || r < innerRadius * 0.6f) return (false, N3.Front);
                float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                while (ang < a0 - 180f) ang += 360f;
                while (ang > a0 + 180f) ang -= 360f;
                if (ang < a0 || ang > a1) return (false, N3.Front);
                // 0 en la cola, 1 en la punta: la estela es fina en la cola y gruesa en la punta.
                float t = (ang - a0) / Mathf.Max(0.001f, a1 - a0);
                if (!leadAtEnd) t = 1f - t;
                float inner = Mathf.Lerp(outerRadius - 2f, innerRadius, t * t);
                return (r >= inner, N3.Front);
            };
        }
    }
}

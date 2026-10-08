using System.Collections.Generic;
using UnityEngine;

namespace Abismo
{
    public enum SfxId
    {
        Swing, HeavySwing, Hit, HeavyHit, Hurt, Jump, Land, Dash, ParryStance, Parry,
        SpellCharge, Spell, Heal, Pickup, EnemySwing, EnemyDie, Chant, Altar, Roar,
        Death, Gate, Tentacle, Fragment, Message
    }

    /// <summary>
    /// Efectos de sonido generados por código (síntesis) más un zumbido ambiental en bucle.
    /// Así el prototipo suena sin necesitar ficheros de audio. Cuando tengas sonidos reales,
    /// puedes sustituir esta clase por AudioClips normales.
    /// Uso: <c>Sfx.Play(SfxId.Hit);</c>
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public class Sfx : MonoBehaviour
    {
        public static Sfx Instance { get; private set; }

        [SerializeField, Range(0f, 1f)] float masterVolume = 0.8f;
        [SerializeField, Range(0f, 1f)] float ambienceVolume = 0.45f;

        const int Rate = 22050;
        const float TwoPi = Mathf.PI * 2f;

        readonly Dictionary<SfxId, AudioClip> clips = new Dictionary<SfxId, AudioClip>();
        readonly System.Random rng = new System.Random(7);
        AudioSource[] voices;
        int nextVoice;
        AudioSource ambience;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            voices = new AudioSource[12];
            for (int i = 0; i < voices.Length; i++)
            {
                voices[i] = gameObject.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
            }

            BuildClips();

            ambience = gameObject.AddComponent<AudioSource>();
            ambience.clip = BuildAmbience();
            ambience.loop = true;
            ambience.playOnAwake = false;
            ambience.volume = ambienceVolume * masterVolume;
            ambience.Play();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static void Play(SfxId id, float volume = 1f, float pitchJitter = 0.06f)
        {
            if (Instance != null) Instance.PlayInternal(id, volume, pitchJitter);
        }

        void PlayInternal(SfxId id, float volume, float pitchJitter)
        {
            if (!clips.TryGetValue(id, out var clip)) return;
            var source = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;
            source.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            source.PlayOneShot(clip, volume * masterVolume);
        }

        // ------------------------------------------------------------------
        // Catálogo de sonidos
        // ------------------------------------------------------------------

        void BuildClips()
        {
            Add(SfxId.Swing, Whoosh(0.18f, 600f, 3500f, 0.05f), 0.45f);
            Add(SfxId.HeavySwing, Whoosh(0.28f, 300f, 2200f, 0.09f), 0.55f);
            Add(SfxId.EnemySwing, Whoosh(0.25f, 200f, 1500f, 0.08f), 0.5f);
            Add(SfxId.Hit, Mix(NoiseBurst(0.12f, 4000f, 0.025f), Thump(0.2f, 160f, 45f, 0.06f), 1f), 0.8f);
            Add(SfxId.HeavyHit, Mix(NoiseBurst(0.2f, 2500f, 0.05f), Thump(0.45f, 120f, 32f, 0.14f), 1.2f), 1f);
            Add(SfxId.Hurt, Hurt(), 0.7f);
            Add(SfxId.Jump, Mix(NoiseBurst(0.12f, 1500f, 0.04f), Sweep(0.1f, 180f, 340f, 0.04f), 0.3f), 0.3f);
            Add(SfxId.Land, Mix(NoiseBurst(0.1f, 600f, 0.03f), Thump(0.12f, 90f, 40f, 0.04f), 1f), 0.4f);
            Add(SfxId.Dash, Whoosh(0.3f, 2000f, 300f, 0.1f), 0.5f);
            Add(SfxId.ParryStance, Metallic(0.12f, 2400f, new[] { 1f, 1.5f }, new[] { 0.03f, 0.02f }), 0.2f);
            Add(SfxId.Parry, Mix(Metallic(0.9f, 880f, new[] { 1f, 2.76f, 5.4f, 8.93f }, new[] { 0.35f, 0.25f, 0.15f, 0.08f }),
                                 NoiseBurst(0.05f, 8000f, 0.01f), 0.8f), 1f);
            Add(SfxId.SpellCharge, SpellCharge(), 0.35f);
            Add(SfxId.Spell, Spell(), 0.7f);
            Add(SfxId.Heal, Arpeggio(0.9f, new[] { 392f, 466.2f, 587.3f, 698.5f }, 0.09f, 0.35f), 0.5f);
            Add(SfxId.Pickup, Arpeggio(0.2f, new[] { 1046.5f, 1568f }, 0.06f, 0.05f), 0.3f);
            Add(SfxId.EnemyDie, Growl(0.8f, 800f, 28f, 130f, 40f, 0.01f, 0.25f), 0.8f);
            Add(SfxId.Chant, Chant(), 0.45f);
            Add(SfxId.Altar, Chord(2.4f, new[] { 110f, 164.8f, 233.1f, 277.2f }, 0.3f, 1f), 0.6f);
            Add(SfxId.Roar, Growl(1.6f, 400f, 35f, 70f, 45f, 0.15f, 0.6f), 1f);
            Add(SfxId.Death, Growl(2.2f, 300f, 8f, 110f, 38f, 0.05f, 0.8f), 0.9f);
            Add(SfxId.Gate, Mix(Metallic(0.9f, 140f, new[] { 1f, 2.05f, 3.1f, 4.7f }, new[] { 0.3f, 0.2f, 0.12f, 0.08f }),
                                NoiseBurst(0.15f, 3000f, 0.04f), 0.7f), 0.8f);
            Add(SfxId.Tentacle, Mix(Whoosh(0.35f, 2000f, 300f, 0.08f), Thump(0.35f, 100f, 35f, 0.12f), 1f), 0.8f);
            Add(SfxId.Fragment, Arpeggio(1f, new[] { 523.3f, 659.3f, 784f, 1046.5f }, 0.12f, 0.4f), 0.5f);
            Add(SfxId.Message, Metallic(0.8f, 660f, new[] { 1f, 1.5f }, new[] { 0.35f, 0.2f }), 0.35f);
        }

        void Add(SfxId id, float[] data, float gain)
        {
            float peak = 0f;
            for (int i = 0; i < data.Length; i++) peak = Mathf.Max(peak, Mathf.Abs(data[i]));
            float k = peak > 0.0001f ? 0.9f * gain / peak : 0f;
            int fade = Mathf.Min(data.Length, 200);
            for (int i = 0; i < data.Length; i++)
            {
                data[i] *= k;
                int fromEnd = data.Length - 1 - i;
                if (fromEnd < fade) data[i] *= fromEnd / (float)fade; // evita chasquidos al final
            }

            var clip = AudioClip.Create(id.ToString(), data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            clips[id] = clip;
        }

        // ------------------------------------------------------------------
        // Bloques de síntesis
        // ------------------------------------------------------------------

        float Noise() => (float)(rng.NextDouble() * 2.0 - 1.0);
        static int Length(float seconds) => Mathf.CeilToInt(seconds * Rate);
        static float Cutoff(float hz) => 1f - Mathf.Exp(-TwoPi * hz / Rate);

        static float Env(float t, float attack, float decay) =>
            t < attack ? t / Mathf.Max(attack, 0.0001f) : Mathf.Exp(-(t - attack) / Mathf.Max(decay, 0.0001f));

        static float[] Mix(float[] a, float[] b, float gainB)
        {
            var result = new float[Mathf.Max(a.Length, b.Length)];
            for (int i = 0; i < result.Length; i++)
            {
                float va = i < a.Length ? a[i] : 0f;
                float vb = i < b.Length ? b[i] : 0f;
                result[i] = va + vb * gainB;
            }
            return result;
        }

        /// <summary>Ruido filtrado cuyo brillo sube y baja: un "fsss" de espada.</summary>
        float[] Whoosh(float duration, float fromHz, float toHz, float decay)
        {
            var d = new float[Length(duration)];
            float lp1 = 0f, lp2 = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float p = t / duration;
                float cut = Mathf.Lerp(fromHz, toHz, p);
                lp1 += Cutoff(cut) * (Noise() - lp1);
                lp2 += Cutoff(cut * 0.3f) * (lp1 - lp2);
                d[i] = (lp1 - lp2) * Env(t, duration * 0.3f, decay) * Mathf.Sin(Mathf.PI * Mathf.Clamp01(p));
            }
            return d;
        }

        float[] NoiseBurst(float duration, float cutHz, float decay)
        {
            var d = new float[Length(duration)];
            float lp = 0f;
            float a = Cutoff(cutHz);
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                lp += a * (Noise() - lp);
                d[i] = lp * Env(t, 0.002f, decay);
            }
            return d;
        }

        /// <summary>Golpe grave cuyo tono cae rápido (el "pum" de un impacto).</summary>
        static float[] Thump(float duration, float startHz, float endHz, float decay)
        {
            var d = new float[Length(duration)];
            float phase = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float f = Mathf.Lerp(endHz, startHz, Mathf.Exp(-t * 25f));
                phase += TwoPi * f / Rate;
                d[i] = Mathf.Sin(phase) * Env(t, 0.002f, decay);
            }
            return d;
        }

        static float[] Sweep(float duration, float fromHz, float toHz, float decay)
        {
            var d = new float[Length(duration)];
            float phase = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                phase += TwoPi * Mathf.Lerp(fromHz, toHz, t / duration) / Rate;
                d[i] = Mathf.Sin(phase) * Env(t, 0.005f, decay);
            }
            return d;
        }

        /// <summary>Parciales inarmónicos: suena a metal, campana o reja.</summary>
        static float[] Metallic(float duration, float baseHz, float[] ratios, float[] decays)
        {
            var d = new float[Length(duration)];
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float v = 0f;
                for (int p = 0; p < ratios.Length; p++)
                {
                    v += Mathf.Sin(TwoPi * baseHz * ratios[p] * t) * Env(t, 0.001f, decays[p]) / (p + 1);
                }
                d[i] = v;
            }
            return d;
        }

        static float[] Arpeggio(float duration, float[] notes, float step, float decay)
        {
            var d = new float[Length(duration)];
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float v = 0f;
                for (int n = 0; n < notes.Length; n++)
                {
                    float tn = t - n * step;
                    if (tn < 0f) continue;
                    float w = Mathf.Sin(TwoPi * notes[n] * tn) + 0.3f * Mathf.Sin(TwoPi * notes[n] * 2f * tn);
                    v += w * Env(tn, 0.01f, decay);
                }
                d[i] = v;
            }
            return d;
        }

        static float[] Chord(float duration, float[] notes, float attack, float decay)
        {
            var d = new float[Length(duration)];
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float v = 0f;
                foreach (float f in notes)
                {
                    v += Mathf.Sin(TwoPi * f * t) + Mathf.Sin(TwoPi * f * 1.004f * t);
                }
                d[i] = v * Env(t, attack, decay);
            }
            return d;
        }

        float[] Hurt()
        {
            var d = new float[Length(0.3f)];
            float phase = 0f, lp = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float f = Mathf.Lerp(240f, 90f, t / 0.3f) * (1f + 0.05f * Mathf.Sin(TwoPi * 30f * t));
                phase += TwoPi * f / Rate;
                float raw = Mathf.Sign(Mathf.Sin(phase)) * 0.5f + Noise() * 0.3f;
                lp += Cutoff(1800f) * (raw - lp);
                d[i] = lp * Env(t, 0.005f, 0.09f);
            }
            return d;
        }

        float[] SpellCharge()
        {
            var d = new float[Length(0.32f)];
            float phase = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                phase += TwoPi * Mathf.Lerp(150f, 420f, t / 0.32f) / Rate;
                d[i] = Mathf.Sin(phase) * (0.6f + 0.4f * Mathf.Sin(TwoPi * 18f * t)) * Env(t, 0.2f, 0.1f);
            }
            return d;
        }

        /// <summary>Síntesis FM: un tono extraño y "arcano".</summary>
        float[] Spell()
        {
            var d = new float[Length(0.7f)];
            float carrier = 0f, modulator = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float f = Mathf.Lerp(220f, 110f, t / 0.7f);
                modulator += TwoPi * f * 1.5f / Rate;
                float index = 5f * Mathf.Exp(-t * 4f);
                carrier += TwoPi * f / Rate;
                float fm = Mathf.Sin(carrier + index * Mathf.Sin(modulator));
                float shimmer = Mathf.Sin(TwoPi * 1760f * t) * 0.2f * (0.5f + 0.5f * Mathf.Sin(TwoPi * 12f * t));
                d[i] = (fm + shimmer) * Env(t, 0.01f, 0.25f) + Noise() * 0.05f * Env(t, 0.001f, 0.05f);
            }
            return d;
        }

        /// <summary>Gruñido: ruido grave modulado más un tono que cae.</summary>
        float[] Growl(float duration, float cutHz, float amHz, float fromHz, float toHz, float attack, float decay)
        {
            var d = new float[Length(duration)];
            float lp = 0f, phase = 0f;
            float a = Cutoff(cutHz);
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                lp += a * (Noise() - lp);
                float am = 0.5f + 0.5f * Mathf.Sin(TwoPi * amHz * t);
                phase += TwoPi * Mathf.Lerp(fromHz, toHz, t / duration) / Rate;
                float saw = (phase / TwoPi) % 1f * 2f - 1f;
                d[i] = (lp * 3f * am + saw * 0.35f) * Env(t, attack, decay);
            }
            return d;
        }

        /// <summary>Susurro ritual: ruido filtrado con pulsos y un zumbido grave.</summary>
        float[] Chant()
        {
            var d = new float[Length(0.7f)];
            float lp1 = 0f, lp2 = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                lp1 += Cutoff(2500f) * (Noise() - lp1);
                lp2 += Cutoff(600f) * (lp1 - lp2);
                float whisper = (lp1 - lp2) * (0.6f + 0.4f * Mathf.Sin(TwoPi * 6f * t));
                float hum = Mathf.Sin(TwoPi * 98f * t) + 0.5f * Mathf.Sin(TwoPi * 196f * t) + 0.25f * Mathf.Sin(TwoPi * 294f * t);
                d[i] = (whisper * 2f + hum * 0.15f) * Env(t, 0.15f, 0.3f);
            }
            return d;
        }

        /// <summary>Ambiente en bucle: mar lejano + un zumbido grave que va y viene.</summary>
        AudioClip BuildAmbience()
        {
            const float loop = 12f;
            int n = Length(loop);
            int crossfade = Length(1.5f);
            var raw = new float[n + crossfade];

            // Frecuencias ajustadas para dar ciclos completos en 12 s (bucle sin corte).
            float f1 = Mathf.Round(41.2f * loop) / loop;
            float f2 = Mathf.Round(61.7f * loop) / loop;
            float f3 = Mathf.Round(82.9f * loop) / loop;

            float brown = 0f, lp = 0f;
            for (int i = 0; i < raw.Length; i++)
            {
                float t = i / (float)Rate;
                brown = (brown + Noise() * 0.02f) * 0.995f;
                lp += Cutoff(300f) * (brown - lp);
                float swell = 0.55f + 0.45f * Mathf.Sin(TwoPi * t / 6f);
                float drone = Mathf.Sin(TwoPi * f1 * t) * 0.5f
                            + Mathf.Sin(TwoPi * f2 * t) * 0.3f
                            + Mathf.Sin(TwoPi * f3 * t) * 0.15f * (0.5f + 0.5f * Mathf.Sin(TwoPi * t / 4f));
                raw[i] = lp * 6f * swell + drone * 0.25f;
            }

            var data = new float[n];
            float peak = 0f;
            for (int i = 0; i < n; i++)
            {
                float v = raw[i];
                if (i < crossfade)
                {
                    float k = i / (float)crossfade;
                    v = raw[i] * Mathf.Sqrt(k) + raw[n + i] * Mathf.Sqrt(1f - k);
                }
                data[i] = v;
                peak = Mathf.Max(peak, Mathf.Abs(v));
            }
            if (peak > 0.0001f)
            {
                for (int i = 0; i < n; i++) data[i] *= 0.5f / peak;
            }

            var clip = AudioClip.Create("Ambiente", n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}

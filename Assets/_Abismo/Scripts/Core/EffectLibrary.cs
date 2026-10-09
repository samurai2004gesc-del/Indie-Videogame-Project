using System.Collections.Generic;
using UnityEngine;

namespace Abismo
{
    /// <summary>
    /// Biblioteca de efectos animados (chispazos, sangre, polvo, el Signo Arcano...): fotogramas pintados que
    /// <see cref="Effects.Play"/> reproduce una vez en el punto de impacto. El constructor automático la rellena
    /// con el arte de Editor/Art/EffectArt.cs; puedes cambiar los fps o los fotogramas en el Inspector.
    /// </summary>
    public class EffectLibrary : MonoBehaviour
    {
        [System.Serializable]
        public class Clip
        {
            public string name;
            public float fps = 20f;
            public Sprite[] frames;
        }

        public static EffectLibrary Instance { get; private set; }

        public List<Clip> clips = new List<Clip>();

        readonly Dictionary<string, Clip> byName = new Dictionary<string, Clip>();

        void Awake()
        {
            Instance = this;
            byName.Clear();
            foreach (var clip in clips)
            {
                if (clip != null && !string.IsNullOrEmpty(clip.name) && clip.frames != null && clip.frames.Length > 0)
                    byName[clip.name] = clip;
            }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public bool TryGet(string clipName, out Clip clip) => byName.TryGetValue(clipName, out clip);
    }
}

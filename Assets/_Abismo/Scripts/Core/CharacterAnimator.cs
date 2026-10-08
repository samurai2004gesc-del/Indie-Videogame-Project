using UnityEngine;

namespace Abismo
{
    /// <summary>
    /// Envoltorio sencillo del Animator: el código del personaje decide QUÉ animación toca
    /// (por nombre: "idle", "run", "attack1"...) y este componente la reproduce desde el principio
    /// solo cuando cambia. Los nombres son los estados que crea el constructor en el Animator Controller.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class CharacterAnimator : MonoBehaviour
    {
        Animator animator;
        int currentHash;
        string currentName = "";

        public string Current => currentName;

        void Awake()
        {
            animator = GetComponent<Animator>();
        }

        /// <summary>Reproduce la animación si no es la actual (o desde el principio si restart = true).</summary>
        public void Play(string animationName, bool restart = false)
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;
            if (!restart && animationName == currentName) return;
            // El estado se busca por su nombre corto y, por si acaso, por su ruta completa en la capa base.
            int hash = Animator.StringToHash(animationName);
            int playHash = hash;
            if (!animator.HasState(0, playHash))
            {
                playHash = Animator.StringToHash("Base Layer." + animationName);
                if (!animator.HasState(0, playHash)) return;
            }
            currentName = animationName;
            currentHash = hash;
            animator.Play(playHash, 0, 0f);
        }

        /// <summary>Progreso de la animación actual: 0 al empezar, 1 al terminar (sigue creciendo si es en bucle).</summary>
        public float NormalizedTime
        {
            get
            {
                if (animator == null || animator.runtimeAnimatorController == null) return 1f;
                var info = animator.GetCurrentAnimatorStateInfo(0);
                return info.shortNameHash == currentHash ? info.normalizedTime : 0f;
            }
        }

        public bool IsFinished => NormalizedTime >= 1f;
    }
}

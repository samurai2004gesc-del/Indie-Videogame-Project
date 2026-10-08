using UnityEngine;

namespace Abismo
{
    /// <summary>
    /// Animación en bucle muy ligera para el decorado (llamas de velas, agua, estandartes...):
    /// una lista de sprites que se van turnando. Para personajes se usa el Animator.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class LoopingSprite : MonoBehaviour
    {
        public Sprite[] frames;
        [SerializeField] float framesPerSecond = 10f;
        [Tooltip("Empieza en un fotograma al azar para que varias velas no parpadeen a la vez.")]
        [SerializeField] bool randomStart = true;

        SpriteRenderer spriteRenderer;
        float time;

        public void Configure(Sprite[] animationFrames, float fps)
        {
            frames = animationFrames;
            framesPerSecond = fps;
        }

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (randomStart) time = Random.value * 10f;
        }

        void Update()
        {
            if (frames == null || frames.Length == 0) return;
            time += Time.deltaTime;
            int index = Mathf.FloorToInt(time * framesPerSecond) % frames.Length;
            spriteRenderer.sprite = frames[index];
        }
    }
}

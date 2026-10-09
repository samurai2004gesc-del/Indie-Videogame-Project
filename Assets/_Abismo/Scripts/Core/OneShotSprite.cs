using UnityEngine;

namespace Abismo
{
    /// <summary>Reproduce una vez una tira de fotogramas y se destruye. La crea <see cref="Effects.Play"/>.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class OneShotSprite : MonoBehaviour
    {
        Sprite[] frames;
        float fps, time;
        Vector2 drift;
        SpriteRenderer spriteRenderer;

        public void Init(Sprite[] animationFrames, float framesPerSecond, Vector2 velocity)
        {
            frames = animationFrames;
            fps = Mathf.Max(1f, framesPerSecond);
            drift = velocity;
            spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = frames[0];
        }

        void Update()
        {
            if (frames == null) return;
            // Con tiempo escalado: durante el hit-stop el efecto se congela en su fotograma más brillante.
            time += Time.deltaTime;
            int index = Mathf.FloorToInt(time * fps);
            if (index >= frames.Length)
            {
                Destroy(gameObject);
                return;
            }
            spriteRenderer.sprite = frames[index];
            if (drift != Vector2.zero) transform.position += (Vector3)(drift * Time.deltaTime);
        }
    }
}

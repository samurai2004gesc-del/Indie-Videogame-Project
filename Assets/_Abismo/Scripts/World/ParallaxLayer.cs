using UnityEngine;

namespace Abismo
{
    /// <summary>
    /// Capa de fondo con paralaje: se mueve más despacio que la cámara para dar profundidad.
    /// factor 0 = pegada al mundo, 1 = pegada a la cámara (infinitamente lejos),
    /// negativo = más cerca que el jugador (niebla en primer plano).
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class ParallaxLayer : MonoBehaviour
    {
        [SerializeField] Vector2 factor = new Vector2(0.5f, 0.5f);
        [SerializeField] Vector2 autoScroll = Vector2.zero;
        [Tooltip("Ancho del sprite que se repite (para que el desplazamiento automático no se acabe nunca).")]
        [SerializeField] float wrapWidth = 0f;

        Transform cam;
        Vector3 startPosition, cameraStart;
        Vector2 scroll;

        public void Configure(Vector2 parallaxFactor, Vector2 scrollSpeed, float repeatWidth)
        {
            factor = parallaxFactor;
            autoScroll = scrollSpeed;
            wrapWidth = repeatWidth;
        }

        void Start()
        {
            var main = Camera.main;
            if (main != null) cam = main.transform;
            startPosition = transform.position;
            if (cam != null) cameraStart = cam.position;
        }

        void LateUpdate()
        {
            if (cam == null) return;
            scroll += autoScroll * Time.deltaTime;
            if (wrapWidth > 0f) scroll.x = Mathf.Repeat(scroll.x, wrapWidth);

            Vector3 delta = cam.position - cameraStart;
            transform.position = new Vector3(
                startPosition.x + delta.x * factor.x + scroll.x,
                startPosition.y + delta.y * factor.y + scroll.y,
                startPosition.z);
        }
    }
}

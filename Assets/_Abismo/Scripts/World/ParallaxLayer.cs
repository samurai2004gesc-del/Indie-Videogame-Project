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
        [Tooltip("Posición de la cámara para la que se colocó la capa (la pone el constructor). Si no, la del primer fotograma.")]
        [SerializeField] bool useReference;
        [SerializeField] Vector2 referenceCamera;

        Transform cam;
        Vector3 startPosition, cameraStart;
        Vector2 scroll;

        public void Configure(Vector2 parallaxFactor, Vector2 scrollSpeed, float repeatWidth)
        {
            factor = parallaxFactor;
            autoScroll = scrollSpeed;
            wrapWidth = repeatWidth;
        }

        /// <summary>La capa se colocó pensando en la cámara en <paramref name="cameraPosition"/>.</summary>
        public void SetReference(Vector2 cameraPosition)
        {
            useReference = true;
            referenceCamera = cameraPosition;
        }

        void Start()
        {
            var main = Camera.main;
            if (main != null) cam = main.transform;
            startPosition = transform.position;
            if (useReference) cameraStart = new Vector3(referenceCamera.x, referenceCamera.y, 0f);
            else if (cam != null) cameraStart = cam.position;
        }

        void LateUpdate()
        {
            if (cam == null) return;
            scroll += autoScroll * Time.deltaTime;
            if (wrapWidth > 0f) scroll.x = Mathf.Repeat(scroll.x, wrapWidth);

            Vector3 delta = cam.position - cameraStart;
            delta.z = 0f;
            transform.position = new Vector3(
                startPosition.x + delta.x * factor.x + scroll.x,
                startPosition.y + delta.y * factor.y + scroll.y,
                startPosition.z);
        }
    }
}

using UnityEngine;

namespace Abismo
{
    /// <summary>
    /// Cámara que sigue al jugador con suavizado, mira un poco hacia donde camina,
    /// no se sale de los límites del nivel y tiembla con los golpes.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        public static CameraFollow Instance { get; private set; }

        public Transform target;

        [Header("Seguimiento")]
        [SerializeField] Vector2 offset = new Vector2(0f, 1.6f);
        [SerializeField] float lookAhead = 1.8f;
        [SerializeField] float smoothTimeX = 0.18f;
        [SerializeField] float smoothTimeY = 0.3f;

        [Header("Límites del nivel")]
        [SerializeField] bool useBounds = true;
        [SerializeField] Rect bounds = new Rect(0f, 0f, 100f, 40f);

        [Header("Temblor")]
        [SerializeField] float maxShakeOffset = 0.45f;
        [SerializeField] float maxShakeAngle = 1.2f;
        [SerializeField] float traumaDecay = 1.6f;

        Camera cam;
        PlayerController player;
        Vector3 basePosition;
        float velocityX, velocityY;
        float lookAheadCurrent;
        float trauma;
        float seed;

        public void Configure(Transform followTarget, Rect levelBounds)
        {
            target = followTarget;
            bounds = levelBounds;
            useBounds = true;
        }

        void Awake()
        {
            Instance = this;
            cam = GetComponent<Camera>();
            basePosition = transform.position;
            seed = Random.value * 100f;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            if (target != null) player = target.GetComponent<PlayerController>();
            SnapToTarget();
        }

        public void AddTrauma(float amount) => trauma = Mathf.Clamp01(trauma + amount);

        /// <summary>Coloca la cámara de golpe sobre el jugador (al reaparecer).</summary>
        public void SnapToTarget()
        {
            if (target == null) return;
            lookAheadCurrent = (player != null ? player.Facing : 1) * lookAhead;
            basePosition = Clamp(DesiredPosition());
            velocityX = velocityY = 0f;
            transform.position = basePosition;
        }

        Vector3 DesiredPosition()
        {
            Vector3 p = target.position + (Vector3)offset;
            p.x += lookAheadCurrent;
            p.z = transform.position.z;
            return p;
        }

        void LateUpdate()
        {
            if (target == null) return;

            float dt = Time.deltaTime;
            if (dt > 0f)
            {
                int facing = player != null ? player.Facing : 1;
                lookAheadCurrent = Mathf.MoveTowards(lookAheadCurrent, facing * lookAhead, 4f * dt);
                Vector3 desired = Clamp(DesiredPosition());
                basePosition.x = Mathf.SmoothDamp(basePosition.x, desired.x, ref velocityX, smoothTimeX);
                basePosition.y = Mathf.SmoothDamp(basePosition.y, desired.y, ref velocityY, smoothTimeY);
                basePosition.z = desired.z;
                basePosition = Clamp(basePosition);
            }

            // El temblor usa tiempo real para que se note incluso durante el hit-stop.
            trauma = Mathf.Max(0f, trauma - traumaDecay * Time.unscaledDeltaTime);
            float shake = trauma * trauma;
            float t = Time.unscaledTime * 25f;
            Vector3 shakeOffset = new Vector3(
                Mathf.PerlinNoise(seed, t) * 2f - 1f,
                Mathf.PerlinNoise(seed + 1f, t) * 2f - 1f,
                0f) * (maxShakeOffset * shake);

            transform.position = basePosition + shakeOffset;
            transform.rotation = Quaternion.Euler(0f, 0f, (Mathf.PerlinNoise(seed + 2f, t) * 2f - 1f) * maxShakeAngle * shake);
        }

        Vector3 Clamp(Vector3 p)
        {
            if (!useBounds || cam == null) return p;
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;

            float minX = bounds.xMin + halfW, maxX = bounds.xMax - halfW;
            float minY = bounds.yMin + halfH, maxY = bounds.yMax - halfH;
            p.x = minX > maxX ? bounds.center.x : Mathf.Clamp(p.x, minX, maxX);
            p.y = minY > maxY ? bounds.center.y : Mathf.Clamp(p.y, minY, maxY);
            return p;
        }

        void OnDrawGizmosSelected()
        {
            if (!useBounds) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }
}

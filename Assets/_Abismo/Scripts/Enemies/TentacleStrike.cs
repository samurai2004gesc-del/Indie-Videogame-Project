using UnityEngine;

namespace Abismo
{
    /// <summary>
    /// Ataque del jefe: el suelo brilla en rojo (aviso) y después brota un tentáculo.
    /// No se puede parar: hay que apartarse o esquivar deslizándose.
    /// </summary>
    public class TentacleStrike : MonoBehaviour
    {
        public Transform tentacle;
        public SpriteRenderer warning;

        [SerializeField] float riseTime = 0.08f;
        [SerializeField] float activeTime = 0.4f;
        [SerializeField] float retractTime = 0.3f;
        [SerializeField] Vector2 hitSize = new Vector2(0.9f, 3.6f);

        int damage = 20;
        float telegraph = 0.8f;
        float t;
        bool risen, hasHit;

        public void Init(int strikeDamage, float telegraphTime)
        {
            damage = strikeDamage;
            telegraph = telegraphTime;
        }

        void Start()
        {
            if (tentacle != null) tentacle.localScale = new Vector3(1f, 0f, 1f);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            t += dt;

            float height;
            float warnAlpha;
            if (t < telegraph)
            {
                height = 0f;
                warnAlpha = Mathf.Lerp(0.2f, 0.85f, t / telegraph) * (0.75f + 0.25f * Mathf.Sin(t * 40f));
            }
            else if (t < telegraph + riseTime)
            {
                if (!risen)
                {
                    risen = true;
                    Sfx.Play(SfxId.Tentacle);
                    GameFeel.Shake(0.2f);
                    Effects.Dust(transform.position, 8);
                }
                height = (t - telegraph) / riseTime;
                warnAlpha = 0.9f;
            }
            else if (t < telegraph + riseTime + activeTime)
            {
                height = 1f;
                warnAlpha = 0.6f;
                if (!hasHit) TryHit();
            }
            else if (t < telegraph + riseTime + activeTime + retractTime)
            {
                height = 1f - (t - telegraph - riseTime - activeTime) / retractTime;
                warnAlpha = 0.6f * height;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            if (tentacle != null) tentacle.localScale = new Vector3(1f + Mathf.Sin(t * 30f) * 0.06f, height, 1f);
            if (warning != null)
            {
                var c = warning.color;
                warning.color = new Color(c.r, c.g, c.b, warnAlpha);
            }
        }

        void TryHit()
        {
            Vector2 center = (Vector2)transform.position + Vector2.up * (hitSize.y * 0.5f);
            var target = Combat.FindPlayerInBox(center, hitSize);
            if (target == null) return;
            var result = target.TakeDamage(new DamageInfo(damage, transform.position, 6f, false, gameObject));
            if (result != DamageResult.Ignored) hasHit = true;
        }
    }
}

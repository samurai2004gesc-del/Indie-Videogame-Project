using System.Collections.Generic;
using UnityEngine;

namespace Abismo
{
    /// <summary>
    /// Proyectil que avanza en línea recta: las esferas de los sectarios y del jefe,
    /// y el Signo Arcano del jugador. Si el jugador PARA una esfera enemiga, la devuelve.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        public SpriteRenderer spriteRenderer;

        [SerializeField] float speed = 6f;
        [SerializeField] int damage = 12;
        [SerializeField] float radius = 0.3f;
        [SerializeField] float lifetime = 5f;
        [SerializeField] bool piercing = false;
        [SerializeField] float spinSpeed = 0f;
        [SerializeField] Color reflectedColor = new Color(0.6f, 1f, 0.9f);

        Vector2 direction = Vector2.right;
        bool fromPlayer;
        LayerMask targetMask;
        float age;
        readonly List<IDamageable> results = new List<IDamageable>();
        readonly List<IDamageable> alreadyHit = new List<IDamageable>();

        /// <summary>Lo usa el constructor automático para crear las distintas variantes.</summary>
        public void Configure(float projectileSpeed, int projectileDamage, float hitRadius, float life, bool pierce, float spin)
        {
            speed = projectileSpeed;
            damage = projectileDamage;
            radius = hitRadius;
            lifetime = life;
            piercing = pierce;
            spinSpeed = spin;
        }

        public void Launch(Vector2 dir, bool launchedByPlayer)
        {
            direction = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
            fromPlayer = launchedByPlayer;
            targetMask = fromPlayer ? GameLayers.EnemyMask : GameLayers.PlayerMask;
            age = 0f;
            if (spriteRenderer != null) spriteRenderer.flipX = direction.x < 0f;
        }

        void Awake()
        {
            targetMask = GameLayers.PlayerMask;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            age += dt;
            if (age >= lifetime)
            {
                Destroy(gameObject);
                return;
            }

            transform.position += (Vector3)(direction * speed * dt);
            if (spinSpeed != 0f && spriteRenderer != null) spriteRenderer.transform.Rotate(0f, 0f, spinSpeed * dt);

            Vector2 pos = transform.position;
            if (Physics2D.OverlapCircle(pos, radius * 0.5f, GameLayers.GroundMask))
            {
                Impact();
                return;
            }

            Combat.OverlapCircle(pos, radius, targetMask, results);
            foreach (var target in results)
            {
                if (alreadyHit.Contains(target)) continue;
                var info = new DamageInfo(damage, pos - direction, 3f, !fromPlayer, gameObject);
                var result = target.TakeDamage(info);

                if (result == DamageResult.Parried)
                {
                    Reflect();
                    return;
                }
                if (result == DamageResult.Ignored) continue; // el jugador está esquivando: lo atraviesa

                alreadyHit.Add(target);
                if (!piercing)
                {
                    Impact();
                    return;
                }
            }
        }

        /// <summary>La esfera vuelve hacia quien la lanzó, más rápida y más fuerte.</summary>
        void Reflect()
        {
            direction = -direction;
            fromPlayer = true;
            targetMask = GameLayers.EnemyMask;
            damage *= 2;
            speed *= 1.5f;
            age = 0f;
            alreadyHit.Clear();
            if (spriteRenderer != null)
            {
                spriteRenderer.color = reflectedColor;
                spriteRenderer.flipX = direction.x < 0f;
            }
        }

        void Impact()
        {
            Color c = spriteRenderer != null ? spriteRenderer.color : Color.white;
            Effects.Burst(transform.position, c, 8, 4f, 0.3f, 0f, 0.8f, true);
            Destroy(gameObject);
        }
    }
}

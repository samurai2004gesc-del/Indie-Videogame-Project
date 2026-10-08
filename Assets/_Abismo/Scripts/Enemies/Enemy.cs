using UnityEngine;

namespace Abismo
{
    /// <summary>
    /// Base de todas las criaturas: vida, recibir golpes, retroceso, aturdimiento tras una
    /// parada (los golpes hacen el doble de daño), muerte con botín y reaparición en los altares.
    /// Cada enemigo concreto hereda de aquí y solo escribe su inteligencia en <see cref="Tick"/>.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public abstract class Enemy : MonoBehaviour, IDamageable, IParryable, IResettable
    {
        [Header("Referencias (las asigna el constructor automático)")]
        public Transform visual;
        public FlashEffect flash;
        public GoldPickup goldPrefab;

        [Header("Criatura")]
        [SerializeField] protected string displayName = "Criatura";
        [SerializeField] protected int maxHealth = 40;
        [SerializeField] protected int goldReward = 20;
        [SerializeField, Range(0f, 1f)] protected float knockbackResistance = 0f;
        [SerializeField] protected float staggerDuration = 1.4f;
        [SerializeField] protected bool respawns = true;
        [SerializeField] protected Color ichorColor = new Color(0.3f, 0.85f, 0.55f);

        public string DisplayName => displayName;
        public int Health => health;
        public int MaxHealth => maxHealth;
        public bool IsDead { get; private set; }
        public bool IsStaggered => staggerTimer > 0f;
        public Vector2 Center => (Vector2)transform.position + centerOffset;

        protected Rigidbody2D rb;
        protected Collider2D body;
        protected PlayerController player;
        protected int health;
        protected int facing = 1;
        protected float staggerTimer;
        protected float knockbackUntil;
        protected float halfWidth = 0.4f;
        protected Vector2 centerOffset = new Vector2(0f, 0.9f);

        // Animación por código (igual que el jugador)
        protected Vector2 squash = Vector2.one;
        protected float lean;   // positivo = inclinarse hacia delante
        protected float bob;

        Vector3 spawnPosition;
        int spawnFacing;

        /// <summary>Lo usa el constructor automático; luego puedes ajustar los valores en el Inspector.</summary>
        public void ConfigureStats(string creatureName, int health, int gold, float knockbackResist, bool canRespawn)
        {
            displayName = creatureName;
            maxHealth = health;
            goldReward = gold;
            knockbackResistance = knockbackResist;
            respawns = canRespawn;
        }

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            body = GetComponent<Collider2D>();
            rb.freezeRotation = true;
            if (body != null)
            {
                body.sharedMaterial = GameLayers.NoFriction;
                centerOffset = body.offset;
                if (body is CapsuleCollider2D capsule) halfWidth = capsule.size.x * 0.5f;
                else if (body is BoxCollider2D box) halfWidth = box.size.x * 0.5f;
                else if (body is CircleCollider2D circle) halfWidth = circle.radius;
            }
            health = maxHealth;
            spawnPosition = transform.position;
            spawnFacing = facing;
        }

        protected virtual void Start()
        {
            player = GameManager.Instance != null ? GameManager.Instance.Player : null;
            if (player == null) player = FindFirstObjectByType<PlayerController>();
        }

        protected virtual void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || IsDead) return;

            if (staggerTimer > 0f)
            {
                staggerTimer -= dt;
                SetHorizontalVelocity(0f);
                lean = Mathf.Sin(Time.time * 30f) * 5f;
            }
            else if (player != null && !player.IsDead)
            {
                Tick(dt);
            }
            else
            {
                SetHorizontalVelocity(0f);
            }

            UpdateVisual(dt);
        }

        /// <summary>La "inteligencia" de la criatura. Se llama cada frame mientras no está aturdida.</summary>
        protected abstract void Tick(float dt);

        // ------------------------------------------------------------------
        // Utilidades para las subclases
        // ------------------------------------------------------------------

        protected void SetHorizontalVelocity(float x)
        {
            if (Time.time < knockbackUntil) return;
            rb.linearVelocity = new Vector2(x, rb.linearVelocity.y);
        }

        protected void SetVelocity(Vector2 v)
        {
            if (Time.time < knockbackUntil) return;
            rb.linearVelocity = v;
        }

        protected void FacePlayer()
        {
            if (player == null) return;
            float dx = player.transform.position.x - transform.position.x;
            if (Mathf.Abs(dx) > 0.1f) facing = dx > 0f ? 1 : -1;
        }

        protected Vector2 ToPlayer() => player != null ? player.Center - Center : Vector2.zero;

        protected bool IsWallAhead(float distance = 0.4f)
        {
            Vector2 origin = (Vector2)transform.position + Vector2.up * 0.5f;
            return Physics2D.Raycast(origin, new Vector2(facing, 0f), halfWidth + distance, GameLayers.GroundMask);
        }

        protected bool IsGroundAhead(int direction)
        {
            Vector2 origin = (Vector2)transform.position + new Vector2(direction * (halfWidth + 0.3f), 0.3f);
            return Physics2D.Raycast(origin, Vector2.down, 1.2f, GameLayers.GroundMask);
        }

        protected bool IsGroundAhead() => IsGroundAhead(facing);

        /// <summary>Golpea al jugador si está en la caja indicada (relativa a donde mira la criatura).</summary>
        protected DamageResult TryHitPlayer(Vector2 localOffset, Vector2 size, int damage, float knockback, bool parryable)
        {
            Vector2 center = (Vector2)transform.position + new Vector2(localOffset.x * facing, localOffset.y);
            var target = Combat.FindPlayerInBox(center, size);
            if (target == null) return DamageResult.Ignored;
            var result = target.TakeDamage(new DamageInfo(damage, transform.position, knockback, parryable, gameObject));
            if (result == DamageResult.Parried) OnParried();
            return result;
        }

        protected virtual void UpdateVisual(float dt)
        {
            if (visual == null) return;
            squash = Vector2.Lerp(squash, Vector2.one, 1f - Mathf.Exp(-10f * dt));
            visual.localScale = new Vector3(squash.x * facing, squash.y, 1f);
            visual.localRotation = Quaternion.Euler(0f, 0f, -lean * facing);
            visual.localPosition = new Vector3(0f, bob, 0f);
        }

        // ------------------------------------------------------------------
        // Daño, aturdimiento, muerte, reaparición
        // ------------------------------------------------------------------

        public virtual DamageResult TakeDamage(DamageInfo info)
        {
            if (IsDead) return DamageResult.Ignored;

            bool critical = IsStaggered;
            health -= critical ? info.Amount * 2 : info.Amount;
            if (flash != null) flash.Flash(Color.white, 0.12f);
            squash = new Vector2(1.2f, 0.85f);

            float dir = Mathf.Sign(transform.position.x - info.SourcePosition.x);
            if (dir == 0f) dir = 1f;
            float knockback = info.Knockback * (1f - knockbackResistance);
            if (knockback > 0.01f)
            {
                float vy = rb.bodyType == RigidbodyType2D.Dynamic ? Mathf.Max(rb.linearVelocity.y, knockback * 0.3f) : 0f;
                rb.linearVelocity = new Vector2(dir * knockback, vy);
                knockbackUntil = Time.time + 0.15f;
            }

            Effects.Blood(Center, ichorColor, critical ? 14 : 7, dir);
            Sfx.Play(critical ? SfxId.HeavyHit : SfxId.Hit);
            OnHurt(info);

            if (health <= 0)
            {
                Die();
                return DamageResult.Killed;
            }
            return DamageResult.Hit;
        }

        /// <summary>El jugador ha parado nuestro golpe: quedamos aturdidos.</summary>
        public virtual void OnParried()
        {
            staggerTimer = staggerDuration;
            if (flash != null)
            {
                flash.ClearHold();
                flash.Flash(new Color(0.7f, 1f, 0.95f), 0.3f);
            }
            OnStaggered();
        }

        protected virtual void Die()
        {
            IsDead = true;
            health = 0;
            Effects.DeathBurst(Center, ichorColor);
            Sfx.Play(SfxId.EnemyDie);
            GameFeel.Shake(0.25f);
            if (goldPrefab != null && goldReward > 0) GoldPickup.SpawnBurst(goldPrefab, Center, goldReward);
            OnDied();
            gameObject.SetActive(false);
        }

        public virtual void ResetState()
        {
            if (IsDead && !respawns) return;
            gameObject.SetActive(true);
            transform.position = spawnPosition;
            rb.position = spawnPosition;
            rb.linearVelocity = Vector2.zero;
            health = maxHealth;
            IsDead = false;
            staggerTimer = 0f;
            knockbackUntil = 0f;
            facing = spawnFacing;
            squash = Vector2.one;
            lean = 0f;
            bob = 0f;
            if (flash != null) flash.ResetFlash();
            OnReset();
        }

        protected virtual void OnHurt(DamageInfo info) { }
        protected virtual void OnDied() { }
        protected virtual void OnReset() { }
        protected virtual void OnStaggered() { }
    }
}

using UnityEngine;

namespace Abismo
{
    /// <summary>
    /// Base de todas las criaturas: vida, recibir golpes, retroceso, aturdimiento tras una
    /// parada (los golpes hacen el doble de daño y se las puede EJECUTAR, como en Blasphemous),
    /// animación de muerte con botín y reaparición en los altares.
    /// Cada enemigo concreto hereda de aquí, escribe su inteligencia en <see cref="Tick"/> y elige
    /// su animación con <see cref="Animate"/> (el aturdimiento, el daño y la muerte tienen prioridad).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public abstract class Enemy : MonoBehaviour, IDamageable, IParryable, IResettable
    {
        [Header("Referencias (las asigna el constructor automático)")]
        public Transform visual;
        public CharacterAnimator anim;
        public FlashEffect flash;
        public GoldPickup goldPrefab;

        [Header("Criatura")]
        [SerializeField] protected string displayName = "Criatura";
        [SerializeField] protected int maxHealth = 40;
        [SerializeField] protected int goldReward = 20;
        [SerializeField, Range(0f, 1f)] protected float knockbackResistance = 0f;
        [SerializeField] protected float staggerDuration = 1.4f;
        [Tooltip("Lo que dura la animación de muerte antes de desaparecer.")]
        [SerializeField] protected float deathDuration = 0.8f;
        [SerializeField] protected bool respawns = true;
        [SerializeField] protected Color ichorColor = new Color(0.55f, 0.07f, 0.09f);
        [Tooltip("Si se la puede rematar con una ejecución mientras está aturdida.")]
        [SerializeField] protected bool executable = true;

        public string DisplayName => displayName;
        public int Health => health;
        public int MaxHealth => maxHealth;
        public bool IsDead { get; private set; }
        public bool IsStaggered => staggerTimer > 0f;
        /// <summary>Aturdida, en el suelo y lista para que el jugador la remate.</summary>
        public bool CanBeExecuted => Executable && IsStaggered && !IsDead && !beingExecuted;
        public bool IsBeingExecuted => beingExecuted;
        public Vector2 Center => (Vector2)transform.position + centerOffset;

        protected Rigidbody2D rb;
        protected Collider2D body;
        protected PlayerController player;
        protected int health;
        protected int facing = 1;
        protected float staggerTimer;
        protected float knockbackUntil;
        protected float hurtAnimUntil;
        protected float halfWidth = 0.4f;
        protected Vector2 centerOffset = new Vector2(0f, 0.9f);

        Vector3 spawnPosition;
        int spawnFacing;
        RigidbodyType2D spawnBodyType;
        float deathTimer;
        bool beingExecuted;

        /// <summary>Lo usa el constructor automático; luego puedes ajustar los valores en el Inspector.</summary>
        public void ConfigureStats(string creatureName, int health, int gold, float knockbackResist, bool canRespawn)
        {
            displayName = creatureName;
            maxHealth = health;
            goldReward = gold;
            knockbackResistance = knockbackResist;
            respawns = canRespawn;
        }

        public void ConfigureDeath(float seconds) => deathDuration = seconds;

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
            spawnBodyType = rb.bodyType;
        }

        protected virtual void Start()
        {
            player = GameManager.Instance != null ? GameManager.Instance.Player : null;
            if (player == null) player = FindAnyObjectByType<PlayerController>();
        }

        protected virtual void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            if (IsDead)
            {
                deathTimer -= dt;
                if (deathTimer <= 0f) gameObject.SetActive(false);
                return;
            }

            if (beingExecuted)
            {
                // Inmóvil mientras el jugador la remata (el jugador decide cuándo muere).
                SetHorizontalVelocity(0f);
                if (anim != null) anim.Play("stagger");
            }
            else if (staggerTimer > 0f)
            {
                staggerTimer -= dt;
                SetHorizontalVelocity(0f);
                Animate("stagger");
            }
            else if (player != null && !player.IsDead)
            {
                Tick(dt);
            }
            else
            {
                SetHorizontalVelocity(0f);
                Animate(IdleAnimation);
            }

            if (visual != null) visual.localScale = new Vector3(facing, 1f, 1f);
        }

        /// <summary>La "inteligencia" de la criatura. Se llama cada frame mientras no está aturdida.</summary>
        protected abstract void Tick(float dt);

        protected virtual string IdleAnimation => "idle";

        /// <summary>Los jefes y las criaturas voladoras no se pueden ejecutar.</summary>
        protected virtual bool Executable => executable;

        /// <summary>¿Se encoge al recibir un golpe? Mientras ataca no (superarmadura, como en Blasphemous).</summary>
        protected virtual bool CanFlinch => true;

        /// <summary>Reproduce una animación respetando prioridades: aturdido > herido > la pedida.</summary>
        protected void Animate(string animationName)
        {
            if (anim == null) return;
            if (IsStaggered) animationName = "stagger";
            else if (Time.time < hurtAnimUntil) animationName = "hurt";
            anim.Play(animationName);
        }

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

        // ------------------------------------------------------------------
        // Ejecuciones
        // ------------------------------------------------------------------

        /// <summary>El jugador empieza a rematarla: se queda inmóvil y deja de ser golpeable.</summary>
        public virtual void BeginExecution()
        {
            beingExecuted = true;
            rb.linearVelocity = Vector2.zero;
            if (flash != null) flash.ClearHold();
        }

        /// <summary>El arma entra: gran salpicadura y destello.</summary>
        public virtual void ExecutionStrike(float direction)
        {
            if (flash != null) flash.Flash(Color.white, 0.2f);
            Effects.Execution(Center, direction, ichorColor);
            Sfx.Play(SfxId.HeavyHit);
        }

        /// <summary>El jugador arranca el arma: muere (con más botín que de costumbre).</summary>
        public virtual void FinishExecution(float direction)
        {
            beingExecuted = false;
            Effects.Blood(Center, ichorColor, 16, direction);
            Die();
            int bonus = goldReward / 2;
            if (goldPrefab != null && bonus > 0) GoldPickup.SpawnBurst(goldPrefab, Center, bonus);
        }

        // ------------------------------------------------------------------
        // Daño, aturdimiento, muerte, reaparición
        // ------------------------------------------------------------------

        /// <summary>¿Detiene este golpe (escudo)? Por defecto nadie bloquea.</summary>
        protected virtual bool TryBlock(DamageInfo info) => false;

        /// <summary>Lo que pasa al bloquear (efectos, animación). Lo llama TakeDamage.</summary>
        protected virtual void OnBlocked(DamageInfo info) { }

        public virtual DamageResult TakeDamage(DamageInfo info)
        {
            if (IsDead || beingExecuted) return DamageResult.Ignored;
            if (!info.Unblockable && !IsStaggered && TryBlock(info))
            {
                OnBlocked(info);
                return DamageResult.Blocked;
            }

            bool critical = IsStaggered;
            health -= critical ? info.Amount * 2 : info.Amount;
            if (flash != null) flash.Flash(Color.white, 0.12f);

            float dir = Mathf.Sign(transform.position.x - info.SourcePosition.x);
            if (dir == 0f) dir = 1f;
            float knockback = info.Knockback * (1f - knockbackResistance);
            if (knockback > 0.01f)
            {
                float vy = rb.bodyType == RigidbodyType2D.Dynamic ? Mathf.Max(rb.linearVelocity.y, knockback * 0.3f) : 0f;
                rb.linearVelocity = new Vector2(dir * knockback, vy);
                knockbackUntil = Time.time + 0.15f;
            }

            Effects.Blood(Center + new Vector2(-dir * 0.2f, 0f), ichorColor, critical ? 18 : 9, dir);
            Sfx.Play(critical ? SfxId.HeavyHit : SfxId.Hit);
            OnHurt(info);

            if (health <= 0)
            {
                Die();
                return DamageResult.Killed;
            }
            if (CanFlinch && !IsStaggered) hurtAnimUntil = Time.time + 0.25f;
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
            staggerTimer = 0f;
            hurtAnimUntil = 0f;
            deathTimer = deathDuration;
            // Se queda quieto y deja de chocar mientras se reproduce su muerte.
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
            if (body != null) body.enabled = false;
            if (flash != null) flash.ResetFlash();
            if (anim != null) anim.Play("death", true);

            Effects.DeathBurst(Center, ichorColor);
            Sfx.Play(SfxId.EnemyDie);
            GameFeel.Shake(0.25f);
            if (goldPrefab != null && goldReward > 0) GoldPickup.SpawnBurst(goldPrefab, Center, goldReward);
            OnDied();
        }

        public virtual void ResetState()
        {
            if (IsDead && !respawns) return;
            gameObject.SetActive(true);
            rb.bodyType = spawnBodyType;
            if (body != null) body.enabled = true;
            transform.position = spawnPosition;
            rb.position = spawnPosition;
            rb.linearVelocity = Vector2.zero;
            health = maxHealth;
            IsDead = false;
            beingExecuted = false;
            staggerTimer = 0f;
            knockbackUntil = 0f;
            hurtAnimUntil = 0f;
            facing = spawnFacing;
            if (flash != null) flash.ResetFlash();
            if (anim != null) anim.Play(IdleAnimation, true);
            OnReset();
        }

        protected virtual void OnHurt(DamageInfo info) { }
        protected virtual void OnDied() { }
        protected virtual void OnReset() { }
        protected virtual void OnStaggered() { }
    }
}

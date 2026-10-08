using System.Collections.Generic;
using UnityEngine;

namespace Abismo
{
    /// <summary>
    /// El Ahogado: movimiento, salto, esquiva, combo de 3 golpes, parada (parry),
    /// curación con láudano, conjuro, daño, muerte y una animación hecha por código
    /// (estirar/aplastar, inclinarse y balancear el arma) para no depender de sprites animados.
    ///
    /// Truco para principiantes: selecciona al Jugador en la escena y cambia los números
    /// del Inspector mientras juegas (en Play) para encontrar el "tacto" que te guste.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D), typeof(PlayerStats))]
    public class PlayerController : MonoBehaviour, IDamageable
    {
        public enum State { Normal, Dashing, Attacking, Parrying, Healing, Casting, Hurt, Resting, Dead }

        [Header("Referencias (las asigna el constructor automático)")]
        public Transform visual;
        public SpriteRenderer bodyRenderer;
        public Transform weaponPivot;
        public SpriteRenderer weaponRenderer;
        public SpriteRenderer slashRenderer;
        public Projectile spellPrefab;
        public FlashEffect flash;

        [Header("Movimiento")]
        [SerializeField] float runSpeed = 6.5f;
        [SerializeField] float groundAcceleration = 80f;
        [SerializeField] float airAcceleration = 50f;
        [SerializeField] float jumpHeight = 3.6f;
        [SerializeField] float gravityScale = 3.2f;
        [SerializeField] float fallGravityMultiplier = 1.6f;
        [SerializeField, Range(0f, 1f)] float jumpCutMultiplier = 0.45f;
        [SerializeField] float maxFallSpeed = 18f;
        [Tooltip("Margen para saltar justo después de salirte de un borde.")]
        [SerializeField] float coyoteTime = 0.1f;
        [Tooltip("Si pulsas saltar un poco antes de tocar el suelo, el salto se recuerda.")]
        [SerializeField] float jumpBufferTime = 0.12f;

        [Header("Esquiva (deslizamiento, invulnerable)")]
        [SerializeField] float dashSpeed = 12f;
        [SerializeField] float dashDuration = 0.32f;
        [SerializeField] float dashCooldown = 0.45f;
        [SerializeField] bool allowAirDash = false;

        [Header("Ataque")]
        [SerializeField] int[] comboDamage = { 10, 12, 20 };
        [SerializeField] float attackDuration = 0.3f;
        [SerializeField] float finisherDuration = 0.42f;
        [SerializeField] float attackActiveStart = 0.06f;
        [SerializeField] float attackActiveEnd = 0.16f;
        [SerializeField] float comboCancelTime = 0.2f;
        [SerializeField] Vector2 attackOffset = new Vector2(1.0f, 1.0f);
        [SerializeField] Vector2 attackSize = new Vector2(1.9f, 1.5f);
        [SerializeField] float attackBufferTime = 0.2f;
        [SerializeField] float revelationPerHit = 5f;

        [Header("Parada (parry)")]
        [SerializeField] float parryWindow = 0.28f;
        [SerializeField] float parryRecovery = 0.3f;
        [SerializeField] float revelationPerParry = 15f;

        [Header("Láudano")]
        [SerializeField] float healDuration = 0.75f;

        [Header("Conjuro: Signo Arcano")]
        [SerializeField] float spellCost = 30f;
        [SerializeField] float castDuration = 0.4f;
        [SerializeField] float castSpawnTime = 0.15f;

        [Header("Daño recibido")]
        [SerializeField] float hurtDuration = 0.3f;
        [SerializeField] float invulnerableTime = 1.0f;
        [SerializeField] Vector2 hurtKnockback = new Vector2(6f, 7f);

        public State CurrentState => state;
        public bool IsDead => state == State.Dead;
        public int Facing => facing;
        public PlayerStats Stats => stats;
        public IInteractable CurrentInteractable => interactable;
        public Vector2 Center => (Vector2)transform.position + Vector2.up;
        public float SpellCost => spellCost;

        Rigidbody2D rb;
        CapsuleCollider2D body;
        PlayerStats stats;
        InputReader input;

        State state = State.Normal;
        float stateTimer;
        int facing = 1;

        bool grounded, wasGrounded;
        float lastGroundedTime = -10f, jumpBufferedAt = -10f, attackBufferedAt = -10f;
        float lastJumpTime = -10f, lastDashTime = -10f;
        bool jumpCut;
        float previousVelocityY;

        int comboIndex;
        bool comboQueued, attackIsAir;
        readonly List<IDamageable> hitThisSwing = new List<IDamageable>();
        readonly List<IDamageable> overlapResults = new List<IDamageable>();

        bool parrySucceeded, spellFired;
        float invulnerableUntil, hazardCooldownUntil;

        Vector2 lastSafePosition;
        float safeCheckTimer;
        readonly List<Collider2D> hazardBuffer = new List<Collider2D>(8);

        IInteractable interactable;
        ContactFilter2D groundFilter;
        readonly ContactPoint2D[] contacts = new ContactPoint2D[8];
        Collider2D ignoredPlatform;
        float ignorePlatformUntil;

        // Animación por código
        Vector2 squash = Vector2.one;
        float visualTime, weaponAngle = -70f, deathTime;
        bool hiddenByHazard;

        bool Locked => GameManager.Instance != null && GameManager.Instance.InputLocked;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            body = GetComponent<CapsuleCollider2D>();
            stats = GetComponent<PlayerStats>();

            rb.gravityScale = gravityScale;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.sharedMaterial = GameLayers.NoFriction;

            // Solo cuenta como "suelo" lo que tiene la normal hacia arriba (no paredes ni techos).
            groundFilter = new ContactFilter2D();
            groundFilter.SetLayerMask(GameLayers.GroundMask);
            groundFilter.SetNormalAngle(45f, 135f);
            groundFilter.useTriggers = false;

            lastSafePosition = transform.position;
            if (slashRenderer != null) slashRenderer.enabled = false;
        }

        void Start()
        {
            input = InputReader.Instance != null ? InputReader.Instance : FindFirstObjectByType<InputReader>();
            if (input == null) input = gameObject.AddComponent<InputReader>();
        }

        // ------------------------------------------------------------------
        // Bucle principal
        // ------------------------------------------------------------------

        void Update()
        {
            bool locked = Locked;
            if (!locked)
            {
                if (input.JumpPressed) jumpBufferedAt = Time.time;
                if (input.AttackPressed) attackBufferedAt = Time.time;
            }
            if (Time.deltaTime <= 0f) return; // pausa o hit-stop

            stateTimer += Time.deltaTime;

            if (state != State.Dead && GameManager.Instance != null && transform.position.y < GameManager.Instance.KillY)
            {
                HitByHazard(25, false);
            }

            // Salto variable: si sueltas el botón pronto, el salto es más bajo.
            if (!jumpCut && rb.linearVelocity.y > 0f && Time.time - lastJumpTime < 0.6f && (locked || !input.JumpHeld))
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
                jumpCut = true;
            }

            switch (state)
            {
                case State.Normal: UpdateNormal(locked); break;
                case State.Dashing: if (stateTimer >= dashDuration) SetState(State.Normal); break;
                case State.Attacking: UpdateAttack(); break;
                case State.Parrying: UpdateParry(); break;
                case State.Healing: UpdateHeal(); break;
                case State.Casting: UpdateCast(); break;
                case State.Hurt: if (stateTimer >= hurtDuration) SetState(State.Normal); break;
                case State.Resting: if (stateTimer >= 1.2f) SetState(State.Normal); break;
            }

            if (ignoredPlatform != null && Time.time >= ignorePlatformUntil)
            {
                Physics2D.IgnoreCollision(body, ignoredPlatform, false);
                ignoredPlatform = null;
            }

            UpdateVisuals();
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            wasGrounded = grounded;
            grounded = rb.simulated && rb.IsTouching(groundFilter) && Time.time - lastJumpTime > 0.1f;
            if (grounded)
            {
                lastGroundedTime = Time.time;
                if (!wasGrounded) OnLand();
            }

            Vector2 v = rb.linearVelocity;
            float moveX = Locked ? 0f : input.Move.x;
            switch (state)
            {
                case State.Normal:
                    v.x = Mathf.MoveTowards(v.x, moveX * runSpeed, (grounded ? groundAcceleration : airAcceleration) * dt);
                    break;
                case State.Dashing:
                    v.x = facing * dashSpeed * Mathf.Lerp(1f, 0.6f, stateTimer / dashDuration);
                    if (!grounded && allowAirDash) v.y = 0f;
                    break;
                case State.Attacking:
                    if (attackIsAir) v.x = Mathf.MoveTowards(v.x, moveX * runSpeed * 0.8f, airAcceleration * dt);
                    else v.x = Mathf.MoveTowards(v.x, stateTimer < 0.1f ? facing * 3f : 0f, groundAcceleration * dt);
                    break;
                case State.Hurt:
                    v.x = Mathf.MoveTowards(v.x, 0f, 10f * dt);
                    break;
                default: // parada, curación, conjuro, descanso, muerte
                    v.x = Mathf.MoveTowards(v.x, 0f, groundAcceleration * dt);
                    break;
            }

            bool floating = state == State.Dashing && allowAirDash && !grounded;
            rb.gravityScale = floating ? 0f : (v.y < 0f ? gravityScale * fallGravityMultiplier : gravityScale);
            if (v.y < -maxFallSpeed) v.y = -maxFallSpeed;
            rb.linearVelocity = v;
            previousVelocityY = v.y;

            // Recordamos el último suelo firme para volver aquí tras caer en pinchos o al abismo.
            safeCheckTimer -= dt;
            if (grounded && state == State.Normal && safeCheckTimer <= 0f && Time.time > hazardCooldownUntil + 0.5f
                && FeetFullySupported() && !IsNearHazard())
            {
                lastSafePosition = rb.position;
                safeCheckTimer = 0.2f;
            }
        }

        void SetState(State newState)
        {
            state = newState;
            stateTimer = 0f;
        }

        // ------------------------------------------------------------------
        // Estados
        // ------------------------------------------------------------------

        void UpdateNormal(bool locked)
        {
            if (locked) return;
            float moveX = input.Move.x, moveY = input.Move.y;
            if (Mathf.Abs(moveX) > 0.1f) facing = moveX > 0f ? 1 : -1;

            if (Time.time - jumpBufferedAt <= jumpBufferTime)
            {
                if (grounded && moveY < -0.5f && TryDropThroughPlatform())
                {
                    jumpBufferedAt = -10f;
                }
                else if (Time.time - lastGroundedTime <= coyoteTime && Time.time - lastJumpTime > 0.2f)
                {
                    Jump();
                }
            }

            if (Time.time - attackBufferedAt <= attackBufferTime)
            {
                attackBufferedAt = -10f;
                BeginSwing(0);
                return;
            }
            if (input.DashPressed && Time.time - lastDashTime >= dashCooldown && (grounded || allowAirDash))
            {
                StartDash();
                return;
            }
            if (input.ParryPressed)
            {
                SetState(State.Parrying);
                parrySucceeded = false;
                Sfx.Play(SfxId.ParryStance);
                return;
            }
            if (input.HealPressed && grounded && stats.Flasks > 0 && stats.Health < stats.MaxHealth)
            {
                SetState(State.Healing);
                Sfx.Play(SfxId.SpellCharge, 0.5f);
                return;
            }
            if (input.SpellPressed)
            {
                if (stats.Revelation >= spellCost)
                {
                    SetState(State.Casting);
                    spellFired = false;
                    Sfx.Play(SfxId.SpellCharge);
                }
                else if (HUD.Instance != null)
                {
                    HUD.Instance.PulseRevelationBar();
                }
                return;
            }
            if (input.InteractPressed && interactable != null && grounded)
            {
                interactable.Interact(this);
            }
        }

        void Jump()
        {
            float gravity = Mathf.Abs(Physics2D.gravity.y) * gravityScale;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Sqrt(2f * gravity * jumpHeight));
            lastJumpTime = Time.time;
            jumpBufferedAt = -10f;
            lastGroundedTime = -10f;
            jumpCut = false;
            grounded = false;
            squash = new Vector2(0.75f, 1.25f);
            Sfx.Play(SfxId.Jump);
            Effects.Dust(transform.position, 4);
        }

        void OnLand()
        {
            float impact = Mathf.Clamp01(-previousVelocityY / maxFallSpeed);
            squash = new Vector2(1f + 0.35f * impact, 1f - 0.3f * impact);
            if (impact > 0.3f)
            {
                Effects.Dust(transform.position, 5);
                Sfx.Play(SfxId.Land, impact);
            }
        }

        bool TryDropThroughPlatform()
        {
            int count = rb.GetContacts(groundFilter, contacts);
            for (int i = 0; i < count; i++)
            {
                var other = contacts[i].collider == body ? contacts[i].otherCollider : contacts[i].collider;
                if (other != null && other.usedByEffector)
                {
                    Physics2D.IgnoreCollision(body, other, true);
                    ignoredPlatform = other;
                    ignorePlatformUntil = Time.time + 0.35f;
                    grounded = false;
                    lastGroundedTime = -10f;
                    return true;
                }
            }
            return false;
        }

        void StartDash()
        {
            SetState(State.Dashing);
            lastDashTime = Time.time;
            squash = new Vector2(1.3f, 0.7f);
            Sfx.Play(SfxId.Dash);
            Effects.Dust(transform.position, 6);
        }

        void BeginSwing(int index)
        {
            SetState(State.Attacking);
            comboIndex = index;
            comboQueued = false;
            attackIsAir = !grounded;
            hitThisSwing.Clear();
            Sfx.Play(index == 2 ? SfxId.HeavySwing : SfxId.Swing);
        }

        void UpdateAttack()
        {
            bool finisher = comboIndex == 2;
            float duration = finisher ? finisherDuration : attackDuration;

            if (Time.time - attackBufferedAt <= attackBufferTime && stateTimer > attackActiveStart * 0.5f)
            {
                comboQueued = true;
                attackBufferedAt = -10f;
            }

            float activeEnd = attackActiveEnd + (finisher ? 0.04f : 0f);
            if (stateTimer >= attackActiveStart && stateTimer <= activeEnd) DoAttackHit();

            if (comboQueued && !attackIsAir && comboIndex < 2 && stateTimer >= comboCancelTime)
            {
                BeginSwing(comboIndex + 1);
                return;
            }
            if (stateTimer >= duration)
            {
                if (comboQueued) BeginSwing(attackIsAir || comboIndex >= 2 ? 0 : comboIndex + 1);
                else SetState(State.Normal);
            }
        }

        void DoAttackHit()
        {
            bool finisher = comboIndex == 2;
            Vector2 center = AttackCenter();
            Vector2 size = finisher ? attackSize * 1.2f : attackSize;
            Combat.OverlapBox(center, size, GameLayers.EnemyMask, overlapResults);

            foreach (var target in overlapResults)
            {
                if (hitThisSwing.Contains(target)) continue;
                hitThisSwing.Add(target);

                int damage = comboDamage[Mathf.Clamp(comboIndex, 0, comboDamage.Length - 1)];
                var result = target.TakeDamage(new DamageInfo(damage, transform.position, finisher ? 7f : 4f, false, gameObject));
                if (result == DamageResult.Ignored) continue;

                stats.AddRevelation(revelationPerHit);
                GameFeel.HitStop(finisher ? 0.09f : 0.05f);
                GameFeel.Shake(finisher ? 0.3f : 0.15f);
                Vector2 targetPos = target is Component c ? (Vector2)c.transform.position + Vector2.up * 0.9f : center;
                Effects.HitSpark(Vector2.Lerp(center, targetPos, 0.5f));
            }
        }

        Vector2 AttackCenter() => (Vector2)transform.position + new Vector2(attackOffset.x * facing, attackOffset.y);

        void UpdateParry()
        {
            if (parrySucceeded)
            {
                // Tras una parada con éxito puedes contraatacar al instante.
                if (Time.time - attackBufferedAt <= attackBufferTime)
                {
                    attackBufferedAt = -10f;
                    BeginSwing(0);
                    return;
                }
                if (stateTimer >= 0.15f) SetState(State.Normal);
            }
            else if (stateTimer >= parryWindow + parryRecovery)
            {
                SetState(State.Normal);
            }
        }

        void UpdateHeal()
        {
            if (stateTimer < healDuration) return;
            if (stats.TryUseFlask())
            {
                Sfx.Play(SfxId.Heal);
                Effects.Sparkle(Center, new Color(1f, 0.6f, 0.3f), 14);
                if (flash != null) flash.Flash(new Color(1f, 0.7f, 0.4f), 0.4f);
            }
            SetState(State.Normal);
        }

        void UpdateCast()
        {
            if (!spellFired && stateTimer >= castSpawnTime)
            {
                spellFired = true;
                if (stats.TrySpendRevelation(spellCost) && spellPrefab != null)
                {
                    Vector2 origin = (Vector2)transform.position + new Vector2(0.8f * facing, 1.1f);
                    var spell = Instantiate(spellPrefab, origin, Quaternion.identity);
                    spell.Launch(new Vector2(facing, 0f), true);
                    Sfx.Play(SfxId.Spell);
                    GameFeel.Shake(0.15f);
                }
            }
            if (stateTimer >= castDuration) SetState(State.Normal);
        }

        // ------------------------------------------------------------------
        // Daño, peligros, muerte
        // ------------------------------------------------------------------

        public DamageResult TakeDamage(DamageInfo info)
        {
            if (state == State.Dead) return DamageResult.Ignored;

            if (state == State.Parrying && !parrySucceeded && stateTimer <= parryWindow && info.Parryable)
            {
                float dx = info.SourcePosition.x - transform.position.x;
                if (Mathf.Abs(dx) < 0.3f || Mathf.Sign(dx) == facing)
                {
                    parrySucceeded = true;
                    stateTimer = 0f;
                    invulnerableUntil = Time.time + 0.3f;
                    stats.AddRevelation(revelationPerParry);
                    Sfx.Play(SfxId.Parry);
                    GameFeel.HitStop(0.15f);
                    GameFeel.Shake(0.35f);
                    if (flash != null) flash.Flash(new Color(0.7f, 1f, 0.95f), 0.25f);
                    Effects.Burst(AttackCenter(), new Color(0.8f, 1f, 0.95f), 12, 10f, 0.25f, 0f, 0.9f, true);
                    return DamageResult.Parried;
                }
            }

            if (Time.time < invulnerableUntil || state == State.Dashing) return DamageResult.Ignored;

            stats.Damage(info.Amount);
            GameFeel.HitStop(0.08f);
            GameFeel.Shake(0.45f);
            Sfx.Play(SfxId.Hurt);
            if (flash != null) flash.Flash(Color.white, 0.15f);
            if (HUD.Instance != null) HUD.Instance.FlashDamage();
            Effects.Blood(Center, new Color(0.55f, 0.05f, 0.08f), 8, Mathf.Sign(transform.position.x - info.SourcePosition.x));

            if (stats.Health <= 0)
            {
                Die(false);
                return DamageResult.Killed;
            }

            float dir = Mathf.Sign(transform.position.x - info.SourcePosition.x);
            if (Mathf.Abs(transform.position.x - info.SourcePosition.x) < 0.05f) dir = -facing;
            rb.linearVelocity = new Vector2(dir * hurtKnockback.x, hurtKnockback.y);
            SetState(State.Hurt);
            invulnerableUntil = Time.time + invulnerableTime;
            return DamageResult.Hit;
        }

        /// <summary>Pinchos, agua abisal o caída al vacío: daño y vuelta al último suelo firme.</summary>
        public void HitByHazard(int damage, bool instantKill)
        {
            if (state == State.Dead || Time.time < hazardCooldownUntil) return;
            hazardCooldownUntil = Time.time + 0.6f;

            stats.Damage(instantKill ? stats.Health : damage);
            Sfx.Play(SfxId.Hurt);
            GameFeel.Shake(0.4f);
            if (HUD.Instance != null) HUD.Instance.FlashDamage();

            if (stats.Health <= 0)
            {
                hiddenByHazard = true;
                rb.linearVelocity = Vector2.zero;
                rb.simulated = false;
                Die(true);
                return;
            }

            rb.position = lastSafePosition;
            transform.position = lastSafePosition;
            rb.linearVelocity = Vector2.zero;
            SetState(State.Hurt);
            invulnerableUntil = Time.time + invulnerableTime;
            if (flash != null) flash.Flash(Color.white, 0.2f);
            if (CameraFollow.Instance != null) CameraFollow.Instance.SnapToTarget();
        }

        void Die(bool byHazard)
        {
            SetState(State.Dead);
            deathTime = Time.time;
            Sfx.Play(SfxId.Death);
            GameFeel.Shake(0.6f);
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPlayerDied(byHazard ? lastSafePosition : (Vector2)transform.position);
            }
        }

        public void Respawn(Vector2 position)
        {
            rb.simulated = true;
            rb.position = position;
            transform.position = position;
            rb.linearVelocity = Vector2.zero;
            stats.RefillAll();
            SetState(State.Normal);
            invulnerableUntil = Time.time + 1.5f;
            lastSafePosition = position;
            hiddenByHazard = false;
            interactable = null;
            facing = 1;
            squash = Vector2.one;
        }

        /// <summary>Lo llama el altar al rezar.</summary>
        public void BeginRest()
        {
            SetState(State.Resting);
            rb.linearVelocity = Vector2.zero;
        }

        public void SetInteractable(IInteractable target) => interactable = target;

        public void ClearInteractable(IInteractable target)
        {
            if (interactable == target) interactable = null;
        }

        bool FeetFullySupported()
        {
            float half = body.size.x * 0.45f;
            Vector2 origin = (Vector2)transform.position + Vector2.up * 0.1f;
            var mask = GameLayers.GroundMask;
            return Physics2D.Raycast(origin + Vector2.left * half, Vector2.down, 0.3f, mask)
                && Physics2D.Raycast(origin + Vector2.right * half, Vector2.down, 0.3f, mask);
        }

        bool IsNearHazard()
        {
            var filter = new ContactFilter2D();
            filter.NoFilter(); // incluye triggers: los pinchos y el agua lo son
            int count = Physics2D.OverlapBox((Vector2)transform.position + Vector2.up * 0.8f, new Vector2(2.2f, 2.2f), 0f, filter, hazardBuffer);
            for (int i = 0; i < count; i++)
            {
                if (hazardBuffer[i].GetComponent<Hazard>() != null) return true;
            }
            return false;
        }

        // ------------------------------------------------------------------
        // Animación por código
        // ------------------------------------------------------------------

        void UpdateVisuals()
        {
            if (visual == null) return;
            float dt = Time.deltaTime;
            visualTime += dt;
            squash = Vector2.Lerp(squash, Vector2.one, 1f - Mathf.Exp(-12f * dt));

            Vector2 scale = squash;
            float lean = 0f;     // positivo = inclinarse hacia delante
            float yOffset = 0f;
            float targetWeapon = -70f;
            bool showSlash = false;
            Vector2 v = rb.linearVelocity;

            switch (state)
            {
                case State.Normal:
                    if (grounded && Mathf.Abs(v.x) > 0.5f)
                    {
                        float step = Mathf.Sin(visualTime * 14f);
                        yOffset = Mathf.Abs(step) * 0.08f;
                        lean = 6f;
                        targetWeapon = -100f + step * 12f;
                    }
                    else if (grounded)
                    {
                        scale.y *= 1f + Mathf.Sin(visualTime * 2.5f) * 0.015f;
                    }
                    else
                    {
                        lean = Mathf.Clamp(v.y * -0.6f, -6f, 8f);
                        targetWeapon = v.y > 0f ? -40f : -120f;
                    }
                    break;
                case State.Dashing:
                    scale = new Vector2(scale.x * 1.15f, scale.y * 0.75f);
                    lean = 20f;
                    targetWeapon = -170f;
                    break;
                case State.Attacking:
                    lean = comboIndex == 2 ? 10f : 5f;
                    weaponAngle = SwingAngle();
                    targetWeapon = weaponAngle;
                    showSlash = stateTimer >= attackActiveStart && stateTimer <= attackActiveEnd + 0.08f;
                    break;
                case State.Parrying:
                    lean = parrySucceeded ? -8f : -4f;
                    targetWeapon = 75f;
                    break;
                case State.Healing:
                case State.Resting:
                    scale = new Vector2(scale.x * 1.08f, scale.y * 0.82f);
                    targetWeapon = -95f;
                    break;
                case State.Casting:
                    lean = -6f;
                    targetWeapon = 20f + Mathf.Sin(visualTime * 40f) * 4f;
                    break;
                case State.Hurt:
                    lean = -12f;
                    targetWeapon = -140f;
                    break;
                case State.Dead:
                    lean = -Mathf.Min(90f, (Time.time - deathTime) * 300f);
                    yOffset = 0f;
                    targetWeapon = -160f;
                    break;
            }

            weaponAngle = state == State.Attacking ? targetWeapon : Mathf.LerpAngle(weaponAngle, targetWeapon, 1f - Mathf.Exp(-18f * dt));

            visual.localPosition = new Vector3(0f, yOffset, 0f);
            visual.localScale = new Vector3(scale.x * facing, scale.y, 1f);
            visual.localRotation = Quaternion.Euler(0f, 0f, -lean * facing);
            if (weaponPivot != null) weaponPivot.localRotation = Quaternion.Euler(0f, 0f, weaponAngle);

            if (slashRenderer != null)
            {
                slashRenderer.enabled = showSlash;
                if (showSlash)
                {
                    float fade = Mathf.InverseLerp(attackActiveEnd + 0.08f, attackActiveStart, stateTimer);
                    float size = comboIndex == 2 ? 1.25f : 1f;
                    slashRenderer.transform.localPosition = new Vector3(attackOffset.x * 0.9f, attackOffset.y, 0f);
                    slashRenderer.transform.localScale = new Vector3(size, comboIndex == 1 ? -size : size, 1f);
                    var c = slashRenderer.color;
                    slashRenderer.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(fade * 1.5f));
                }
            }

            // Parpadeo mientras eres invulnerable tras un golpe.
            bool blinkOff = state != State.Dead && Time.time < invulnerableUntil && Mathf.Repeat(Time.time, 0.12f) < 0.06f;
            bool visible = !hiddenByHazard && !blinkOff;
            if (bodyRenderer != null) bodyRenderer.enabled = visible;
            if (weaponRenderer != null) weaponRenderer.enabled = visible;
        }

        float SwingAngle()
        {
            // Ángulos mirando a la derecha: 0 = al frente, 90 = arriba, -90 = abajo.
            float from, to;
            if (attackIsAir) { from = 140f; to = -110f; }
            else if (comboIndex == 0) { from = 120f; to = -60f; }
            else if (comboIndex == 1) { from = -70f; to = 100f; }
            else { from = 170f; to = -95f; }

            if (stateTimer < attackActiveStart)
            {
                return Mathf.Lerp(from - 15f * Mathf.Sign(to - from), from, stateTimer / attackActiveStart);
            }
            float t = Mathf.InverseLerp(attackActiveStart, attackActiveEnd, stateTimer);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            return Mathf.Lerp(from, to, eased);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.6f);
            int f = Application.isPlaying ? facing : 1;
            Vector3 center = transform.position + new Vector3(attackOffset.x * f, attackOffset.y, 0f);
            Gizmos.DrawWireCube(center, attackSize);
        }
    }
}

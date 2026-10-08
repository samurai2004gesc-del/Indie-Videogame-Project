using System.Collections.Generic;
using UnityEngine;

namespace Abismo
{
    /// <summary>
    /// El Ahogado: movimiento, salto, esquiva, combo de 3 golpes, parada (parry),
    /// curación con láudano, conjuro, daño y muerte. Cada estado reproduce su animación
    /// fotograma a fotograma ("idle", "run", "attack1"...) y los golpes se activan exactamente
    /// en los fotogramas de impacto (ver AttackData).
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
        public CharacterAnimator anim;
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

        /// <summary>Un golpe: su animación, daño, cuándo hace daño (sincronizado con los fotogramas) y dónde.</summary>
        [System.Serializable]
        public class AttackData
        {
            public string animation = "attack1";
            public int damage = 10;
            [Tooltip("Duración total (segundos). Debe coincidir con la animación: fotogramas / fps.")]
            public float duration = 0.3f;
            [Tooltip("Ventana en la que la hoja hace daño (los fotogramas con estela).")]
            public float activeStart = 0.1f;
            public float activeEnd = 0.2f;
            public Vector2 offset = new Vector2(1.35f, 1.05f);
            public Vector2 size = new Vector2(2.5f, 1.5f);
            public float knockback = 4f;
            public float hitStop = 0.05f;
            public float shake = 0.15f;
        }

        [Header("Ataque (combo de 3 + aéreo)")]
        [SerializeField] AttackData[] combo =
        {
            new AttackData { animation = "attack1", damage = 10, duration = 0.30f, activeStart = 0.10f, activeEnd = 0.20f, offset = new Vector2(1.35f, 1.05f), size = new Vector2(2.5f, 1.5f), knockback = 4f, hitStop = 0.05f, shake = 0.15f },
            new AttackData { animation = "attack2", damage = 12, duration = 0.30f, activeStart = 0.10f, activeEnd = 0.20f, offset = new Vector2(1.1f, 1.6f), size = new Vector2(2.2f, 2.4f), knockback = 4f, hitStop = 0.05f, shake = 0.15f },
            new AttackData { animation = "attack3", damage = 20, duration = 0.40f, activeStart = 0.15f, activeEnd = 0.25f, offset = new Vector2(1.45f, 0.9f), size = new Vector2(2.9f, 1.9f), knockback = 7f, hitStop = 0.09f, shake = 0.32f },
        };
        [SerializeField] AttackData airAttack = new AttackData { animation = "airattack", damage = 10, duration = 0.30f, activeStart = 0.10f, activeEnd = 0.20f, offset = new Vector2(1.1f, 0.7f), size = new Vector2(2.4f, 2.2f), knockback = 4f, hitStop = 0.05f, shake = 0.15f };
        [Tooltip("A partir de este momento del golpe, si ya pulsaste atacar, se encadena el siguiente.")]
        [SerializeField] float comboCancelTime = 0.2f;
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

        // Animación
        float landAnimUntil;
        bool hiddenByHazard;
        AttackData CurrentAttack => attackIsAir ? airAttack : combo[Mathf.Clamp(comboIndex, 0, combo.Length - 1)];

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
        }

        void Start()
        {
            input = InputReader.Instance != null ? InputReader.Instance : FindAnyObjectByType<InputReader>();
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
            if (anim != null) anim.Play("jump", true);
            Sfx.Play(SfxId.Jump);
            Effects.Dust(transform.position, 4);
        }

        void OnLand()
        {
            float impact = Mathf.Clamp01(-previousVelocityY / maxFallSpeed);
            if (impact > 0.3f)
            {
                landAnimUntil = Time.time + 0.2f;
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
            if (anim != null) anim.Play(CurrentAttack.animation, true);
            Sfx.Play(index == 2 ? SfxId.HeavySwing : SfxId.Swing);
        }

        void UpdateAttack()
        {
            var attack = CurrentAttack;

            if (Time.time - attackBufferedAt <= attackBufferTime && stateTimer > attack.activeStart * 0.5f)
            {
                comboQueued = true;
                attackBufferedAt = -10f;
            }

            if (stateTimer >= attack.activeStart && stateTimer <= attack.activeEnd) DoAttackHit();

            if (comboQueued && !attackIsAir && comboIndex < 2 && stateTimer >= comboCancelTime)
            {
                BeginSwing(comboIndex + 1);
                return;
            }
            if (stateTimer >= attack.duration)
            {
                if (comboQueued) BeginSwing(attackIsAir || comboIndex >= 2 ? 0 : comboIndex + 1);
                else SetState(State.Normal);
            }
        }

        void DoAttackHit()
        {
            var attack = CurrentAttack;
            Vector2 center = AttackCenter();
            Combat.OverlapBox(center, attack.size, GameLayers.EnemyMask, overlapResults);

            foreach (var target in overlapResults)
            {
                if (hitThisSwing.Contains(target)) continue;
                hitThisSwing.Add(target);

                var result = target.TakeDamage(new DamageInfo(attack.damage, transform.position, attack.knockback, false, gameObject));
                if (result == DamageResult.Ignored) continue;

                stats.AddRevelation(revelationPerHit);
                GameFeel.HitStop(attack.hitStop);
                GameFeel.Shake(attack.shake);
                Vector2 targetPos = target is Component c ? (Vector2)c.transform.position + Vector2.up * 0.9f : center;
                Effects.HitSpark(Vector2.Lerp(center, targetPos, 0.5f));
            }
        }

        Vector2 AttackCenter()
        {
            var attack = CurrentAttack;
            return (Vector2)transform.position + new Vector2(attack.offset.x * facing, attack.offset.y);
        }

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
                    CameraFX.Chromatic(0.4f);
                    if (flash != null) flash.Flash(new Color(0.7f, 1f, 0.95f), 0.25f);
                    Effects.Burst(AttackCenter(), new Color(0.8f, 1f, 0.95f), 12, 10f, 0.25f, 0f, 0.9f, true);
                    return DamageResult.Parried;
                }
            }

            if (Time.time < invulnerableUntil || state == State.Dashing) return DamageResult.Ignored;

            stats.Damage(info.Amount);
            GameFeel.HitStop(0.08f);
            GameFeel.Shake(0.45f);
            CameraFX.Chromatic(0.6f);
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
            CameraFX.Chromatic(0.5f);
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
            var filter = ContactFilter2D.noFilter; // incluye triggers: los pinchos y el agua lo son
            int count = Physics2D.OverlapBox((Vector2)transform.position + Vector2.up * 0.8f, new Vector2(2.2f, 2.2f), 0f, filter, hazardBuffer);
            for (int i = 0; i < count; i++)
            {
                if (hazardBuffer[i].TryGetComponent<Hazard>(out _)) return true;
            }
            return false;
        }

        // ------------------------------------------------------------------
        // Animación: cada estado elige su animación fotograma a fotograma
        // ------------------------------------------------------------------

        void UpdateVisuals()
        {
            if (visual != null) visual.localScale = new Vector3(facing, 1f, 1f);
            if (anim != null) anim.Play(AnimationForState());

            // Parpadeo mientras eres invulnerable tras un golpe.
            bool blinkOff = state != State.Dead && Time.time < invulnerableUntil && Mathf.Repeat(Time.time, 0.12f) < 0.06f;
            if (bodyRenderer != null) bodyRenderer.enabled = !hiddenByHazard && !blinkOff;
        }

        string AnimationForState()
        {
            switch (state)
            {
                case State.Dashing: return "dodge";
                case State.Attacking: return CurrentAttack.animation;
                case State.Parrying: return parrySucceeded ? "parry_success" : "parry";
                case State.Healing: return "heal";
                case State.Casting: return "cast";
                case State.Hurt: return "hurt";
                case State.Resting: return "pray";
                case State.Dead: return "death";
            }
            if (!grounded) return rb.linearVelocity.y > 0.5f ? "jump" : "fall";
            if (Mathf.Abs(rb.linearVelocity.x) > 0.6f) return "run";
            return Time.time < landAnimUntil ? "land" : "idle";
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.6f);
            int f = Application.isPlaying ? facing : 1;
            if (combo == null) return;
            foreach (var attack in combo)
            {
                Vector3 center = transform.position + new Vector3(attack.offset.x * f, attack.offset.y, 0f);
                Gizmos.DrawWireCube(center, attack.size);
            }
        }
    }
}

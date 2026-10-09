using UnityEngine;

namespace Abismo
{
    /// <summary>
    /// Guardián de la Concha: Profundo veterano con un escudo de concha gigante que DETIENE todos los golpes
    /// que le llegan de frente (el jugador rebota). Se gira despacio, así que hay que esquivar por detrás de él,
    /// romperle la guardia a golpes, o PARAR su lanzada para aturdirlo y ejecutarlo. Los conjuros atraviesan el escudo.
    /// </summary>
    public class GuardianEnemy : Enemy
    {
        enum AI { Patrol, Approach, Guard, Windup, Attack, Recover }

        [Header("Guardián")]
        [SerializeField] float patrolSpeed = 1f;
        [SerializeField] float approachSpeed = 1.7f;
        [SerializeField] float aggroRange = 7.5f;
        [SerializeField] float attackRange = 2.4f;
        [Tooltip("Tiempo que tarda en darse la vuelta cuando el jugador se le pone detrás.")]
        [SerializeField] float turnDelay = 0.55f;
        [SerializeField] float guardTime = 0.8f;
        [SerializeField] float windupTime = 0.7f;
        [SerializeField] float attackTime = 0.22f;
        [SerializeField] float recoverTime = 0.95f;
        [SerializeField] float lungeSpeed = 5f;
        [SerializeField] int attackDamage = 20;
        [SerializeField] Vector2 attackOffset = new Vector2(1.55f, 1.05f);
        [SerializeField] Vector2 attackSize = new Vector2(2.6f, 1.0f);

        [Header("Escudo")]
        [Tooltip("Golpes bloqueados seguidos que rompen su guardia (y lo aturden).")]
        [SerializeField] int guardBreakHits = 4;
        [SerializeField] float guardMemory = 1.6f;
        [SerializeField] Vector2 shieldOffset = new Vector2(0.75f, 1.1f);

        AI ai = AI.Patrol;
        float aiTimer, lostTimer, behindTimer, blockAnimUntil, lastBlockTime;
        int blockedHits;
        bool hasHit;

        protected override bool CanFlinch => ai != AI.Windup && ai != AI.Attack;

        void ChangeState(AI next)
        {
            ai = next;
            aiTimer = 0f;
            if (flash != null) flash.ClearHold();
        }

        protected override void Tick(float dt)
        {
            aiTimer += dt;
            Vector2 toPlayer = ToPlayer();
            bool canSee = Mathf.Abs(toPlayer.x) < aggroRange && Mathf.Abs(toPlayer.y) < 2.5f;
            bool shieldAnim = Time.time < blockAnimUntil;

            switch (ai)
            {
                case AI.Patrol:
                    if (IsWallAhead() || !IsGroundAhead()) facing = -facing;
                    SetHorizontalVelocity(facing * patrolSpeed);
                    Animate(shieldAnim ? "blockhit" : "walk");
                    if (canSee) ChangeState(AI.Approach);
                    break;

                case AI.Approach:
                    TurnSlowly(toPlayer.x, dt);
                    lostTimer = canSee ? 0f : lostTimer + dt;
                    if (lostTimer > 2.5f)
                    {
                        ChangeState(AI.Patrol);
                        break;
                    }
                    if (InFront(toPlayer.x) && Mathf.Abs(toPlayer.x) <= attackRange && Mathf.Abs(toPlayer.y) < 1.5f)
                    {
                        SetHorizontalVelocity(0f);
                        ChangeState(AI.Guard);
                        break;
                    }
                    bool canAdvance = InFront(toPlayer.x) && IsGroundAhead() && !IsWallAhead();
                    SetHorizontalVelocity(canAdvance && !shieldAnim ? facing * approachSpeed : 0f);
                    Animate(shieldAnim ? "blockhit" : canAdvance ? "walk" : "block");
                    break;

                case AI.Guard:
                    // Agazapado tras la concha, esperando el momento de lanzar el arpón.
                    SetHorizontalVelocity(0f);
                    TurnSlowly(toPlayer.x, dt);
                    Animate(shieldAnim ? "blockhit" : "block");
                    if (!InFront(toPlayer.x) || Mathf.Abs(toPlayer.x) > attackRange + 0.8f)
                    {
                        ChangeState(AI.Approach);
                        break;
                    }
                    if (aiTimer >= guardTime && !shieldAnim)
                    {
                        ChangeState(AI.Windup);
                        Sfx.Play(SfxId.Chant, 0.45f, 0.2f);
                    }
                    break;

                case AI.Windup:
                    SetHorizontalVelocity(0f);
                    Animate("windup");
                    if (flash != null) flash.Hold(new Color(1f, 0.55f, 0.15f), Mathf.Clamp01(aiTimer / windupTime) * 0.6f);
                    if (aiTimer >= windupTime)
                    {
                        ChangeState(AI.Attack);
                        hasHit = false;
                        Sfx.Play(SfxId.EnemySwing);
                    }
                    break;

                case AI.Attack:
                    Animate("attack");
                    SetHorizontalVelocity(IsGroundAhead() && !IsWallAhead() ? facing * lungeSpeed : 0f);
                    if (!hasHit)
                    {
                        var result = TryHitPlayer(attackOffset, attackSize, attackDamage, 7f, true);
                        if (result != DamageResult.Ignored) hasHit = true;
                        if (result == DamageResult.Parried) return; // ya estamos aturdidos
                    }
                    if (aiTimer >= attackTime) ChangeState(AI.Recover);
                    break;

                case AI.Recover:
                    // Con el escudo bajado: es el momento de castigarlo de frente.
                    SetHorizontalVelocity(0f);
                    Animate("recover");
                    if (aiTimer >= recoverTime) ChangeState(AI.Approach);
                    break;
            }
        }

        bool InFront(float dx) => Mathf.Abs(dx) < 0.2f || Mathf.Sign(dx) == facing;

        /// <summary>Si el jugador se le cuela por detrás, tarda un poco en darse la vuelta.</summary>
        void TurnSlowly(float dx, float dt)
        {
            if (InFront(dx))
            {
                behindTimer = 0f;
                return;
            }
            behindTimer += dt;
            if (behindTimer >= turnDelay)
            {
                facing = -facing;
                behindTimer = 0f;
            }
        }

        bool ShieldUp => ai == AI.Patrol || ai == AI.Approach || ai == AI.Guard || ai == AI.Windup;

        protected override bool TryBlock(DamageInfo info)
        {
            if (!ShieldUp) return false;
            float dx = info.SourcePosition.x - transform.position.x;
            return Mathf.Abs(dx) < 0.2f || Mathf.Sign(dx) == facing;
        }

        protected override void OnBlocked(DamageInfo info)
        {
            Vector2 shield = (Vector2)transform.position + new Vector2(shieldOffset.x * facing, shieldOffset.y);
            Effects.BlockSpark(shield, -facing);
            Sfx.Play(SfxId.Block);
            GameFeel.Shake(0.12f);
            blockAnimUntil = Time.time + 0.2f;
            if (flash != null) flash.Flash(new Color(0.85f, 0.95f, 1f), 0.06f);
            // El golpe lo empuja un poco hacia atrás, pero no lo mueve del sitio.
            if (rb.bodyType == RigidbodyType2D.Dynamic) rb.linearVelocity = new Vector2(-facing * 1.6f, rb.linearVelocity.y);

            blockedHits = Time.time - lastBlockTime > guardMemory ? 1 : blockedHits + 1;
            lastBlockTime = Time.time;
            if (ai == AI.Patrol) ChangeState(AI.Approach);
            if (blockedHits >= guardBreakHits)
            {
                // Guardia rota: la concha se aparta y queda aturdido (¡a ejecutarlo!).
                blockedHits = 0;
                Sfx.Play(SfxId.Parry, 0.7f);
                GameFeel.HitStop(0.08f);
                OnParried();
            }
        }

        protected override void OnHurt(DamageInfo info)
        {
            if (ai == AI.Patrol) ChangeState(AI.Approach);
        }

        protected override void OnStaggered() => ChangeState(AI.Recover);

        protected override void OnReset()
        {
            ChangeState(AI.Patrol);
            blockedHits = 0;
            behindTimer = 0f;
            blockAnimUntil = 0f;
        }

        void OnDrawGizmosSelected()
        {
            int f = Application.isPlaying ? facing : 1;
            Gizmos.color = new Color(1f, 0.5f, 0.1f, 0.6f);
            Gizmos.DrawWireCube(transform.position + new Vector3(attackOffset.x * f, attackOffset.y), attackSize);
            Gizmos.color = new Color(0.6f, 0.85f, 1f, 0.6f);
            Gizmos.DrawWireCube(transform.position + new Vector3(shieldOffset.x * f, shieldOffset.y), new Vector3(0.4f, 1.5f));
        }
    }
}

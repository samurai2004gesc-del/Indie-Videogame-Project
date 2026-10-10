using UnityEngine;

namespace Abismo
{
    /// <summary>
    /// Rasgo de los Profundos: mole con cabeza de calamar. Lento y muy resistente; alterna un barrido de tentáculos
    /// que se puede PARAR (brilla en naranja) con un golpe aplastante que NO (brilla en rojo: hay que esquivarlo).
    /// </summary>
    public class RasgoEnemy : Enemy
    {
        enum AI { Patrol, Chase, SweepWindup, Sweep, SlamWindup, Slam, Recover }

        [Header("Rasgo de los Profundos")]
        [SerializeField] float patrolSpeed = 0.9f;
        [SerializeField] float chaseSpeed = 2f;
        [SerializeField] float aggroRange = 8f;
        [SerializeField] float attackRange = 2.6f;
        [SerializeField] float sweepWindup = 0.65f;
        [SerializeField] float sweepTime = 0.25f;
        [SerializeField] int sweepDamage = 22;
        [SerializeField] Vector2 sweepOffset = new Vector2(1.5f, 1.3f);
        [SerializeField] Vector2 sweepSize = new Vector2(2.8f, 1.8f);
        [SerializeField] float slamWindup = 0.95f;
        [SerializeField] float slamTime = 0.2f;
        [SerializeField] int slamDamage = 32;
        [SerializeField] Vector2 slamOffset = new Vector2(1.8f, 0.6f);
        [SerializeField] Vector2 slamSize = new Vector2(3f, 1.4f);
        [SerializeField] float recoverTime = 1f;

        AI ai = AI.Patrol;
        float aiTimer, lostTimer;
        bool hasHit;
        int attacksDone;

        protected override bool CanFlinch => ai == AI.Patrol || ai == AI.Chase || ai == AI.Recover;

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
            bool canSee = Mathf.Abs(toPlayer.x) < aggroRange && Mathf.Abs(toPlayer.y) < 3f;

            switch (ai)
            {
                case AI.Patrol:
                    if (IsWallAhead() || !IsGroundAhead()) facing = -facing;
                    SetHorizontalVelocity(facing * patrolSpeed);
                    Animate("walk");
                    if (canSee) ChangeState(AI.Chase);
                    break;

                case AI.Chase:
                    FacePlayer();
                    lostTimer = canSee ? 0f : lostTimer + dt;
                    if (lostTimer > 2.5f)
                    {
                        ChangeState(AI.Patrol);
                        break;
                    }
                    if (Mathf.Abs(toPlayer.x) <= attackRange && Mathf.Abs(toPlayer.y) < 2f)
                    {
                        SetHorizontalVelocity(0f);
                        // Cada tercer ataque es el golpe rojo.
                        bool slam = attacksDone % 3 == 2;
                        ChangeState(slam ? AI.SlamWindup : AI.SweepWindup);
                        Sfx.Play(slam ? SfxId.Roar : SfxId.Chant, slam ? 0.5f : 0.45f, 0.2f);
                        break;
                    }
                    bool canAdvance = IsGroundAhead() && !IsWallAhead();
                    SetHorizontalVelocity(canAdvance ? facing * chaseSpeed : 0f);
                    Animate(canAdvance ? "walk" : "idle");
                    break;

                case AI.SweepWindup:
                    SetHorizontalVelocity(0f);
                    Animate("windup");
                    if (flash != null) flash.Hold(new Color(1f, 0.55f, 0.15f), Mathf.Clamp01(aiTimer / sweepWindup) * 0.6f);
                    if (aiTimer >= sweepWindup)
                    {
                        ChangeState(AI.Sweep);
                        hasHit = false;
                        Sfx.Play(SfxId.HeavySwing);
                    }
                    break;

                case AI.Sweep:
                    Animate("attack");
                    SetHorizontalVelocity(0f);
                    if (!hasHit)
                    {
                        var result = TryHitPlayer(sweepOffset, sweepSize, sweepDamage, 7f, true);
                        if (result != DamageResult.Ignored) hasHit = true;
                        if (result == DamageResult.Parried) return;
                    }
                    if (aiTimer >= sweepTime) EndAttack();
                    break;

                case AI.SlamWindup:
                    SetHorizontalVelocity(0f);
                    Animate("slam_windup");
                    // ROJO = imparable.
                    if (flash != null) flash.Hold(new Color(1f, 0.1f, 0.1f), 0.3f + 0.4f * Mathf.PingPong(aiTimer * 8f, 1f));
                    if (aiTimer >= slamWindup)
                    {
                        ChangeState(AI.Slam);
                        hasHit = false;
                        GameFeel.Shake(0.45f);
                        Sfx.Play(SfxId.HeavyHit);
                        Effects.Dust((Vector2)transform.position + new Vector2(facing * slamOffset.x, 0f), 12, facing);
                    }
                    break;

                case AI.Slam:
                    Animate("slam");
                    SetHorizontalVelocity(0f);
                    if (!hasHit && TryHitPlayer(slamOffset, slamSize, slamDamage, 10f, false) != DamageResult.Ignored) hasHit = true;
                    if (aiTimer >= slamTime) EndAttack();
                    break;

                case AI.Recover:
                    SetHorizontalVelocity(0f);
                    Animate("recover");
                    if (aiTimer >= recoverTime) ChangeState(AI.Chase);
                    break;
            }
        }

        void EndAttack()
        {
            attacksDone++;
            ChangeState(AI.Recover);
        }

        protected override void OnHurt(DamageInfo info)
        {
            if (ai == AI.Patrol) ChangeState(AI.Chase);
        }

        protected override void OnStaggered() => ChangeState(AI.Recover);

        protected override void OnReset()
        {
            ChangeState(AI.Patrol);
            attacksDone = 0;
        }

        void OnDrawGizmosSelected()
        {
            int f = Application.isPlaying ? facing : 1;
            Gizmos.color = new Color(1f, 0.5f, 0.1f, 0.6f);
            Gizmos.DrawWireCube(transform.position + new Vector3(sweepOffset.x * f, sweepOffset.y), sweepSize);
            Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.6f);
            Gizmos.DrawWireCube(transform.position + new Vector3(slamOffset.x * f, slamOffset.y), slamSize);
        }
    }
}

using UnityEngine;

namespace Abismo
{
    /// <summary>
    /// Profundo: criatura anfibia que patrulla, te persigue y lanza un zarpazo.
    /// El zarpazo se anuncia (se echa hacia atrás y brilla en naranja): ¡es el momento de PARAR!
    /// </summary>
    public class DeepOneEnemy : Enemy
    {
        enum AI { Patrol, Chase, Windup, Attack, Recover }

        [Header("Profundo")]
        [SerializeField] float patrolSpeed = 1.5f;
        [SerializeField] float chaseSpeed = 3.4f;
        [SerializeField] float aggroRange = 7f;
        [SerializeField] float attackRange = 1.7f;
        [SerializeField] float windupTime = 0.55f;
        [SerializeField] float attackTime = 0.2f;
        [SerializeField] float recoverTime = 0.75f;
        [SerializeField] float lungeSpeed = 6f;
        [SerializeField] int attackDamage = 15;
        [SerializeField] Vector2 attackOffset = new Vector2(1.0f, 0.9f);
        [SerializeField] Vector2 attackSize = new Vector2(1.7f, 1.4f);

        AI ai = AI.Patrol;
        float aiTimer, lostTimer;
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
                    if (lostTimer > 2f)
                    {
                        ChangeState(AI.Patrol);
                        break;
                    }
                    if (Mathf.Abs(toPlayer.x) <= attackRange && Mathf.Abs(toPlayer.y) < 1.5f)
                    {
                        SetHorizontalVelocity(0f);
                        ChangeState(AI.Windup);
                        Sfx.Play(SfxId.Chant, 0.4f, 0.2f);
                        break;
                    }
                    bool canAdvance = IsGroundAhead() && !IsWallAhead();
                    SetHorizontalVelocity(canAdvance ? facing * chaseSpeed : 0f);
                    Animate(canAdvance ? "run" : "idle");
                    break;

                case AI.Windup:
                    SetHorizontalVelocity(0f);
                    float charge = Mathf.Clamp01(aiTimer / windupTime);
                    Animate("windup");
                    if (flash != null) flash.Hold(new Color(1f, 0.55f, 0.15f), charge * 0.6f);
                    if (aiTimer >= windupTime)
                    {
                        ChangeState(AI.Attack);
                        hasHit = false;
                        Sfx.Play(SfxId.EnemySwing);
                    }
                    break;

                case AI.Attack:
                    Animate("attack");
                    SetHorizontalVelocity(IsGroundAhead() ? facing * lungeSpeed : 0f);
                    if (!hasHit)
                    {
                        var result = TryHitPlayer(attackOffset, attackSize, attackDamage, 6f, true);
                        if (result != DamageResult.Ignored) hasHit = true;
                        if (result == DamageResult.Parried) return; // ya estamos aturdidos
                    }
                    if (aiTimer >= attackTime) ChangeState(AI.Recover);
                    break;

                case AI.Recover:
                    SetHorizontalVelocity(0f);
                    Animate("recover");
                    if (aiTimer >= recoverTime) ChangeState(AI.Chase);
                    break;
            }
        }

        protected override void OnStaggered() => ChangeState(AI.Recover);

        protected override void OnReset() => ChangeState(AI.Patrol);

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.5f, 0.1f, 0.6f);
            int f = Application.isPlaying ? facing : 1;
            Gizmos.DrawWireCube(transform.position + new Vector3(attackOffset.x * f, attackOffset.y), attackSize);
            Gizmos.color = new Color(1f, 1f, 0f, 0.25f);
            Gizmos.DrawWireCube(transform.position + Vector3.up, new Vector3(aggroRange * 2f, 5f));
        }
    }
}

using UnityEngine;

namespace Abismo
{
    /// <summary>
    /// Sectario de la Orden Esotérica: mantiene la distancia y lanza esferas de energía.
    /// Las esferas se pueden PARAR para devolvérselas.
    /// </summary>
    public class CultistEnemy : Enemy
    {
        enum AI { Idle, Windup, Recover }

        [Header("Sectario")]
        public Projectile projectilePrefab;
        [SerializeField] float castRange = 10f;
        [SerializeField] float windupTime = 0.8f;
        [SerializeField] float cooldown = 2.4f;
        [SerializeField] float retreatDistance = 2.5f;
        [SerializeField] float retreatSpeed = 2.2f;
        [SerializeField] Vector2 castPoint = new Vector2(1.7f, 1.7f); // la punta del báculo al lanzar

        AI ai = AI.Idle;
        float aiTimer, cooldownTimer = 1f;

        protected override bool CanFlinch => ai != AI.Windup;

        void ChangeState(AI next)
        {
            ai = next;
            aiTimer = 0f;
            if (flash != null) flash.ClearHold();
        }

        protected override void Tick(float dt)
        {
            aiTimer += dt;
            cooldownTimer -= dt;
            Vector2 toPlayer = ToPlayer();
            bool canSee = Mathf.Abs(toPlayer.x) < castRange && Mathf.Abs(toPlayer.y) < 5f;

            switch (ai)
            {
                case AI.Idle:
                    if (!canSee)
                    {
                        SetHorizontalVelocity(0f);
                        Animate("idle");
                        break;
                    }
                    FacePlayer();
                    bool tooClose = Mathf.Abs(toPlayer.x) < retreatDistance;
                    if (tooClose && IsGroundAhead(-facing))
                    {
                        SetHorizontalVelocity(-facing * retreatSpeed);
                        Animate("walk");
                    }
                    else
                    {
                        SetHorizontalVelocity(0f);
                        Animate("idle");
                    }
                    if (cooldownTimer <= 0f)
                    {
                        ChangeState(AI.Windup);
                        Sfx.Play(SfxId.Chant, 0.7f);
                    }
                    break;

                case AI.Windup:
                    SetHorizontalVelocity(0f);
                    FacePlayer();
                    float charge = Mathf.Clamp01(aiTimer / windupTime);
                    Animate("windup");
                    if (flash != null) flash.Hold(new Color(0.85f, 0.4f, 1f), charge * 0.55f);
                    if (aiTimer >= windupTime)
                    {
                        Fire();
                        ChangeState(AI.Recover);
                    }
                    break;

                case AI.Recover:
                    SetHorizontalVelocity(0f);
                    Animate("cast");
                    if (aiTimer >= 0.5f)
                    {
                        cooldownTimer = cooldown;
                        ChangeState(AI.Idle);
                    }
                    break;
            }
        }

        void Fire()
        {
            if (projectilePrefab == null) return;
            Vector2 origin = (Vector2)transform.position + new Vector2(castPoint.x * facing, castPoint.y);
            Vector2 dir = player.Center - origin;
            var orb = Instantiate(projectilePrefab, origin, Quaternion.identity);
            orb.Launch(dir, false);
            Sfx.Play(SfxId.Spell, 0.6f);
        }

        protected override void OnStaggered() => ChangeState(AI.Recover);

        protected override void OnReset()
        {
            cooldownTimer = 1f;
            ChangeState(AI.Idle);
        }
    }
}

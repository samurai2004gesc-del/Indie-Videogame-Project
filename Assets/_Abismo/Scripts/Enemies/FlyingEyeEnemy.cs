using UnityEngine;

namespace Abismo
{
    /// <summary>
    /// Ojo del Vacío: flota sobre ti, atraviesa paredes (no pertenece a este mundo)
    /// y se lanza en picado. Tocarlo duele. Su picado se puede PARAR.
    /// </summary>
    public class FlyingEyeEnemy : Enemy
    {
        enum AI { Idle, Follow, Windup, Dive, Return }

        [Header("Ojo del Vacío")]
        [SerializeField] float aggroRange = 8f;
        [SerializeField] float leashRange = 18f;
        [SerializeField] float hoverHeight = 3f;
        [SerializeField] float followSpeed = 2.8f;
        [SerializeField] float windupTime = 0.55f;
        [SerializeField] float diveSpeed = 10f;
        [SerializeField] float diveTime = 0.55f;
        [SerializeField] float returnTime = 0.9f;
        [SerializeField] float cooldown = 1.6f;
        [SerializeField] int contactDamage = 10;
        [SerializeField] float contactRadius = 0.45f;

        AI ai = AI.Idle;
        float aiTimer, cooldownTimer;
        Vector2 home, diveDirection;

        protected override void Awake()
        {
            base.Awake();
            rb.bodyType = RigidbodyType2D.Kinematic;
            centerOffset = Vector2.zero;
            home = transform.position;
        }

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
            Vector2 pos = transform.position;
            float distance = Vector2.Distance(pos, player.Center);
            FacePlayer();
            lean = 0f;
            bob = Mathf.Sin(Time.time * 3f) * 0.12f;

            if (ai != AI.Idle && Vector2.Distance(pos, home) > leashRange) ChangeState(AI.Idle);

            switch (ai)
            {
                case AI.Idle:
                    MoveTowards(home + Vector2.up * Mathf.Sin(Time.time) * 0.3f, followSpeed * 0.6f);
                    if (distance < aggroRange && Vector2.Distance(pos, home) < leashRange * 0.5f) ChangeState(AI.Follow);
                    break;

                case AI.Follow:
                    Vector2 above = player.Center + new Vector2(Mathf.Sin(Time.time * 0.8f) * 2f, hoverHeight);
                    MoveTowards(above, followSpeed);
                    if (cooldownTimer <= 0f && distance < 7f) ChangeState(AI.Windup);
                    if (distance > aggroRange * 1.6f) ChangeState(AI.Idle);
                    break;

                case AI.Windup:
                    SetVelocity(Vector2.zero);
                    float charge = Mathf.Clamp01(aiTimer / windupTime);
                    bob = Random.Range(-0.05f, 0.05f) * charge;
                    if (flash != null) flash.Hold(new Color(1f, 0.2f, 0.2f), charge * 0.6f);
                    if (aiTimer >= windupTime)
                    {
                        diveDirection = (player.Center - pos).normalized;
                        squash = new Vector2(0.8f, 1.2f);
                        Sfx.Play(SfxId.EnemySwing);
                        ChangeState(AI.Dive);
                    }
                    break;

                case AI.Dive:
                    SetVelocity(diveDirection * diveSpeed);
                    bool hitGround = Physics2D.OverlapCircle(pos, contactRadius * 0.6f, GameLayers.GroundMask);
                    if (aiTimer >= diveTime || hitGround)
                    {
                        cooldownTimer = cooldown;
                        ChangeState(AI.Return);
                    }
                    break;

                case AI.Return:
                    MoveTowards(player.Center + Vector2.up * hoverHeight, followSpeed * 1.5f);
                    if (aiTimer >= returnTime) ChangeState(AI.Follow);
                    break;
            }

            // Daño por contacto (más fuerte durante el picado, y solo entonces se puede parar).
            var target = Combat.FindPlayerInCircle(pos, contactRadius);
            if (target != null)
            {
                bool diving = ai == AI.Dive;
                var result = target.TakeDamage(new DamageInfo(diving ? contactDamage + 4 : contactDamage, pos, 5f, diving, gameObject));
                if (result == DamageResult.Parried) OnParried();
            }
        }

        void MoveTowards(Vector2 target, float speed)
        {
            Vector2 delta = target - (Vector2)transform.position;
            SetVelocity(Vector2.ClampMagnitude(delta * 2.5f, speed));
        }

        protected override void OnStaggered()
        {
            rb.linearVelocity = Vector2.down * 0.5f;
            ChangeState(AI.Return);
        }

        protected override void OnReset() => ChangeState(AI.Idle);
    }
}

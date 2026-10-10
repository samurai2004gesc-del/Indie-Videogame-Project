using UnityEngine;

namespace Abismo
{
    /// <summary>
    /// Jefe: El Arcipreste de las Mareas (y, con otro arte y otros valores, El Que Susurra en la Oscuridad).
    /// Ataques: tentáculos que brotan del suelo, abanico de esferas (¡párelas para devolverlas!)
    /// y una embestida imparable (brilla en ROJO: no se puede parar, hay que esquivarla).
    /// Al bajar de la mitad de vida entra en la segunda fase y se vuelve más rápido.
    /// </summary>
    public class BossArchpriest : Enemy
    {
        enum Phase { Dormant, Intro, Idle, Tentacles, OrbsWindup, ChargeWindup, Charging, Recover, PhaseShift }

        [Header("Arcipreste")]
        public TentacleStrike tentaclePrefab;
        public Projectile orbPrefab;
        [SerializeField] float walkSpeed = 1.4f;
        [SerializeField] float chargeSpeed = 11f;
        [SerializeField] float chargeMaxTime = 1.6f;
        [SerializeField] int chargeDamage = 25;
        [SerializeField] int tentacleDamage = 20;
        [SerializeField] Vector2 orbOrigin = new Vector2(2.2f, 3.8f); // la brasa del báculo al apuntar
        [Tooltip("Lo que se lee al pasar a la segunda fase.")]
        [SerializeField] string phaseMessage = "La marea se embravece...";

        Phase phase = Phase.Dormant;
        float timer, recoverTime = 0.8f, nextTentacleAt;
        int stage = 1, tentaclesLeft, lastAttack = -1;
        bool chargeHit;

        public bool IsAwake => phase != Phase.Dormant;

        /// <summary>El mismo combate sirve para otros jefes (El Que Susurra en la Oscuridad): de dónde salen las esferas y su mensaje.</summary>
        public void ConfigureBoss(Vector2 castPoint, string secondPhaseMessage)
        {
            orbOrigin = castPoint;
            phaseMessage = secondPhaseMessage;
        }
        float Pace => stage == 2 ? 0.7f : 1f;

        /// <summary>Lo llama la arena cuando entras: empieza el combate.</summary>
        public void Awaken()
        {
            if (IsDead || phase != Phase.Dormant) return;
            ChangePhase(Phase.Intro);
            Sfx.Play(SfxId.Roar);
            GameFeel.Shake(0.6f);
            CameraFX.Warp(-0.35f);
        }

        void ChangePhase(Phase next)
        {
            phase = next;
            timer = 0f;
            if (flash != null) flash.ClearHold();
        }

        protected override bool CanFlinch => false;
        protected override bool Executable => false;

        static string AnimationFor(Phase phase, bool walking)
        {
            switch (phase)
            {
                case Phase.Intro: return "intro";
                case Phase.Idle: return walking ? "walk" : "idle";
                case Phase.Tentacles: return "tentacles";
                case Phase.OrbsWindup: return "orbs";
                case Phase.ChargeWindup: return "charge_windup";
                case Phase.Charging: return "charge";
                case Phase.Recover: return "recover";
                case Phase.PhaseShift: return "phase";
                default: return "idle";
            }
        }

        protected override void Tick(float dt)
        {
            timer += dt;
            bool walkingNow = false;

            switch (phase)
            {
                case Phase.Dormant:
                    SetHorizontalVelocity(0f);
                    FacePlayer();
                    break;

                case Phase.Intro:
                    SetHorizontalVelocity(0f);
                    if (timer >= 1.8f) ChangePhase(Phase.Idle);
                    break;

                case Phase.Idle:
                    FacePlayer();
                    float dx = Mathf.Abs(player.transform.position.x - transform.position.x);
                    bool walking = dx > 3.5f && !IsWallAhead();
                    SetHorizontalVelocity(walking ? facing * walkSpeed : 0f);
                    walkingNow = walking;
                    if (timer >= 1.1f * Pace) ChooseAttack();
                    break;

                case Phase.Tentacles:
                    SetHorizontalVelocity(0f);
                    if (tentaclesLeft > 0 && timer >= nextTentacleAt)
                    {
                        SpawnTentacle(player.transform.position.x);
                        tentaclesLeft--;
                        nextTentacleAt = timer + 0.45f * Pace;
                    }
                    if (tentaclesLeft == 0 && timer >= nextTentacleAt + 0.6f)
                    {
                        recoverTime = 0.8f;
                        ChangePhase(Phase.Recover);
                    }
                    break;

                case Phase.OrbsWindup:
                    SetHorizontalVelocity(0f);
                    FacePlayer();
                    float charge = Mathf.Clamp01(timer / (0.9f * Pace));
                    if (flash != null) flash.Hold(new Color(0.85f, 0.4f, 1f), charge * 0.6f);
                    if (timer >= 0.9f * Pace)
                    {
                        FireOrbs();
                        recoverTime = 0.9f;
                        ChangePhase(Phase.Recover);
                    }
                    break;

                case Phase.ChargeWindup:
                    SetHorizontalVelocity(0f);
                    // ROJO = imparable (como en Blasphemous): no se puede parar.
                    if (flash != null) flash.Hold(new Color(1f, 0.1f, 0.1f), 0.3f + 0.4f * Mathf.PingPong(timer * 8f, 1f));
                    if (timer >= 0.85f * Pace)
                    {
                        chargeHit = false;
                        ChangePhase(Phase.Charging);
                        Sfx.Play(SfxId.Roar, 0.6f);
                    }
                    break;

                case Phase.Charging:
                    SetHorizontalVelocity(facing * chargeSpeed);
                    if (!chargeHit)
                    {
                        var result = TryHitPlayer(new Vector2(0.8f, 1.4f), new Vector2(2.4f, 2.6f), chargeDamage, 9f, false);
                        if (result != DamageResult.Ignored) chargeHit = true;
                    }
                    if (IsWallAhead(0.3f) || timer >= chargeMaxTime)
                    {
                        GameFeel.Shake(0.5f);
                        Sfx.Play(SfxId.HeavyHit);
                        Effects.Dust(transform.position + new Vector3(facing, 0f), 12);
                        recoverTime = 1.2f;
                        ChangePhase(Phase.Recover);
                    }
                    break;

                case Phase.Recover:
                    SetHorizontalVelocity(0f);
                    if (timer >= recoverTime * Pace) ChangePhase(Phase.Idle);
                    break;

                case Phase.PhaseShift:
                    SetHorizontalVelocity(0f);
                    if (flash != null) flash.Hold(new Color(0.4f, 1f, 0.7f), 0.3f + 0.3f * Mathf.PingPong(timer * 4f, 1f));
                    if (timer >= 1.6f) ChangePhase(Phase.Idle);
                    break;
            }
            Animate(AnimationFor(phase, walkingNow));
        }

        void ChooseAttack()
        {
            int attack = Random.Range(0, 3);
            if (attack == lastAttack && Random.value < 0.7f) attack = (attack + 1) % 3;
            lastAttack = attack;

            switch (attack)
            {
                case 0:
                    tentaclesLeft = stage == 2 ? 5 : 3;
                    nextTentacleAt = 0.3f;
                    ChangePhase(Phase.Tentacles);
                    Sfx.Play(SfxId.Chant);
                    break;
                case 1:
                    ChangePhase(Phase.OrbsWindup);
                    Sfx.Play(SfxId.Chant, 0.8f);
                    break;
                default:
                    FacePlayer();
                    ChangePhase(Phase.ChargeWindup);
                    break;
            }
        }

        void SpawnTentacle(float x)
        {
            if (tentaclePrefab == null) return;
            Vector2 origin = new Vector2(x, transform.position.y + 3f);
            var hit = Physics2D.Raycast(origin, Vector2.down, 10f, GameLayers.GroundMask);
            float y = hit ? hit.point.y : transform.position.y;
            var strike = Instantiate(tentaclePrefab, new Vector3(x, y, 0f), Quaternion.identity);
            strike.Init(tentacleDamage, stage == 2 ? 0.6f : 0.8f);
        }

        void FireOrbs()
        {
            if (orbPrefab == null) return;
            int count = stage == 2 ? 7 : 5;
            const float spread = 70f;
            Vector2 origin = (Vector2)transform.position + new Vector2(orbOrigin.x * facing, orbOrigin.y);
            Vector2 aim = (player.Center - origin).normalized;
            float baseAngle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
            for (int i = 0; i < count; i++)
            {
                float angle = (baseAngle + Mathf.Lerp(-spread * 0.5f, spread * 0.5f, i / (count - 1f))) * Mathf.Deg2Rad;
                var orb = Instantiate(orbPrefab, origin, Quaternion.identity);
                orb.Launch(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)), false);
            }
            Sfx.Play(SfxId.Spell);
        }

        protected override void OnHurt(DamageInfo info)
        {
            if (stage == 1 && health > 0 && health <= maxHealth / 2)
            {
                stage = 2;
                ChangePhase(Phase.PhaseShift);
                Sfx.Play(SfxId.Roar);
                GameFeel.Shake(0.7f);
                CameraFX.Warp(-0.45f);
                CameraFX.Chromatic(0.8f);
                if (HUD.Instance != null && !string.IsNullOrEmpty(phaseMessage)) HUD.Instance.ShowMessage(phaseMessage);
            }
        }

        // El Arcipreste no se aturde con las paradas: solo sus esferas devueltas le hieren.
        public override void OnParried() { }

        protected override void OnDied()
        {
            if (GameManager.Instance != null) GameManager.Instance.OnBossDefeated(this);
        }

        protected override void OnReset()
        {
            stage = 1;
            lastAttack = -1;
            ChangePhase(Phase.Dormant);
        }
    }
}

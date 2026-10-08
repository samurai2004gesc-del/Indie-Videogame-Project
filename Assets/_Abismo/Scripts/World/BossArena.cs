using UnityEngine;

namespace Abismo
{
    /// <summary>
    /// Arena del jefe: al entrar se cierran las rejas y despierta el jefe.
    /// Si mueres, todo vuelve a empezar; si lo vences, las rejas se abren.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class BossArena : MonoBehaviour, IResettable
    {
        public BossArchpriest boss;
        public GameObject[] gates;

        bool fightStarted;

        void Start() => SetGates(false);

        void OnTriggerEnter2D(Collider2D other)
        {
            if (fightStarted || boss == null || boss.IsDead) return;
            if (other.GetComponentInParent<PlayerController>() == null) return;

            fightStarted = true;
            SetGates(true);
            Sfx.Play(SfxId.Gate);
            GameFeel.Shake(0.4f);
            boss.Awaken();
            if (HUD.Instance != null) HUD.Instance.ShowBossBar(boss);
        }

        void Update()
        {
            if (fightStarted && boss != null && boss.IsDead)
            {
                fightStarted = false;
                SetGates(false);
                Sfx.Play(SfxId.Gate);
            }
        }

        public void ResetState()
        {
            if (boss != null && boss.IsDead) return;
            fightStarted = false;
            SetGates(false);
        }

        void SetGates(bool closed)
        {
            if (gates == null) return;
            foreach (var gate in gates)
            {
                if (gate != null) gate.SetActive(closed);
            }
        }
    }
}

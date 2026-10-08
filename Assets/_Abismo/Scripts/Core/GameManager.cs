using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Abismo
{
    /// <summary>
    /// Director de la partida: puntos de control (altares), muerte y reaparición,
    /// fragmentos de mente perdidos, pausa, hit-stop y cámara lenta.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Referencias (las asigna el constructor automático)")]
        public PlayerController player;
        public CameraFollow cameraFollow;
        public HUD hud;
        public MindFragment mindFragmentPrefab;
        public Material additiveMaterial;
        [Tooltip("Material de sprite SIN iluminación (partículas, brillos que no deben oscurecerse).")]
        public Material unlitMaterial;

        [Header("Mundo")]
        [SerializeField] float killY = -4f;
        [SerializeField] bool showTitleCard = true;
        [SerializeField] string firstAreaTitle = "Costa de Innsmouth";

        public PlayerController Player => player;
        public float KillY => killY;
        public bool IsPaused => paused;
        public bool InputLocked => lockCount > 0 || paused || respawning || Time.frameCount <= unlockFrame;

        readonly List<IResettable> resettables = new List<IResettable>();
        Vector2 checkpoint;
        ElderSignAltar currentAltar;
        MindFragment activeFragment;
        string currentArea = "";
        int lockCount;
        int unlockFrame = -1;
        bool paused, respawning;
        float hitStopUntil, slowMotionUntil;

        public void Configure(float killHeight, string areaTitle)
        {
            killY = killHeight;
            firstAreaTitle = areaTitle;
        }

        void Awake()
        {
            Instance = this;
            GameLayers.SetupCollisionMatrix();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Time.timeScale = 1f;
        }

        void Start()
        {
            if (player == null) player = FindAnyObjectByType<PlayerController>();
            if (cameraFollow == null) cameraFollow = FindAnyObjectByType<CameraFollow>();
            if (hud == null) hud = FindAnyObjectByType<HUD>();
            if (player != null) checkpoint = player.transform.position;

            foreach (var behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
            {
                if (behaviour is IResettable resettable) resettables.Add(resettable);
            }

            StartCoroutine(IntroRoutine());
        }

        IEnumerator IntroRoutine()
        {
            if (showTitleCard && hud != null)
            {
                LockInput();
                hud.ShowTitleCard(true);
                yield return new WaitForSecondsRealtime(1.2f);
                while (InputReader.Instance == null || !InputReader.Instance.AnyPressed) yield return null;
                hud.ShowTitleCard(false);
                yield return new WaitForSecondsRealtime(0.6f);
                UnlockInput();
            }
            EnterArea(firstAreaTitle);
        }

        void Update()
        {
            var input = InputReader.Instance;
            bool canPause = player != null && !player.IsDead && !respawning && (hud == null || !hud.IsBusy);
            if (input != null && input.PausePressed && canPause) SetPaused(!paused);

            if (paused) Time.timeScale = 0f;
            else if (Time.unscaledTime < hitStopUntil) Time.timeScale = 0f;
            else if (Time.unscaledTime < slowMotionUntil) Time.timeScale = 0.35f;
            else Time.timeScale = 1f;
        }

        // ------------------------------------------------------------------
        // Sensaciones
        // ------------------------------------------------------------------

        /// <summary>Congela el juego unas centésimas: hace que los golpes "pesen".</summary>
        public void HitStop(float seconds) => hitStopUntil = Mathf.Max(hitStopUntil, Time.unscaledTime + seconds);

        public void SlowMotion(float seconds) => slowMotionUntil = Mathf.Max(slowMotionUntil, Time.unscaledTime + seconds);

        public void SetPaused(bool value)
        {
            paused = value;
            if (hud != null) hud.ShowPause(value);
        }

        public void LockInput() => lockCount++;

        public void UnlockInput()
        {
            lockCount = Mathf.Max(0, lockCount - 1);
            unlockFrame = Time.frameCount; // la pulsación que desbloquea no cuenta como acción
        }

        public void EnterArea(string title)
        {
            if (string.IsNullOrEmpty(title) || title == currentArea) return;
            currentArea = title;
            if (hud != null) hud.ShowAreaTitle(title);
        }

        // ------------------------------------------------------------------
        // Altares, muerte y reaparición
        // ------------------------------------------------------------------

        public void RestAtAltar(ElderSignAltar altar)
        {
            if (currentAltar != null && currentAltar != altar) currentAltar.SetLit(false);
            currentAltar = altar;
            altar.SetLit(true);
            checkpoint = altar.RespawnPoint;

            player.Stats.RefillAll();
            ResetWorld();
            Sfx.Play(SfxId.Altar);
            Effects.Sparkle(altar.transform.position + Vector3.up * 1.5f, new Color(0.5f, 1f, 0.85f), 24);
            if (hud != null) hud.ShowMessage("Rezas ante el Signo Antiguo.\nTus heridas sanan... y las criaturas despiertan.");
        }

        public void OnPlayerDied(Vector2 fragmentPosition)
        {
            if (!respawning) StartCoroutine(DeathRoutine(fragmentPosition));
        }

        IEnumerator DeathRoutine(Vector2 fragmentPosition)
        {
            respawning = true;
            SlowMotion(0.6f);
            yield return new WaitForSecondsRealtime(1.1f);
            if (hud != null) hud.ShowDeathScreen(true);
            yield return new WaitForSecondsRealtime(2.8f);

            // Parte de tu mente se queda donde caíste (como la Culpa en Blasphemous).
            if (activeFragment != null) Destroy(activeFragment.gameObject);
            if (mindFragmentPrefab != null)
            {
                activeFragment = Instantiate(mindFragmentPrefab, fragmentPosition + Vector2.up * 1f, Quaternion.identity);
            }
            player.Stats.SetMindPenalty(true);

            player.Respawn(checkpoint);
            ResetWorld();
            if (cameraFollow != null) cameraFollow.SnapToTarget();
            if (hud != null)
            {
                hud.HideBossBar();
                hud.ShowDeathScreen(false);
            }
            yield return new WaitForSecondsRealtime(0.8f);
            respawning = false;
            if (hud != null) hud.ShowMessage("Un fragmento de tu mente quedó atrás.\nRecupéralo donde caíste.");
        }

        public void OnMindFragmentRecovered(MindFragment fragment)
        {
            if (activeFragment == fragment) activeFragment = null;
            player.Stats.SetMindPenalty(false);
            Sfx.Play(SfxId.Fragment);
            if (hud != null) hud.ShowMessage("Has recuperado un fragmento de tu mente.");
        }

        public void OnBossDefeated(Enemy boss)
        {
            SlowMotion(1.2f);
            GameFeel.Shake(0.8f);
            Sfx.Play(SfxId.Roar, 0.8f);
            if (hud != null)
            {
                hud.HideBossBar();
                hud.ShowBanner(boss.DisplayName.ToUpperInvariant(), "HA REGRESADO AL SUEÑO");
            }
        }

        /// <summary>Devuelve enemigos, puertas, etc. a su estado inicial y borra ataques en vuelo.</summary>
        void ResetWorld()
        {
            foreach (var resettable in resettables)
            {
                if (resettable is Object unityObject && unityObject == null) continue;
                resettable.ResetState();
            }
            foreach (var projectile in FindObjectsByType<Projectile>()) Destroy(projectile.gameObject);
            foreach (var tentacle in FindObjectsByType<TentacleStrike>()) Destroy(tentacle.gameObject);
        }
    }
}

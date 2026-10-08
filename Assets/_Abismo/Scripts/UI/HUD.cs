using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Abismo
{
    /// <summary>
    /// Toda la interfaz se construye por código al empezar (no hace falta montar nada a mano):
    /// barras de Vida y Revelación, frascos de láudano, oro, avisos de interacción, nombres de zona,
    /// mensajes, lectura de inscripciones, pausa, pantalla de muerte, barra del jefe y pantalla de título.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    public class HUD : MonoBehaviour
    {
        public static HUD Instance { get; private set; }

        [Header("Sprites (los asigna el constructor; si faltan se usan rectángulos)")]
        public Sprite flaskFullSprite;
        public Sprite flaskEmptySprite;
        public Sprite goldSprite;
        public Sprite vignetteSprite;

        static readonly Color HealthColor = new Color(0.66f, 0.1f, 0.12f);
        static readonly Color RevelationColor = new Color(0.25f, 0.8f, 0.65f);
        static readonly Color TrailColor = new Color(0.95f, 0.85f, 0.6f);
        static readonly Color TitleColor = new Color(0.85f, 0.95f, 0.88f);
        static readonly Color GoldColor = new Color(0.95f, 0.78f, 0.35f);
        static readonly Color DeathColor = new Color(0.65f, 0.08f, 0.1f);

        Font font;

        RectTransform healthFill, healthTrail, revelationFill, revelationLock, revelationBar;
        float healthTrailValue = 1f, revelationPulse;
        RectTransform flaskRow;
        readonly List<Image> flaskIcons = new List<Image>();
        Text goldText;
        int shownGold = -1;

        CanvasGroup promptGroup;
        Text promptText;

        CanvasGroup areaGroup;
        Text areaText;
        float areaTimer = -1f;

        CanvasGroup messageGroup;
        Text messageText;
        float messageTimer = -1f;

        CanvasGroup bannerGroup;
        Text bannerTitle, bannerSubtitle;
        float bannerTimer = -1f;

        CanvasGroup bossGroup;
        RectTransform bossFill, bossTrail;
        Text bossName;
        Enemy boss;
        float bossTrailValue = 1f;

        CanvasGroup readingGroup, pauseGroup, deathGroup, titleGroup;
        Text readingText, titlePrompt;
        Image vignette, damageFlash;
        float damageFlashAlpha;

        float readingTarget, pauseTarget, deathTarget, titleTarget, bossTarget;
        int readingOpenedFrame;

        public bool IsReading { get; private set; }
        public bool IsBusy => IsReading || titleTarget > 0f;

        void Awake()
        {
            Instance = this;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildInterface();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ------------------------------------------------------------------
        // API pública
        // ------------------------------------------------------------------

        public void ShowAreaTitle(string title)
        {
            areaText.text = title.ToUpperInvariant();
            areaTimer = 0f;
        }

        public void ShowMessage(string message)
        {
            messageText.text = message;
            messageTimer = 0f;
            Sfx.Play(SfxId.Message, 0.6f, 0f);
        }

        public void ShowBanner(string title, string subtitle)
        {
            bannerTitle.text = title;
            bannerSubtitle.text = subtitle;
            bannerTimer = 0f;
        }

        public void ShowReading(string text)
        {
            if (IsReading) return;
            IsReading = true;
            readingText.text = text;
            readingTarget = 1f;
            readingOpenedFrame = Time.frameCount;
            if (GameManager.Instance != null) GameManager.Instance.LockInput();
        }

        public void ShowPause(bool visible) => pauseTarget = visible ? 1f : 0f;
        public void ShowDeathScreen(bool visible) => deathTarget = visible ? 1f : 0f;
        public void ShowTitleCard(bool visible)
        {
            titleTarget = visible ? 1f : 0f;
            if (visible) titleGroup.alpha = 1f;
        }

        public void ShowBossBar(Enemy target)
        {
            boss = target;
            bossName.text = target.DisplayName;
            bossTrailValue = 1f;
            bossTarget = 1f;
        }

        public void HideBossBar() => bossTarget = 0f;

        public void FlashDamage() => damageFlashAlpha = 0.35f;

        public void PulseRevelationBar() => revelationPulse = 1f;

        // ------------------------------------------------------------------
        // Actualización
        // ------------------------------------------------------------------

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            var player = GameManager.Instance != null ? GameManager.Instance.Player : null;

            if (player != null)
            {
                var stats = player.Stats;
                float hp = stats.MaxHealth > 0 ? (float)stats.Health / stats.MaxHealth : 0f;
                SetFill(healthFill, hp);
                healthTrailValue = hp < healthTrailValue ? Mathf.MoveTowards(healthTrailValue, hp, dt * 0.5f) : hp;
                SetFill(healthTrail, healthTrailValue);

                float baseMax = Mathf.Max(1, stats.MaxRevelationBase);
                SetFill(revelationFill, stats.Revelation / baseMax);
                revelationLock.gameObject.SetActive(stats.MindPenalty);
                revelationLock.anchorMin = new Vector2(stats.MaxRevelation / baseMax, 0f);

                revelationPulse = Mathf.MoveTowards(revelationPulse, 0f, dt * 3f);
                revelationBar.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(revelationPulse * Mathf.PI * 4f) * revelationPulse);

                UpdateFlasks(stats.Flasks, stats.MaxFlasks);
                if (stats.Gold != shownGold)
                {
                    shownGold = stats.Gold;
                    goldText.text = shownGold.ToString();
                }

                var interactable = player.CurrentInteractable;
                bool showPrompt = interactable != null && !IsReading && !player.IsDead && titleTarget <= 0f;
                if (interactable != null) promptText.text = "[E]  " + interactable.Prompt;
                promptGroup.alpha = Mathf.MoveTowards(promptGroup.alpha, showPrompt ? 1f : 0f, dt * 6f);

                float danger = hp < 0.3f && !player.IsDead ? 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f) : 0f;
                if (vignette != null) vignette.color = Color.Lerp(new Color(0f, 0f, 0f, 0.85f), new Color(0.45f, 0f, 0.02f, 0.95f), danger * 0.7f);
            }

            damageFlashAlpha = Mathf.MoveTowards(damageFlashAlpha, 0f, dt * 1.2f);
            damageFlash.color = new Color(0.6f, 0f, 0.05f, damageFlashAlpha);

            areaGroup.alpha = TimedAlpha(ref areaTimer, dt, 0.8f, 2.6f, 1.2f);
            messageGroup.alpha = TimedAlpha(ref messageTimer, dt, 0.3f, 3.2f, 0.8f);
            bannerGroup.alpha = TimedAlpha(ref bannerTimer, dt, 1f, 3.5f, 1.5f);

            Approach(readingGroup, readingTarget, dt * 6f);
            Approach(pauseGroup, pauseTarget, dt * 8f);
            Approach(deathGroup, deathTarget, dt * 1.2f);
            Approach(titleGroup, titleTarget, dt * 1.5f);
            Approach(bossGroup, bossTarget, dt * 2f);
            if (titlePrompt != null) titlePrompt.color = new Color(1f, 1f, 1f, 0.35f + 0.35f * Mathf.Sin(Time.unscaledTime * 3f));

            if (boss != null)
            {
                float v = boss.MaxHealth > 0 ? Mathf.Max(0f, (float)boss.Health / boss.MaxHealth) : 0f;
                SetFill(bossFill, v);
                bossTrailValue = v < bossTrailValue ? Mathf.MoveTowards(bossTrailValue, v, dt * 0.4f) : v;
                SetFill(bossTrail, bossTrailValue);
            }

            if (IsReading && Time.frameCount > readingOpenedFrame)
            {
                var input = InputReader.Instance;
                if (input != null && (input.InteractPressed || input.AttackPressed || input.JumpPressed || input.PausePressed))
                {
                    IsReading = false;
                    readingTarget = 0f;
                    if (GameManager.Instance != null) GameManager.Instance.UnlockInput();
                }
            }
        }

        void UpdateFlasks(int current, int max)
        {
            while (flaskIcons.Count < max)
            {
                var icon = NewImage("Frasco", flaskRow, Color.white, flaskFullSprite);
                Place(icon.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                      new Vector2(flaskIcons.Count * 26f, 0f), new Vector2(18f, 26f));
                icon.preserveAspect = true;
                flaskIcons.Add(icon);
            }
            for (int i = 0; i < flaskIcons.Count; i++)
            {
                var icon = flaskIcons[i];
                icon.gameObject.SetActive(i < max);
                bool full = i < current;
                if (flaskFullSprite != null && flaskEmptySprite != null)
                {
                    icon.sprite = full ? flaskFullSprite : flaskEmptySprite;
                    icon.color = Color.white;
                }
                else
                {
                    icon.color = full ? new Color(0.9f, 0.5f, 0.2f) : new Color(0.25f, 0.2f, 0.2f);
                }
            }
        }

        static float TimedAlpha(ref float timer, float dt, float fadeIn, float hold, float fadeOut)
        {
            if (timer < 0f) return 0f;
            timer += dt;
            if (timer < fadeIn) return timer / fadeIn;
            if (timer < fadeIn + hold) return 1f;
            if (timer < fadeIn + hold + fadeOut) return 1f - (timer - fadeIn - hold) / fadeOut;
            timer = -1f;
            return 0f;
        }

        static void Approach(CanvasGroup group, float target, float speed) =>
            group.alpha = Mathf.MoveTowards(group.alpha, target, speed);

        static void SetFill(RectTransform rt, float value)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // ------------------------------------------------------------------
        // Construcción de la interfaz
        // ------------------------------------------------------------------

        void BuildInterface()
        {
            var canvasGO = new GameObject("Canvas HUD", typeof(RectTransform));
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            Transform root = canvasGO.transform;

            // Viñeta y destello de daño (debajo de todo lo demás)
            if (vignetteSprite != null)
            {
                vignette = NewImage("Viñeta", root, new Color(0f, 0f, 0f, 0.85f), vignetteSprite);
                Stretch(vignette.rectTransform);
            }
            damageFlash = NewImage("DestelloDaño", root, new Color(0.6f, 0f, 0.05f, 0f));
            Stretch(damageFlash.rectTransform);

            BuildStatusPanel(root);
            BuildGoldCounter(root);

            // Aviso de interacción
            promptGroup = NewGroup("Aviso", root);
            Place((RectTransform)promptGroup.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(700f, 40f));
            promptText = NewText("Texto", promptGroup.transform, "", 24, TextAnchor.MiddleCenter, TitleColor);
            Stretch(promptText.rectTransform);
            promptGroup.alpha = 0f;

            // Nombre de la zona
            areaGroup = NewGroup("NombreZona", root);
            Place((RectTransform)areaGroup.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 170f), new Vector2(1000f, 90f));
            areaText = NewText("Titulo", areaGroup.transform, "", 46, TextAnchor.MiddleCenter, TitleColor);
            Stretch(areaText.rectTransform);
            AddDecorLine(areaGroup.transform, -32f);
            AddDecorLine(areaGroup.transform, 32f);
            areaGroup.alpha = 0f;

            // Mensajes
            messageGroup = NewGroup("Mensaje", root);
            Place((RectTransform)messageGroup.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -150f), new Vector2(1000f, 90f));
            messageText = NewText("Texto", messageGroup.transform, "", 24, TextAnchor.MiddleCenter, new Color(0.8f, 0.92f, 0.86f));
            Stretch(messageText.rectTransform);
            messageGroup.alpha = 0f;

            // Estandarte (jefe derrotado)
            bannerGroup = NewGroup("Estandarte", root);
            Stretch((RectTransform)bannerGroup.transform);
            var bannerBack = NewImage("Fondo", bannerGroup.transform, new Color(0f, 0f, 0f, 0.6f));
            Place(bannerBack.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(1400f, 170f));
            bannerTitle = NewText("Titulo", bannerGroup.transform, "", 52, TextAnchor.MiddleCenter, GoldColor);
            Place(bannerTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 45f), new Vector2(1300f, 70f));
            bannerSubtitle = NewText("Subtitulo", bannerGroup.transform, "", 28, TextAnchor.MiddleCenter, TitleColor);
            Place(bannerSubtitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -15f), new Vector2(1300f, 50f));
            bannerGroup.alpha = 0f;

            BuildBossBar(root);
            BuildReadingPanel(root);
            BuildPausePanel(root);
            BuildDeathScreen(root);
            BuildTitleCard(root);
        }

        void BuildStatusPanel(Transform root)
        {
            var panel = NewRect("Estado", root);
            Place(panel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -28f), new Vector2(360f, 90f));

            healthFill = MakeBar(panel, new Vector2(0f, 0f), new Vector2(320f, 18f), HealthColor, out healthTrail, out _);
            revelationFill = MakeBar(panel, new Vector2(0f, -28f), new Vector2(220f, 10f), RevelationColor, out var revTrail, out revelationBar);
            revTrail.gameObject.SetActive(false);

            // Parte "bloqueada" de la Revelación mientras falta un fragmento de mente.
            var lockImage = NewImage("Bloqueo", revelationFill.parent, new Color(0.25f, 0.1f, 0.3f, 0.95f));
            revelationLock = lockImage.rectTransform;
            revelationLock.anchorMin = new Vector2(0.66f, 0f);
            revelationLock.anchorMax = Vector2.one;
            revelationLock.offsetMin = revelationLock.offsetMax = Vector2.zero;
            revelationLock.gameObject.SetActive(false);

            flaskRow = NewRect("Frascos", panel);
            Place(flaskRow, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -50f), new Vector2(200f, 26f));
        }

        void BuildGoldCounter(Transform root)
        {
            var panel = NewRect("Oro", root);
            Place(panel, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-32f, -28f), new Vector2(200f, 32f));
            var icon = NewImage("Icono", panel, goldSprite != null ? Color.white : GoldColor, goldSprite);
            Place(icon.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(24f, 24f));
            icon.preserveAspect = true;
            goldText = NewText("Cantidad", panel, "0", 26, TextAnchor.MiddleRight, GoldColor);
            Place(goldText.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-34f, 0f), new Vector2(160f, 32f));
        }

        void BuildBossBar(Transform root)
        {
            bossGroup = NewGroup("BarraJefe", root);
            Place((RectTransform)bossGroup.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(700f, 60f));
            bossName = NewText("Nombre", bossGroup.transform, "", 24, TextAnchor.MiddleCenter, TitleColor);
            Place(bossName.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(700f, 30f));
            bossFill = MakeBar(bossGroup.transform, new Vector2(10f, -34f), new Vector2(680f, 14f), HealthColor, out bossTrail, out _);
            bossGroup.alpha = 0f;
        }

        void BuildReadingPanel(Transform root)
        {
            readingGroup = NewGroup("Lectura", root);
            Stretch((RectTransform)readingGroup.transform);
            var dim = NewImage("Oscurecer", readingGroup.transform, new Color(0f, 0f, 0f, 0.55f));
            Stretch(dim.rectTransform);
            var frame = NewImage("Marco", readingGroup.transform, new Color(0.35f, 0.6f, 0.5f, 0.9f));
            Place(frame.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860f, 320f));
            var back = NewImage("Fondo", frame.transform, new Color(0.04f, 0.06f, 0.06f, 0.97f));
            Stretch(back.rectTransform, 3f);
            readingText = NewText("Texto", back.transform, "", 25, TextAnchor.MiddleCenter, new Color(0.85f, 0.92f, 0.86f));
            Stretch(readingText.rectTransform, 40f);
            readingText.horizontalOverflow = HorizontalWrapMode.Wrap;
            readingText.lineSpacing = 1.15f;
            var hint = NewText("Pista", back.transform, "[E]  Continuar", 18, TextAnchor.LowerRight, new Color(0.6f, 0.75f, 0.68f));
            Stretch(hint.rectTransform, 14f);
            readingGroup.alpha = 0f;
        }

        void BuildPausePanel(Transform root)
        {
            pauseGroup = NewGroup("Pausa", root);
            Stretch((RectTransform)pauseGroup.transform);
            var dim = NewImage("Oscurecer", pauseGroup.transform, new Color(0f, 0.02f, 0.02f, 0.8f));
            Stretch(dim.rectTransform);
            var title = NewText("Titulo", pauseGroup.transform, "PAUSA", 56, TextAnchor.MiddleCenter, TitleColor);
            Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 230f), new Vector2(800f, 80f));
            const string controls =
                "Mover ............ A / D  ·  Flechas  ·  Stick\n" +
                "Saltar ........... Espacio  ·  K  ·  Botón A\n" +
                "Atacar ........... J  ·  Botón X\n" +
                "Esquivar ......... L  ·  Shift  ·  Botón B\n" +
                "Parar (parry) .... I  ·  RB\n" +
                "Signo Arcano ..... U  ·  Botón Y\n" +
                "Beber láudano .... F  ·  LB\n" +
                "Interactuar ...... E  ·  W  ·  Arriba\n" +
                "Bajar plataforma . Abajo + Saltar\n\n" +
                "Esc / Start para continuar";
            var list = NewText("Controles", pauseGroup.transform, controls, 24, TextAnchor.MiddleCenter, new Color(0.75f, 0.88f, 0.82f));
            Place(list.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(900f, 420f));
            pauseGroup.alpha = 0f;
        }

        void BuildDeathScreen(Transform root)
        {
            deathGroup = NewGroup("Muerte", root);
            Stretch((RectTransform)deathGroup.transform);
            var black = NewImage("Negro", deathGroup.transform, new Color(0f, 0f, 0f, 0.92f));
            Stretch(black.rectTransform);
            var text = NewText("Texto", deathGroup.transform, "EL ABISMO TE RECLAMA", 60, TextAnchor.MiddleCenter, DeathColor);
            Place(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(1200f, 100f));
            deathGroup.alpha = 0f;
        }

        void BuildTitleCard(Transform root)
        {
            titleGroup = NewGroup("Titulo", root);
            Stretch((RectTransform)titleGroup.transform);
            var black = NewImage("Negro", titleGroup.transform, new Color(0.01f, 0.02f, 0.02f, 1f));
            Stretch(black.rectTransform);
            var title = NewText("Nombre", titleGroup.transform, "A B I S M O", 96, TextAnchor.MiddleCenter, TitleColor);
            Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 90f), new Vector2(1200f, 130f));
            var subtitle = NewText("Subtitulo", titleGroup.transform, "El Sueño de R'lyeh", 32, TextAnchor.MiddleCenter, RevelationColor);
            Place(subtitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(1000f, 50f));
            var quote = NewText("Cita", titleGroup.transform,
                "«Que no está muerto lo que yace eternamente,\ny con los eones extraños incluso la muerte puede morir.»",
                20, TextAnchor.MiddleCenter, new Color(0.55f, 0.65f, 0.6f));
            Place(quote.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -90f), new Vector2(1000f, 70f));
            titlePrompt = NewText("Pulsa", titleGroup.transform, "Pulsa cualquier botón", 24, TextAnchor.MiddleCenter, Color.white);
            Place(titlePrompt.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(800f, 40f));
            titleGroup.alpha = 0f;
        }

        RectTransform MakeBar(Transform parent, Vector2 position, Vector2 size, Color fillColor,
                              out RectTransform trail, out RectTransform frameRect)
        {
            var frame = NewImage("Barra", parent, new Color(0.02f, 0.02f, 0.03f, 0.9f));
            frameRect = frame.rectTransform;
            Place(frameRect, new Vector2(0f, 1f), new Vector2(0f, 1f), position, size + new Vector2(6f, 6f));
            var back = NewImage("Fondo", frame.transform, new Color(0.13f, 0.11f, 0.12f));
            Stretch(back.rectTransform, 3f);
            var trailImage = NewImage("Rastro", back.transform, TrailColor);
            trail = trailImage.rectTransform;
            SetFill(trail, 1f);
            var fill = NewImage("Relleno", back.transform, fillColor);
            SetFill(fill.rectTransform, 1f);
            var shine = NewImage("Brillo", fill.transform, new Color(1f, 1f, 1f, 0.18f));
            shine.rectTransform.anchorMin = new Vector2(0f, 0.55f);
            shine.rectTransform.anchorMax = Vector2.one;
            shine.rectTransform.offsetMin = shine.rectTransform.offsetMax = Vector2.zero;
            return fill.rectTransform;
        }

        void AddDecorLine(Transform parent, float y)
        {
            var line = NewImage("Linea", parent, new Color(0.6f, 0.85f, 0.75f, 0.6f));
            Place(line.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(520f, 2f));
        }

        // ------------------------------------------------------------------
        // Ayudas para crear elementos de UI
        // ------------------------------------------------------------------

        static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static CanvasGroup NewGroup(string name, Transform parent)
        {
            var rt = NewRect(name, parent);
            var group = rt.gameObject.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;
            return group;
        }

        static Image NewImage(string name, Transform parent, Color color, Sprite sprite = null)
        {
            var rt = NewRect(name, parent);
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        Text NewText(string name, Transform parent, string content, int size, TextAnchor anchor, Color color)
        {
            var rt = NewRect(name, parent);
            var text = rt.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = color;
            text.text = content;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            var shadow = rt.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            shadow.effectDistance = new Vector2(2f, -2f);
            return text;
        }

        static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        static void Stretch(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }
    }
}

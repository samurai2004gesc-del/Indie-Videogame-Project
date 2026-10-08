using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Abismo
{
    /// <summary>
    /// Toda la interfaz se construye por código al empezar (no hace falta montar nada a mano), a escala de
    /// pixel art: un lienzo de 640×360 que se amplía ×2, ×3... Marcos góticos de oro, barras de Vida y
    /// Revelación, frascos de láudano, oro, avisos, nombres de zona, inscripciones, pausa, muerte, jefe y título.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    public class HUD : MonoBehaviour
    {
        public static HUD Instance { get; private set; }

        [Header("Sprites (los asigna el constructor; si faltan se usan rectángulos)")]
        public Sprite portraitSprite;
        public Sprite barFrameSprite;
        public Sprite healthFillSprite;
        public Sprite revelationFillSprite;
        public Sprite barBackSprite;
        public Sprite barTrailSprite;
        public Sprite flaskFullSprite;
        public Sprite flaskEmptySprite;
        public Sprite goldSprite;
        public Sprite goldFrameSprite;
        public Sprite panelSprite;
        public Sprite separatorSprite;
        public Sprite bossFrameSprite;
        public Sprite promptSprite;
        public Sprite vignetteSprite;
        public Sprite signSprite;

        [Header("Fuentes pixel (tamaño de diseño: 24 la de títulos, 10 la de texto)")]
        public Font titleFont;
        public Font textFont;

        const float RefWidth = 640f, RefHeight = 360f;
        const int TitleSize = 24, TextSize = 10;

        static readonly Color HealthColor = new Color(0.66f, 0.1f, 0.12f);
        static readonly Color RevelationColor = new Color(0.3f, 0.84f, 0.66f);
        static readonly Color TitleColor = new Color(0.9f, 0.88f, 0.78f);
        static readonly Color TextColor = new Color(0.82f, 0.86f, 0.8f);
        static readonly Color GoldColor = new Color(0.95f, 0.8f, 0.42f);
        static readonly Color DeathColor = new Color(0.72f, 0.1f, 0.12f);
        static readonly Color ShadowColor = new Color(0f, 0f, 0f, 0.9f);

        RectTransform healthFill, healthTrail, revelationFill, revelationLock, revelationBar;
        float healthTrailValue = 1f, revelationPulse;
        RectTransform flaskRow;
        readonly List<Image> flaskIcons = new List<Image>();
        Text goldText;
        int shownGold = -1;

        CanvasGroup promptGroup;
        Text promptText;
        RectTransform promptFrame;

        CanvasGroup areaGroup, messageGroup, bannerGroup;
        Text areaText, messageText, bannerTitle, bannerSubtitle;
        float areaTimer = -1f, messageTimer = -1f, bannerTimer = -1f;

        CanvasGroup bossGroup;
        RectTransform bossFill, bossTrail;
        Text bossName;
        Enemy boss;
        float bossTrailValue = 1f;

        CanvasGroup readingGroup, pauseGroup, deathGroup, titleGroup;
        Text readingText, titlePrompt;
        Image damageFlash;
        float damageFlashAlpha;

        float readingTarget, pauseTarget, deathTarget, titleTarget, bossTarget;
        int readingOpenedFrame;

        public bool IsReading { get; private set; }
        public bool IsBusy => IsReading || titleTarget > 0f;

        void Awake()
        {
            Instance = this;
            var fallback = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (titleFont == null) titleFont = fallback;
            if (textFont == null) textFont = fallback;
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
            areaText.text = title;
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

        public void FlashDamage() => damageFlashAlpha = 0.28f;

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
                revelationBar.anchoredPosition = new Vector2(46f, -27f + Mathf.Round(Mathf.Sin(revelationPulse * Mathf.PI * 4f) * revelationPulse));

                UpdateFlasks(stats.Flasks, stats.MaxFlasks);
                if (stats.Gold != shownGold)
                {
                    shownGold = stats.Gold;
                    goldText.text = shownGold.ToString();
                }

                var interactable = player.CurrentInteractable;
                bool showPrompt = interactable != null && !IsReading && !player.IsDead && titleTarget <= 0f;
                if (interactable != null)
                {
                    promptText.text = "E   " + interactable.Prompt;
                    promptFrame.sizeDelta = new Vector2(Mathf.Ceil(promptText.preferredWidth) + 24f, 18f);
                }
                promptGroup.alpha = Mathf.MoveTowards(promptGroup.alpha, showPrompt ? 1f : 0f, dt * 6f);

                // Poca vida: la viñeta del post-procesado late en rojo.
                CameraFX.SetDanger(hp < 0.3f && !player.IsDead ? 1f - hp / 0.3f * 0.6f : 0f);
            }

            damageFlashAlpha = Mathf.MoveTowards(damageFlashAlpha, 0f, dt * 1.2f);
            damageFlash.color = new Color(0.55f, 0f, 0.05f, damageFlashAlpha);

            areaGroup.alpha = TimedAlpha(ref areaTimer, dt, 0.8f, 2.6f, 1.2f);
            messageGroup.alpha = TimedAlpha(ref messageTimer, dt, 0.3f, 3.2f, 0.8f);
            bannerGroup.alpha = TimedAlpha(ref bannerTimer, dt, 1f, 3.5f, 1.5f);

            Approach(readingGroup, readingTarget, dt * 6f);
            Approach(pauseGroup, pauseTarget, dt * 8f);
            Approach(deathGroup, deathTarget, dt * 1.2f);
            Approach(titleGroup, titleTarget, dt * 1.5f);
            Approach(bossGroup, bossTarget, dt * 2f);
            if (titlePrompt != null) titlePrompt.color = new Color(TextColor.r, TextColor.g, TextColor.b, 0.35f + 0.35f * Mathf.Sin(Time.unscaledTime * 3f));

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
                Place(icon.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(flaskIcons.Count * 12f, 0f), new Vector2(10f, 14f));
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
            canvas.pixelPerfect = true;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(RefWidth, RefHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
            scaler.referencePixelsPerUnit = 100f;
            Transform root = canvasGO.transform;

            // Viñeta y destello de daño (debajo de todo lo demás).
            if (vignetteSprite != null)
            {
                var vignette = NewImage("Viñeta", root, new Color(0f, 0f, 0f, 0.55f), vignetteSprite);
                Stretch(vignette.rectTransform);
            }
            damageFlash = NewImage("DestelloDaño", root, new Color(0.55f, 0f, 0.05f, 0f));
            Stretch(damageFlash.rectTransform);

            BuildStatusPanel(root);
            BuildGoldCounter(root);
            BuildPrompt(root);

            // Nombre de la zona entre dos filigranas.
            areaGroup = NewGroup("NombreZona", root);
            Place((RectTransform)areaGroup.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 92f), new Vector2(560f, 60f));
            areaText = NewText("Titulo", areaGroup.transform, "", titleFont, TitleSize, TextAnchor.MiddleCenter, TitleColor);
            Stretch(areaText.rectTransform);
            AddSeparator(areaGroup.transform, 20f);
            AddSeparator(areaGroup.transform, -20f);
            areaGroup.alpha = 0f;

            // Mensajes.
            messageGroup = NewGroup("Mensaje", root);
            Place((RectTransform)messageGroup.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -76f), new Vector2(560f, 30f));
            messageText = NewText("Texto", messageGroup.transform, "", textFont, TextSize, TextAnchor.MiddleCenter, TextColor);
            Stretch(messageText.rectTransform);
            messageGroup.alpha = 0f;

            // Estandarte (jefe derrotado).
            bannerGroup = NewGroup("Estandarte", root);
            Stretch((RectTransform)bannerGroup.transform);
            var bannerBack = NewImage("Fondo", bannerGroup.transform, new Color(0f, 0f, 0f, 0.65f));
            Place(bannerBack.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 8f), new Vector2(RefWidth + 40f, 74f));
            AddSeparator(bannerGroup.transform, 40f);
            AddSeparator(bannerGroup.transform, -24f);
            bannerTitle = NewText("Titulo", bannerGroup.transform, "", titleFont, TitleSize, TextAnchor.MiddleCenter, GoldColor);
            Place(bannerTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 16f), new Vector2(600f, 32f));
            bannerSubtitle = NewText("Subtitulo", bannerGroup.transform, "", textFont, TextSize, TextAnchor.MiddleCenter, TextColor);
            Place(bannerSubtitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -8f), new Vector2(600f, 16f));
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
            Place(panel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(6f, -6f), new Vector2(220f, 60f));

            var portrait = NewImage("Retrato", panel, portraitSprite != null ? Color.white : new Color(0.3f, 0.25f, 0.15f), portraitSprite);
            Place(portrait.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(44f, 44f));

            var healthBar = MakeBar(panel, new Vector2(0f, 1f), new Vector2(42f, -10f), new Vector2(152f, 12f), healthFillSprite, HealthColor, barFrameSprite, new Vector4(5f, 3f, 5f, 3f),
                                    out healthFill, out healthTrail);
            healthBar.SetSiblingIndex(0); // detrás del retrato: el medallón tapa el arranque de la barra
            revelationBar = MakeBar(panel, new Vector2(0f, 1f), new Vector2(46f, -27f), new Vector2(104f, 10f), revelationFillSprite, RevelationColor, barFrameSprite, new Vector4(5f, 3f, 5f, 3f),
                                    out revelationFill, out var revTrail);
            revelationBar.SetSiblingIndex(1);
            revTrail.gameObject.SetActive(false);

            // Parte "bloqueada" de la Revelación mientras falta un fragmento de mente.
            var lockImage = NewImage("Bloqueo", revelationFill.parent, new Color(0.3f, 0.12f, 0.38f, 0.95f));
            revelationLock = lockImage.rectTransform;
            revelationLock.anchorMin = new Vector2(0.66f, 0f);
            revelationLock.anchorMax = Vector2.one;
            revelationLock.offsetMin = revelationLock.offsetMax = Vector2.zero;
            revelationLock.gameObject.SetActive(false);

            flaskRow = NewRect("Frascos", panel);
            Place(flaskRow, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(50f, -40f), new Vector2(120f, 14f));
        }

        void BuildGoldCounter(Transform root)
        {
            var frame = NewImage("Oro", root, goldFrameSprite != null ? Color.white : new Color(0f, 0f, 0f, 0.6f), goldFrameSprite, true);
            Place(frame.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-8f, -8f), new Vector2(76f, 18f));
            var icon = NewImage("Icono", frame.transform, goldSprite != null ? Color.white : GoldColor, goldSprite);
            Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(5f, 0f), new Vector2(12f, 12f));
            goldText = NewText("Cantidad", frame.transform, "0", textFont, TextSize, TextAnchor.MiddleRight, GoldColor);
            Place(goldText.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-6f, 0f), new Vector2(54f, 14f));
        }

        void BuildPrompt(Transform root)
        {
            promptGroup = NewGroup("Aviso", root);
            Place((RectTransform)promptGroup.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 46f), new Vector2(300f, 18f));
            var frame = NewImage("Marco", promptGroup.transform, promptSprite != null ? Color.white : new Color(0f, 0f, 0f, 0.6f), promptSprite, true);
            promptFrame = frame.rectTransform;
            Place(promptFrame, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(160f, 18f));
            promptText = NewText("Texto", promptGroup.transform, "", textFont, TextSize, TextAnchor.MiddleCenter, TitleColor);
            Stretch(promptText.rectTransform);
            promptGroup.alpha = 0f;
        }

        void BuildBossBar(Transform root)
        {
            bossGroup = NewGroup("BarraJefe", root);
            Place((RectTransform)bossGroup.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(340f, 48f));
            bossName = NewText("Nombre", bossGroup.transform, "", titleFont, TitleSize, TextAnchor.MiddleCenter, TitleColor);
            Place(bossName.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(400f, 30f));
            MakeBar(bossGroup.transform, new Vector2(0.5f, 1f), new Vector2(0f, -32f), new Vector2(340f, 14f), healthFillSprite, HealthColor, bossFrameSprite, new Vector4(9f, 4f, 9f, 4f),
                    out bossFill, out bossTrail);
            bossGroup.alpha = 0f;
        }

        void BuildReadingPanel(Transform root)
        {
            readingGroup = NewGroup("Lectura", root);
            Stretch((RectTransform)readingGroup.transform);
            var dim = NewImage("Oscurecer", readingGroup.transform, new Color(0f, 0f, 0f, 0.6f));
            Stretch(dim.rectTransform);
            var panel = NewImage("Panel", readingGroup.transform, panelSprite != null ? Color.white : new Color(0.04f, 0.05f, 0.05f, 0.97f), panelSprite, true);
            Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 180f));
            readingText = NewText("Texto", panel.transform, "", textFont, TextSize, TextAnchor.MiddleCenter, TextColor);
            Stretch(readingText.rectTransform, 22f);
            readingText.horizontalOverflow = HorizontalWrapMode.Wrap;
            readingText.lineSpacing = 1.2f;
            var hint = NewText("Pista", panel.transform, "E  Continuar", textFont, TextSize, TextAnchor.LowerRight, new Color(0.55f, 0.75f, 0.66f));
            Stretch(hint.rectTransform, 12f);
            readingGroup.alpha = 0f;
        }

        void BuildPausePanel(Transform root)
        {
            pauseGroup = NewGroup("Pausa", root);
            Stretch((RectTransform)pauseGroup.transform);
            var dim = NewImage("Oscurecer", pauseGroup.transform, new Color(0f, 0.01f, 0.02f, 0.78f));
            Stretch(dim.rectTransform);
            var panel = NewImage("Panel", pauseGroup.transform, panelSprite != null ? Color.white : new Color(0.04f, 0.05f, 0.05f, 0.97f), panelSprite, true);
            Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(330f, 250f));
            var title = NewText("Titulo", panel.transform, "Pausa", titleFont, TitleSize, TextAnchor.MiddleCenter, TitleColor);
            Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(300f, 30f));
            AddSeparator(panel.transform, -46f, 1f);
            const string controls =
                "Mover ............. A / D  ·  Flechas  ·  Stick\n" +
                "Saltar ............ Espacio  ·  K  ·  Botón A\n" +
                "Atacar ............ J  ·  Botón X\n" +
                "Esquivar .......... L  ·  Shift  ·  Botón B\n" +
                "Parar (parry) ..... I  ·  RB\n" +
                "Signo Arcano ...... U  ·  Botón Y\n" +
                "Beber láudano ..... F  ·  LB\n" +
                "Interactuar ....... E  ·  W  ·  Arriba\n" +
                "Bajar plataforma .. Abajo + Saltar\n\n" +
                "Esc / Start para continuar";
            var list = NewText("Controles", panel.transform, controls, textFont, TextSize, TextAnchor.MiddleCenter, TextColor);
            Place(list.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -18f), new Vector2(300f, 170f));
            list.lineSpacing = 1.1f;
            pauseGroup.alpha = 0f;
        }

        void BuildDeathScreen(Transform root)
        {
            deathGroup = NewGroup("Muerte", root);
            Stretch((RectTransform)deathGroup.transform);
            var black = NewImage("Negro", deathGroup.transform, new Color(0f, 0f, 0f, 0.9f));
            Stretch(black.rectTransform);
            var text = NewText("Texto", deathGroup.transform, "El Abismo te reclama", titleFont, TitleSize * 2, TextAnchor.MiddleCenter, DeathColor);
            Place(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(620f, 60f));
            AddSeparator(deathGroup.transform, -30f);
            deathGroup.alpha = 0f;
        }

        void BuildTitleCard(Transform root)
        {
            titleGroup = NewGroup("Titulo", root);
            Stretch((RectTransform)titleGroup.transform);
            var black = NewImage("Negro", titleGroup.transform, new Color(0.01f, 0.015f, 0.02f, 1f));
            Stretch(black.rectTransform);
            if (signSprite != null)
            {
                var sign = NewImage("Signo", titleGroup.transform, new Color(1f, 1f, 1f, 0.9f), signSprite);
                Place(sign.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 104f), new Vector2(32f, 32f));
            }
            var title = NewText("Nombre", titleGroup.transform, "Abismo", titleFont, TitleSize * 3, TextAnchor.MiddleCenter, TitleColor);
            Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 46f), new Vector2(620f, 90f));
            AddSeparator(titleGroup.transform, 2f);
            var subtitle = NewText("Subtitulo", titleGroup.transform, "El Sueño de R'lyeh", titleFont, TitleSize, TextAnchor.MiddleCenter, RevelationColor);
            Place(subtitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(600f, 30f));
            var quote = NewText("Cita", titleGroup.transform,
                "«Que no está muerto lo que yace eternamente,\ny con los eones extraños incluso la muerte puede morir.»",
                textFont, TextSize, TextAnchor.MiddleCenter, new Color(0.55f, 0.65f, 0.6f));
            Place(quote.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -58f), new Vector2(600f, 30f));
            titlePrompt = NewText("Pulsa", titleGroup.transform, "Pulsa cualquier botón", textFont, TextSize, TextAnchor.MiddleCenter, TextColor);
            Place(titlePrompt.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(400f, 16f));
            titleGroup.alpha = 0f;
        }

        /// <summary>Barra con fondo, rastro (lo que se acaba de perder), relleno y marco 9-slice encima.</summary>
        RectTransform MakeBar(Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Sprite fillSprite, Color fallbackColor, Sprite frameSprite,
                              Vector4 inset, out RectTransform fill, out RectTransform trail)
        {
            var bar = NewRect("Barra", parent);
            Place(bar, anchor, anchor, position, size);
            var inner = NewRect("Interior", bar);
            inner.anchorMin = Vector2.zero;
            inner.anchorMax = Vector2.one;
            inner.offsetMin = new Vector2(inset.x, inset.y);
            inner.offsetMax = new Vector2(-inset.z, -inset.w);
            var back = NewImage("Fondo", inner, barBackSprite != null ? Color.white : new Color(0.05f, 0.04f, 0.05f), barBackSprite, true);
            Stretch(back.rectTransform);
            var trailImage = NewImage("Rastro", inner, barTrailSprite != null ? Color.white : new Color(0.95f, 0.85f, 0.6f), barTrailSprite, true);
            trail = trailImage.rectTransform;
            SetFill(trail, 1f);
            var fillImage = NewImage("Relleno", inner, fillSprite != null ? Color.white : fallbackColor, fillSprite, true);
            fill = fillImage.rectTransform;
            SetFill(fill, 1f);
            var frame = NewImage("Marco", bar, frameSprite != null ? Color.white : new Color(0f, 0f, 0f, 0f), frameSprite, true);
            Stretch(frame.rectTransform);
            return bar;
        }

        /// <summary>Filigrana dorada horizontal centrada (y relativo al ancla; por defecto, el centro del padre).</summary>
        void AddSeparator(Transform parent, float y, float anchorY = 0.5f)
        {
            if (separatorSprite == null) return;
            var line = NewImage("Filigrana", parent, Color.white, separatorSprite);
            Place(line.rectTransform, new Vector2(0.5f, anchorY), new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(160f, 9f));
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

        static Image NewImage(string name, Transform parent, Color color, Sprite sprite = null, bool sliced = false)
        {
            var rt = NewRect(name, parent);
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            if (sliced && sprite != null && sprite.border != Vector4.zero)
            {
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 1f;
            }
            return image;
        }

        static Text NewText(string name, Transform parent, string content, Font font, int size, TextAnchor anchor, Color color)
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
            text.supportRichText = false;
            var shadow = rt.gameObject.AddComponent<Shadow>();
            shadow.effectColor = ShadowColor;
            shadow.effectDistance = new Vector2(1f, -1f);
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

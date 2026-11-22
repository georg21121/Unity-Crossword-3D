using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Crossword3D
{
    public class CrosswordUI : MonoBehaviour
    {
        public static CrosswordUI Instance { get; private set; }

        [Header("HUD References")]
        public Text titleText;
        public Text timerText;
        public Text scoreText;
        public Text hintButtonText;

        [Header("Clues Lists")]
        public Transform horizontalCluesContent;
        public Transform verticalCluesContent;

        [Header("Settings Modal")]
        public GameObject settingsModal;
        public Slider masterVolumeSlider;
        public Slider musicVolumeSlider;
        public Slider sfxVolumeSlider;
        public Text masterVolumeLabel;
        public Text musicVolumeLabel;
        public Text sfxVolumeLabel;
        public Text muteButtonLabel;

        [Header("Victory Modal")]
        public GameObject victoryModal;
        public Text victoryTimeText;
        public Text victoryScoreText;

        private Dictionary<string, Text> clueTextMap = new Dictionary<string, Text>();
        private Dictionary<string, Image> clueBackgroundMap = new Dictionary<string, Image>();
        private Font cachedFont;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            cachedFont = GetSafeFont();
            EnsureUIHierarchy();
        }

        private void Update()
        {
            if (CrosswordManager.Instance != null && CrosswordManager.Instance.isGameActive)
            {
                UpdateStats(CrosswordManager.Instance.timer, CrosswordManager.Instance.score, CrosswordManager.Instance.hintsRemaining);
            }
        }

        public static Font GetSafeFont()
        {
            Font f = null;
            try
            {
                f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            catch { }

            if (f == null)
            {
                try
                {
                    f = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }
                catch { }
            }

            if (f == null)
            {
                try
                {
                    f = Font.CreateDynamicFontFromOSFont("Arial", 14);
                }
                catch { }
            }

            return f;
        }

        public void InitializeLevelUI(CrosswordLevel level)
        {
            if (titleText != null)
                titleText.text = level.title;

            if (victoryModal != null)
                victoryModal.SetActive(false);

            if (settingsModal != null)
                settingsModal.SetActive(false);

            BuildCluesList(level);
        }

        public void UpdateStats(float timer, int score, int hints)
        {
            if (timerText != null)
            {
                int minutes = Mathf.FloorToInt(timer / 60F);
                int seconds = Mathf.FloorToInt(timer - minutes * 60);
                timerText.text = $"⏱ Время: {minutes:00}:{seconds:00}";
            }

            if (scoreText != null)
            {
                scoreText.text = $"★ Счёт: {score}";
            }

            if (hintButtonText != null)
            {
                hintButtonText.text = $"💡 Подсказка ({hints})";
            }
        }

        private void BuildCluesList(CrosswordLevel level)
        {
            clueTextMap.Clear();
            clueBackgroundMap.Clear();

            // Clear previous items
            if (horizontalCluesContent != null)
            {
                for (int i = horizontalCluesContent.childCount - 1; i >= 0; i--)
                    Destroy(horizontalCluesContent.GetChild(i).gameObject);
            }

            if (verticalCluesContent != null)
            {
                for (int i = verticalCluesContent.childCount - 1; i >= 0; i--)
                    Destroy(verticalCluesContent.GetChild(i).gameObject);
            }

            foreach (var word in level.words)
            {
                Transform parent = (word.orientation == WordOrientation.Horizontal) ? horizontalCluesContent : verticalCluesContent;
                if (parent == null) continue;

                GameObject itemObj = new GameObject($"ClueItem_{word.id}_{word.orientation}");
                itemObj.transform.SetParent(parent, false);

                RectTransform rt = itemObj.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(280, 52);

                Image bg = itemObj.AddComponent<Image>();
                bg.color = new Color(1.0f, 1.0f, 1.0f, 0.96f);

                Button btn = itemObj.AddComponent<Button>();
                btn.targetGraphic = bg;
                ColorBlock cb = btn.colors;
                cb.highlightedColor = new Color(0.93f, 0.95f, 0.98f, 1.0f);
                cb.pressedColor = new Color(0.85f, 0.90f, 0.96f, 1.0f);
                btn.colors = cb;

                WordEntry currentWord = word;
                btn.onClick.AddListener(() =>
                {
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
                    if (CrosswordManager.Instance != null)
                    {
                        CrosswordManager.Instance.SelectWord(currentWord);
                    }
                });

                GameObject textObj = new GameObject("Text");
                textObj.transform.SetParent(itemObj.transform, false);
                RectTransform textRt = textObj.AddComponent<RectTransform>();
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.offsetMin = new Vector2(10, 4);
                textRt.offsetMax = new Vector2(-10, -4);

                Text txt = textObj.AddComponent<Text>();
                txt.font = cachedFont;
                txt.fontSize = 12;
                txt.color = new Color(0.15f, 0.17f, 0.20f);
                txt.alignment = TextAnchor.MiddleLeft;
                txt.text = $"<b><color=#795548>{word.id}.</color></b> {word.clue} <i><color=#6c757d>({word.answer.Length} б.)</color></i>";

                string key = $"{word.id}_{word.orientation}";
                clueTextMap[key] = txt;
                clueBackgroundMap[key] = bg;
            }
        }

        public void HighlightClueInList(int wordId, WordOrientation orientation)
        {
            string targetKey = $"{wordId}_{orientation}";
            foreach (var kvp in clueBackgroundMap)
            {
                if (kvp.Key == targetKey)
                {
                    kvp.Value.color = new Color(1.0f, 0.95f, 0.78f, 1.0f); // Warm gold paper glow
                }
                else
                {
                    string k = kvp.Key;
                    if (clueTextMap.ContainsKey(k) && clueTextMap[k].text.StartsWith("✔"))
                    {
                        kvp.Value.color = new Color(0.88f, 0.96f, 0.90f, 0.95f);
                    }
                    else
                    {
                        kvp.Value.color = new Color(1.0f, 1.0f, 1.0f, 0.96f);
                    }
                }
            }
        }

        public void MarkClueSolved(int wordId, WordOrientation orientation)
        {
            string key = $"{wordId}_{orientation}";
            if (clueTextMap.ContainsKey(key))
            {
                Text txt = clueTextMap[key];
                txt.color = new Color(0.12f, 0.45f, 0.22f);
                txt.text = $"✔ " + txt.text;
            }
            if (clueBackgroundMap.ContainsKey(key))
            {
                clueBackgroundMap[key].color = new Color(0.88f, 0.96f, 0.90f, 0.95f);
            }
        }

        public void OpenSettingsModal()
        {
            if (settingsModal != null)
            {
                settingsModal.SetActive(true);
                SyncSettingsSliders();
            }
        }

        public void CloseSettingsModal()
        {
            if (settingsModal != null)
                settingsModal.SetActive(false);
        }

        private void SyncSettingsSliders()
        {
            if (AudioManager.Instance == null) return;

            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.value = AudioManager.Instance.masterVolume;
                if (masterVolumeLabel != null)
                    masterVolumeLabel.text = $"{Mathf.RoundToInt(AudioManager.Instance.masterVolume * 100)}%";
            }

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.value = AudioManager.Instance.musicVolume;
                if (musicVolumeLabel != null)
                    musicVolumeLabel.text = $"{Mathf.RoundToInt(AudioManager.Instance.musicVolume * 100)}%";
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.value = AudioManager.Instance.sfxVolume;
                if (sfxVolumeLabel != null)
                    sfxVolumeLabel.text = $"{Mathf.RoundToInt(AudioManager.Instance.sfxVolume * 100)}%";
            }

            if (muteButtonLabel != null)
            {
                muteButtonLabel.text = AudioManager.Instance.isMuted ? "🔇 Звук: Выключен" : "🔊 Звук: Включен";
            }
        }

        public void ShowVictoryScreen(float finalTime, int finalScore)
        {
            if (victoryModal != null)
            {
                victoryModal.SetActive(true);

                int minutes = Mathf.FloorToInt(finalTime / 60F);
                int seconds = Mathf.FloorToInt(finalTime - minutes * 60);

                if (victoryTimeText != null)
                    victoryTimeText.text = $"⏱ Время прохождения: {minutes:00}:{seconds:00}";

                if (victoryScoreText != null)
                    victoryScoreText.text = $"★ Финальный счёт: {finalScore} очков";
            }
        }

        private void EnsureUIHierarchy()
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("GameCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            BuildHUD(canvas.transform);
            BuildPaperCluesSheet(canvas.transform);
            BuildSettingsModal(canvas.transform);
            BuildVictoryPopup(canvas.transform);
        }

        private void BuildHUD(Transform canvasTransform)
        {
            if (titleText != null) return;

            // Top Header Bar
            GameObject headerObj = new GameObject("TopHUD");
            headerObj.transform.SetParent(canvasTransform, false);
            RectTransform hRt = headerObj.AddComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0, 1);
            hRt.anchorMax = new Vector2(1, 1);
            hRt.pivot = new Vector2(0.5f, 1);
            hRt.offsetMin = new Vector2(0, -56);
            hRt.offsetMax = new Vector2(0, 0);

            Image hBg = headerObj.AddComponent<Image>();
            hBg.color = new Color(0.12f, 0.14f, 0.18f, 0.96f);

            // Left: Title
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(headerObj.transform, false);
            RectTransform tRt = titleObj.AddComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0, 0);
            tRt.anchorMax = new Vector2(0, 1);
            tRt.pivot = new Vector2(0, 0.5f);
            tRt.anchoredPosition = new Vector2(24, 0);
            tRt.sizeDelta = new Vector2(380, 50);

            titleText = titleObj.AddComponent<Text>();
            titleText.font = cachedFont;
            titleText.fontSize = 20;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = new Color(1.0f, 0.85f, 0.35f);
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.text = "3D Кроссворд: Мультимедиа";

            // Center: Stats Container (Timer & Score)
            GameObject statsObj = new GameObject("StatsContainer");
            statsObj.transform.SetParent(headerObj.transform, false);
            RectTransform statsRt = statsObj.AddComponent<RectTransform>();
            statsRt.anchorMin = new Vector2(0.5f, 0.5f);
            statsRt.anchorMax = new Vector2(0.5f, 0.5f);
            statsRt.pivot = new Vector2(0.5f, 0.5f);
            statsRt.sizeDelta = new Vector2(380, 50);

            HorizontalLayoutGroup statsHlg = statsObj.AddComponent<HorizontalLayoutGroup>();
            statsHlg.childControlWidth = false;
            statsHlg.childControlHeight = false;
            statsHlg.childAlignment = TextAnchor.MiddleCenter;
            statsHlg.spacing = 30;

            // Timer Text
            GameObject timerObj = new GameObject("TimerText");
            timerObj.transform.SetParent(statsObj.transform, false);
            RectTransform timRt = timerObj.AddComponent<RectTransform>();
            timRt.sizeDelta = new Vector2(160, 40);
            timerText = timerObj.AddComponent<Text>();
            timerText.font = cachedFont;
            timerText.fontSize = 17;
            timerText.color = Color.white;
            timerText.alignment = TextAnchor.MiddleCenter;
            timerText.text = "⏱ Время: 00:00";

            // Score Text
            GameObject scoreObj = new GameObject("ScoreText");
            scoreObj.transform.SetParent(statsObj.transform, false);
            RectTransform sRt = scoreObj.AddComponent<RectTransform>();
            sRt.sizeDelta = new Vector2(160, 40);
            scoreText = scoreObj.AddComponent<Text>();
            scoreText.font = cachedFont;
            scoreText.fontSize = 17;
            scoreText.color = new Color(0.4f, 0.95f, 0.55f);
            scoreText.alignment = TextAnchor.MiddleCenter;
            scoreText.text = "★ Счёт: 0";

            // Right: Control Buttons Container (Fixed right alignment, never overlaps)
            GameObject topBtns = new GameObject("TopControls");
            topBtns.transform.SetParent(headerObj.transform, false);
            RectTransform tbRt = topBtns.AddComponent<RectTransform>();
            tbRt.anchorMin = new Vector2(1, 0);
            tbRt.anchorMax = new Vector2(1, 1);
            tbRt.pivot = new Vector2(1, 0.5f);
            tbRt.anchoredPosition = new Vector2(-20, 0);
            tbRt.sizeDelta = new Vector2(520, 50);

            HorizontalLayoutGroup hlg = topBtns.AddComponent<HorizontalLayoutGroup>();
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childAlignment = TextAnchor.MiddleRight;
            hlg.spacing = 10;

            // 1. Hint Button (Подсказка с счетчиком)
            CreateButtonWithRef(topBtns.transform, "💡 Подсказка (5)", 140, 36, () =>
            {
                if (CrosswordManager.Instance != null)
                    CrosswordManager.Instance.UseHint();
            }, out hintButtonText, new Color(0.65f, 0.48f, 0.18f));

            // 2. Toggle Direction Button (Смена направления)
            CreateButton(topBtns.transform, "⟳ Направление", 130, 36, () =>
            {
                if (CrosswordManager.Instance != null)
                    CrosswordManager.Instance.ToggleOrientation();
            }, new Color(0.28f, 0.40f, 0.58f));

            // 3. View Button (Вид камеры)
            CreateButton(topBtns.transform, "⤾ Вид", 80, 36, () =>
            {
                if (CameraOrbitController.Instance != null)
                    CameraOrbitController.Instance.ResetViewPerspective();
            }, new Color(0.28f, 0.32f, 0.40f));

            // 4. Sound Settings Button (Настройки звука)
            CreateButton(topBtns.transform, "⚙ Звук", 85, 36, () =>
            {
                OpenSettingsModal();
            }, new Color(0.20f, 0.46f, 0.65f));
        }

        private void BuildPaperCluesSheet(Transform canvasTransform)
        {
            if (horizontalCluesContent != null && verticalCluesContent != null) return;

            // Outer Notepad / Board Holder
            GameObject sheetHolder = new GameObject("PaperCluesSheet");
            sheetHolder.transform.SetParent(canvasTransform, false);
            RectTransform pRt = sheetHolder.AddComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0, 0);
            pRt.anchorMax = new Vector2(0, 1);
            pRt.pivot = new Vector2(0, 0.5f);
            pRt.offsetMin = new Vector2(18, 18);
            pRt.offsetMax = new Vector2(335, -74);

            Image paperBg = sheetHolder.AddComponent<Image>();
            paperBg.color = new Color(0.97f, 0.96f, 0.92f, 0.98f);

            // Clipboard Header
            GameObject clipHeader = new GameObject("ClipboardHeader");
            clipHeader.transform.SetParent(sheetHolder.transform, false);
            RectTransform clipRt = clipHeader.AddComponent<RectTransform>();
            clipRt.anchorMin = new Vector2(0, 1);
            clipRt.anchorMax = new Vector2(1, 1);
            clipRt.pivot = new Vector2(0.5f, 1);
            clipRt.offsetMin = new Vector2(0, -42);
            clipRt.offsetMax = new Vector2(0, 0);

            Image clipBg = clipHeader.AddComponent<Image>();
            clipBg.color = new Color(0.32f, 0.22f, 0.16f, 1.0f);

            GameObject clipTitle = new GameObject("ClipTitle");
            clipTitle.transform.SetParent(clipHeader.transform, false);
            RectTransform ctRt = clipTitle.AddComponent<RectTransform>();
            ctRt.anchorMin = Vector2.zero;
            ctRt.anchorMax = Vector2.one;
            ctRt.offsetMin = new Vector2(8, 0);
            ctRt.offsetMax = new Vector2(-8, 0);
            Text ctTxt = clipTitle.AddComponent<Text>();
            ctTxt.font = cachedFont;
            ctTxt.fontSize = 15;
            ctTxt.fontStyle = FontStyle.Bold;
            ctTxt.color = new Color(1.0f, 0.92f, 0.75f);
            ctTxt.alignment = TextAnchor.MiddleCenter;
            ctTxt.text = "📋 ЛИСТ С ВОПРОСАМИ";

            // Horizontal Label
            CreatePaperSectionLabel(sheetHolder.transform, "➡ ПО ГОРИЗОНТАЛИ", new Vector2(0, 0.93f), new Vector2(1, 0.97f));

            // Horizontal Scroll Container
            GameObject horizScroll = CreatePaperScrollContainer(sheetHolder.transform, new Vector2(0, 0.51f), new Vector2(1, 0.92f));
            horizontalCluesContent = horizScroll.transform;

            // Vertical Label
            CreatePaperSectionLabel(sheetHolder.transform, "⬇ ПО ВЕРТИКАЛИ", new Vector2(0, 0.45f), new Vector2(1, 0.49f));

            // Vertical Scroll Container
            GameObject vertScroll = CreatePaperScrollContainer(sheetHolder.transform, new Vector2(0, 0.02f), new Vector2(1, 0.44f));
            verticalCluesContent = vertScroll.transform;
        }

        private void CreatePaperSectionLabel(Transform parent, string title, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject obj = new GameObject($"Label_{title}");
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = new Vector2(12, 0);
            rt.offsetMax = new Vector2(-12, 0);

            Text txt = obj.AddComponent<Text>();
            txt.font = cachedFont;
            txt.fontSize = 13;
            txt.fontStyle = FontStyle.Bold;
            txt.color = new Color(0.35f, 0.25f, 0.15f);
            txt.alignment = TextAnchor.MiddleLeft;
            txt.text = title;
        }

        private GameObject CreatePaperScrollContainer(Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject scrollObj = new GameObject("PaperScroll");
            scrollObj.transform.SetParent(parent, false);
            RectTransform sRt = scrollObj.AddComponent<RectTransform>();
            sRt.anchorMin = anchorMin;
            sRt.anchorMax = anchorMax;
            sRt.offsetMin = new Vector2(8, 2);
            sRt.offsetMax = new Vector2(-8, -2);

            ScrollRect sr = scrollObj.AddComponent<ScrollRect>();
            sr.horizontal = false;
            sr.vertical = true;
            sr.scrollSensitivity = 25f;

            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObj.transform, false);
            RectTransform vpRt = viewport.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero;
            vpRt.offsetMax = Vector2.zero;
            viewport.AddComponent<RectMask2D>();

            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            RectTransform cRt = content.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0, 1);
            cRt.anchorMax = new Vector2(1, 1);
            cRt.pivot = new Vector2(0.5f, 1);
            cRt.offsetMin = Vector2.zero;
            cRt.offsetMax = Vector2.zero;

            VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 5;
            vlg.padding = new RectOffset(2, 2, 2, 2);

            ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            sr.viewport = vpRt;
            sr.content = cRt;

            return content;
        }

        /// <summary>
        /// Builds the Settings Modal. Clean vertical flow with zero overlaps.
        /// </summary>
        private void BuildSettingsModal(Transform canvasTransform)
        {
            if (settingsModal != null) return;

            // Full screen dimming overlay
            settingsModal = new GameObject("SettingsModal");
            settingsModal.transform.SetParent(canvasTransform, false);
            RectTransform mRt = settingsModal.AddComponent<RectTransform>();
            mRt.anchorMin = Vector2.zero;
            mRt.anchorMax = Vector2.one;
            mRt.offsetMin = Vector2.zero;
            mRt.offsetMax = Vector2.zero;

            Image dimBg = settingsModal.AddComponent<Image>();
            dimBg.color = new Color(0, 0, 0, 0.65f);

            // Dialog Box
            GameObject boxObj = new GameObject("SettingsBox");
            boxObj.transform.SetParent(settingsModal.transform, false);
            RectTransform bRt = boxObj.AddComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.5f, 0.5f);
            bRt.anchorMax = new Vector2(0.5f, 0.5f);
            bRt.pivot = new Vector2(0.5f, 0.5f);
            bRt.sizeDelta = new Vector2(460, 380);

            Image boxBg = boxObj.AddComponent<Image>();
            boxBg.color = new Color(0.14f, 0.16f, 0.22f, 0.98f);

            // Title
            GameObject titleObj = new GameObject("SettingsTitle");
            titleObj.transform.SetParent(boxObj.transform, false);
            RectTransform tRt = titleObj.AddComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0, 1);
            tRt.anchorMax = new Vector2(1, 1);
            tRt.pivot = new Vector2(0.5f, 1);
            tRt.offsetMin = new Vector2(0, -50);
            tRt.offsetMax = new Vector2(0, 0);

            Text sTitle = titleObj.AddComponent<Text>();
            sTitle.font = cachedFont;
            sTitle.fontSize = 20;
            sTitle.fontStyle = FontStyle.Bold;
            sTitle.color = new Color(1.0f, 0.85f, 0.35f);
            sTitle.alignment = TextAnchor.MiddleCenter;
            sTitle.text = "⚙ НАСТРОЙКИ ЗВУКА";

            // Content container using VerticalLayoutGroup for automatic clean spacing
            GameObject contentObj = new GameObject("ContentContainer");
            contentObj.transform.SetParent(boxObj.transform, false);
            RectTransform cRt = contentObj.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0, 0);
            cRt.anchorMax = new Vector2(1, 1);
            cRt.offsetMin = new Vector2(25, 20);
            cRt.offsetMax = new Vector2(-25, -55);

            VerticalLayoutGroup vlg = contentObj.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 16;
            vlg.padding = new RectOffset(5, 5, 10, 5);

            // 1. Music Slider
            CreateVolumeRowVertical(contentObj.transform, "🎵 Фоновая музыка:", 
                (val) => {
                    if (AudioManager.Instance != null) AudioManager.Instance.SetMusicVolume(val);
                    if (musicVolumeLabel != null) musicVolumeLabel.text = $"{Mathf.RoundToInt(val * 100)}%";
                }, 
                out musicVolumeSlider, out musicVolumeLabel);

            // 2. SFX Slider
            CreateVolumeRowVertical(contentObj.transform, "🔔 Звуковые эффекты:", 
                (val) => {
                    if (AudioManager.Instance != null) AudioManager.Instance.SetSFXVolume(val);
                    if (sfxVolumeLabel != null) sfxVolumeLabel.text = $"{Mathf.RoundToInt(val * 100)}%";
                }, 
                out sfxVolumeSlider, out sfxVolumeLabel);

            // 3. Master Slider
            CreateVolumeRowVertical(contentObj.transform, "🔊 Общая громкость:", 
                (val) => {
                    if (AudioManager.Instance != null) AudioManager.Instance.SetMasterVolume(val);
                    if (masterVolumeLabel != null) masterVolumeLabel.text = $"{Mathf.RoundToInt(val * 100)}%";
                }, 
                out masterVolumeSlider, out masterVolumeLabel);

            // 4. Buttons Row (Mute & Close side-by-side)
            GameObject btnsRow = new GameObject("SettingsButtonsRow");
            btnsRow.transform.SetParent(contentObj.transform, false);
            RectTransform brRt = btnsRow.AddComponent<RectTransform>();
            brRt.sizeDelta = new Vector2(400, 44);

            HorizontalLayoutGroup bHlg = btnsRow.AddComponent<HorizontalLayoutGroup>();
            bHlg.childControlWidth = false;
            bHlg.childControlHeight = false;
            bHlg.childAlignment = TextAnchor.MiddleCenter;
            bHlg.spacing = 16;

            // Mute Button
            GameObject muteBtnObj = new GameObject("MuteBtn");
            muteBtnObj.transform.SetParent(btnsRow.transform, false);
            RectTransform mbRt = muteBtnObj.AddComponent<RectTransform>();
            mbRt.sizeDelta = new Vector2(190, 40);

            Image mbImg = muteBtnObj.AddComponent<Image>();
            mbImg.color = new Color(0.25f, 0.35f, 0.48f);
            Button mbBtn = muteBtnObj.AddComponent<Button>();
            mbBtn.targetGraphic = mbImg;

            GameObject mbTextObj = new GameObject("Text");
            mbTextObj.transform.SetParent(muteBtnObj.transform, false);
            RectTransform mbtRt = mbTextObj.AddComponent<RectTransform>();
            mbtRt.anchorMin = Vector2.zero;
            mbtRt.anchorMax = Vector2.one;
            mbtRt.offsetMin = Vector2.zero;
            mbtRt.offsetMax = Vector2.zero;

            muteButtonLabel = mbTextObj.AddComponent<Text>();
            muteButtonLabel.font = cachedFont;
            muteButtonLabel.fontSize = 14;
            muteButtonLabel.fontStyle = FontStyle.Bold;
            muteButtonLabel.color = Color.white;
            muteButtonLabel.alignment = TextAnchor.MiddleCenter;
            muteButtonLabel.text = "🔊 Звук: Включен";

            mbBtn.onClick.AddListener(() =>
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.ToggleMute();
                    muteButtonLabel.text = AudioManager.Instance.isMuted ? "🔇 Звук: Выключен" : "🔊 Звук: Включен";
                }
            });

            // Close Button
            GameObject closeBtnObj = new GameObject("CloseBtn");
            closeBtnObj.transform.SetParent(btnsRow.transform, false);
            RectTransform cbRt = closeBtnObj.AddComponent<RectTransform>();
            cbRt.sizeDelta = new Vector2(150, 40);

            Image cbImg = closeBtnObj.AddComponent<Image>();
            cbImg.color = new Color(0.65f, 0.24f, 0.22f);
            Button cbBtn = closeBtnObj.AddComponent<Button>();
            cbBtn.targetGraphic = cbImg;

            GameObject cbTextObj = new GameObject("Text");
            cbTextObj.transform.SetParent(closeBtnObj.transform, false);
            RectTransform cbtRt = cbTextObj.AddComponent<RectTransform>();
            cbtRt.anchorMin = Vector2.zero;
            cbtRt.anchorMax = Vector2.one;
            cbtRt.offsetMin = Vector2.zero;
            cbtRt.offsetMax = Vector2.zero;

            Text cbTxt = cbTextObj.AddComponent<Text>();
            cbTxt.font = cachedFont;
            cbTxt.fontSize = 15;
            cbTxt.fontStyle = FontStyle.Bold;
            cbTxt.color = Color.white;
            cbTxt.alignment = TextAnchor.MiddleCenter;
            cbTxt.text = "✕ Закрыть";

            cbBtn.onClick.AddListener(() =>
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
                CloseSettingsModal();
            });

            settingsModal.SetActive(false);
        }

        private void CreateVolumeRowVertical(Transform parent, string label, UnityEngine.Events.UnityAction<float> onValueChanged, out Slider outSlider, out Text outLabel)
        {
            GameObject rowObj = new GameObject($"Row_{label}");
            rowObj.transform.SetParent(parent, false);
            RectTransform rRt = rowObj.AddComponent<RectTransform>();
            rRt.sizeDelta = new Vector2(400, 48);

            // Top text row (Label + Percentage)
            GameObject textRow = new GameObject("TextRow");
            textRow.transform.SetParent(rowObj.transform, false);
            RectTransform trRt = textRow.AddComponent<RectTransform>();
            trRt.anchorMin = new Vector2(0, 0.55f);
            trRt.anchorMax = new Vector2(1, 1f);
            trRt.offsetMin = Vector2.zero;
            trRt.offsetMax = Vector2.zero;

            // Title
            GameObject lblObj = new GameObject("Title");
            lblObj.transform.SetParent(textRow.transform, false);
            RectTransform lRt = lblObj.AddComponent<RectTransform>();
            lRt.anchorMin = Vector2.zero;
            lRt.anchorMax = new Vector2(0.7f, 1f);
            lRt.offsetMin = Vector2.zero;
            lRt.offsetMax = Vector2.zero;
            Text lblTxt = lblObj.AddComponent<Text>();
            lblTxt.font = cachedFont;
            lblTxt.fontSize = 14;
            lblTxt.fontStyle = FontStyle.Bold;
            lblTxt.color = Color.white;
            lblTxt.alignment = TextAnchor.MiddleLeft;
            lblTxt.text = label;

            // Percent Text
            GameObject pctObj = new GameObject("PctText");
            pctObj.transform.SetParent(textRow.transform, false);
            RectTransform pRt = pctObj.AddComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0.7f, 0);
            pRt.anchorMax = Vector2.one;
            pRt.offsetMin = Vector2.zero;
            pRt.offsetMax = Vector2.zero;
            outLabel = pctObj.AddComponent<Text>();
            outLabel.font = cachedFont;
            outLabel.fontSize = 14;
            outLabel.fontStyle = FontStyle.Bold;
            outLabel.color = new Color(0.45f, 0.95f, 0.55f);
            outLabel.alignment = TextAnchor.MiddleRight;
            outLabel.text = "100%";

            // Slider in lower half
            GameObject sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(rowObj.transform, false);
            RectTransform sRt = sliderObj.AddComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0, 0);
            sRt.anchorMax = new Vector2(1, 0.45f);
            sRt.offsetMin = Vector2.zero;
            sRt.offsetMax = Vector2.zero;

            outSlider = sliderObj.AddComponent<Slider>();
            outSlider.minValue = 0f;
            outSlider.maxValue = 1f;

            // Background track
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(sliderObj.transform, false);
            RectTransform bgRt = bgObj.AddComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0, 0.2f);
            bgRt.anchorMax = new Vector2(1, 0.8f);
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.24f, 0.27f, 0.35f);

            // Fill area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform faRt = fillArea.AddComponent<RectTransform>();
            faRt.anchorMin = new Vector2(0, 0.2f);
            faRt.anchorMax = new Vector2(1, 0.8f);
            faRt.offsetMin = Vector2.zero;
            faRt.offsetMax = Vector2.zero;

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fRt = fill.AddComponent<RectTransform>();
            fRt.sizeDelta = Vector2.zero;
            Image fImg = fill.AddComponent<Image>();
            fImg.color = new Color(0.35f, 0.75f, 0.95f);

            outSlider.fillRect = fRt;
            outSlider.targetGraphic = bgImg;
            outSlider.onValueChanged.AddListener(onValueChanged);
        }

        private void CreateButton(Transform parent, string label, float w, float h, UnityEngine.Events.UnityAction action, Color? bgColor = null)
        {
            CreateButtonWithRef(parent, label, w, h, action, out _, bgColor);
        }

        private void CreateButtonWithRef(Transform parent, string label, float w, float h, UnityEngine.Events.UnityAction action, out Text labelRef, Color? bgColor = null)
        {
            GameObject btnObj = new GameObject($"Btn_{label}");
            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(w, h);

            Image img = btnObj.AddComponent<Image>();
            img.color = bgColor.HasValue ? bgColor.Value : new Color(0.25f, 0.28f, 0.36f);

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            ColorBlock cb = btn.colors;
            cb.highlightedColor = Color.Lerp(img.color, Color.white, 0.25f);
            cb.pressedColor = Color.black;
            btn.colors = cb;
            btn.onClick.AddListener(() =>
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
                action?.Invoke();
            });

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform textRt = textObj.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            labelRef = textObj.AddComponent<Text>();
            labelRef.font = cachedFont;
            labelRef.fontSize = 13;
            labelRef.fontStyle = FontStyle.Bold;
            labelRef.color = Color.white;
            labelRef.alignment = TextAnchor.MiddleCenter;
            labelRef.text = label;
        }

        private void BuildVictoryPopup(Transform canvasTransform)
        {
            if (victoryModal != null) return;

            // Full screen dimming overlay
            victoryModal = new GameObject("VictoryModal");
            victoryModal.transform.SetParent(canvasTransform, false);
            RectTransform mRt = victoryModal.AddComponent<RectTransform>();
            mRt.anchorMin = Vector2.zero;
            mRt.anchorMax = Vector2.one;
            mRt.offsetMin = Vector2.zero;
            mRt.offsetMax = Vector2.zero;

            Image dimBg = victoryModal.AddComponent<Image>();
            dimBg.color = new Color(0, 0, 0, 0.72f);

            // Centered Victory Plaque
            GameObject boxObj = new GameObject("VictoryBox");
            boxObj.transform.SetParent(victoryModal.transform, false);
            RectTransform bRt = boxObj.AddComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.5f, 0.5f);
            bRt.anchorMax = new Vector2(0.5f, 0.5f);
            bRt.pivot = new Vector2(0.5f, 0.5f);
            bRt.sizeDelta = new Vector2(520, 360);

            Image boxBg = boxObj.AddComponent<Image>();
            boxBg.color = new Color(0.12f, 0.15f, 0.22f, 0.98f);

            // Gold Header Bar
            GameObject headerObj = new GameObject("VictoryHeader");
            headerObj.transform.SetParent(boxObj.transform, false);
            RectTransform hRt = headerObj.AddComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0, 1);
            hRt.anchorMax = new Vector2(1, 1);
            hRt.pivot = new Vector2(0.5f, 1);
            hRt.offsetMin = new Vector2(0, -90);
            hRt.offsetMax = new Vector2(0, 0);

            VerticalLayoutGroup headerVlg = headerObj.AddComponent<VerticalLayoutGroup>();
            headerVlg.childControlWidth = true;
            headerVlg.childControlHeight = false;
            headerVlg.childAlignment = TextAnchor.MiddleCenter;
            headerVlg.spacing = 4;
            headerVlg.padding = new RectOffset(0, 0, 16, 0);

            // Title
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(headerObj.transform, false);
            RectTransform tRt = titleObj.AddComponent<RectTransform>();
            tRt.sizeDelta = new Vector2(480, 40);
            Text vTitle = titleObj.AddComponent<Text>();
            vTitle.font = cachedFont;
            vTitle.fontSize = 28;
            vTitle.fontStyle = FontStyle.Bold;
            vTitle.color = new Color(1.0f, 0.85f, 0.35f);
            vTitle.alignment = TextAnchor.MiddleCenter;
            vTitle.text = "🏆 ПОБЕДА! 🏆";

            // Subtitle
            GameObject subObj = new GameObject("SubtitleText");
            subObj.transform.SetParent(headerObj.transform, false);
            RectTransform subRt = subObj.AddComponent<RectTransform>();
            subRt.sizeDelta = new Vector2(480, 24);
            Text vSub = subObj.AddComponent<Text>();
            vSub.font = cachedFont;
            vSub.fontSize = 14;
            vSub.color = new Color(0.85f, 0.90f, 0.95f);
            vSub.alignment = TextAnchor.MiddleCenter;
            vSub.text = "КРОССВОРД УСПЕШНО РЕШЁН!";

            // Stats Container
            GameObject statsObj = new GameObject("StatsBox");
            statsObj.transform.SetParent(boxObj.transform, false);
            RectTransform stRt = statsObj.AddComponent<RectTransform>();
            stRt.anchorMin = new Vector2(0, 0);
            stRt.anchorMax = new Vector2(1, 1);
            stRt.offsetMin = new Vector2(30, 80);
            stRt.offsetMax = new Vector2(-30, -95);

            VerticalLayoutGroup statsVlg = statsObj.AddComponent<VerticalLayoutGroup>();
            statsVlg.childControlWidth = true;
            statsVlg.childControlHeight = false;
            statsVlg.childAlignment = TextAnchor.MiddleCenter;
            statsVlg.spacing = 14;

            // Time Stat Card
            GameObject timeObj = new GameObject("TimeCard");
            timeObj.transform.SetParent(statsObj.transform, false);
            RectTransform timRt = timeObj.AddComponent<RectTransform>();
            timRt.sizeDelta = new Vector2(440, 48);
            Image timBg = timeObj.AddComponent<Image>();
            timBg.color = new Color(0.18f, 0.22f, 0.30f, 0.90f);

            GameObject timeTextObj = new GameObject("Text");
            timeTextObj.transform.SetParent(timeObj.transform, false);
            RectTransform ttRt = timeTextObj.AddComponent<RectTransform>();
            ttRt.anchorMin = Vector2.zero;
            ttRt.anchorMax = Vector2.one;
            ttRt.offsetMin = Vector2.zero;
            ttRt.offsetMax = Vector2.zero;
            victoryTimeText = timeTextObj.AddComponent<Text>();
            victoryTimeText.font = cachedFont;
            victoryTimeText.fontSize = 18;
            victoryTimeText.fontStyle = FontStyle.Bold;
            victoryTimeText.color = Color.white;
            victoryTimeText.alignment = TextAnchor.MiddleCenter;
            victoryTimeText.text = "⏱ Время прохождения: 00:00";

            // Score Stat Card
            GameObject scoreObj = new GameObject("ScoreCard");
            scoreObj.transform.SetParent(statsObj.transform, false);
            RectTransform sRt = scoreObj.AddComponent<RectTransform>();
            sRt.sizeDelta = new Vector2(440, 48);
            Image scoreBg = scoreObj.AddComponent<Image>();
            scoreBg.color = new Color(0.18f, 0.22f, 0.30f, 0.90f);

            GameObject scoreTextObj = new GameObject("Text");
            scoreTextObj.transform.SetParent(scoreObj.transform, false);
            RectTransform sttRt = scoreTextObj.AddComponent<RectTransform>();
            sttRt.anchorMin = Vector2.zero;
            sttRt.anchorMax = Vector2.one;
            sttRt.offsetMin = Vector2.zero;
            sttRt.offsetMax = Vector2.zero;
            victoryScoreText = scoreTextObj.AddComponent<Text>();
            victoryScoreText.font = cachedFont;
            victoryScoreText.fontSize = 20;
            victoryScoreText.fontStyle = FontStyle.Bold;
            victoryScoreText.color = new Color(0.45f, 0.95f, 0.55f);
            victoryScoreText.alignment = TextAnchor.MiddleCenter;
            victoryScoreText.text = "★ Итоговый счёт: 0 очков";

            // Buttons Container at Bottom (Centered inside box, no overflow)
            GameObject btnsObj = new GameObject("VictoryButtons");
            btnsObj.transform.SetParent(boxObj.transform, false);
            RectTransform btnAreaRt = btnsObj.AddComponent<RectTransform>();
            btnAreaRt.anchorMin = new Vector2(0.5f, 0);
            btnAreaRt.anchorMax = new Vector2(0.5f, 0);
            btnAreaRt.pivot = new Vector2(0.5f, 0);
            btnAreaRt.anchoredPosition = new Vector2(0, 24);
            btnAreaRt.sizeDelta = new Vector2(440, 48);

            HorizontalLayoutGroup bHlg = btnsObj.AddComponent<HorizontalLayoutGroup>();
            bHlg.childControlWidth = false;
            bHlg.childControlHeight = false;
            bHlg.childAlignment = TextAnchor.MiddleCenter;
            bHlg.spacing = 18;

            // 1. Next Level Button
            CreateButton(btnsObj.transform, "▶ Следующий уровень", 190, 44, () =>
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
                if (CrosswordManager.Instance != null)
                {
                    CrosswordManager.Instance.LoadNextLevel();
                }
            }, new Color(0.20f, 0.55f, 0.32f));

            // 2. Restart Current Level Button
            CreateButton(btnsObj.transform, "⟳ Пройти заново", 170, 44, () =>
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
                if (CrosswordManager.Instance != null)
                {
                    CrosswordManager.Instance.LoadLevelByIndex(CrosswordManager.Instance.currentLevelIndex);
                }
            }, new Color(0.25f, 0.38f, 0.55f));

            victoryModal.SetActive(false);
        }
    }
}

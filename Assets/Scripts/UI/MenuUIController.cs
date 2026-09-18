using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using GardenGuardians.Core;
using GardenGuardians.Services;

namespace GardenGuardians.UI
{
    public class MenuUIController : MonoBehaviour
    {
        [Header("Level Buttons")]
        [SerializeField] private Button level1Button;
        [SerializeField] private Button level2Button;
        [SerializeField] private Button level3Button;

        [Header("Daily Challenge Display")]
        [SerializeField] private Text challengeDateText;
        [SerializeField] private Text challengeDetailsText;
        [SerializeField] private Text offlineStatusBadge;
        [SerializeField] private Button dailyPlayButton;

        [Header("Settings")]
        [SerializeField] private Button soundToggleButton;
        [SerializeField] private Text soundButtonText;

        private DailyChallengeData currentDailyData;

        private void Awake()
        {
            FixEventSystemInputModule();
            SetupBackground();

            if (level1Button == null)
            {
                BuildRuntimeHomeUI();
            }
        }

        private void FixEventSystemInputModule()
        {
            var eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                var esObj = new GameObject("EventSystem");
                eventSystem = esObj.AddComponent<EventSystem>();
            }

            var standalone = eventSystem.GetComponent<StandaloneInputModule>();
            if (standalone != null)
            {
                DestroyImmediate(standalone);
            }

            var inputModule = eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            if (inputModule == null)
            {
                eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
        }

        private void SetupBackground()
        {
            var bgObj = GameObject.Find("MenuBackground");
            if (bgObj == null)
            {
                bgObj = new GameObject("MenuBackground");
                bgObj.transform.position = new Vector3(0, 0, 5f);
                var sr = bgObj.AddComponent<SpriteRenderer>();
                sr.sortingOrder = -10;
                var bgSprite = LoadSpriteAsset("Assets/Sprites/bg_cosmic.png");
                if (bgSprite != null)
                {
                    sr.sprite = bgSprite;
                    bgObj.transform.localScale = new Vector3(1.15f, 1.15f, 1f);
                }
            }
        }

        private void Start()
        {
            var save = SaveService.Load();

            if (level1Button != null)
                level1Button.onClick.AddListener(() => StartLevel(1));

            if (level2Button != null)
            {
                bool unlocked = save.highestUnlockedLevel >= 2;
                level2Button.interactable = unlocked;
                level2Button.onClick.AddListener(() => StartLevel(2));
            }

            if (level3Button != null)
            {
                bool unlocked = save.highestUnlockedLevel >= 3;
                level3Button.interactable = unlocked;
                level3Button.onClick.AddListener(() => StartLevel(3));
            }

            if (DailyChallengeService.Instance != null)
            {
                DailyChallengeService.Instance.OnChallengeLoaded += OnDailyChallengeReceived;
                DailyChallengeService.Instance.FetchDailyChallenge();
            }

            if (dailyPlayButton != null)
            {
                dailyPlayButton.onClick.AddListener(PlayDailyChallenge);
            }

            if (soundToggleButton != null)
            {
                soundToggleButton.onClick.AddListener(ToggleSound);
                UpdateSoundUI();
            }
        }

        private void OnDestroy()
        {
            if (DailyChallengeService.Instance != null)
            {
                DailyChallengeService.Instance.OnChallengeLoaded -= OnDailyChallengeReceived;
            }
        }

        private void OnDailyChallengeReceived(DailyChallengeData challenge)
        {
            if (challenge == null) return;
            currentDailyData = challenge;

            if (challengeDateText != null)
                challengeDateText.text = $"📅 Date: {challenge.date}";

            if (challengeDetailsText != null)
                challengeDetailsText.text = $"Wave: {challenge.wavePreset}  |  Energy: {challenge.startingEnergy} ⚡";

            if (offlineStatusBadge != null)
            {
                offlineStatusBadge.gameObject.SetActive(true);
                offlineStatusBadge.text = challenge.isOfflineFallback ? "● Offline Fallback" : "● Live Online API";
                offlineStatusBadge.color = challenge.isOfflineFallback ? new Color(1f, 0.6f, 0.2f) : new Color(0.2f, 0.9f, 0.4f);
            }
        }

        public void StartLevel(int levelIndex)
        {
            GameManager.SelectedLevelIndex = levelIndex;
            GameManager.IsDailyChallenge = false;
            Debug.Log($"[Home Screen] Launching Level {levelIndex}...");
            SceneManager.LoadScene("Game");
        }

        public void PlayDailyChallenge()
        {
            GameManager.IsDailyChallenge = true;
            GameManager.ActiveDailyChallenge = currentDailyData ?? new DailyChallengeData();
            Debug.Log("[Home Screen] Launching Daily Challenge...");
            SceneManager.LoadScene("Game");
        }

        private void ToggleSound()
        {
            var save = SaveService.Load();
            save.soundEnabled = !save.soundEnabled;
            SaveService.Save(save);
            AudioListener.pause = !save.soundEnabled;
            UpdateSoundUI();
        }

        private void UpdateSoundUI()
        {
            var save = SaveService.Load();
            if (soundButtonText != null)
            {
                soundButtonText.text = save.soundEnabled ? "🔊 Sound: ON" : "🔇 Sound: OFF";
            }
        }

        private void BuildRuntimeHomeUI()
        {
            var save = SaveService.Load();

            // 1. Header (Title & Subtitle)
            CreateText(gameObject, "Title", "GARDEN GUARDIANS", 54, new Vector2(0.5f, 0.88f), new Vector2(0.5f, 0.88f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 70), new Color(0.35f, 0.95f, 0.65f)).alignment = TextAnchor.MiddleCenter;
            CreateText(gameObject, "Subtitle", "TACTICAL 2D MOBILE TOWER DEFENSE", 20, new Vector2(0.5f, 0.81f), new Vector2(0.5f, 0.81f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 35), new Color(0.7f, 0.85f, 1f)).alignment = TextAnchor.MiddleCenter;

            // 2. Three Level Selection Cards (Horizontal Layout)
            float cardWidth = 320f;
            float cardHeight = 360f;
            float spacing = 360f;

            // --- Level 1 Card ---
            GameObject card1 = CreatePanel("Level1Card", new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.5f), new Vector2(-spacing, 0), new Vector2(cardWidth, cardHeight), new Color(0.10f, 0.16f, 0.24f, 0.94f));
            CreateText(card1, "L1Icon", "🌿", 44, new Vector2(0.5f, 0.82f), new Vector2(0.5f, 0.82f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100, 60), Color.white).alignment = TextAnchor.MiddleCenter;
            CreateText(card1, "L1Title", "LEVEL 1", 24, new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(280, 30), new Color(0.4f, 0.9f, 0.6f)).alignment = TextAnchor.MiddleCenter;
            CreateText(card1, "L1Sub", "The Outskirts\nCrawlers • Fundamentals", 17, new Vector2(0.5f, 0.50f), new Vector2(0.5f, 0.50f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(280, 50), new Color(0.8f, 0.85f, 0.9f)).alignment = TextAnchor.MiddleCenter;
            level1Button = CreateButton(card1, "L1Btn", "PLAY LEVEL 1", new Vector2(0.5f, 0.20f), new Vector2(0.5f, 0.20f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240, 55), new Color(0.18f, 0.65f, 0.35f));

            // --- Level 2 Card ---
            bool l2Unlocked = save.highestUnlockedLevel >= 2;
            Color card2Col = l2Unlocked ? new Color(0.10f, 0.16f, 0.24f, 0.94f) : new Color(0.08f, 0.10f, 0.14f, 0.85f);
            GameObject card2 = CreatePanel("Level2Card", new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(cardWidth, cardHeight), card2Col);
            CreateText(card2, "L2Icon", l2Unlocked ? "⚡" : "🔒", 44, new Vector2(0.5f, 0.82f), new Vector2(0.5f, 0.82f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100, 60), Color.white).alignment = TextAnchor.MiddleCenter;
            CreateText(card2, "L2Title", "LEVEL 2", 24, new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(280, 30), l2Unlocked ? new Color(0.95f, 0.75f, 0.2f) : Color.gray).alignment = TextAnchor.MiddleCenter;
            CreateText(card2, "L2Sub", l2Unlocked ? "Fast Surge\nRunners • Overlapping Fire" : "Complete Level 1 to Unlock", 17, new Vector2(0.5f, 0.50f), new Vector2(0.5f, 0.50f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(280, 50), new Color(0.8f, 0.85f, 0.9f)).alignment = TextAnchor.MiddleCenter;
            level2Button = CreateButton(card2, "L2Btn", l2Unlocked ? "PLAY LEVEL 2" : "LOCKED 🔒", new Vector2(0.5f, 0.20f), new Vector2(0.5f, 0.20f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240, 55), l2Unlocked ? new Color(0.9f, 0.6f, 0.15f) : new Color(0.25f, 0.28f, 0.32f));

            // --- Level 3 Card ---
            bool l3Unlocked = save.highestUnlockedLevel >= 3;
            Color card3Col = l3Unlocked ? new Color(0.10f, 0.16f, 0.24f, 0.94f) : new Color(0.08f, 0.10f, 0.14f, 0.85f);
            GameObject card3 = CreatePanel("Level3Card", new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.5f), new Vector2(spacing, 0), new Vector2(cardWidth, cardHeight), card3Col);
            CreateText(card3, "L3Icon", l3Unlocked ? "🌀" : "🔒", 44, new Vector2(0.5f, 0.82f), new Vector2(0.5f, 0.82f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100, 60), Color.white).alignment = TextAnchor.MiddleCenter;
            CreateText(card3, "L3Title", "LEVEL 3", 24, new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(280, 30), l3Unlocked ? new Color(0.7f, 0.4f, 1f) : Color.gray).alignment = TextAnchor.MiddleCenter;
            CreateText(card3, "L3Sub", l3Unlocked ? "The Labyrinth\nBrutes • Maze Shaping" : "Complete Level 2 to Unlock", 17, new Vector2(0.5f, 0.50f), new Vector2(0.5f, 0.50f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(280, 50), new Color(0.8f, 0.85f, 0.9f)).alignment = TextAnchor.MiddleCenter;
            level3Button = CreateButton(card3, "L3Btn", l3Unlocked ? "PLAY LEVEL 3" : "LOCKED 🔒", new Vector2(0.5f, 0.20f), new Vector2(0.5f, 0.20f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240, 55), l3Unlocked ? new Color(0.6f, 0.3f, 0.9f) : new Color(0.25f, 0.28f, 0.32f));

            // 3. Daily Challenge REST API Bar (Bottom Left)
            GameObject dailyPanel = CreatePanel("DailyChallengePanel", new Vector2(0.5f, 0.16f), new Vector2(0.5f, 0.16f), new Vector2(0.5f, 0.5f), new Vector2(-120, 0), new Vector2(680, 110), new Color(0.12f, 0.16f, 0.22f, 0.95f));
            CreateText(dailyPanel, "DH", "🌐 DAILY CHALLENGE (REST API)", 19, new Vector2(0, 0.75f), new Vector2(0, 0.75f), new Vector2(0, 0.5f), new Vector2(20, 0), new Vector2(380, 25), new Color(1f, 0.85f, 0.3f));
            challengeDateText = CreateText(dailyPanel, "DDate", $"📅 Date: {DateTime.UtcNow:yyyy-MM-dd}", 17, new Vector2(0, 0.48f), new Vector2(0, 0.48f), new Vector2(0, 0.5f), new Vector2(20, 0), new Vector2(380, 24), Color.white);
            challengeDetailsText = CreateText(dailyPanel, "DDet", "Preset: fast-rush  |  Energy: 75 ⚡", 16, new Vector2(0, 0.22f), new Vector2(0, 0.22f), new Vector2(0, 0.5f), new Vector2(20, 0), new Vector2(380, 24), new Color(0.8f, 0.8f, 0.85f));
            offlineStatusBadge = CreateText(dailyPanel, "Badge", "● Connecting...", 15, new Vector2(0.70f, 0.75f), new Vector2(0.70f, 0.75f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(160, 25), Color.cyan);
            dailyPlayButton = CreateButton(dailyPanel, "DailyPlayBtn", "PLAY DAILY", new Vector2(1, 0.45f), new Vector2(1, 0.45f), new Vector2(1, 0.5f), new Vector2(-15, 0), new Vector2(170, 55), new Color(0.2f, 0.55f, 0.85f));

            // 4. Sound Settings Button (Bottom Right)
            soundToggleButton = CreateButton(gameObject, "SoundBtn", "🔊 Sound: ON", new Vector2(0.5f, 0.16f), new Vector2(0.5f, 0.16f), new Vector2(0.5f, 0.5f), new Vector2(400, 0), new Vector2(190, 55), new Color(0.22f, 0.28f, 0.38f));
            soundButtonText = soundToggleButton.GetComponentInChildren<Text>();
        }

        private GameObject CreatePanel(string name, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size, Color col)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(transform, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = aMin;
            rect.anchorMax = aMax;
            rect.pivot = pivot;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            var img = obj.AddComponent<Image>();
            img.color = col;
            return obj;
        }

        private Text CreateText(GameObject parent, string name, string content, int fontSize, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size, Color col)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent.transform, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = aMin;
            rect.anchorMax = aMax;
            rect.pivot = pivot;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            var txt = obj.AddComponent<Text>();
            txt.text = content;
            txt.fontSize = fontSize;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.color = col;
            txt.alignment = TextAnchor.MiddleLeft;
            return txt;
        }

        private Button CreateButton(GameObject parent, string name, string label, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size, Color col)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent.transform, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = aMin;
            rect.anchorMax = aMax;
            rect.pivot = pivot;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            var img = obj.AddComponent<Image>();
            img.color = col;

            var btn = obj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = col;
            cb.highlightedColor = Color.Lerp(col, Color.white, 0.3f);
            cb.pressedColor = col * 0.7f;
            cb.disabledColor = new Color(0.2f, 0.22f, 0.25f, 0.6f);
            btn.colors = cb;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(obj.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            var txt = textObj.AddComponent<Text>();
            txt.text = label;
            txt.fontSize = 20;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;

            return btn;
        }

        private Sprite LoadSpriteAsset(string path)
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
#else
            return null;
#endif
        }
    }
}

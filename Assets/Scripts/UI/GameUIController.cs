using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using GardenGuardians.Core;
using GardenGuardians.Gameplay;
using GardenGuardians.Data;

namespace GardenGuardians.UI
{
    public class GameUIController : MonoBehaviour
    {
        [Header("Status HUD")]
        [SerializeField] private Text livesText;
        [SerializeField] private Text energyText;
        [SerializeField] private Text waveText;
        [SerializeField] private Text notificationText;

        [Header("Build Buttons & Definitions")]
        [SerializeField] private Button turretButton;
        [SerializeField] private Button slowTowerButton;
        [SerializeField] private Button wallButton;
        [SerializeField] private Button startWaveButton;

        [SerializeField] private TowerData turretData;
        [SerializeField] private TowerData slowTowerData;
        [SerializeField] private TowerData wallData;

        [Header("Game Over Panels")]
        [SerializeField] private GameObject winPanel;
        [SerializeField] private GameObject lossPanel;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button menuButton;

        [Header("Controls")]
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button muteButton;

        private float notificationTimer = 0f;

        private void Awake()
        {
            FixEventSystemInputModule();

            if (turretButton == null)
            {
                BuildRuntimeUI();
            }
        }

        private void FixEventSystemInputModule()
        {
            // Fix Unity 6 Input System conflict: ensure InputSystemUIInputModule is present
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

        private void Start()
        {
            if (turretButton != null) turretButton.onClick.AddListener(() => SelectBuild(turretData));
            if (slowTowerButton != null) slowTowerButton.onClick.AddListener(() => SelectBuild(slowTowerData));
            if (wallButton != null) wallButton.onClick.AddListener(() => SelectBuild(wallData));
            if (startWaveButton != null) startWaveButton.onClick.AddListener(OnStartWaveClicked);

            if (restartButton != null) restartButton.onClick.AddListener(OnRestartClicked);
            if (menuButton != null) menuButton.onClick.AddListener(OnMenuClicked);

            if (pauseButton != null) pauseButton.onClick.AddListener(OnPauseClicked);
            if (muteButton != null) muteButton.onClick.AddListener(OnMuteClicked);

            if (winPanel != null) winPanel.SetActive(false);
            if (lossPanel != null) lossPanel.SetActive(false);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnLivesChanged += UpdateLivesUI;
                GameManager.Instance.OnEnergyChanged += UpdateEnergyUI;
                GameManager.Instance.OnGameFinished += ShowGameOverPanel;

                UpdateLivesUI(GameManager.Instance.CurrentLives);
                UpdateEnergyUI(GameManager.Instance.CurrentEnergy);
            }

            if (BoardController.Instance != null)
            {
                BoardController.Instance.OnNotificationMessage += ShowNotification;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnLivesChanged -= UpdateLivesUI;
                GameManager.Instance.OnEnergyChanged -= UpdateEnergyUI;
                GameManager.Instance.OnGameFinished -= ShowGameOverPanel;
            }

            if (BoardController.Instance != null)
            {
                BoardController.Instance.OnNotificationMessage -= ShowNotification;
            }
        }

        private void Update()
        {
            if (notificationTimer > 0f)
            {
                notificationTimer -= Time.deltaTime;
                if (notificationTimer <= 0f && notificationText != null)
                {
                    notificationText.text = "";
                }
            }

            var spawner = FindFirstObjectByType<WaveSpawner>();
            if (spawner != null && waveText != null)
            {
                waveText.text = $"🌊 Wave: {spawner.CurrentWaveIndex} / {spawner.TotalWaves}";
            }
        }

        public void SelectBuild(TowerData data)
        {
            if (BoardController.Instance != null)
            {
                BoardController.Instance.SetActiveBuildType(data);
            }
        }

        private void OnStartWaveClicked()
        {
            var spawner = FindFirstObjectByType<WaveSpawner>();
            if (spawner != null)
            {
                spawner.StartNextWave();
            }
        }

        private void UpdateLivesUI(int lives)
        {
            if (livesText != null)
            {
                livesText.text = $"❤️ Lives: {lives}";
            }
        }

        private void UpdateEnergyUI(int energy)
        {
            if (energyText != null)
            {
                energyText.text = $"⚡ Energy: {energy}";
            }
        }

        private void ShowNotification(string msg)
        {
            if (notificationText != null)
            {
                notificationText.text = msg;
                notificationTimer = 3.0f;
            }
        }

        private void ShowGameOverPanel(bool won)
        {
            if (won)
            {
                if (winPanel != null) winPanel.SetActive(true);
            }
            else
            {
                if (lossPanel != null) lossPanel.SetActive(true);
            }
        }

        private void OnRestartClicked()
        {
            if (GameManager.Instance != null) GameManager.Instance.RestartLevel();
        }

        private void OnMenuClicked()
        {
            if (GameManager.Instance != null) GameManager.Instance.LoadMenu();
        }

        private void OnPauseClicked()
        {
            if (GameManager.Instance != null) GameManager.Instance.TogglePause();
        }

        private void OnMuteClicked()
        {
            AudioListener.pause = !AudioListener.pause;
        }

        private void BuildRuntimeUI()
        {
            if (turretData == null)
            {
                turretData = ScriptableObject.CreateInstance<TowerData>();
                turretData.towerType = TowerType.Turret;
                turretData.towerName = "Blaster Turret";
                turretData.cost = 25;
                turretData.range = 2f;
                turretData.attackInterval = 1f;
                turretData.damage = 1;
                turretData.towerColor = new Color(0.2f, 0.6f, 0.9f, 1f);
            }

            if (slowTowerData == null)
            {
                slowTowerData = ScriptableObject.CreateInstance<TowerData>();
                slowTowerData.towerType = TowerType.SlowTower;
                slowTowerData.towerName = "Frost Spire";
                slowTowerData.cost = 35;
                slowTowerData.range = 2f;
                slowTowerData.attackInterval = 1f;
                slowTowerData.damage = 1;
                slowTowerData.slowDuration = 1.5f;
                slowTowerData.slowMultiplier = 0.5f;
                slowTowerData.towerColor = new Color(0.6f, 0.3f, 0.9f, 1f);
            }

            if (wallData == null)
            {
                wallData = ScriptableObject.CreateInstance<TowerData>();
                wallData.towerType = TowerType.Wall;
                wallData.towerName = "Barrier Wall";
                wallData.cost = 15;
                wallData.range = 0f;
                wallData.attackInterval = 0f;
                wallData.damage = 0;
                wallData.towerColor = new Color(0.5f, 0.55f, 0.6f, 1f);
            }

            // Top Glassmorphism HUD Bar
            GameObject topBar = CreatePanel("TopBar", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(0, 80), new Color(0.08f, 0.12f, 0.18f, 0.92f));

            livesText = CreateText(topBar, "LivesText", "❤️ Lives: 5", 26, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(30, 0), new Vector2(160, 50), new Color(1f, 0.35f, 0.35f));
            energyText = CreateText(topBar, "EnergyText", "⚡ Energy: 100", 26, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(200, 0), new Vector2(180, 50), new Color(1f, 0.85f, 0.2f));
            waveText = CreateText(topBar, "WaveText", "🌊 Wave: 0 / 1", 26, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(390, 0), new Vector2(180, 50), new Color(0.3f, 0.85f, 1f));

            notificationText = CreateText(topBar, "NotificationText", "", 24, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(50, 0), new Vector2(500, 50), new Color(1f, 0.92f, 0.4f));
            notificationText.alignment = TextAnchor.MiddleCenter;

            pauseButton = CreateButton(topBar, "PauseButton", "⏸️ Pause", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-160, 0), new Vector2(110, 48), new Color(0.2f, 0.28f, 0.38f));
            muteButton = CreateButton(topBar, "MuteButton", "🔊 Mute", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-40, 0), new Vector2(100, 48), new Color(0.2f, 0.28f, 0.38f));

            // Bottom Action Dock
            GameObject bottomBar = CreatePanel("BottomBar", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 65), new Vector2(0, 130), new Color(0.08f, 0.12f, 0.18f, 0.95f));

            turretButton = CreateButton(bottomBar, "TurretButton", "🔫 Turret (25 ⚡)", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-360, 0), new Vector2(220, 80), new Color(0.15f, 0.48f, 0.88f));
            slowTowerButton = CreateButton(bottomBar, "SlowTowerButton", "❄️ Frost Spire (35 ⚡)", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-120, 0), new Vector2(220, 80), new Color(0.58f, 0.22f, 0.88f));
            wallButton = CreateButton(bottomBar, "WallButton", "🧱 Wall (15 ⚡)", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(120, 0), new Vector2(220, 80), new Color(0.35f, 0.42f, 0.52f));
            startWaveButton = CreateButton(bottomBar, "StartWaveButton", "▶️ Start Wave", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(360, 0), new Vector2(220, 80), new Color(0.12f, 0.72f, 0.35f));

            // Victory Panel
            winPanel = CreatePanel("WinPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520, 340), new Color(0.08f, 0.18f, 0.12f, 0.98f));
            CreateText(winPanel, "WinTitle", "🎉 VICTORY! 🎉", 42, new Vector2(0.5f, 0.8f), new Vector2(0.5f, 0.8f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400, 60), new Color(0.3f, 1f, 0.5f)).alignment = TextAnchor.MiddleCenter;
            restartButton = CreateButton(winPanel, "RestartButton", "Play Again", new Vector2(0.5f, 0.45f), new Vector2(0.5f, 0.45f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240, 55), new Color(0.18f, 0.65f, 0.32f));
            menuButton = CreateButton(winPanel, "MenuButton", "Back to Menu", new Vector2(0.5f, 0.2f), new Vector2(0.5f, 0.2f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240, 55), new Color(0.3f, 0.38f, 0.48f));
            winPanel.SetActive(false);

            // Defeat Panel
            lossPanel = CreatePanel("LossPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520, 340), new Color(0.22f, 0.08f, 0.08f, 0.98f));
            CreateText(lossPanel, "LossTitle", "💀 CORE BREACHED! 💀", 38, new Vector2(0.5f, 0.8f), new Vector2(0.5f, 0.8f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(450, 60), new Color(1f, 0.3f, 0.3f)).alignment = TextAnchor.MiddleCenter;
            Button restartLoss = CreateButton(lossPanel, "RestartLossButton", "Retry", new Vector2(0.5f, 0.45f), new Vector2(0.5f, 0.45f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240, 55), new Color(0.82f, 0.22f, 0.22f));
            restartLoss.onClick.AddListener(OnRestartClicked);
            Button menuLoss = CreateButton(lossPanel, "MenuLossButton", "Back to Menu", new Vector2(0.5f, 0.2f), new Vector2(0.5f, 0.2f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240, 55), new Color(0.3f, 0.38f, 0.48f));
            menuLoss.onClick.AddListener(OnMenuClicked);
            lossPanel.SetActive(false);
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
    }
}

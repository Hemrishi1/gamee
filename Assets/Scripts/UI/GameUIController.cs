using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GardenGuardians.Core;
using GardenGuardians.Gameplay;
using GardenGuardians.Data;

namespace GardenGuardians.UI
{
    public class GameUIController : MonoBehaviour
    {
        [Header("Status HUD")]
        [SerializeField] private TextMeshProUGUI livesText;
        [SerializeField] private TextMeshProUGUI energyText;
        [SerializeField] private TextMeshProUGUI waveText;
        [SerializeField] private TextMeshProUGUI notificationText;

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

            // Update wave text
            var spawner = FindFirstObjectByType<WaveSpawner>();
            if (spawner != null && waveText != null)
            {
                waveText.text = $"Wave: {spawner.CurrentWaveIndex} / {spawner.TotalWaves}";
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
                livesText.text = $"❤️ {lives}";
            }
        }

        private void UpdateEnergyUI(int energy)
        {
            if (energyText != null)
            {
                energyText.text = $"⚡ {energy}";
            }
        }

        private void ShowNotification(string msg)
        {
            if (notificationText != null)
            {
                notificationText.text = msg;
                notificationTimer = 2.5f;
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
    }
}

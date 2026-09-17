using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
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
        [SerializeField] private TextMeshProUGUI challengeDateText;
        [SerializeField] private TextMeshProUGUI challengeDetailsText;
        [SerializeField] private TextMeshProUGUI offlineStatusBadge;
        [SerializeField] private Button dailyPlayButton;

        [Header("Settings")]
        [SerializeField] private Button soundToggleButton;
        [SerializeField] private TextMeshProUGUI soundButtonText;

        private void Start()
        {
            var save = SaveService.Load();

            // Setup level buttons
            if (level1Button != null)
                level1Button.onClick.AddListener(() => LoadLevel(1));

            if (level2Button != null)
            {
                bool unlocked = save.highestUnlockedLevel >= 2;
                level2Button.interactable = unlocked;
                level2Button.onClick.AddListener(() => LoadLevel(2));
            }

            if (level3Button != null)
            {
                bool unlocked = save.highestUnlockedLevel >= 3;
                level3Button.interactable = unlocked;
                level3Button.onClick.AddListener(() => LoadLevel(3));
            }

            // Setup Daily Challenge
            if (DailyChallengeService.Instance != null)
            {
                DailyChallengeService.Instance.OnChallengeLoaded += OnDailyChallengeReceived;
                DailyChallengeService.Instance.FetchDailyChallenge();
            }

            if (dailyPlayButton != null)
            {
                dailyPlayButton.onClick.AddListener(PlayDailyChallenge);
            }

            // Audio toggle
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

            if (challengeDateText != null)
                challengeDateText.text = $"Date: {challenge.date}";

            if (challengeDetailsText != null)
                challengeDetailsText.text = $"Preset: {challenge.wavePreset} | Starting Energy: {challenge.startingEnergy}";

            if (offlineStatusBadge != null)
            {
                offlineStatusBadge.gameObject.SetActive(challenge.isOfflineFallback);
                offlineStatusBadge.text = challenge.isOfflineFallback ? "[Offline Mode Active]" : "[Online Challenge]";
            }
        }

        private void LoadLevel(int levelIndex)
        {
            SceneManager.LoadScene("Game");
        }

        private void PlayDailyChallenge()
        {
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
    }
}

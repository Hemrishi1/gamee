using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace GardenGuardians.Services
{
    [Serializable]
    public class DailyChallengeData
    {
        public string date = "2026-09-15";
        public int startingEnergy = 75;
        public string wavePreset = "fast-rush";
        public bool isOfflineFallback = false;
    }

    public class DailyChallengeService : MonoBehaviour
    {
        public static DailyChallengeService Instance { get; private set; }

        // Default endpoint (can be configured or point to raw gist / mock)
        [SerializeField] private string challengeUrl = "https://raw.githubusercontent.com/Hemrishi1/gamee/main/daily_challenge.json";
        [SerializeField] private float requestTimeout = 4f;

        private DailyChallengeData cachedChallenge;

        public event Action<DailyChallengeData> OnChallengeLoaded;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void FetchDailyChallenge()
        {
            StartCoroutine(FetchRoutine());
        }

        private IEnumerator FetchRoutine()
        {
            using (UnityWebRequest req = UnityWebRequest.Get(challengeUrl))
            {
                req.timeout = Mathf.RoundToInt(requestTimeout);
                yield return req.SendWebRequest();

                DailyChallengeData result = null;

                if (req.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        string json = req.downloadHandler.text;
                        result = JsonUtility.FromJson<DailyChallengeData>(json);

                        // Validate payload (Section 8.2)
                        if (result != null && result.startingEnergy > 0 && !string.IsNullOrEmpty(result.wavePreset))
                        {
                            result.isOfflineFallback = false;
                            cachedChallenge = result;
                            Debug.Log($"[DailyChallenge] Online challenge loaded for {result.date}, Preset: {result.wavePreset}");
                        }
                        else
                        {
                            result = null; // Malformed payload
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[DailyChallenge] JSON parse error: {ex.Message}. Using fallback.");
                        result = null;
                    }
                }
                else
                {
                    Debug.Log($"[DailyChallenge] Network request unsuccessful ({req.error}). Activating offline fallback.");
                }

                // Fallback handling
                if (result == null)
                {
                    result = cachedChallenge ?? GetBuiltinOfflineChallenge();
                }

                OnChallengeLoaded?.Invoke(result);
            }
        }

        public DailyChallengeData GetBuiltinOfflineChallenge()
        {
            return new DailyChallengeData
            {
                date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                startingEnergy = 75,
                wavePreset = "fast-rush",
                isOfflineFallback = true
            };
        }
    }
}

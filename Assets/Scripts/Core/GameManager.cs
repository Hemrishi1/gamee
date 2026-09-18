using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using GardenGuardians.Gameplay;
using GardenGuardians.Data;
using GardenGuardians.Services;

namespace GardenGuardians.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public static int SelectedLevelIndex = 1;
        public static bool IsDailyChallenge = false;
        public static DailyChallengeData ActiveDailyChallenge = null;

        [Header("Level Configuration")]
        [SerializeField] private LevelData currentLevel;
        [SerializeField] private List<LevelData> allLevels = new List<LevelData>();

        [Header("Runtime State")]
        private int currentLives = 5;
        private int currentEnergy = 100;
        private int aliveEnemiesCount = 0;
        private bool isGameOver = false;
        private bool isPaused = false;

        public int CurrentLives => currentLives;
        public int CurrentEnergy => currentEnergy;
        public int AliveEnemiesCount => aliveEnemiesCount;
        public bool IsGameOver => isGameOver;
        public bool IsPaused => isPaused;
        public LevelData CurrentLevel => currentLevel;

        public event Action<int> OnLivesChanged;
        public event Action<int> OnEnergyChanged;
        public event Action<bool> OnGameFinished;
        public event Action<bool> OnPauseToggled;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            InitializeLevel();
        }

        public void InitializeLevel()
        {
            isGameOver = false;
            Time.timeScale = 1f;

            LoadSelectedLevelData();

            if (currentLevel != null)
            {
                currentLives = currentLevel.startingLives;
                currentEnergy = currentLevel.startingEnergy;
            }
            else
            {
                currentLives = 5;
                currentEnergy = 100;
            }

            // Daily challenge energy override
            if (IsDailyChallenge && ActiveDailyChallenge != null)
            {
                currentEnergy = ActiveDailyChallenge.startingEnergy;
            }

            aliveEnemiesCount = 0;
            OnLivesChanged?.Invoke(currentLives);
            OnEnergyChanged?.Invoke(currentEnergy);

            var spawner = FindFirstObjectByType<WaveSpawner>();
            if (spawner != null && currentLevel != null)
            {
                spawner.InitWaves(currentLevel.waves);
            }
        }

        private void LoadSelectedLevelData()
        {
            // Load levels from assets or create procedural presets
            if (allLevels == null || allLevels.Count < 3)
            {
                allLevels = new List<LevelData>
                {
                    LoadLevelAsset("Assets/Data/Levels/Level1.asset", 1),
                    LoadLevelAsset("Assets/Data/Levels/Level2.asset", 2),
                    LoadLevelAsset("Assets/Data/Levels/Level3.asset", 3)
                };
            }

            int idx = Mathf.Clamp(SelectedLevelIndex - 1, 0, allLevels.Count - 1);
            currentLevel = allLevels[idx];

            if (IsDailyChallenge && ActiveDailyChallenge != null)
            {
                // Create custom daily challenge level
                var daily = ScriptableObject.CreateInstance<LevelData>();
                daily.levelIndex = 99;
                daily.levelTitle = $"Daily Challenge: {ActiveDailyChallenge.date}";
                daily.objectiveDescription = $"Survive the {ActiveDailyChallenge.wavePreset} assault!";
                daily.startingEnergy = ActiveDailyChallenge.startingEnergy;
                daily.startingLives = 5;

                var wave = ScriptableObject.CreateInstance<WaveData>();
                wave.waveNumber = 1;

                var crawler = LoadEnemyAsset("Assets/Data/Enemies/CrawlerData.asset", EnemyType.Normal);
                var runner = LoadEnemyAsset("Assets/Data/Enemies/RunnerData.asset", EnemyType.Fast);
                var brute = LoadEnemyAsset("Assets/Data/Enemies/BruteData.asset", EnemyType.Tank);

                wave.segments.Add(new WaveSegment { enemyData = runner, count = 5, spawnInterval = 1.2f });
                wave.segments.Add(new WaveSegment { enemyData = crawler, count = 4, spawnInterval = 1.5f });
                wave.segments.Add(new WaveSegment { enemyData = brute, count = 1, spawnInterval = 2.5f });

                daily.waves.Add(wave);
                currentLevel = daily;
            }
        }

        private LevelData LoadLevelAsset(string path, int fallbackIndex)
        {
#if UNITY_EDITOR
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelData>(path);
            if (asset != null) return asset;
#endif
            var fallback = ScriptableObject.CreateInstance<LevelData>();
            fallback.levelIndex = fallbackIndex;
            fallback.startingEnergy = 100;
            fallback.startingLives = 5;

            var wave = ScriptableObject.CreateInstance<WaveData>();
            wave.waveNumber = 1;

            var crawler = LoadEnemyAsset("Assets/Data/Enemies/CrawlerData.asset", EnemyType.Normal);
            var runner = LoadEnemyAsset("Assets/Data/Enemies/RunnerData.asset", EnemyType.Fast);

            if (fallbackIndex == 1)
            {
                fallback.levelTitle = "Level 1: The Outskirts";
                fallback.objectiveDescription = "Learn placement and defend against Crawlers.";
                wave.segments.Add(new WaveSegment { enemyData = crawler, count = 5, spawnInterval = 2.0f });
            }
            else if (fallbackIndex == 2)
            {
                fallback.levelTitle = "Level 2: Fast Surge";
                fallback.objectiveDescription = "Fast Runners rush the core. Use multiple turrets!";
                wave.segments.Add(new WaveSegment { enemyData = crawler, count = 4, spawnInterval = 1.8f });
                wave.segments.Add(new WaveSegment { enemyData = runner, count = 3, spawnInterval = 1.3f });
            }
            else
            {
                fallback.levelTitle = "Level 3: The Labyrinth";
                fallback.objectiveDescription = "Shape a maze with walls and place Frost Spires.";
                var brute = LoadEnemyAsset("Assets/Data/Enemies/BruteData.asset", EnemyType.Tank);
                wave.segments.Add(new WaveSegment { enemyData = crawler, count = 5, spawnInterval = 1.5f });
                wave.segments.Add(new WaveSegment { enemyData = runner, count = 4, spawnInterval = 1.2f });
                wave.segments.Add(new WaveSegment { enemyData = brute, count = 2, spawnInterval = 3.0f });
            }

            fallback.waves.Add(wave);
            return fallback;
        }

        private EnemyData LoadEnemyAsset(string path, EnemyType fallbackType)
        {
#if UNITY_EDITOR
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            if (asset != null) return asset;
#endif
            var ed = ScriptableObject.CreateInstance<EnemyData>();
            ed.enemyType = fallbackType;
            if (fallbackType == EnemyType.Fast)
            {
                ed.enemyName = "Runner";
                ed.maxHealth = 2;
                ed.moveSpeed = 2.5f;
                ed.energyReward = 10;
                ed.enemyColor = new Color(0.95f, 0.6f, 0.1f);
            }
            else if (fallbackType == EnemyType.Tank)
            {
                ed.enemyName = "Brute";
                ed.maxHealth = 6;
                ed.moveSpeed = 0.8f;
                ed.energyReward = 15;
                ed.enemyColor = new Color(0.7f, 0.15f, 0.15f);
            }
            else
            {
                ed.enemyName = "Crawler";
                ed.maxHealth = 3;
                ed.moveSpeed = 1.5f;
                ed.energyReward = 10;
                ed.enemyColor = new Color(0.9f, 0.3f, 0.2f);
            }
            return ed;
        }

        public void AddEnergy(int amount)
        {
            if (isGameOver) return;
            currentEnergy += amount;
            OnEnergyChanged?.Invoke(currentEnergy);
        }

        public bool SpendEnergy(int amount)
        {
            if (currentEnergy < amount) return false;
            currentEnergy -= amount;
            OnEnergyChanged?.Invoke(currentEnergy);
            return true;
        }

        public void LoseLife(int amount)
        {
            if (isGameOver) return;
            currentLives = Mathf.Max(0, currentLives - amount);
            OnLivesChanged?.Invoke(currentLives);

            if (currentLives <= 0)
            {
                TriggerGameOver(false);
            }
        }

        public void OnEnemySpawned(EnemyController enemy)
        {
            aliveEnemiesCount++;
        }

        public void OnEnemyDefeated(EnemyController enemy)
        {
            BoardController.ActiveEnemies.Remove(enemy);
            aliveEnemiesCount = Mathf.Max(0, aliveEnemiesCount - 1);
            CheckVictoryCondition();
        }

        public void OnEnemyReachedGoal(EnemyController enemy)
        {
            BoardController.ActiveEnemies.Remove(enemy);
            aliveEnemiesCount = Mathf.Max(0, aliveEnemiesCount - 1);
            CheckVictoryCondition();
        }

        private void CheckVictoryCondition()
        {
            if (isGameOver) return;

            var spawner = FindFirstObjectByType<WaveSpawner>();
            bool allWavesDone = spawner != null && spawner.AllWavesFinished;

            if (allWavesDone && aliveEnemiesCount == 0 && currentLives > 0)
            {
                TriggerGameOver(true);
            }
        }

        private void TriggerGameOver(bool won)
        {
            if (isGameOver) return;
            isGameOver = true;

            if (won)
            {
                Debug.Log($"LEVEL {SelectedLevelIndex} COMPLETED! Player Won!");
                var save = SaveService.Load();
                if (SelectedLevelIndex >= save.highestUnlockedLevel && SelectedLevelIndex < 3)
                {
                    save.highestUnlockedLevel = SelectedLevelIndex + 1;
                    SaveService.Save(save);
                    Debug.Log($"Unlocked Level {save.highestUnlockedLevel}!");
                }
            }
            else
            {
                Debug.Log("GAME OVER! Player Lost!");
            }

            OnGameFinished?.Invoke(won);
        }

        public void TogglePause()
        {
            isPaused = !isPaused;
            Time.timeScale = isPaused ? 0f : 1f;
            OnPauseToggled?.Invoke(isPaused);
        }

        public void RestartLevel()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Game");
        }

        public void LoadMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Menu");
        }
    }
}

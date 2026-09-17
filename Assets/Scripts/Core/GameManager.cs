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

        [Header("Level Configuration")]
        [SerializeField] private LevelData currentLevel;
        [SerializeField] private List<LevelData> allLevels;

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
        public event Action<bool> OnGameFinished; // true for Win, false for Loss
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

            aliveEnemiesCount = 0;
            OnLivesChanged?.Invoke(currentLives);
            OnEnergyChanged?.Invoke(currentEnergy);

            var spawner = FindFirstObjectByType<WaveSpawner>();
            if (spawner != null && currentLevel != null)
            {
                spawner.InitWaves(currentLevel.waves);
            }
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

            // Win only when all waves have completed AND no live enemies remain
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
                Debug.Log("LEVEL COMPLETED! Player Won!");
                // Save progress
                var save = SaveService.Load();
                if (currentLevel != null && currentLevel.levelIndex >= save.highestUnlockedLevel)
                {
                    save.highestUnlockedLevel = currentLevel.levelIndex + 1;
                    SaveService.Save(save);
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
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void LoadMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Menu");
        }
    }
}

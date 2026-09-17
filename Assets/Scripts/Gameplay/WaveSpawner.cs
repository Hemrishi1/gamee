using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GardenGuardians.Data;
using GardenGuardians.Core;

namespace GardenGuardians.Gameplay
{
    public class WaveSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private EnemyData fallbackEnemyData;

        private List<WaveData> currentLevelWaves = new List<WaveData>();
        private int currentWaveIndex = 0;
        private bool isSpawning = false;
        private bool allWavesFinished = false;

        public bool AllWavesFinished => allWavesFinished;
        public int CurrentWaveIndex => currentWaveIndex;
        public int TotalWaves => currentLevelWaves != null ? currentLevelWaves.Count : 0;

        public void InitWaves(List<WaveData> waves)
        {
            currentLevelWaves = waves ?? new List<WaveData>();
            currentWaveIndex = 0;
            allWavesFinished = false;
        }

        public void StartNextWave()
        {
            if (isSpawning || allWavesFinished) return;

            if (currentWaveIndex < currentLevelWaves.Count)
            {
                StartCoroutine(SpawnWaveRoutine(currentLevelWaves[currentWaveIndex]));
            }
            else
            {
                allWavesFinished = true;
            }
        }

        private IEnumerator SpawnWaveRoutine(WaveData wave)
        {
            isSpawning = true;
            Debug.Log($"Starting Wave {currentWaveIndex + 1}/{currentLevelWaves.Count}");

            foreach (var segment in wave.segments)
            {
                EnemyData enemyType = segment.enemyData != null ? segment.enemyData : fallbackEnemyData;
                for (int i = 0; i < segment.count; i++)
                {
                    SpawnEnemy(enemyType);
                    yield return new WaitForSeconds(segment.spawnInterval);
                }
            }

            currentWaveIndex++;
            isSpawning = false;

            if (currentWaveIndex >= currentLevelWaves.Count)
            {
                allWavesFinished = true;
                Debug.Log("All waves finished spawning. Awaiting clearance of remaining enemies.");
            }
        }

        private void SpawnEnemy(EnemyData enemyData)
        {
            if (BoardController.Instance == null || enemyPrefab == null) return;

            List<Vector3> waypoints = BoardController.Instance.GetCurrentPathFromStart();
            if (waypoints.Count == 0)
            {
                Debug.LogWarning("Cannot spawn enemy: no valid path from start to goal!");
                return;
            }

            Vector3 spawnPos = waypoints[0];
            GameObject obj = Instantiate(enemyPrefab, spawnPos, Quaternion.identity, transform);
            EnemyController enemy = obj.GetComponent<EnemyController>();
            if (enemy != null)
            {
                enemy.Init(enemyData, waypoints);
                BoardController.ActiveEnemies.Add(enemy);
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.OnEnemySpawned(enemy);
                }
            }
        }
    }
}

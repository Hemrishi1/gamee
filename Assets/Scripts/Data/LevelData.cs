using System;
using System.Collections.Generic;
using UnityEngine;

namespace GardenGuardians.Data
{
    [Serializable]
    public class WaveSegment
    {
        public EnemyData enemyData;
        public int count = 5;
        public float spawnInterval = 2.0f;
    }

    [CreateAssetMenu(fileName = "NewWaveData", menuName = "GardenGuardians/Wave Data")]
    public class WaveData : ScriptableObject
    {
        public int waveNumber = 1;
        public List<WaveSegment> segments = new List<WaveSegment>();
    }

    [CreateAssetMenu(fileName = "NewLevelData", menuName = "GardenGuardians/Level Data")]
    public class LevelData : ScriptableObject
    {
        public int levelIndex = 1;
        public string levelTitle = "Level 1: The Outskirts";
        [TextArea]
        public string objectiveDescription = "Learn placement and stop enemy waves.";
        public int startingEnergy = 100;
        public int startingLives = 5;
        public List<WaveData> waves = new List<WaveData>();
    }
}

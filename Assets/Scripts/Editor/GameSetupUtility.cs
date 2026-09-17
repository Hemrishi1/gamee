#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using GardenGuardians.Data;
using GardenGuardians.Gameplay;

namespace GardenGuardians.Editor
{
    public static class GameSetupUtility
    {
        [MenuItem("GardenGuardians/Setup Default Assets and Prefabs", priority = 1)]
        public static void SetupAssetsAndPrefabs()
        {
            EnsureDirectories();
            var towers = CreateTowerAssets();
            var enemies = CreateEnemyAssets();
            CreateLevelAssets(enemies);
            CreatePrefabs(towers, enemies);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[GardenGuardians] Default ScriptableObject assets and Prefabs generated successfully!");
        }

        private static void EnsureDirectories()
        {
            string[] dirs = {
                "Assets/Data/Towers",
                "Assets/Data/Enemies",
                "Assets/Data/Waves",
                "Assets/Data/Levels",
                "Assets/Prefabs"
            };

            foreach (var d in dirs)
            {
                if (!Directory.Exists(d))
                {
                    Directory.CreateDirectory(d);
                }
            }
        }

        private static Dictionary<string, TowerData> CreateTowerAssets()
        {
            var dict = new Dictionary<string, TowerData>();

            // 1. Turret
            var turret = ScriptableObject.CreateInstance<TowerData>();
            turret.towerType = TowerType.Turret;
            turret.towerName = "Blaster Turret";
            turret.cost = 25;
            turret.range = 2f;
            turret.attackInterval = 1f;
            turret.damage = 1;
            turret.towerColor = new Color(0.2f, 0.6f, 0.9f, 1f);
            AssetDatabase.CreateAsset(turret, "Assets/Data/Towers/TurretData.asset");
            dict["Turret"] = turret;

            // 2. Slow Tower
            var slow = ScriptableObject.CreateInstance<TowerData>();
            slow.towerType = TowerType.SlowTower;
            slow.towerName = "Frost Spire";
            slow.cost = 35;
            slow.range = 2f;
            slow.attackInterval = 1f;
            slow.damage = 1;
            slow.slowDuration = 1.5f;
            slow.slowMultiplier = 0.5f;
            slow.towerColor = new Color(0.6f, 0.3f, 0.9f, 1f);
            AssetDatabase.CreateAsset(slow, "Assets/Data/Towers/SlowTowerData.asset");
            dict["SlowTower"] = slow;

            // 3. Wall
            var wall = ScriptableObject.CreateInstance<TowerData>();
            wall.towerType = TowerType.Wall;
            wall.towerName = "Barrier Wall";
            wall.cost = 15;
            wall.range = 0f;
            wall.attackInterval = 0f;
            wall.damage = 0;
            wall.towerColor = new Color(0.5f, 0.55f, 0.6f, 1f);
            AssetDatabase.CreateAsset(wall, "Assets/Data/Towers/WallData.asset");
            dict["Wall"] = wall;

            return dict;
        }

        private static Dictionary<string, EnemyData> CreateEnemyAssets()
        {
            var dict = new Dictionary<string, EnemyData>();

            // 1. Crawler (Normal)
            var normal = ScriptableObject.CreateInstance<EnemyData>();
            normal.enemyType = EnemyType.Normal;
            normal.enemyName = "Crawler";
            normal.maxHealth = 3;
            normal.moveSpeed = 1.5f;
            normal.energyReward = 10;
            normal.enemyColor = new Color(0.9f, 0.3f, 0.2f, 1f);
            AssetDatabase.CreateAsset(normal, "Assets/Data/Enemies/CrawlerData.asset");
            dict["Crawler"] = normal;

            // 2. Runner (Fast)
            var fast = ScriptableObject.CreateInstance<EnemyData>();
            fast.enemyType = EnemyType.Fast;
            fast.enemyName = "Runner";
            fast.maxHealth = 2;
            fast.moveSpeed = 2.5f;
            fast.energyReward = 10;
            fast.enemyColor = new Color(0.95f, 0.6f, 0.1f, 1f);
            AssetDatabase.CreateAsset(fast, "Assets/Data/Enemies/RunnerData.asset");
            dict["Runner"] = fast;

            // 3. Brute (Tank)
            var tank = ScriptableObject.CreateInstance<EnemyData>();
            tank.enemyType = EnemyType.Tank;
            tank.enemyName = "Brute";
            tank.maxHealth = 6;
            tank.moveSpeed = 0.8f;
            tank.energyReward = 15;
            tank.enemyColor = new Color(0.7f, 0.15f, 0.15f, 1f);
            AssetDatabase.CreateAsset(tank, "Assets/Data/Enemies/BruteData.asset");
            dict["Brute"] = tank;

            return dict;
        }

        private static void CreateLevelAssets(Dictionary<string, EnemyData> enemies)
        {
            // Level 1: Teaches placement (Crawlers only)
            var l1 = ScriptableObject.CreateInstance<LevelData>();
            l1.levelIndex = 1;
            l1.levelTitle = "Level 1: Garden Perimeter";
            l1.objectiveDescription = "Build turrets along the lane to defend the core.";
            l1.startingEnergy = 100;
            l1.startingLives = 5;

            var w1 = ScriptableObject.CreateInstance<WaveData>();
            w1.waveNumber = 1;
            w1.segments.Add(new WaveSegment { enemyData = enemies["Crawler"], count = 5, spawnInterval = 2f });
            AssetDatabase.CreateAsset(w1, "Assets/Data/Waves/L1_Wave1.asset");
            l1.waves.Add(w1);
            AssetDatabase.CreateAsset(l1, "Assets/Data/Levels/Level1.asset");

            // Level 2: Adds fast enemies
            var l2 = ScriptableObject.CreateInstance<LevelData>();
            l2.levelIndex = 2;
            l2.levelTitle = "Level 2: Fast Surge";
            l2.objectiveDescription = "Fast Runners rush the core. Use multiple turrets for overlapping fire.";
            l2.startingEnergy = 100;
            l2.startingLives = 5;

            var w2_1 = ScriptableObject.CreateInstance<WaveData>();
            w2_1.waveNumber = 1;
            w2_1.segments.Add(new WaveSegment { enemyData = enemies["Crawler"], count = 4, spawnInterval = 1.8f });
            w2_1.segments.Add(new WaveSegment { enemyData = enemies["Runner"], count = 3, spawnInterval = 1.5f });
            AssetDatabase.CreateAsset(w2_1, "Assets/Data/Waves/L2_Wave1.asset");
            l2.waves.Add(w2_1);
            AssetDatabase.CreateAsset(l2, "Assets/Data/Levels/Level2.asset");

            // Level 3: Rewards maze shaping & slow towers
            var l3 = ScriptableObject.CreateInstance<LevelData>();
            l3.levelIndex = 3;
            l3.levelTitle = "Level 3: The Labyrinth";
            l3.objectiveDescription = "Place walls to force long detours and combine with Frost Spires.";
            l3.startingEnergy = 120;
            l3.startingLives = 5;

            var w3_1 = ScriptableObject.CreateInstance<WaveData>();
            w3_1.waveNumber = 1;
            w3_1.segments.Add(new WaveSegment { enemyData = enemies["Crawler"], count = 5, spawnInterval = 1.5f });
            w3_1.segments.Add(new WaveSegment { enemyData = enemies["Runner"], count = 4, spawnInterval = 1.2f });
            w3_1.segments.Add(new WaveSegment { enemyData = enemies["Brute"], count = 2, spawnInterval = 3.0f });
            AssetDatabase.CreateAsset(w3_1, "Assets/Data/Waves/L3_Wave1.asset");
            l3.waves.Add(w3_1);
            AssetDatabase.CreateAsset(l3, "Assets/Data/Levels/Level3.asset");
        }

        private static void CreatePrefabs(Dictionary<string, TowerData> towers, Dictionary<string, EnemyData> enemies)
        {
            // Cell Prefab
            GameObject cellObj = new GameObject("Cell");
            var sr = cellObj.AddComponent<SpriteRenderer>();
            var col = cellObj.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.95f, 0.95f);
            cellObj.AddComponent<CellView>();
            Sprite tileSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/cell_tile.png");
            if (tileSprite != null) sr.sprite = tileSprite;
            PrefabUtility.SaveAsPrefabAsset(cellObj, "Assets/Prefabs/Cell.prefab");
            GameObject.DestroyImmediate(cellObj);

            // Turret Prefab
            GameObject turretObj = new GameObject("Turret");
            var tsr = turretObj.AddComponent<SpriteRenderer>();
            var tc = turretObj.AddComponent<TurretController>();
            tc.Init(towers["Turret"]);
            Sprite turretSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/turret.png");
            if (turretSprite != null) tsr.sprite = turretSprite;
            PrefabUtility.SaveAsPrefabAsset(turretObj, "Assets/Prefabs/Turret.prefab");
            GameObject.DestroyImmediate(turretObj);

            // Enemy Prefab
            GameObject enemyObj = new GameObject("Enemy");
            var esr = enemyObj.AddComponent<SpriteRenderer>();
            var ecol = enemyObj.AddComponent<CircleCollider2D>();
            ecol.radius = 0.35f;
            var ec = enemyObj.AddComponent<EnemyController>();
            Sprite crawlerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/enemy_normal.png");
            if (crawlerSprite != null) esr.sprite = crawlerSprite;
            PrefabUtility.SaveAsPrefabAsset(enemyObj, "Assets/Prefabs/Enemy.prefab");
            GameObject.DestroyImmediate(enemyObj);
        }
    }
}
#endif

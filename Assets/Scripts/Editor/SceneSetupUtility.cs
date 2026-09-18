#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using GardenGuardians.Core;
using GardenGuardians.Gameplay;
using GardenGuardians.Data;
using GardenGuardians.UI;
using GardenGuardians.Services;

namespace GardenGuardians.Editor
{
    public static class SceneSetupUtility
    {
        [MenuItem("GardenGuardians/Setup Game and Menu Scenes", priority = 2)]
        public static void SetupBothScenes()
        {
            SetupGameScene();
            SetupMenuScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[GardenGuardians] Game and Menu scenes built and wired with full visual UI!");
        }

        public static void SetupGameScene()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");

            var roots = scene.GetRootGameObjects();
            for (int i = roots.Length - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(roots[i]);
            }

            // 1. Camera with 2D Physics Raycaster
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            var cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 4.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.10f, 0.13f, 1f);
            camObj.transform.position = new Vector3(0, 0, -10f);
            camObj.AddComponent<AudioListener>();
            camObj.AddComponent<Physics2DRaycaster>();

            // 2. GridRoot & BoardController
            GameObject gridRoot = new GameObject("GridRoot");
            var board = gridRoot.AddComponent<BoardController>();

            SerializedObject soBoard = new SerializedObject(board);
            var cellPrefab = AssetDatabase.LoadAssetAtPath<CellView>("Assets/Prefabs/Cell.prefab");
            var turretPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Turret.prefab");

            soBoard.FindProperty("cellPrefab").objectReferenceValue = cellPrefab;
            soBoard.FindProperty("turretPrefab").objectReferenceValue = turretPrefab;
            soBoard.FindProperty("slowTowerPrefab").objectReferenceValue = turretPrefab;
            soBoard.FindProperty("wallPrefab").objectReferenceValue = turretPrefab;
            soBoard.ApplyModifiedProperties();

            // 3. GameManager & WaveSpawner
            GameObject gmObj = new GameObject("GameManager");
            var gm = gmObj.AddComponent<GameManager>();
            var spawner = gmObj.AddComponent<WaveSpawner>();

            SerializedObject soSpawner = new SerializedObject(spawner);
            var enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy.prefab");
            var crawlerData = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/Data/Enemies/CrawlerData.asset");
            soSpawner.FindProperty("enemyPrefab").objectReferenceValue = enemyPrefab;
            soSpawner.FindProperty("fallbackEnemyData").objectReferenceValue = crawlerData;
            soSpawner.ApplyModifiedProperties();

            var level1Data = AssetDatabase.LoadAssetAtPath<LevelData>("Assets/Data/Levels/Level1.asset");
            SerializedObject soGM = new SerializedObject(gm);
            soGM.FindProperty("currentLevel").objectReferenceValue = level1Data;
            soGM.ApplyModifiedProperties();

            // 4. Services
            GameObject serviceObj = new GameObject("DailyChallengeService");
            serviceObj.AddComponent<DailyChallengeService>();

            // 5. Canvas & EventSystem (Unity 6 InputSystemUIInputModule)
            GameObject canvasObj = new GameObject("Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<InputSystemUIInputModule>();

            canvasObj.AddComponent<GameUIController>();

            EditorSceneManager.SaveScene(scene);
            Debug.Log("[GardenGuardians] Game.unity scene setup complete!");
        }

        public static void SetupMenuScene()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Menu.unity");

            var roots = scene.GetRootGameObjects();
            for (int i = roots.Length - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(roots[i]);
            }

            // Camera
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            var cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.10f, 0.13f, 1f);
            camObj.transform.position = new Vector3(0, 0, -10f);
            camObj.AddComponent<AudioListener>();

            // Services
            GameObject serviceObj = new GameObject("DailyChallengeService");
            serviceObj.AddComponent<DailyChallengeService>();

            // Canvas & EventSystem
            GameObject canvasObj = new GameObject("Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<InputSystemUIInputModule>();

            canvasObj.AddComponent<MenuUIController>();

            EditorSceneManager.SaveScene(scene);
            Debug.Log("[GardenGuardians] Menu.unity scene setup complete!");
        }
    }
}
#endif

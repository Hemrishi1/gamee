#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
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

            // 1. Camera
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            var cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 4.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.10f, 0.13f, 1f);
            camObj.transform.position = new Vector3(0, 0, -10f);
            camObj.AddComponent<AudioListener>();

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

            // 5. Canvas & EventSystem
            GameObject canvasObj = new GameObject("Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            var gameUI = canvasObj.AddComponent<GameUIController>();

            // Top HUD Bar
            GameObject topBar = CreateUIPanel(canvasObj, "TopBar", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(0, 80), new Color(0.1f, 0.13f, 0.18f, 0.92f));
            
            Text livesText = CreateUIText(topBar, "LivesText", "Lives: 5", 28, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(40, 0), new Vector2(180, 50), new Color(1f, 0.4f, 0.4f));
            Text energyText = CreateUIText(topBar, "EnergyText", "Energy: 100", 28, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(240, 0), new Vector2(200, 50), new Color(1f, 0.85f, 0.2f));
            Text waveText = CreateUIText(topBar, "WaveText", "Wave: 0 / 1", 28, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(460, 0), new Vector2(200, 50), new Color(0.3f, 0.9f, 1f));
            Text notifText = CreateUIText(topBar, "NotificationText", "", 26, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(40, 0), new Vector2(500, 50), Color.yellow);
            notifText.alignment = TextAnchor.MiddleCenter;

            Button pauseBtn = CreateUIButton(topBar, "PauseButton", "Pause", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-160, 0), new Vector2(110, 48), new Color(0.25f, 0.32f, 0.42f));
            Button muteBtn = CreateUIButton(topBar, "MuteButton", "Mute", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-40, 0), new Vector2(100, 48), new Color(0.25f, 0.32f, 0.42f));

            // Bottom Action Bar
            GameObject bottomBar = CreateUIPanel(canvasObj, "BottomBar", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 60), new Vector2(0, 120), new Color(0.1f, 0.13f, 0.18f, 0.95f));

            Button turretBtn = CreateUIButton(bottomBar, "TurretButton", "Turret (25)", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-360, 0), new Vector2(210, 75), new Color(0.18f, 0.5f, 0.85f));
            Button slowBtn = CreateUIButton(bottomBar, "SlowTowerButton", "Frost Spire (35)", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-120, 0), new Vector2(210, 75), new Color(0.55f, 0.25f, 0.85f));
            Button wallBtn = CreateUIButton(bottomBar, "WallButton", "Wall (15)", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(120, 0), new Vector2(210, 75), new Color(0.42f, 0.48f, 0.55f));
            Button startWaveBtn = CreateUIButton(bottomBar, "StartWaveButton", "Start Wave", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(360, 0), new Vector2(210, 75), new Color(0.18f, 0.75f, 0.38f));

            // Win Panel
            GameObject winPanel = CreateUIPanel(canvasObj, "WinPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500, 320), new Color(0.12f, 0.2f, 0.15f, 0.98f));
            CreateUIText(winPanel, "WinTitle", "VICTORY!", 44, new Vector2(0.5f, 0.8f), new Vector2(0.5f, 0.8f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400, 60), new Color(0.3f, 1f, 0.5f)).alignment = TextAnchor.MiddleCenter;
            Button restartWinBtn = CreateUIButton(winPanel, "RestartButton", "Play Again", new Vector2(0.5f, 0.45f), new Vector2(0.5f, 0.45f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240, 55), new Color(0.2f, 0.6f, 0.3f));
            Button menuWinBtn = CreateUIButton(winPanel, "MenuButton", "Back to Menu", new Vector2(0.5f, 0.2f), new Vector2(0.5f, 0.2f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240, 55), new Color(0.35f, 0.4f, 0.48f));
            winPanel.SetActive(false);

            // Loss Panel
            GameObject lossPanel = CreateUIPanel(canvasObj, "LossPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500, 320), new Color(0.22f, 0.12f, 0.12f, 0.98f));
            CreateUIText(lossPanel, "LossTitle", "DEFEAT!", 44, new Vector2(0.5f, 0.8f), new Vector2(0.5f, 0.8f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400, 60), new Color(1f, 0.3f, 0.3f)).alignment = TextAnchor.MiddleCenter;
            Button restartLossBtn = CreateUIButton(lossPanel, "RestartButton", "Retry", new Vector2(0.5f, 0.45f), new Vector2(0.5f, 0.45f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240, 55), new Color(0.8f, 0.25f, 0.25f));
            Button menuLossBtn = CreateUIButton(lossPanel, "MenuButton", "Back to Menu", new Vector2(0.5f, 0.2f), new Vector2(0.5f, 0.2f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240, 55), new Color(0.35f, 0.4f, 0.48f));
            lossPanel.SetActive(false);

            // Wire all into GameUIController
            SerializedObject soUI = new SerializedObject(gameUI);
            soUI.FindProperty("livesText").objectReferenceValue = livesText;
            soUI.FindProperty("energyText").objectReferenceValue = energyText;
            soUI.FindProperty("waveText").objectReferenceValue = waveText;
            soUI.FindProperty("notificationText").objectReferenceValue = notifText;

            soUI.FindProperty("turretButton").objectReferenceValue = turretBtn;
            soUI.FindProperty("slowTowerButton").objectReferenceValue = slowBtn;
            soUI.FindProperty("wallButton").objectReferenceValue = wallBtn;
            soUI.FindProperty("startWaveButton").objectReferenceValue = startWaveBtn;

            soUI.FindProperty("winPanel").objectReferenceValue = winPanel;
            soUI.FindProperty("lossPanel").objectReferenceValue = lossPanel;
            soUI.FindProperty("restartButton").objectReferenceValue = restartWinBtn;
            soUI.FindProperty("menuButton").objectReferenceValue = menuWinBtn;

            soUI.FindProperty("pauseButton").objectReferenceValue = pauseBtn;
            soUI.FindProperty("muteButton").objectReferenceValue = muteBtn;

            var turretData = AssetDatabase.LoadAssetAtPath<TowerData>("Assets/Data/Towers/TurretData.asset");
            var slowData = AssetDatabase.LoadAssetAtPath<TowerData>("Assets/Data/Towers/SlowTowerData.asset");
            var wallData = AssetDatabase.LoadAssetAtPath<TowerData>("Assets/Data/Towers/WallData.asset");

            soUI.FindProperty("turretData").objectReferenceValue = turretData;
            soUI.FindProperty("slowTowerData").objectReferenceValue = slowData;
            soUI.FindProperty("wallData").objectReferenceValue = wallData;
            soUI.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene);
            Debug.Log("[GardenGuardians] Game.unity visual UI created and wired!");
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

            // Canvas
            GameObject canvasObj = new GameObject("Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            var menuUI = canvasObj.AddComponent<MenuUIController>();

            // Title
            CreateUIText(canvasObj, "Title", "GARDEN GUARDIANS", 64, new Vector2(0.5f, 0.85f), new Vector2(0.5f, 0.85f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800, 100), new Color(0.3f, 0.9f, 0.6f)).alignment = TextAnchor.MiddleCenter;
            CreateUIText(canvasObj, "Subtitle", "2D Mobile Tower-Defense Portfolio Project", 24, new Vector2(0.5f, 0.77f), new Vector2(0.5f, 0.77f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800, 50), Color.white).alignment = TextAnchor.MiddleCenter;

            // Levels Container
            Button l1Btn = CreateUIButton(canvasObj, "Level1Button", "Level 1: The Outskirts", new Vector2(0.5f, 0.60f), new Vector2(0.5f, 0.60f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(380, 60), new Color(0.2f, 0.55f, 0.85f));
            Button l2Btn = CreateUIButton(canvasObj, "Level2Button", "Level 2: Fast Surge", new Vector2(0.5f, 0.50f), new Vector2(0.5f, 0.50f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(380, 60), new Color(0.2f, 0.55f, 0.85f));
            Button l3Btn = CreateUIButton(canvasObj, "Level3Button", "Level 3: The Labyrinth", new Vector2(0.5f, 0.40f), new Vector2(0.5f, 0.40f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(380, 60), new Color(0.2f, 0.55f, 0.85f));

            // Daily Challenge Panel
            GameObject challengePanel = CreateUIPanel(canvasObj, "DailyChallengePanel", new Vector2(0.5f, 0.22f), new Vector2(0.5f, 0.22f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500, 140), new Color(0.14f, 0.18f, 0.24f, 0.95f));
            CreateUIText(challengePanel, "ChallengeHeader", "DAILY CHALLENGE (REST API)", 22, new Vector2(0.5f, 0.8f), new Vector2(0.5f, 0.8f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(460, 30), new Color(1f, 0.85f, 0.3f)).alignment = TextAnchor.MiddleCenter;
            Text dateText = CreateUIText(challengePanel, "DateText", "Loading challenge...", 20, new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(460, 30), Color.white);
            dateText.alignment = TextAnchor.MiddleCenter;
            Text detailsText = CreateUIText(challengePanel, "DetailsText", "", 18, new Vector2(0.5f, 0.26f), new Vector2(0.5f, 0.26f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(460, 30), new Color(0.8f, 0.8f, 0.8f));
            detailsText.alignment = TextAnchor.MiddleCenter;
            Text badgeText = CreateUIText(challengePanel, "BadgeText", "", 16, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-10, -10), new Vector2(150, 25), Color.cyan);

            // Audio Toggle
            Button soundBtn = CreateUIButton(canvasObj, "SoundButton", "Sound: ON", new Vector2(0.5f, 0.08f), new Vector2(0.5f, 0.08f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200, 45), new Color(0.28f, 0.35f, 0.45f));
            Text soundText = soundBtn.GetComponentInChildren<Text>();

            // Wire MenuUI
            SerializedObject soMenu = new SerializedObject(menuUI);
            soMenu.FindProperty("level1Button").objectReferenceValue = l1Btn;
            soMenu.FindProperty("level2Button").objectReferenceValue = l2Btn;
            soMenu.FindProperty("level3Button").objectReferenceValue = l3Btn;
            soMenu.FindProperty("challengeDateText").objectReferenceValue = dateText;
            soMenu.FindProperty("challengeDetailsText").objectReferenceValue = detailsText;
            soMenu.FindProperty("offlineStatusBadge").objectReferenceValue = badgeText;
            soMenu.FindProperty("soundToggleButton").objectReferenceValue = soundBtn;
            soMenu.FindProperty("soundButtonText").objectReferenceValue = soundText;
            soMenu.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene);
            Debug.Log("[GardenGuardians] Menu.unity visual UI created and wired!");
        }

        private static GameObject CreateUIPanel(GameObject parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent.transform, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;

            var img = obj.AddComponent<Image>();
            img.color = color;
            return obj;
        }

        private static Text CreateUIText(GameObject parent, string name, string content, int fontSize, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent.transform, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;

            var txt = obj.AddComponent<Text>();
            txt.text = content;
            txt.fontSize = fontSize;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (txt.font == null)
            {
                txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            txt.color = color;
            txt.alignment = TextAnchor.MiddleLeft;
            return txt;
        }

        private static Button CreateUIButton(GameObject parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta, Color btnColor)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent.transform, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;

            var img = obj.AddComponent<Image>();
            img.color = btnColor;

            var btn = obj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.highlightedColor = btnColor * 1.2f;
            cb.pressedColor = btnColor * 0.8f;
            btn.colors = cb;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(obj.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            var txt = textObj.AddComponent<Text>();
            txt.text = label;
            txt.fontSize = 22;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (txt.font == null)
            {
                txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;

            return btn;
        }
    }
}
#endif

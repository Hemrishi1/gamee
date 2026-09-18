using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using GardenGuardians.Pathfinding;
using GardenGuardians.Data;
using GardenGuardians.Core;

namespace GardenGuardians.Gameplay
{
    public class BoardController : MonoBehaviour
    {
        public static BoardController Instance { get; private set; }

        public const int Rows = 6;
        public const int Columns = 8;

        public static readonly GridCoord StartCoord = new GridCoord(2, 0); // (0, 2)
        public static readonly GridCoord GoalCoord = new GridCoord(2, 7);  // (7, 2)

        [Header("Prefabs & Configuration")]
        [SerializeField] private CellView cellPrefab;
        [SerializeField] private GameObject turretPrefab;
        [SerializeField] private GameObject slowTowerPrefab;
        [SerializeField] private GameObject wallPrefab;

        private CellView[,] cells = new CellView[Rows, Columns];
        private bool[,] blockedGrid = new bool[Rows, Columns];
        private GridPathfinder pathfinder;

        private CellView selectedCell;
        private TowerData activeBuildTower;
        private GameObject rangeIndicatorObj;

        public static List<EnemyController> ActiveEnemies { get; } = new List<EnemyController>();

        public event Action<string> OnNotificationMessage;
        public event Action<CellView> OnCellSelected;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            pathfinder = new GridPathfinder(Rows, Columns);
            ActiveEnemies.Clear();

            // Ensure Main Camera has Physics2DRaycaster for 2D event system clicks
            var cam = Camera.main;
            if (cam != null && cam.GetComponent<Physics2DRaycaster>() == null)
            {
                cam.gameObject.AddComponent<Physics2DRaycaster>();
            }
        }

        private void Start()
        {
            SetupBackground();
            GenerateBoard();
            CreateRangeIndicator();
        }

        private void SetupBackground()
        {
            // Spawn cosmic space background quad behind grid
            var bgObj = GameObject.Find("CosmicBackground");
            if (bgObj == null)
            {
                bgObj = new GameObject("CosmicBackground");
                bgObj.transform.position = new Vector3(0, 0, 5f);
                var sr = bgObj.AddComponent<SpriteRenderer>();
                sr.sortingOrder = -10;
                var bgSprite = LoadSpriteAsset("Assets/Sprites/bg_cosmic.png");
                if (bgSprite != null)
                {
                    sr.sprite = bgSprite;
                    // Fit 16:9 camera view
                    bgObj.transform.localScale = new Vector3(1.15f, 1.15f, 1f);
                }
            }
        }

        private void CreateRangeIndicator()
        {
            if (rangeIndicatorObj == null)
            {
                rangeIndicatorObj = new GameObject("RangeIndicator");
                rangeIndicatorObj.transform.SetParent(transform, false);
                var sr = rangeIndicatorObj.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 15;
                var rangeSprite = LoadSpriteAsset("Assets/Sprites/range_circle.png");
                if (rangeSprite != null)
                {
                    sr.sprite = rangeSprite;
                }
                rangeIndicatorObj.SetActive(false);
            }
        }

        public void GenerateBoard()
        {
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Columns; c++)
                {
                    if (cells[r, c] != null)
                    {
                        Destroy(cells[r, c].gameObject);
                    }
                    blockedGrid[r, c] = false;
                }
            }

            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Columns; col++)
                {
                    Vector3 pos = new Vector3(col - 3.5f, 2.5f - row, 0);

                    CellView cell = null;
                    if (cellPrefab != null)
                    {
                        cell = Instantiate(cellPrefab, pos, Quaternion.identity, transform);
                    }
                    else
                    {
                        // Direct procedural creation if prefab missing
                        GameObject go = new GameObject($"Cell_{row}_{col}");
                        go.transform.position = pos;
                        go.transform.SetParent(transform, false);
                        var sr = go.AddComponent<SpriteRenderer>();
                        sr.sortingOrder = 1;
                        var col2d = go.AddComponent<BoxCollider2D>();
                        col2d.size = new Vector2(0.95f, 0.95f);
                        cell = go.AddComponent<CellView>();
                    }

                    cell.name = $"Cell_{row}_{col}";
                    cell.Init(row, col, this);
                    cells[row, col] = cell;
                }
            }
        }

        private void Update()
        {
            // Direct Pointer Fallback (supports Mouse & Mobile Touchscreen in Unity 6)
            CheckDirectPointerInput();
        }

        private void CheckDirectPointerInput()
        {
            bool hasPointerDown = false;
            Vector2 screenPosition = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
            {
                hasPointerDown = true;
                screenPosition = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            }
            else if (UnityEngine.InputSystem.Touchscreen.current != null && UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                hasPointerDown = true;
                screenPosition = UnityEngine.InputSystem.Touchscreen.current.primaryTouch.position.ReadValue();
            }
#endif
            if (!hasPointerDown && Input.GetMouseButtonDown(0))
            {
                hasPointerDown = true;
                screenPosition = Input.mousePosition;
            }

            if (hasPointerDown)
            {
                // Don't trigger board clicks if clicking on UI buttons
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }

                var cam = Camera.main;
                if (cam != null)
                {
                    Vector3 worldPos = cam.ScreenToWorldPoint(screenPosition);
                    RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
                    if (hit.collider != null)
                    {
                        CellView cell = hit.collider.GetComponent<CellView>();
                        if (cell != null)
                        {
                            TrySelectCell(cell.Row, cell.Column);
                        }
                    }
                }
            }
        }

        public void SetActiveBuildType(TowerData towerData)
        {
            activeBuildTower = towerData;
            if (activeBuildTower != null)
            {
                Notify($"Selected {activeBuildTower.towerName} ({activeBuildTower.cost} Energy). Tap a tile to place.");
                if (rangeIndicatorObj != null && activeBuildTower.range > 0f)
                {
                    rangeIndicatorObj.SetActive(true);
                    float diameter = activeBuildTower.range * 2f;
                    rangeIndicatorObj.transform.localScale = new Vector3(diameter, diameter, 1f);
                }
            }
            else
            {
                if (rangeIndicatorObj != null) rangeIndicatorObj.SetActive(false);
            }
        }

        public void ClearActiveBuildType()
        {
            activeBuildTower = null;
            if (selectedCell != null)
            {
                selectedCell.SetHighlight(false);
                selectedCell = null;
            }
            if (rangeIndicatorObj != null)
            {
                rangeIndicatorObj.SetActive(false);
            }
        }

        public void TrySelectCell(int row, int col)
        {
            if (!pathfinder.IsInside(row, col))
                return;

            CellView cell = cells[row, col];

            if (activeBuildTower == null)
            {
                if (selectedCell != null)
                    selectedCell.SetHighlight(false);

                selectedCell = cell;
                selectedCell.SetHighlight(true);
                OnCellSelected?.Invoke(selectedCell);

                if (rangeIndicatorObj != null)
                {
                    rangeIndicatorObj.transform.position = cell.transform.position;
                    rangeIndicatorObj.SetActive(true);
                    rangeIndicatorObj.transform.localScale = new Vector3(4f, 4f, 1f);
                }
                return;
            }

            ExecutePlacement(cell);
        }

        private void ExecutePlacement(CellView cell)
        {
            int row = cell.Row;
            int col = cell.Column;

            if (cell.IsStart || cell.IsGoal)
            {
                Notify("Cannot build on Start or Goal!");
                return;
            }

            if (cell.Occupied)
            {
                Notify("This tile is already occupied!");
                return;
            }

            if (GameManager.Instance != null && GameManager.Instance.CurrentEnergy < activeBuildTower.cost)
            {
                Notify("Not enough energy!");
                return;
            }

            if (activeBuildTower.towerType == TowerType.Wall)
            {
                blockedGrid[row, col] = true;
                bool routeExists = pathfinder.HasValidPath(blockedGrid, StartCoord, GoalCoord);

                if (routeExists)
                {
                    for (int i = 0; i < ActiveEnemies.Count; i++)
                    {
                        var enemy = ActiveEnemies[i];
                        if (enemy == null || !enemy.IsAlive) continue;
                        GridCoord enemyCoord = WorldToGridCoord(enemy.CurrentPosition);
                        if (!pathfinder.HasValidPath(blockedGrid, enemyCoord, GoalCoord))
                        {
                            routeExists = false;
                            break;
                        }
                    }
                }

                if (!routeExists)
                {
                    blockedGrid[row, col] = false;
                    Notify("Keep a route open!");
                    return;
                }
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SpendEnergy(activeBuildTower.cost);
            }

            cell.Occupied = true;
            if (activeBuildTower.towerType == TowerType.Wall)
            {
                blockedGrid[row, col] = true;
            }

            // Spawn tower / wall with high-res sprite
            Vector3 spawnPos = cell.transform.position;
            GameObject towerObj = new GameObject(activeBuildTower.towerName);
            towerObj.transform.position = spawnPos;
            towerObj.transform.SetParent(transform, true);

            var sr = towerObj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 5;

            string spritePath = activeBuildTower.towerType switch
            {
                TowerType.Wall => "Assets/Sprites/wall_barrier.png",
                TowerType.SlowTower => "Assets/Sprites/turret_frost.png",
                _ => "Assets/Sprites/turret_plasma.png"
            };
            sr.sprite = LoadSpriteAsset(spritePath);

            var turret = towerObj.AddComponent<TurretController>();
            turret.Init(activeBuildTower);

            Notify($"Placed {activeBuildTower.towerName}!");

            if (activeBuildTower.towerType == TowerType.Wall)
            {
                RerouteActiveEnemies();
            }
        }

        public void RerouteActiveEnemies()
        {
            for (int i = 0; i < ActiveEnemies.Count; i++)
            {
                var enemy = ActiveEnemies[i];
                if (enemy == null || !enemy.IsAlive) continue;

                GridCoord currentCoord = WorldToGridCoord(enemy.CurrentPosition);
                List<GridCoord> newPathCoords = pathfinder.FindPath(blockedGrid, currentCoord, GoalCoord);
                if (newPathCoords != null && newPathCoords.Count > 0)
                {
                    List<Vector3> worldWaypoints = CoordsToWorldWaypoints(newPathCoords);
                    enemy.SetWaypoints(worldWaypoints);
                }
            }
        }

        public List<Vector3> GetCurrentPathFromStart()
        {
            List<GridCoord> coords = pathfinder.FindPath(blockedGrid, StartCoord, GoalCoord);
            if (coords == null) return new List<Vector3>();
            return CoordsToWorldWaypoints(coords);
        }

        public List<Vector3> CoordsToWorldWaypoints(List<GridCoord> coords)
        {
            List<Vector3> worldPoints = new List<Vector3>(coords.Count);
            for (int i = 0; i < coords.Count; i++)
            {
                worldPoints.Add(GridCoordToWorld(coords[i]));
            }
            return worldPoints;
        }

        public Vector3 GridCoordToWorld(GridCoord coord)
        {
            return new Vector3(coord.Col - 3.5f, 2.5f - coord.Row, 0);
        }

        public GridCoord WorldToGridCoord(Vector3 worldPos)
        {
            int col = Mathf.RoundToInt(worldPos.x + 3.5f);
            int row = Mathf.RoundToInt(2.5f - worldPos.y);
            col = Mathf.Clamp(col, 0, Columns - 1);
            row = Mathf.Clamp(row, 0, Rows - 1);
            return new GridCoord(row, col);
        }

        private Sprite LoadSpriteAsset(string path)
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
#else
            return null;
#endif
        }

        private void Notify(string msg)
        {
            Debug.Log($"[BoardController] {msg}");
            OnNotificationMessage?.Invoke(msg);
        }
    }
}

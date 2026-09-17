using System;
using System.Collections.Generic;
using UnityEngine;
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

        public static readonly GridCoord StartCoord = new GridCoord(2, 0); // (0, 2) in (col, row)
        public static readonly GridCoord GoalCoord = new GridCoord(2, 7);  // (7, 2) in (col, row)

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

        // Static active enemies list for high-performance zero-allocation searches
        public static List<EnemyController> ActiveEnemies { get; } = new List<EnemyController>();

        public event Action<string> OnNotificationMessage;
        public event Action<CellView> OnCellSelected;

        private void Awake()
        {
            if (Instance != null && Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            pathfinder = new GridPathfinder(Rows, Columns);
            ActiveEnemies.Clear();
        }

        private void Start()
        {
            GenerateBoard();
        }

        public void GenerateBoard()
        {
            // Clear any previous cells if re-generating
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

            // Exactly 48 cells matching Section 2.2 formula
            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Columns; col++)
                {
                    Vector3 pos = new Vector3(col - 3.5f, 2.5f - row, 0);
                    CellView cell = Instantiate(cellPrefab, pos, Quaternion.identity, transform);
                    cell.name = $"Cell_{row}_{col}";
                    cell.Init(row, col, this);
                    cells[row, col] = cell;
                }
            }
        }

        public void SetActiveBuildType(TowerData towerData)
        {
            activeBuildTower = towerData;
            if (activeBuildTower != null)
            {
                Notify($"Selected {activeBuildTower.towerName} ({activeBuildTower.cost} Energy). Tap a tile to place.");
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
        }

        public void TrySelectCell(int row, int col)
        {
            if (!pathfinder.IsInside(row, col))
                return;

            CellView cell = cells[row, col];

            // Tapping with no active build tool selected: highlight for inspection
            if (activeBuildTower == null)
            {
                if (selectedCell != null)
                    selectedCell.SetHighlight(false);

                selectedCell = cell;
                selectedCell.SetHighlight(true);
                OnCellSelected?.Invoke(selectedCell);
                Debug.Log($"Selected cell: Row {row}, Col {col} (Occupied: {cell.Occupied})");
                return;
            }

            // Attempting placement
            ExecutePlacement(cell);
        }

        private void ExecutePlacement(CellView cell)
        {
            int row = cell.Row;
            int col = cell.Column;

            // Reject start and goal cells
            if (cell.IsStart || cell.IsGoal)
            {
                Notify("Cannot build on Start or Goal!");
                return;
            }

            // Reject occupied cells
            if (cell.Occupied)
            {
                Notify("This tile is already occupied!");
                return;
            }

            // Check player energy
            if (GameManager.Instance != null && GameManager.Instance.CurrentEnergy < activeBuildTower.cost)
            {
                Notify("Not enough energy!");
                return;
            }

            // If placing a Wall, perform BFS validation (Section 5.3)
            if (activeBuildTower.towerType == TowerType.Wall)
            {
                blockedGrid[row, col] = true;
                bool routeExists = pathfinder.HasValidPath(blockedGrid, StartCoord, GoalCoord);

                // Also ensure active enemies can still reach goal from their current positions
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
                    // Restore grid and reject
                    blockedGrid[row, col] = false;
                    Notify("Keep a route open!");
                    return;
                }
            }

            // Placement approved: deduct energy and mark occupied
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SpendEnergy(activeBuildTower.cost);
            }

            cell.Occupied = true;
            if (activeBuildTower.towerType == TowerType.Wall)
            {
                blockedGrid[row, col] = true;
            }

            // Instantiate corresponding prefab
            GameObject prefabToSpawn = activeBuildTower.towerType switch
            {
                TowerType.Wall => wallPrefab ?? turretPrefab,
                TowerType.SlowTower => slowTowerPrefab ?? turretPrefab,
                _ => turretPrefab
            };

            Vector3 spawnPos = cell.transform.position;
            GameObject towerObj = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity, transform);
            var turret = towerObj.GetComponent<TurretController>();
            if (turret != null)
            {
                turret.Init(activeBuildTower);
            }

            Notify($"Placed {activeBuildTower.towerName}!");

            // Recalculate routes for all active enemies if a wall was placed
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

        private void Notify(string msg)
        {
            Debug.Log($"[BoardController] {msg}");
            OnNotificationMessage?.Invoke(msg);
        }
    }
}

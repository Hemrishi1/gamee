using System;
using System.Collections.Generic;

namespace GardenGuardians.Pathfinding
{
    [Serializable]
    public struct GridCoord : IEquatable<GridCoord>
    {
        public int Row;
        public int Col;

        public GridCoord(int row, int col)
        {
            Row = row;
            Col = col;
        }

        public bool Equals(GridCoord other) => Row == other.Row && Col == other.Col;
        public override bool Equals(object obj) => obj is GridCoord other && Equals(other);
        public override int GetHashCode() => (Row * 397) ^ Col;
        public override string ToString() => $"({Col},{Row})";

        public static bool operator ==(GridCoord a, GridCoord b) => a.Equals(b);
        public static bool operator !=(GridCoord a, GridCoord b) => !a.Equals(b);
    }

    /// <summary>
    /// Pure C# Breadth-First Search (BFS) pathfinder for 6-row by 8-col grid.
    /// Fully decoupled from UnityEngine objects for robust unit testing and fast execution.
    /// </summary>
    public class GridPathfinder
    {
        public const int DefaultRows = 6;
        public const int DefaultCols = 8;

        public static readonly GridCoord DefaultStart = new GridCoord(2, 0); // (0, 2) in (x, y)
        public static readonly GridCoord DefaultGoal = new GridCoord(2, 7);  // (7, 2) in (x, y)

        private readonly int rows;
        private readonly int cols;

        // Up, Down, Left, Right
        private static readonly int[] DeltaRow = { -1, 1, 0, 0 };
        private static readonly int[] DeltaCol = { 0, 0, -1, 1 };

        public GridPathfinder(int rows = DefaultRows, int cols = DefaultCols)
        {
            this.rows = rows;
            this.cols = cols;
        }

        public bool IsInside(GridCoord coord)
        {
            return coord.Row >= 0 && coord.Row < rows && coord.Col >= 0 && coord.Col < cols;
        }

        public bool IsInside(int row, int col)
        {
            return row >= 0 && row < rows && col >= 0 && col < cols;
        }

        public List<GridCoord> FindPath(bool[,] blocked, GridCoord start, GridCoord goal)
        {
            if (!IsInside(start) || !IsInside(goal))
                return null;

            if (blocked != null && (blocked[start.Row, start.Col] || blocked[goal.Row, goal.Col]))
                return null;

            if (start == goal)
                return new List<GridCoord> { start };

            bool[,] visited = new bool[rows, cols];
            GridCoord?[,] parent = new GridCoord?[rows, cols];
            Queue<GridCoord> queue = new Queue<GridCoord>();

            queue.Enqueue(start);
            visited[start.Row, start.Col] = true;

            bool found = false;

            while (queue.Count > 0)
            {
                GridCoord current = queue.Dequeue();

                if (current == goal)
                {
                    found = true;
                    break;
                }

                for (int i = 0; i < 4; i++)
                {
                    int nRow = current.Row + DeltaRow[i];
                    int nCol = current.Col + DeltaCol[i];

                    if (!IsInside(nRow, nCol))
                        continue;

                    if (visited[nRow, nCol])
                        continue;

                    if (blocked != null && blocked[nRow, nCol])
                        continue;

                    visited[nRow, nCol] = true;
                    parent[nRow, nCol] = current;
                    queue.Enqueue(new GridCoord(nRow, nCol));
                }
            }

            if (!found)
                return null;

            // Reconstruct path from goal back to start
            List<GridCoord> path = new List<GridCoord>();
            GridCoord curr = goal;

            while (curr != start)
            {
                path.Add(curr);
                GridCoord? prev = parent[curr.Row, curr.Col];
                if (!prev.HasValue)
                    return null;
                curr = prev.Value;
            }

            path.Add(start);
            path.Reverse();
            return path;
        }

        public bool HasValidPath(bool[,] blocked, GridCoord start, GridCoord goal)
        {
            return FindPath(blocked, start, goal) != null;
        }
    }
}

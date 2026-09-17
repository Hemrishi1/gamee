using NUnit.Framework;
using System.Collections.Generic;
using GardenGuardians.Pathfinding;

namespace GardenGuardians.Tests.Editor
{
    public class PathfindingTests
    {
        private GridPathfinder pathfinder;
        private GridCoord start;
        private GridCoord goal;

        [SetUp]
        public void Setup()
        {
            pathfinder = new GridPathfinder(6, 8);
            start = new GridCoord(2, 0); // Row 2, Col 0 -> (0, 2)
            goal = new GridCoord(2, 7);  // Row 2, Col 7 -> (7, 2)
        }

        [Test]
        public void EmptyBoard_RouteExists()
        {
            bool[,] blocked = new bool[6, 8];
            List<GridCoord> path = pathfinder.FindPath(blocked, start, goal);

            Assert.IsNotNull(path, "Route should exist on an empty board.");
            Assert.AreEqual(start, path[0], "Path should begin at start.");
            Assert.AreEqual(goal, path[path.Count - 1], "Path should end at goal.");
            Assert.AreEqual(8, path.Count, "Direct path on empty board should have 8 steps.");
        }

        [Test]
        public void OneWallOnDirectRoute_FindsDetour()
        {
            bool[,] blocked = new bool[6, 8];
            // Place a wall right in front of the direct route (Row 2, Col 3)
            blocked[2, 3] = true;

            List<GridCoord> path = pathfinder.FindPath(blocked, start, goal);

            Assert.IsNotNull(path, "Pathfinder should find a detour around single wall.");
            Assert.AreEqual(start, path[0]);
            Assert.AreEqual(goal, path[path.Count - 1]);
            Assert.IsFalse(path.Contains(new GridCoord(2, 3)), "Path must not pass through blocked cell.");
            Assert.Greater(path.Count, 8, "Detour should be longer than direct straight line.");
        }

        [Test]
        public void CompleteBarrier_ReturnsNoRoute()
        {
            bool[,] blocked = new bool[6, 8];
            // Block all rows at column 4 to form an impassable vertical wall
            for (int r = 0; r < 6; r++)
            {
                blocked[r, 4] = true;
            }

            List<GridCoord> path = pathfinder.FindPath(blocked, start, goal);

            Assert.IsNull(path, "Complete barrier should return no route.");
            Assert.IsFalse(pathfinder.HasValidPath(blocked, start, goal), "HasValidPath should return false.");
        }

        [Test]
        public void StartOrGoalPlacement_AlwaysRejected()
        {
            bool[,] blocked = new bool[6, 8];

            // Case A: Start cell blocked
            blocked[start.Row, start.Col] = true;
            List<GridCoord> pathStartBlocked = pathfinder.FindPath(blocked, start, goal);
            Assert.IsNull(pathStartBlocked, "Blocking the start cell must return null.");

            // Reset and test Case B: Goal cell blocked
            blocked[start.Row, start.Col] = false;
            blocked[goal.Row, goal.Col] = true;
            List<GridCoord> pathGoalBlocked = pathfinder.FindPath(blocked, start, goal);
            Assert.IsNull(pathGoalBlocked, "Blocking the goal cell must return null.");
        }
    }
}

using UnityEngine;
using GardenGuardians.Pathfinding;

namespace GardenGuardians.Gameplay
{
    [RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
    public class CellView : MonoBehaviour
    {
        public int Row { get; private set; }
        public int Column { get; private set; }
        public bool Occupied { get; set; }
        public bool IsStart { get; private set; }
        public bool IsGoal { get; private set; }

        private BoardController board;
        private SpriteRenderer spriteRenderer;
        private Color baseColor = new Color(0.22f, 0.28f, 0.35f, 0.9f); // Sleek grid tile
        private Color highlightColor = new Color(1.0f, 0.9f, 0.3f, 1.0f); // Tapped/selected yellow

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Init(int row, int column, BoardController owner)
        {
            Row = row;
            Column = column;
            board = owner;

            // Start cell is (0,2) in (x,y) -> Row 2, Col 0
            // Goal cell is (7,2) in (x,y) -> Row 2, Col 7
            IsStart = (row == 2 && column == 0);
            IsGoal = (row == 2 && column == 7);

            if (IsStart)
            {
                baseColor = new Color(0.18f, 0.8f, 0.44f, 1f); // Vibrant Green
                Occupied = false; // Cannot build on start
            }
            else if (IsGoal)
            {
                baseColor = new Color(0.95f, 0.45f, 0.15f, 1f); // Vibrant Orange
                Occupied = false; // Cannot build on goal
            }
            else
            {
                // Checkerboard subtle pattern for visual clarity
                bool isAlt = (row + column) % 2 == 1;
                baseColor = isAlt ? new Color(0.20f, 0.25f, 0.32f, 0.95f) : new Color(0.24f, 0.30f, 0.38f, 0.95f);
            }

            ResetColor();
        }

        public void ResetColor()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = baseColor;
            }
        }

        public void SetHighlight(bool highlighted)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = highlighted ? highlightColor : baseColor;
            }
        }

        public void SetOccupiedColor(Color color)
        {
            baseColor = color;
            ResetColor();
        }

        private void OnMouseDown()
        {
            if (board != null)
            {
                board.TrySelectCell(Row, Column);
            }
        }

        public GridCoord Coord => new GridCoord(Row, Column);
    }
}

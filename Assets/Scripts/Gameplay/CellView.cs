using UnityEngine;
using UnityEngine.EventSystems;
using GardenGuardians.Pathfinding;

namespace GardenGuardians.Gameplay
{
    [RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
    public class CellView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public int Row { get; private set; }
        public int Column { get; private set; }
        public bool Occupied { get; set; }
        public bool IsStart { get; private set; }
        public bool IsGoal { get; private set; }

        private BoardController board;
        private SpriteRenderer spriteRenderer;
        private Color baseColor = Color.white;
        private Color highlightColor = new Color(1.0f, 0.95f, 0.3f, 1.0f);
        private bool isHovered = false;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Init(int row, int column, BoardController owner)
        {
            Row = row;
            Column = column;
            board = owner;

            IsStart = (row == 2 && column == 0);
            IsGoal = (row == 2 && column == 7);

            // Assign high-res specialized sprites if available
            if (IsStart)
            {
                var startSprite = Resources.Load<Sprite>("Sprites/portal_start") ?? LoadAssetSprite("Assets/Sprites/portal_start.png");
                if (startSprite != null && spriteRenderer != null)
                {
                    spriteRenderer.sprite = startSprite;
                }
                baseColor = Color.white;
                Occupied = false;
            }
            else if (IsGoal)
            {
                var goalSprite = Resources.Load<Sprite>("Sprites/core_goal") ?? LoadAssetSprite("Assets/Sprites/core_goal.png");
                if (goalSprite != null && spriteRenderer != null)
                {
                    spriteRenderer.sprite = goalSprite;
                }
                baseColor = Color.white;
                Occupied = false;
            }
            else
            {
                var tileSprite = Resources.Load<Sprite>("Sprites/cell_tile_highres") ?? LoadAssetSprite("Assets/Sprites/cell_tile_highres.png");
                if (tileSprite != null && spriteRenderer != null)
                {
                    spriteRenderer.sprite = tileSprite;
                }

                // Subtle alternating checkerboard tint for tactical contrast
                bool isAlt = (row + column) % 2 == 1;
                baseColor = isAlt ? new Color(0.85f, 0.90f, 0.95f, 1f) : Color.white;
            }

            ResetColor();
        }

        private Sprite LoadAssetSprite(string path)
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
#else
            return null;
#endif
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

        // Pointer event implementations (works 100% with Unity New Input System & Mobile)
        public void OnPointerClick(PointerEventData eventData)
        {
            TriggerSelect();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
            if (!Occupied && !IsStart && !IsGoal && spriteRenderer != null)
            {
                spriteRenderer.color = Color.Lerp(baseColor, highlightColor, 0.4f);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            ResetColor();
        }

        // Fallback for direct mouse down
        private void OnMouseDown()
        {
            TriggerSelect();
        }

        private void TriggerSelect()
        {
            if (board != null)
            {
                board.TrySelectCell(Row, Column);
            }
        }

        public GridCoord Coord => new GridCoord(Row, Column);
    }
}

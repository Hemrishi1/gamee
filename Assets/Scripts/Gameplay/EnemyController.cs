using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GardenGuardians.Data;
using GardenGuardians.Core;

namespace GardenGuardians.Gameplay
{
    public class EnemyController : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        private EnemyData data;
        private int currentHealth;
        private int maxHealth = 3;
        private float baseSpeed;
        private float currentSpeed;
        private float slowTimer = 0f;

        private List<Vector3> waypoints = new List<Vector3>();
        private int currentWaypointIndex = 0;
        private bool isDead = false;

        // Visual Health Bar
        private GameObject healthBarObj;
        private Transform healthFillTransform;
        private SpriteRenderer healthFillRenderer;

        public bool IsAlive => !isDead && currentHealth > 0;
        public int CurrentHealth => currentHealth;
        public Vector3 CurrentPosition => transform.position;

        private void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            CreateHealthBar();
        }

        private void CreateHealthBar()
        {
            healthBarObj = new GameObject("HealthBar");
            healthBarObj.transform.SetParent(transform, false);
            healthBarObj.transform.localPosition = new Vector3(0, 0.55f, 0);

            // Dark border/bg
            var bgObj = new GameObject("BarBG");
            bgObj.transform.SetParent(healthBarObj.transform, false);
            var bgSr = bgObj.AddComponent<SpriteRenderer>();
            bgSr.sortingOrder = 20;
            bgSr.sprite = CreateQuadSprite(new Color(0.1f, 0.1f, 0.15f, 0.85f));
            bgObj.transform.localScale = new Vector3(0.8f, 0.12f, 1f);

            // Health Fill
            var fillObj = new GameObject("BarFill");
            fillObj.transform.SetParent(healthBarObj.transform, false);
            healthFillTransform = fillObj.transform;
            healthFillRenderer = fillObj.AddComponent<SpriteRenderer>();
            healthFillRenderer.sortingOrder = 21;
            healthFillRenderer.sprite = CreateQuadSprite(new Color(0.2f, 0.9f, 0.35f, 1f));
            fillObj.transform.localScale = new Vector3(0.76f, 0.08f, 1f);
        }

        private Sprite CreateQuadSprite(Color color)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        }

        public void Init(EnemyData enemyData, List<Vector3> initialWaypoints)
        {
            data = enemyData;
            maxHealth = data != null ? data.maxHealth : 3;
            currentHealth = maxHealth;
            baseSpeed = data != null ? data.moveSpeed : 1.5f;
            currentSpeed = baseSpeed;

            // Load specialized high-res sprite if available
            if (spriteRenderer != null && data != null)
            {
                string path = data.enemyType switch
                {
                    EnemyType.Fast => "Assets/Sprites/enemy_runner.png",
                    EnemyType.Tank => "Assets/Sprites/enemy_brute.png",
                    _ => "Assets/Sprites/enemy_crawler.png"
                };
                var spr = LoadSpriteAsset(path);
                if (spr != null)
                {
                    spriteRenderer.sprite = spr;
                    spriteRenderer.color = Color.white;
                }
                else
                {
                    spriteRenderer.color = data.enemyColor;
                }
            }

            SetWaypoints(initialWaypoints);
            UpdateHealthBarUI();
            isDead = false;
        }

        public void SetWaypoints(List<Vector3> newWaypoints)
        {
            waypoints = newWaypoints ?? new List<Vector3>();
            currentWaypointIndex = 0;
        }

        private void Update()
        {
            if (isDead) return;

            if (slowTimer > 0f)
            {
                slowTimer -= Time.deltaTime;
                if (slowTimer <= 0f)
                {
                    currentSpeed = baseSpeed;
                    if (spriteRenderer != null)
                        spriteRenderer.color = Color.white;
                }
            }

            if (waypoints == null || currentWaypointIndex >= waypoints.Count)
                return;

            Vector3 target = waypoints[currentWaypointIndex];
            transform.position = Vector3.MoveTowards(transform.position, target, currentSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, target) < 0.05f)
            {
                currentWaypointIndex++;
                if (currentWaypointIndex >= waypoints.Count)
                {
                    OnReachGoal();
                }
            }
        }

        public void ApplySlow(float multiplier, float duration)
        {
            if (isDead) return;
            slowTimer = duration;
            currentSpeed = baseSpeed * Mathf.Clamp(multiplier, 0.1f, 1f);
            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(0.4f, 0.75f, 1f, 1f); // Frost glow
            }
        }

        public void TakeDamage(int amount)
        {
            if (isDead) return;

            currentHealth -= amount;
            UpdateHealthBarUI();
            StartCoroutine(FlashDamageRoutine());

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        private void UpdateHealthBarUI()
        {
            if (healthFillTransform != null)
            {
                float pct = Mathf.Clamp01((float)currentHealth / maxHealth);
                healthFillTransform.localScale = new Vector3(0.76f * pct, 0.08f, 1f);

                if (healthFillRenderer != null)
                {
                    // Tint health bar color from Green -> Yellow -> Red
                    healthFillRenderer.color = Color.Lerp(new Color(0.9f, 0.2f, 0.2f), new Color(0.2f, 0.9f, 0.35f), pct);
                }
            }
        }

        private IEnumerator FlashDamageRoutine()
        {
            if (spriteRenderer != null)
            {
                Color prev = spriteRenderer.color;
                spriteRenderer.color = Color.yellow;
                yield return new WaitForSeconds(0.06f);
                if (spriteRenderer != null && !isDead)
                {
                    spriteRenderer.color = prev;
                }
            }
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true;

            int reward = data != null ? data.energyReward : 10;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddEnergy(reward);
                GameManager.Instance.OnEnemyDefeated(this);
            }

            Destroy(gameObject);
        }

        private void OnReachGoal()
        {
            if (isDead) return;
            isDead = true;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoseLife(1);
                GameManager.Instance.OnEnemyReachedGoal(this);
            }

            Destroy(gameObject);
        }

        private Sprite LoadSpriteAsset(string path)
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
#else
            return null;
#endif
        }
    }
}

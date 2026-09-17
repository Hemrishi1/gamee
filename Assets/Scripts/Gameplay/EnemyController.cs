using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GardenGuardians.Data;
using GardenGuardians.Core;
using GardenGuardians.Pathfinding;

namespace GardenGuardians.Gameplay
{
    public class EnemyController : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        private EnemyData data;
        private int currentHealth;
        private float baseSpeed;
        private float currentSpeed;
        private float slowTimer = 0f;

        private List<Vector3> waypoints = new List<Vector3>();
        private int currentWaypointIndex = 0;
        private bool isDead = false;

        public bool IsAlive => !isDead && currentHealth > 0;
        public int CurrentHealth => currentHealth;
        public Vector3 CurrentPosition => transform.position;

        private void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        public void Init(EnemyData enemyData, List<Vector3> initialWaypoints)
        {
            data = enemyData;
            currentHealth = data != null ? data.maxHealth : 3;
            baseSpeed = data != null ? data.moveSpeed : 1.5f;
            currentSpeed = baseSpeed;

            if (spriteRenderer != null && data != null)
            {
                spriteRenderer.color = data.enemyColor;
            }

            SetWaypoints(initialWaypoints);
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

            // Handle slow expiration
            if (slowTimer > 0f)
            {
                slowTimer -= Time.deltaTime;
                if (slowTimer <= 0f)
                {
                    currentSpeed = baseSpeed;
                    if (spriteRenderer != null && data != null)
                        spriteRenderer.color = data.enemyColor;
                }
            }

            // Waypoint movement
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
                // Tint blueish to indicate slowed
                spriteRenderer.color = new Color(0.4f, 0.7f, 1.0f, 1f);
            }
        }

        public void TakeDamage(int amount)
        {
            if (isDead) return;

            currentHealth -= amount;
            StartCoroutine(FlashDamageRoutine());

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        private IEnumerator FlashDamageRoutine()
        {
            if (spriteRenderer != null)
            {
                Color original = spriteRenderer.color;
                spriteRenderer.color = Color.white;
                yield return new WaitForSeconds(0.08f);
                if (spriteRenderer != null && !isDead)
                {
                    spriteRenderer.color = original;
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
    }
}

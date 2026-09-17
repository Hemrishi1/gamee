using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GardenGuardians.Data;

namespace GardenGuardians.Gameplay
{
    public class TurretController : MonoBehaviour
    {
        [SerializeField] private TowerData towerData;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private LineRenderer laserLine; // Visual attack effect

        private float searchTimer = 0f;
        private float attackTimer = 0f;
        private EnemyController currentTarget;

        public TowerData Data => towerData;

        public void Init(TowerData data)
        {
            towerData = data;
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            if (spriteRenderer != null && towerData != null)
            {
                spriteRenderer.color = towerData.towerColor;
            }
        }

        private void Update()
        {
            if (towerData == null || towerData.towerType == TowerType.Wall)
                return;

            // Search for nearest enemy every 0.2s as specified in Section 4.2
            searchTimer += Time.deltaTime;
            if (searchTimer >= 0.2f)
            {
                searchTimer = 0f;
                FindNearestTarget();
            }

            // Attack cooldown
            attackTimer += Time.deltaTime;
            if (attackTimer >= towerData.attackInterval && currentTarget != null)
            {
                if (currentTarget.IsAlive && Vector3.Distance(transform.position, currentTarget.CurrentPosition) <= towerData.range)
                {
                    PerformAttack(currentTarget);
                    attackTimer = 0f;
                }
                else
                {
                    currentTarget = null;
                }
            }
        }

        private void FindNearestTarget()
        {
            var enemies = BoardController.ActiveEnemies;
            EnemyController nearest = null;
            float minDistance = float.MaxValue;
            Vector3 myPos = transform.position;

            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy == null || !enemy.IsAlive) continue;

                float dist = Vector3.Distance(myPos, enemy.CurrentPosition);
                if (dist <= towerData.range && dist < minDistance)
                {
                    minDistance = dist;
                    nearest = enemy;
                }
            }

            currentTarget = nearest;
        }

        private void PerformAttack(EnemyController enemy)
        {
            if (enemy == null || !enemy.IsAlive) return;

            enemy.TakeDamage(towerData.damage);

            if (towerData.slowDuration > 0f)
            {
                enemy.ApplySlow(towerData.slowMultiplier, towerData.slowDuration);
            }

            StartCoroutine(ShowAttackBeam(enemy.CurrentPosition));
        }

        private IEnumerator ShowAttackBeam(Vector3 targetPos)
        {
            if (laserLine != null)
            {
                laserLine.enabled = true;
                laserLine.SetPosition(0, transform.position);
                laserLine.SetPosition(1, targetPos);
                yield return new WaitForSeconds(0.08f);
                laserLine.enabled = false;
            }
            else
            {
                // Quick flash on turret barrel/body
                if (spriteRenderer != null)
                {
                    Color orig = spriteRenderer.color;
                    spriteRenderer.color = Color.white;
                    yield return new WaitForSeconds(0.08f);
                    if (spriteRenderer != null)
                        spriteRenderer.color = orig;
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (towerData != null && towerData.towerType != TowerType.Wall)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, towerData.range);
            }
        }
    }
}

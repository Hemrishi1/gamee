using UnityEngine;

namespace GardenGuardians.Data
{
    public enum EnemyType
    {
        Normal,
        Fast,
        Tank
    }

    [CreateAssetMenu(fileName = "NewEnemyData", menuName = "GardenGuardians/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        public EnemyType enemyType;
        public string enemyName = "Crawler";
        public int maxHealth = 3;
        public float moveSpeed = 1.5f;
        public int energyReward = 10;
        public Color enemyColor = Color.red;
    }
}

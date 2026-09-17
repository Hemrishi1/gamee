using UnityEngine;

namespace GardenGuardians.Data
{
    public enum TowerType
    {
        Turret,
        SlowTower,
        Wall
    }

    [CreateAssetMenu(fileName = "NewTowerData", menuName = "GardenGuardians/Tower Data")]
    public class TowerData : ScriptableObject
    {
        public TowerType towerType;
        public string towerName = "Turret";
        public int cost = 25;
        public float range = 2f;
        public float attackInterval = 1f;
        public int damage = 1;
        public float slowDuration = 0f;
        [Range(0.1f, 1f)]
        public float slowMultiplier = 1f;
        public Color towerColor = Color.cyan;
    }
}

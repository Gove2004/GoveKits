using GoveKits.Runtime.Core.Pool;

namespace GoveKits.Test.Pool.Scene
{
    public class EnemyData : IPoolable
    {
        private static int s_nextRuntimeId = 1;

        public int RuntimeId { get; private set; }
        public int Level { get; private set; }
        public float MaxHp { get; private set; }
        public float CurrentHp { get; private set; }
        public bool IsDead { get; private set; }

        public EnemyData()
        {
            RuntimeId = s_nextRuntimeId++;
        }

        public void Initialize(int level, float maxHp)
        {
            Level = level;
            MaxHp = maxHp;
            CurrentHp = maxHp;
            IsDead = false;
        }

        public void TakeDamage(float damage)
        {
            if (IsDead) return;

            CurrentHp -= damage;
            if (CurrentHp <= 0f)
            {
                CurrentHp = 0f;
                IsDead = true;
            }
        }

        public void OnGetFromPool()
        {
            Level = 0;
            MaxHp = 0f;
            CurrentHp = 0f;
            IsDead = false;
        }

        public void OnReturnToPool()
        {
            Level = 0;
            MaxHp = 0f;
            CurrentHp = 0f;
            IsDead = false;
        }
    }
}
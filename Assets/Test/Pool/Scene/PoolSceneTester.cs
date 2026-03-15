using GoveKits.Runtime.Core.Pool;
using UnityEngine;

namespace GoveKits.Test.Pool.Scene
{
    public class PoolSceneTester : MonoBehaviour
    {
        [Header("Bullet Pool")]
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private int warmupCount = 5;
        [SerializeField] private int maxSize = 32;

        [Header("EnemyData Pool")]
        [SerializeField] private int enemyLevel = 3;
        [SerializeField] private float enemyMaxHp = 100f;
        [SerializeField] private float damagePerHit = 25f;

        private EnemyData _lastEnemyData;

        private void Start()
        {
            PoolCore.Create<EnemyData>(count: 2, maxSize: 16);

            if (bulletPrefab != null)
            {
                PoolCore.Create(bulletPrefab, count: warmupCount, maxSize: maxSize);
            }

            Debug.Log("[PoolSceneTester] Start completed. Press E to test EnemyData, Space to spawn Bullet, C to clear all pools.");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                TestEnemyDataPool();
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                SpawnBullet();
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                PoolCore.ClearAll();
                Debug.Log("[PoolSceneTester] ClearAll called.");
            }

            if (Input.GetKeyDown(KeyCode.D))
            {
                _lastEnemyData.TakeDamage(damagePerHit);
                Debug.Log($"[EnemyData] Took damage, RuntimeId={_lastEnemyData.RuntimeId}, HP={_lastEnemyData.CurrentHp}/{_lastEnemyData.MaxHp}, IsDead={_lastEnemyData.IsDead}");
            }
        }

        private void TestEnemyDataPool()
        {
            if (_lastEnemyData != null)
            {
                PoolCore.Return(_lastEnemyData);
                Debug.Log($"[EnemyData] Returned RuntimeId={_lastEnemyData.RuntimeId}");
            }

            _lastEnemyData = PoolCore.Get<EnemyData>();
            _lastEnemyData.Initialize(enemyLevel, enemyMaxHp);
            _lastEnemyData.TakeDamage(damagePerHit);

            Debug.Log(
                $"[EnemyData] RuntimeId={_lastEnemyData.RuntimeId}, Level={_lastEnemyData.Level}, HP={_lastEnemyData.CurrentHp}/{_lastEnemyData.MaxHp}, IsDead={_lastEnemyData.IsDead}");
        }

        private void SpawnBullet()
        {
            if (bulletPrefab == null)
            {
                Debug.LogWarning("[PoolSceneTester] bulletPrefab is null.");
                return;
            }

            GameObject bulletObject = PoolCore.Get(bulletPrefab);
            Bullet bullet = bulletObject.GetComponent<Bullet>();

            if (bullet == null)
            {
                Debug.LogError("[PoolSceneTester] Spawned object does not have a Bullet component.");
                return;
            }

            Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position;
            Vector3 direction = firePoint != null ? firePoint.forward : transform.forward;
            bullet.Fire(spawnPosition, direction);

            Debug.Log($"[Bullet] Spawned {bulletObject.name} at {spawnPosition}");
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10f, 10f, 520f, 160f), GUI.skin.box);
            GUILayout.Label("Pool Scene Test Controls");
            GUILayout.Label("E: Get/Return one EnemyData instance and print reuse info");
            GUILayout.Label("Space: Spawn one Bullet from GameObjectPool");
            GUILayout.Label("C: Clear all pools");
            GUILayout.Label("Requirement: bulletPrefab must have both Bullet and PoolRecord components");
            GUILayout.EndArea();
        }
    }
}
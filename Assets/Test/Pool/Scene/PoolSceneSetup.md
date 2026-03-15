# Pool Scene Setup

## Files
- EnemyData.cs: pure C# pooled example
- Bullet.cs: GameObject pooled example
- PoolSceneTester.cs: scene entry for manual testing

## Scene Setup
1. Create an empty scene object named PoolTester.
2. Add PoolSceneTester to PoolTester.
3. Create a Bullet prefab.
4. Add Bullet to the prefab.
5. Add PoolRecord to the prefab.
6. Add a visible component such as MeshRenderer or SpriteRenderer to the prefab.
7. Assign the prefab to PoolSceneTester.bulletPrefab.
8. Optional: create a child transform as firePoint and assign it.

## Play Test
1. Enter Play Mode.
2. Press E to test the pure C# EnemyData pool.
3. Press Space to spawn a pooled Bullet.
4. Wait for Bullet lifetime to end and observe it returning to the pool.
5. Press C to clear all pools.

## What To Verify
1. Repeated E presses should log reused EnemyData RuntimeId values after return.
2. Space should spawn bullets from the same prefab without Instantiate usage outside the pool path.
3. Bullet should disable itself by returning to the pool after lifetime ends.
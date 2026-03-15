# Pool 系统功能测试说明

## 位置
- 测试程序集: Assets/Test/Pool/GoveKits.Pool.Tests.asmdef
- 测试代码: Assets/Test/Pool/PoolCoreTests.cs

## 目标
验证 Pool 系统在 C# 对象池和 GameObject 对象池下的核心行为是否正确。

## 覆盖用例
1. CSharpPool 预热
- 用例: CSharpPool_Create_WarmupCreatesExpectedInstances
- 断言: 调用 Create(count: 3) 后，预热对象创建数量为 3。

2. CSharpPool 复用
- 用例: CSharpPool_GetReturn_ReusesSameInstance
- 断言: Get -> Return -> Get 后，返回同一实例，且 OnGetFromPool/OnReturnToPool 调用次数符合预期。

3. GameObjectPool 回调与激活状态
- 用例: GameObjectPool_GetReturn_TogglesActiveAndInvokesCallbacks
- 断言: Get 后对象激活并触发 OnGetFromPool；通过池实例 Return 后对象失活并触发 OnReturnToPool。

4. 非池对象归还容错
- 用例: GameObjectPool_ReturnNonPooledObject_ThrowsArgumentException
- 断言: 对非池对象调用 Return 抛出 ArgumentException。

## 前置约束
- 传入 GameObjectPool 的 prefab 必须带 PoolRecord 组件，否则会抛出 ArgumentException。

## 执行方式
1. 打开 Unity Test Runner。
2. 选择 EditMode。
3. 运行 PoolCoreTests 全部测试。

## 预期结果
- 4 个用例全部通过。
- 每个测试结束后会清理已创建对象，避免测试间相互污染。

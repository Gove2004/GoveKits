// using UnityEngine;


// namespace GoveKits.Buff
// {
//     /// <summary>
//     /// Buff类
//     /// </summary>
//     public class Buff
//     {
//         // 配置
//         public readonly string name;
//         public readonly string description;
//         public readonly string[] tags;  // 标签
//         public readonly int priority = 1; // 优先级，数值越大优先级越高
//         public readonly float duration = -1; // 持续时间，单位秒, <=0表示无限
//         public readonly int maxStack = 1; // 最大叠加层数，<=0表示无限
//         // 运行时
//         public float ElapsedTime { get; private set; } = 0f; // 已经过的时间，单位秒
//         public int CurrentStack { get; private set; } = 1; // 当前叠加层数
//         public object Source { get; set; } // Buff来源，可以是技能、物品等
//         public object Target { get; set; } // Buff目标，可以是角色等
//         public object Context { get; set; } // 额外上下文信息

//         /// <summary>
//         /// 施加Buff时调用
//         /// </summary>
//         public virtual void OnApplyBuff()
//         {

//         }

//         /// <summary>
//         /// 更新Buff时调用
//         /// </summary>
//         /// <param name="deltaTime"></param>
//         public virtual void OnUpdate(float deltaTime)
//         {
//             if (duration > 0)
//             {
//                 ElapsedTime += deltaTime;
//                 if (ElapsedTime >= duration)
//                 {
//                     OnRemoveBuff();
//                 }
//             }
//         }

//         /// <summary>
//         /// Buff失效时调用
//         /// </summary>
//         public virtual void OnRemoveBuff()
//         {
//             // 清理逻辑, 如移除属性加成等， 记得从BuffContainer中移除
//         }

//         /// <summary>
//         /// Buff堆叠时调用
//         /// </summary>
//         public virtual void OnStack(int count = 1)
//         {
//             if (maxStack < 0)
//             {
//                 CurrentStack += count;
//             }
//             else
//             {
//                 CurrentStack = Mathf.Min(CurrentStack + count, maxStack);
//             }
//         }
//     }
// }
// using System.Collections.Generic;


// namespace GoveKits.Buff
// {
//     /// <summary>
//     /// Buff容器类
//     /// </summary>
//     public class BuffContainer
//     {
//         private List<Buff> buffs;

//         public BuffContainer()
//         {
//             buffs = new List<Buff>();
//         }

//         /// <summary>
//         /// 添加Buff
//         /// </summary>
//         public void AddBuff(Buff buff)
//         {
//             if (buff == null) return;

//             // 检查是否已有相同名称的Buff
//             var existingBuff = buffs.Find(b => b.name == buff.name);
//             if (existingBuff != null)
//             {
//                 existingBuff.OnStack();
//             }
//             else
//             {
//                 buffs.Add(buff);
//                 buff.OnApplyBuff();
//             }
//         }

//         /// <summary>
//         /// 移除Buff
//         /// </summary>
//         public void RemoveBuff(Buff buff)
//         {
//             if (buff == null) return;
//             if (buffs.Remove(buff))
//             {
//                 buff.OnRemoveBuff();
//             }
//         }

//         /// <summary>
//         /// 根据名称移除Buff
//         /// </summary>
//         public void RemoveBuffByName(string buffName)
//         {
//             var buff = buffs.Find(b => b.name == buffName);
//             if (buff != null)
//             {
//                 RemoveBuff(buff);
//             }
//         }

//         /// <summary>
//         /// 更新所有Buff状态
//         /// </summary>
//         public void Update(float deltaTime)
//         {
//             for (int i = buffs.Count - 1; i >= 0; i--)
//             {
//                 buffs[i].OnUpdate(deltaTime);
//             }
//         }
//     }
// }
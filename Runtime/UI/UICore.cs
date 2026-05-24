using System;
using System.Collections.Generic;
using UnityEngine;

namespace GoveKits.Runtime.UI
{
    public static class UICore
    {
        #region ViewPanel 管理

        // 已经存在的 ViewPanel 实例注册到这里，以便统一管理和访问
        private static Dictionary<Type, ViewPanel> _viewPanels = new();
        

        public static void Register<TVP>(ViewPanel panel) where TVP : ViewPanel => Register(typeof(TVP), panel);
        public static void Register(Type type, ViewPanel panel)
        {
            if (!_viewPanels.ContainsKey(type))
            {
                _viewPanels[type] = panel;
            }
        }


        public static void UnRegister<TVP>() where TVP : ViewPanel => UnRegister(typeof(TVP));
        public static void UnRegister(Type type)
        {
            if (_viewPanels.ContainsKey(type))
            {
                _viewPanels.Remove(type);
            }
        }


        #endregion

        #region Show/Hide 面板接口

        public static void Show<T>(object param = null) where T : ViewPanel => Show(typeof(T), param);
        public static void Show(Type type, object param = null)
        {
            if (_viewPanels.TryGetValue(type, out var panel))
            {
                panel.OnBindVM();
                panel.OnReceiveShowParam(param);
                panel.OnShow();
            }
        }


        public static void Hide<T>() where T : ViewPanel => Hide(typeof(T));
        public static void Hide(Type type)
        {
            if (_viewPanels.TryGetValue(type, out var panel))
            {
                panel.OnHide();
                panel.OnUnbindVM();
            }
        }

        #endregion


        #region ViewModel 管理

        private static Dictionary<Type, ViewModel> _viewModels = new();

        public static TVM GetVM<TVM>() where TVM : ViewModel, new()
        {
            var type = typeof(TVM);
            if (!_viewModels.ContainsKey(type))
            {
                _viewModels[type] = new TVM();
                _viewModels[type].OnInit();
            }
            return _viewModels[type] as TVM;
        }

        #endregion
    }
}
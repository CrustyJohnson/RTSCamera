﻿using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.Config;
using RTSCamera.Logic;
using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews.Order;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;

namespace RTSCamera.Patch.Fix
{
    public class Patch_MissionOrderVM
    {
        private static readonly PropertyInfo LastSelectedOrderSetType =
            typeof(MissionOrderVM).GetProperty(nameof(MissionOrderVM.LastSelectedOrderSetType),
                BindingFlags.Instance | BindingFlags.Public);
        private static readonly PropertyInfo OrderSubTypeProperty = typeof(OrderItemVM).GetProperty("OrderSubType", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly PropertyInfo LastSelectedOrderItem = typeof(MissionOrderVM).GetProperty(nameof(MissionOrderVM.LastSelectedOrderItem),
                BindingFlags.Instance | BindingFlags.Public);
        public static readonly MethodInfo UpdateTitleOrdersKeyVisualVisibility = typeof(MissionOrderVM).GetMethod("UpdateTitleOrdersKeyVisualVisibility", BindingFlags.Instance | BindingFlags.NonPublic);

        public static bool AllowEscape = true;

        private static bool _patched;
        public static bool Patch(Harmony harmony)
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;

                harmony.Patch(
                    typeof(MissionOrderVM).GetMethod("CheckCanBeOpened",
                        BindingFlags.NonPublic | BindingFlags.Instance),
                    new HarmonyMethod(typeof(Patch_MissionOrderVM).GetMethod(
                        nameof(Prefix_CheckCanBeOpened), BindingFlags.Static | BindingFlags.Public)));
                harmony.Patch(
                    typeof(MissionOrderVM).GetMethod("AfterInitialize", BindingFlags.Instance | BindingFlags.Public),
                    postfix: new HarmonyMethod(typeof(Patch_MissionOrderVM).GetMethod(nameof(Postfix_AfterInitialize),
                        BindingFlags.Static | BindingFlags.Public)));
                harmony.Patch(
                    typeof(MissionOrderVM).GetMethod(nameof(MissionOrderVM.OnEscape),
                        BindingFlags.Instance | BindingFlags.Public),
                    prefix: new HarmonyMethod(typeof(Patch_MissionOrderVM).GetMethod(nameof(Prefix_OnEscape),
                        BindingFlags.Static | BindingFlags.Public)));
                harmony.Patch(
                    typeof(MissionOrderVM).GetMethod("OnOrder",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(typeof(Patch_MissionOrderVM).GetMethod(nameof(Prefix_OnOrder),
                        BindingFlags.Static | BindingFlags.Public)));
                harmony.Patch(
                    typeof(MissionOrderVM).GetMethod("OnTransferFinished",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    postfix: new HarmonyMethod(typeof(Patch_MissionOrderVM).GetMethod(nameof(Postfix_OnTransferFinished),
                        BindingFlags.Static | BindingFlags.Public)));
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
                return false;
            }
        }

        public static bool Prefix_CheckCanBeOpened(MissionOrderVM __instance, bool displayMessage, ref bool __result)
        {
            // In free camera mode, order UI can be opened event if main agent is controller by AI
            if (Agent.Main != null && !Agent.Main.IsPlayerControlled && Mission.Current
                .GetMissionBehavior<RTSCameraLogic>()?.SwitchFreeCameraLogic.IsSpectatorCamera == true)
            {
                __result = true;
                return false;
            }

            return true;
        }

        public static void Postfix_AfterInitialize(MissionOrderVM __instance)
        {
            LastSelectedOrderSetType.SetValue(__instance, (object)OrderSetType.None);
            AllowEscape = true;
        }

        public static bool Prefix_OnEscape(MissionOrderVM __instance, MissionOrderVM.ActivationType ____currentActivationType, Dictionary<OrderSetType, OrderSetVM> ___OrderSetsWithOrdersByType)
        {
            // Do nothing during draging camera using right mouse button.
            return AllowEscape;
            //if (!AllowEscape)
            //    return false;
            //if (!__instance.IsToggleOrderShown)
            //    return false;
            //if (____currentActivationType == MissionOrderVM.ActivationType.Hold)
            //{
            //    if (__instance.LastSelectedOrderItem == null)
            //        return false;
            //    UpdateTitleOrdersKeyVisualVisibility.Invoke(__instance, null);
            //    ___OrderSetsWithOrdersByType[__instance.LastSelectedOrderSetType].ShowOrders = false;
            //    LastSelectedOrderItem.SetValue(__instance, null);
            //}
            //else
            //{
            //    if (____currentActivationType != MissionOrderVM.ActivationType.Click)
            //        return false;
            //    LastSelectedOrderItem.SetValue(__instance, null);
            //    if (__instance.LastSelectedOrderSetType != OrderSetType.None)
            //    {
            //        ___OrderSetsWithOrdersByType[__instance.LastSelectedOrderSetType].ShowOrders = false;
            //        __instance.LastSelectedOrderSetType = OrderSetType.None;
            //        UpdateTitleOrdersKeyVisualVisibility.Invoke(__instance, null);
            //    }
            //    else
            //    {
            //        __instance.LastSelectedOrderSetType = OrderSetType.None;
            //        __instance.TryCloseToggleOrder();
            //    }
            //}
            //return false;
        }

        public static bool Prefix_OnOrder(MissionOrderVM __instance, OrderItemVM orderItem, OrderSetType orderSetType, bool fromSelection, ref MissionOrderVM.ActivationType ____currentActivationType)
        {

            if (__instance.LastSelectedOrderItem == orderItem && fromSelection)
                return false;
            (typeof(MissionOrderVM).GetField("_onBeforeOrderDelegate", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance) as OnBeforeOrderDelegate).Invoke();
            if (__instance.LastSelectedOrderItem != null)
                __instance.LastSelectedOrderItem.IsSelected = false;
            //if (orderItem.IsTitle)
            //    this.LastSelectedOrderSetType = orderSetType;
            LastSelectedOrderItem.SetValue(__instance, orderItem);

            var orderSetsWithOrdersByType = typeof(MissionOrderVM).GetField("OrderSetsWithOrdersByType", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance) as Dictionary<OrderSetType, OrderSetVM>;

            if (__instance.LastSelectedOrderItem != null)
            {
                __instance.LastSelectedOrderItem.IsSelected = true;
                if ((OrderSubType)OrderSubTypeProperty.GetValue( __instance.LastSelectedOrderItem) == OrderSubType.None)
                {
                    if (orderSetsWithOrdersByType.ContainsKey(__instance.LastSelectedOrderSetType))
                        orderSetsWithOrdersByType[__instance.LastSelectedOrderSetType].ShowOrders = false;
                    LastSelectedOrderSetType.SetValue(__instance, orderSetType);
                    orderSetsWithOrdersByType[__instance.LastSelectedOrderSetType].ShowOrders = true;
                }
            }
            if (__instance.LastSelectedOrderItem != null && (OrderSubType)OrderSubTypeProperty.GetValue(__instance.LastSelectedOrderItem) != OrderSubType.None && !fromSelection)
            {
                OrderSetVM orderSetVm;
                if ((OrderSubType)OrderSubTypeProperty.GetValue(__instance.LastSelectedOrderItem) == OrderSubType.Return && orderSetsWithOrdersByType.TryGetValue(__instance.LastSelectedOrderSetType, out orderSetVm))
                {
                    orderSetVm.ShowOrders = false;
                    UpdateTitleOrdersKeyVisualVisibility.Invoke(__instance, null);
                    LastSelectedOrderSetType.SetValue(__instance, OrderSetType.None);
                }
                else if (____currentActivationType == MissionOrderVM.ActivationType.Hold && __instance.LastSelectedOrderSetType != OrderSetType.None)
                {
                    __instance.ApplySelectedOrder();
                    if (__instance.LastSelectedOrderItem != null && __instance.LastSelectedOrderSetType != OrderSetType.None)
                    {
                        orderSetsWithOrdersByType[__instance.LastSelectedOrderSetType].ShowOrders = false;
                        UpdateTitleOrdersKeyVisualVisibility.Invoke(__instance, null);
                        LastSelectedOrderItem.SetValue(__instance, (object)null);
                    }
                    __instance.OrderSets.ApplyActionOnAllItems((Action<OrderSetVM>)(s => s.Orders.ApplyActionOnAllItems((Action<OrderItemVM>)(o => o.IsSelected = false))));
                }
                else if (__instance.IsDeployment)
                    __instance.ApplySelectedOrder();
                else
                    __instance.TryCloseToggleOrder();
            }
            if (fromSelection)
                return false;
            UpdateTitleOrdersKeyVisualVisibility.Invoke(__instance, null);


            // TODO: don't close the order ui and open it again.
            // Keep orders UI open after issuing an order in free camera mode.
            if (!__instance.IsToggleOrderShown && !__instance.TroopController.IsTransferActive && RTSCameraLogic.Instance?.SwitchFreeCameraLogic.IsSpectatorCamera == true && RTSCameraLogic.Instance?.SwitchFreeCameraLogic.ShouldKeepUIOpen == true && RTSCameraConfig.Get().KeepOrderUIOpenInFreeCamera)
            {
                __instance.OpenToggleOrder(false);
            }
            var orderTroopPlacer = Mission.Current.GetMissionBehavior<OrderTroopPlacer>();
            if (orderTroopPlacer != null)
            {
                typeof(OrderTroopPlacer).GetMethod("Reset", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(orderTroopPlacer, null);
            }
            //var orderUIHandler = Mission.Current.GetMissionBehavior<MissionGauntletSingleplayerOrderUIHandler>();
            //if (orderUIHandler == null)
            //{
            //    return false;
            //}
            //var missionOrderVM = typeof(MissionGauntletSingleplayerOrderUIHandler).GetField("_dataSource", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(orderUIHandler) as MissionOrderVM;
            //var setActiveOrders = typeof(MissionOrderVM).GetMethod("SetActiveOrders", BindingFlags.Instance | BindingFlags.NonPublic);
            //setActiveOrders.Invoke(missionOrderVM, new object[] { });

            return false;
        }

        public static void Postfix_OnTransferFinished(MissionOrderVM __instance)
        {
            // Keep orders UI open after transfer finished in free camera mode.
            if (!__instance.IsToggleOrderShown && RTSCameraLogic.Instance?.SwitchFreeCameraLogic.IsSpectatorCamera == true && RTSCameraConfig.Get().KeepOrderUIOpenInFreeCamera)
            {
                __instance.OpenToggleOrder(false);
            }
        }
    }
}

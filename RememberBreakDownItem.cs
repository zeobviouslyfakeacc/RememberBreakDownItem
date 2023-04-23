using System;
using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;
using MelonLoader;
using Il2Cpp;

internal static class RememberBreakDownItem {

	internal class RememberBreakDownItemMod : MelonMod {
        public override void OnInitializeMelon()
        {
            Debug.Log($"[{Info.Name}] version {Info.Version} loaded");
            new MelonLogger.Instance($"{Info.Name}").Msg($"Version {Info.Version} loaded");
        }
    }

	private static readonly Dictionary<string, int> rememberedToolIDs = new Dictionary<string, int>();
	private static int lastUsedID = -1;

	[HarmonyPatch(typeof(Panel_BreakDown), "Update", new Type[0])]
	private static class KeyboardShortcuts {

		private static void Postfix(Panel_BreakDown __instance) {
			if (!__instance.IsEnabled() || InterfaceManager.ShouldImmediatelyExitOverlay()
				|| InputManager.GetEscapePressed(__instance) || __instance.IsBreakingDown())
				return;

			if (__instance.m_FramesInPanel > 1 && Utils.IsMouseActive()) {
				float movement = Utils.GetMenuMovementHorizontal(__instance, true, true);
				if (movement < 0) {
					__instance.OnPrevTool();
				} else if (movement > 0) {
					__instance.OnNextTool();
				} else if (InputManager.GetRadialButton(__instance)) {
					__instance.OnBreakDown();
				}
			}
		}
	}

	[HarmonyPatch(typeof(Panel_BreakDown), "OnBreakDown", new Type[0])]
	private static class Store {

		private static void Prefix(Panel_BreakDown __instance) {
			BreakDown breakDown = __instance.m_BreakDown;
			if (breakDown == null)
				return;

			string breakDownName = breakDown.m_LocalizedDisplayName.m_LocalizationID;

			GearItem selectedTool = __instance.GetSelectedTool();
			int instanceId = (selectedTool == null) ? 0 : selectedTool.m_InstanceID;

			rememberedToolIDs[breakDownName] = instanceId;
			lastUsedID = instanceId;
		}
	}

	[HarmonyPatch(typeof(Panel_BreakDown), "Enable", new Type[] { typeof(bool) })]
	private static class Load {

		private static void Postfix(Panel_BreakDown __instance, bool enable) {
			if (!enable)
				return;
			BreakDown breakDown = __instance.m_BreakDown;
			if (breakDown == null)
				return;

			string breakDownName = breakDown.m_LocalizedDisplayName.m_LocalizationID;

			if (rememberedToolIDs.ContainsKey(breakDownName)) {
				Use(__instance, rememberedToolIDs[breakDownName]);
			} else {
				Use(__instance, lastUsedID);
			}

			__instance.m_FramesInPanel = 0;
			__instance.Update();
		}
	}

	private static void Use(Panel_BreakDown panel, int toolInstanceID) {
		if (toolInstanceID == 0) {
			panel.m_SelectedToolItemIndex = 0;
			return;
		}

		var tools = panel.m_Tools;

		int index = -1;
		for (int i = 0; i < tools.Count; ++i) {
			GearItem tool = tools[i];
			if (tool != null && tool.m_InstanceID == toolInstanceID) {
				index = i;
				break;
			}
		}

		if (index >= 0) {
			panel.m_SelectedToolItemIndex = index;
		}
	}
}

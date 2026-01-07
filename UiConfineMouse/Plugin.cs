// Copyright 2025-2026 Nicolas Schäfer & Contributors
// Licensed under Apache License, Version 2.0

using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using SpaceCraft;
using UnityEngine;


namespace Nicki0.UiConfineMouse {
	[BepInPlugin("Nicki0.theplanetcraftermods.UiConfineMouse", "(UI) Confine Mouse", PluginInfo.PLUGIN_VERSION)]
	public class Plugin : BaseUnityPlugin {
		public static ConfigEntry<bool> enableMod;
		private void Awake() {
			// Plugin startup logic
			enableMod = Config.Bind<bool>("Config", "enable", true, "Enable mod (requires restart to take effekt)");
			if (enableMod.Value) {
				Harmony.CreateAndPatchAll(typeof(Plugin));
				Cursor.lockState = CursorLockMode.Confined;
			}
			Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
		}
		[HarmonyPrefix] // Prefix + return false so Cursor.lockState isn't called *again* in a postfix, of which I don't know how it could impact performance.
		[HarmonyPatch(typeof(CursorStateManager), nameof(CursorStateManager.SetLockCursorStatus))]
		private static bool CursorStateManager_SetLockCursorStatus(bool isLocked) {
			Cursor.lockState = (isLocked ? CursorLockMode.Locked : CursorLockMode.Confined);
			Cursor.visible = !GamepadConfig.Instance.GetIsUsingController() && !isLocked;
			return false;
		}
	}
}

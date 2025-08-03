// Copyright 2025-2025 Nicolas Schäfer & Contributors
// Licensed under Apache License, Version 2.0

using BepInEx;
using HarmonyLib;
using SpaceCraft;
using UnityEngine;


namespace Nicki0.UiConfineMouse {
	[BepInPlugin("Nicki0.theplanetcraftermods.UiConfineMouse", "(UI) Confine Mouse", PluginInfo.PLUGIN_VERSION)]
	public class Plugin : BaseUnityPlugin {

		private void Awake() {
			// Plugin startup logic
			Harmony.CreateAndPatchAll(typeof(Plugin));

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

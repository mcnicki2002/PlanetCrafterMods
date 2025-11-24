// Copyright 2025-2025 Nicolas Schäfer & Contributors
// Licensed under Apache License, Version 2.0

using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using SpaceCraft;
using UnityEngine;

namespace Nicki0.FixRocketReturn {
	[BepInPlugin("Nicki0.theplanetcraftermods.FixRocketReturn", "(Fix) Rocket Return", PluginInfo.PLUGIN_VERSION)]
	public class Plugin : BaseUnityPlugin {
		static ManualLogSource log;

		private void Awake() {
			// Plugin startup logic
			log = Logger;

			Harmony.CreateAndPatchAll(typeof(Plugin));
			Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
		}

		private static Vector3 epsilonDown = new Vector3(0, -0.001f, 0);

		[HarmonyPostfix]
		[HarmonyPatch(typeof(MachineRocketBackAndForth), "SetRocketUpThereAndDisableIt")]
		public static void MachineRocketBackAndForth_SetRocketUpThereAndDisableIt(GameObject ___rocket) {
			if (___rocket != null && ___rocket.transform != null) ___rocket.transform.position += epsilonDown;
		}

	}
}

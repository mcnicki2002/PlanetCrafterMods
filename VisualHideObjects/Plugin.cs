// Copyright 2025-2025 Nicolas Schäfer & Contributors
// Licensed under Apache License, Version 2.0

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SpaceCraft;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nicki0.VisualHideObjects {
	[BepInPlugin("Nicki0.theplanetcraftermods.VisualHideObjects", "(Visual) Hide Objects", PluginInfo.PLUGIN_VERSION)]
	public class Plugin : BaseUnityPlugin {

		public static ConfigEntry<string> occulsionNames;
		public static ConfigEntry<int> occulsionDistance;
		public static ConfigEntry<Key> occulsionKey;
		public static ConfigEntry<bool> occulsionPrevent;

		private static string[] occulsionStrings;

		static ManualLogSource log;

		private void Awake() {
			// Plugin startup logic
			log = Logger;

			occulsionNames = Config.Bind<string>("Config", "namesOfObjectsToHide", "TreesSpreader", "comma separated list of objects to hide when far away. For specific tree spreaders, use TreesSpreader0,TreesSpreader1,TreesSpreader2");
			occulsionDistance = Config.Bind<int>("Config", "hideDistance", 15, "Distance from player to object to show it.");
			occulsionKey = Config.Bind<Key>("Config", "keyShow", Key.LeftCtrl, "Hold this key to show all objects again.");
			occulsionPrevent = Config.Bind<bool>("Config", "preventOcculsion", false, "Prevent occulsion in occulsion colliders (e.g. in the maze and the region north of it)");

			UpdateStrings();

			Harmony.CreateAndPatchAll(typeof(Plugin));
			Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(MeshOccluder), nameof(MeshOccluder.TryToOcclude))]
		public static bool MeshOccluder_TryToOcclude(MeshOccluder __instance, Vector3 playerObjectPosition, int occlusionMask) {
			foreach (string substring in occulsionStrings) {
				if (__instance.transform.name.StartsWith(substring, StringComparison.Ordinal)) {
					__instance.SetRenderersStatus(Vector3.Distance(playerObjectPosition, __instance.transform.position) < occulsionDistance.Value || Keyboard.current[occulsionKey.Value].isPressed);
					return false;
				}
			}

			return !occulsionPrevent.Value;
		}

		private static void UpdateStrings() {
			if (String.IsNullOrEmpty(occulsionNames.Value)) {
				occulsionStrings = new string[0];
				return;
			}
			occulsionStrings = occulsionNames.Value.Split(',');
		}

		public static void OnModConfigChanged(ConfigEntryBase _) {
			UpdateStrings();
		}
	}
}

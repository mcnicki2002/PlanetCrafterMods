// Copyright 2025-2026 Nicolas Schäfer & Contributors
// Licensed under Apache License, Version 2.0

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using LibCommon;
using SpaceCraft;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nicki0.QoLSkipTutorial {

	/*
	 * ToUpdate:
	 * 
	 */

	[BepInPlugin("Nicki0.theplanetcraftermods.QoLSkipTutorial", "(QoL) Skip Tutorial", PluginInfo.PLUGIN_VERSION)]
	public class Plugin : BaseUnityPlugin {

		private static ManualLogSource log;
		private static Plugin Instance;

		public static ConfigEntry<bool> config_enable;
		private static ConfigEntry<bool> config_SkipTutorial;
		private static ConfigEntry<bool> config_SkipBlueSkyTutorial;

		private void Awake() {
			// Plugin startup logic
			log = Logger;
			Instance = this;

			if (LibCommon.ModVersionCheck.Check(this, Logger.LogInfo)) {
				LibCommon.ModVersionCheck.NotifyUser(this, Logger.LogInfo);
			}

			config_enable = Config.Bind<bool>("General", "enableMod", true, "Enable Mod");
			config_SkipTutorial = Config.Bind<bool>("General", "skipNewTutorial", true, "Skips new tutorials");
			config_SkipBlueSkyTutorial = Config.Bind<bool>("General", "skipBlueSkyTutorial", true, "Hides the Blue Sky tutorial steps");

			HarmonyIntegrityCheck.Check(typeof(Plugin));
			Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
			Harmony.CreateAndPatchAll(typeof(Plugin));
		}

		// --- Skip new tutorial --->
		[HarmonyPrefix]
		[HarmonyPatch(typeof(PopupOneTimeInfos), nameof(PopupOneTimeInfos.SetDependingOnStoryEventStatus))]
		static void PopupOneTimeInfos_SetDependingOnStoryEventStatus(PopupOneTimeInfos __instance) {
			if (!config_enable.Value) { return; }
			if (config_SkipTutorial.Value) {
				StoryEventsHandler manager = Managers.GetManager<StoryEventsHandler>();
				if (__instance.storyEventForFirstTimeAlert != null && manager != null && manager.GetHasInitedStoryEvent() && !manager.EventHasBeenTriggered(__instance.storyEventForFirstTimeAlert)) {
					__instance.CloseFirstTimeDisplay();
				}
			}
		}
		// <--- Skip new tutorial ---

		[HarmonyPrefix]
		[HarmonyPatch(typeof(TutorialHandler), nameof(TutorialHandler.StartTutorial))]
		static void Postfix_TutorialHandler_StartTutorial(TutorialHandler __instance, PlanetData planetData, ref List<TutorialStep> __state) {
			if (!config_enable.Value) { return; }
			if (!config_SkipBlueSkyTutorial.Value) { return; }

			__state ??= new List<TutorialStep>();
			foreach (var el in planetData.tutorialSteps) {
				__state.Add(el);
			}
			planetData.tutorialSteps.Clear();
		}
		[HarmonyPostfix]
		[HarmonyPatch(typeof(TutorialHandler), nameof(TutorialHandler.StartTutorial))]
		static void Postfix_TutorialHandler_StartTutorial(TutorialHandler __instance, PlanetData planetData, List<TutorialStep> __state) {
			if (!config_enable.Value) { return; }
			if (!config_SkipBlueSkyTutorial.Value) { return; }

			foreach (var el in __state) {
				planetData.tutorialSteps.Add(el);
			}
			__instance.StartCoroutine(ExecuteLater(delegate() {
				__instance.gameObject.SetActive(false);
			}));
		}
		private static IEnumerator ExecuteLater(Action toExecute, int waitFrames = 1) {
			for (int i = 0; i < waitFrames; i++) yield return new WaitForEndOfFrame();
			toExecute.Invoke();
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(TutorialHandler), "CheckConditions")]
		static bool TutorialHandler_CheckConditions(TutorialHandler __instance, ref IEnumerator __result) {
			if (!config_enable.Value) { return true; }
			if (!config_SkipBlueSkyTutorial.Value) { return true; }
			__result = CheckConditionsReplacement();
			return false;
		}
		private static IEnumerator CheckConditionsReplacement() {
			yield break;
		}
	}
}

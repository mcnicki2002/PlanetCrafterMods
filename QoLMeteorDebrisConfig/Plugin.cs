// Copyright 2025-2026 Nicolas Schäfer & Contributors
// Licensed under Apache License, Version 2.0

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SpaceCraft;

namespace Nicki0.QoLMeteorDebrisConfig {

	[BepInPlugin("Nicki0.theplanetcraftermods.QoLMeteorDebrisConfig", "(QoL) Meteor Debris Config", PluginInfo.PLUGIN_VERSION)]
	public class Plugin : BaseUnityPlugin {

		private static ManualLogSource log;
		private static Plugin Instance;

		private static ConfigEntry<float> multiplier;

		private void Awake() {
			// Plugin startup logic
			log = Logger;
			Instance = this;

			if (LibCommon.ModVersionCheck.Check(this, Logger.LogInfo)) {
				LibCommon.ModVersionCheck.NotifyUser(this, Logger.LogInfo);
			}

			multiplier = Config.Bind<float>("Config", "multiplier", 1f, "Multiplier for how long debris exists. Example: 0.1 => debris disappears 10 times faster.");

			Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
			Harmony.CreateAndPatchAll(typeof(Plugin));
		}


		struct AIH_CI_state {
			public float asteroidBodyTimeStay;
			public float spawnedResourcesDestroyMultiplier;
		}
		[HarmonyPrefix]
		[HarmonyPatch(typeof(AsteroidsImpactHandler), nameof(AsteroidsImpactHandler.CreateImpact))]
		static void Prefix_AsteroidsImpactHandler_CreateImpact(AsteroidsImpactHandler __instance, ref AIH_CI_state __state, ref float ___asteroidBodyTimeStay, ref float ___spawnedResourcesDestroyMultiplier) {
			__state.spawnedResourcesDestroyMultiplier = ___spawnedResourcesDestroyMultiplier;
			__state.asteroidBodyTimeStay = ___asteroidBodyTimeStay;
			___spawnedResourcesDestroyMultiplier /= multiplier.Value;
			___asteroidBodyTimeStay *= multiplier.Value;
		}
		[HarmonyPostfix]
		[HarmonyPatch(typeof(AsteroidsImpactHandler), nameof(AsteroidsImpactHandler.CreateImpact))]
		static void Postfix_AsteroidsImpactHandler_CreateImpact(AsteroidsImpactHandler __instance, ref AIH_CI_state __state, ref float ___asteroidBodyTimeStay, ref float ___spawnedResourcesDestroyMultiplier) {
			___spawnedResourcesDestroyMultiplier = __state.spawnedResourcesDestroyMultiplier;
			___asteroidBodyTimeStay = __state.asteroidBodyTimeStay;
		}
		[HarmonyPostfix]
		[HarmonyPatch(typeof(Asteroid), nameof(Asteroid.GetDebrisDestroyTime))]
		static void Asteroid_GetDebrisDestroyTime(ref float __result) {
			__result *= multiplier.Value;
		}
		[HarmonyPostfix]
		[HarmonyPatch(typeof(Asteroid), nameof(Asteroid.GetCraterDestroyTime))]
		static void Asteroid_GetCraterDestroyTime(ref float __result) {
			__result *= multiplier.Value;
		}
	}
}

// Copyright 2025-2026 Nicolas Schäfer & Contributors
// Licensed under Apache License, Version 2.0

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using LibCommon;
using SpaceCraft;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nicki0.FixWaterWaveConfig {

	[BepInPlugin("Nicki0.theplanetcraftermods.FixWaterWaveConfig", "(Fix) Water Wave Config", PluginInfo.PLUGIN_VERSION)]
	public class Plugin : BaseUnityPlugin {

		private static ManualLogSource log;
		private static Plugin Instance;

		public static ConfigEntry<bool> config_enable;
		public static ConfigEntry<float> config_aqualisOceanDisplacement;
		public static ConfigEntry<float> config_aqualisOceanHeight;

		private void Awake() {
			// Plugin startup logic
			log = Logger;
			Instance = this;

			if (LibCommon.ModVersionCheck.Check(this, Logger.LogInfo, out bool hashError, out string repoURL)) {
				LibCommon.ModVersionCheck.NotifyUser(this, hashError, repoURL, Logger.LogInfo);
			}

			config_enable = Config.Bind<bool>("General", "enableMod", true, "Enable Mod");
			config_aqualisOceanDisplacement = Config.Bind<float>("General", "aqualisOceanDisplacement", 0.5f, "Set the wave height on Aqualis");
			config_aqualisOceanHeight = Config.Bind<float>("General", "aqualisOceanHeightOffset", 0, "Set the height of the water surface on Aqualis");

			HarmonyIntegrityCheck.Check(typeof(Plugin));
			Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
			Harmony.CreateAndPatchAll(typeof(Plugin));
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(PlanetLoader), "HandleDataAfterLoad")]
		private static void PlanetLoader_HandleDataAfterLoad() {
			if (!config_enable.Value) { return; }

			Scene aqualisScene = SceneManager.GetSceneByName("Planet-Aqualis");
			if (aqualisScene == null || !aqualisScene.isLoaded) { return; }

			foreach (GameObject rootObject in aqualisScene.GetRootGameObjects()) {
				if (rootObject == null) { continue; }

				Transform oceanTransform = rootObject.transform.Find("Water/Ocean");
				if (oceanTransform == null) { continue; }

				oceanTransform.position += config_aqualisOceanHeight.Value * Vector3.up;

				Material oceanMaterial = oceanTransform.GetComponent<MeshRenderer>().material;
				Shader oceanShader = oceanMaterial.shader;

				for (int i = 0; i < oceanShader.GetPropertyCount(); i++) {
					string propertyDescription = oceanShader.GetPropertyDescription(i);
					if (!propertyDescription.Equals("Displacement")) { continue; }

					oceanMaterial.SetFloat(oceanShader.GetPropertyName(i), config_aqualisOceanDisplacement.Value);
					log.LogInfo("Set Displacement property for ocean on Aqualis");
				}
			}
		}
	}
}

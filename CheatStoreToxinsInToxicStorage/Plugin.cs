// Copyright 2025-2026 Nicolas Schäfer & Contributors
// Licensed under Apache License, Version 2.0

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SpaceCraft;
using System.Collections.Generic;

namespace Nicki0.CheatStoreToxinsInToxicStorage {

	[BepInPlugin("Nicki0.theplanetcraftermods.CheatStoreToxinsInToxicStorage", "(Cheat) Store Toxins In Toxic Storage", PluginInfo.PLUGIN_VERSION)]
	public class Plugin : BaseUnityPlugin {

		private static ManualLogSource log;
		private static Plugin Instance;

		static ConfigEntry<string> config_gIDsToStoreInToxicStorage;

		private void Awake() {
			// Plugin startup logic
			log = Logger;
			Instance = this;

			if (LibCommon.ModVersionCheck.Check(this, Logger.LogInfo)) {
				LibCommon.ModVersionCheck.NotifyUser(this, Logger.LogInfo);
			}

			config_gIDsToStoreInToxicStorage = Config.Bind<string>("Config", "gIDsToStoreInToxicStorage", "Toxins,PurifiedWater,ChlorineCapsule1,MicroPlastics", "Group/Item IDs that can be stored in toxic storage.");

			Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
			Harmony.CreateAndPatchAll(typeof(Plugin));
		}
		
		[HarmonyPrefix]
		[HarmonyPatch(typeof(StaticDataHandler), nameof(StaticDataHandler.LoadStaticData))]
		public static void StaticDataHandler_LoadStaticData(List<GroupData> ___groupsData) {
			if (string.IsNullOrEmpty(config_gIDsToStoreInToxicStorage.Value)) { return; }
			foreach (string s in config_gIDsToStoreInToxicStorage.Value.Split(",")) {
				GroupDataItem gdi = ___groupsData.Find(x => x.id == s.Trim()) as GroupDataItem;
				if (gdi != null) {
					gdi.itemCategory = DataConfig.ItemCategory.Toxic;
				}
			}
			
		}
	}
}

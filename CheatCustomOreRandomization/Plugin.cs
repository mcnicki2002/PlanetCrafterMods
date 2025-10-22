// Copyright 2025-2025 Nicolas Schäfer & Contributors
// Licensed under Apache License, Version 2.0

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SpaceCraft;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Nicki0.CheatCustomOreRandomization {
	[BepInPlugin("Nicki0.theplanetcraftermods.CheatCustomOreRandomization", "(Cheat) Custom Ore Randomization", PluginInfo.PLUGIN_VERSION)]
	public class Plugin : BaseUnityPlugin {

		private static Plugin Instance;

		private static readonly int StateObjectId = SaveState.GenerateId(typeof(Plugin));

		private static ConfigEntry<bool> enable;
		private static ConfigEntry<bool> debug;
		private static ConfigEntry<string> configOre;

		static ManualLogSource log;

		private void Awake() {
			// Plugin startup logic
			log = Logger;
			Instance = this;

			enable = Config.Bind<bool>("Config", "enabled", true, "enable mod");
			debug = Config.Bind<bool>("Config", "debug", false, "print messages");
			configOre = Config.Bind<string>("Config", "OreConfig", "", "Ore randomization for new randomized ore save files");

			if (!enable.Value) return;

			Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
			Harmony.CreateAndPatchAll(typeof(Plugin));
		}
		//public void Update() { }

		public static void OnModConfigChanged(ConfigEntryBase _) { }

		private static Func<string, string> StringIdentity = s => s;


		[HarmonyPostfix]
		[HarmonyPatch(typeof(WorldRandomizer), nameof(WorldRandomizer.Init))]
		private static void WorldRandomizer_Init(Dictionary<GroupData, GroupData> ___groupsReplaced) {
			if (!enable.Value) { return; }

			if (!Managers.GetManager<GameSettingsHandler>().GetCurrentGameSettings().GetRandomizeMineables()) { return; }

			WorldObject stateObject;

			bool configExists = SaveState.GetStateObject(StateObjectId, out stateObject);
			if (!configExists) {
				if (!SaveState.GetAndCreateStateObject(StateObjectId, out stateObject)) {
					if (debug.Value) log.LogWarning("State Object not found");
					return;
				}
				Dictionary<string, string> newOreConfig = new Dictionary<string, string>();
				foreach (string cfgEntry in configOre.Value.Split(";")) {
					string[] cfgSplit = cfgEntry.Split(":");
					if (cfgSplit.Length != 2) { continue; }
					if (cfgSplit[0].Trim() == "Uranium") { cfgSplit[0] = "Uranim"; }
					if (cfgSplit[1].Trim() == "Uranium") { cfgSplit[1] = "Uranim"; }
					newOreConfig[cfgSplit[0].Trim()] = cfgSplit[1].Trim();
				}

				SaveState.SetDictionaryData<string, string>(stateObject, newOreConfig);
			}

			Dictionary<string, string> oreConfig = SaveState.GetDictionaryData<string, string>(stateObject, StringIdentity, StringIdentity);

			Dictionary<string, GroupData> toReplaceGIdsToGroupData = new Dictionary<string, GroupData>();
			foreach (GroupData gd in ___groupsReplaced.Keys) {
				toReplaceGIdsToGroupData[gd.id] = gd;
				if (debug.Value) log.LogInfo("toReplace " + gd.id);
			}
			Dictionary<string, GroupData> replacementGIdsToGroupData = new Dictionary<string, GroupData>();
			foreach (GroupData gd in ___groupsReplaced.Values) {
				replacementGIdsToGroupData[gd.id] = gd;
				if (debug.Value) log.LogInfo("replacement " + gd.id);
			}

			foreach (KeyValuePair<string, string> cfgEntry in oreConfig) {

				if (!toReplaceGIdsToGroupData.ContainsKey(cfgEntry.Key)) { if (debug.Value) { log.LogWarning("Couldn't find GroupData for " + cfgEntry.Key + " as Group to Replace"); } continue; }
				if (!replacementGIdsToGroupData.ContainsKey(cfgEntry.Value)) { if (debug.Value) { log.LogWarning("Couldn't find GroupData for " + cfgEntry.Key + " as replacement Group"); } continue; }

				GroupData keyGroup = toReplaceGIdsToGroupData[cfgEntry.Key];
				GroupData replacementGroup = replacementGIdsToGroupData[cfgEntry.Value];
				GroupData replacedGroup = ___groupsReplaced[keyGroup];

				GroupData keyForReplacedGroup;
				try {
					keyForReplacedGroup = ___groupsReplaced.First(e => e.Value.id == replacementGroup.id).Key;
				} catch (Exception e) {
					if (debug.Value) log.LogError("No key for replaced group " + replacedGroup.id + " found!");
					continue;
				}


				___groupsReplaced[keyGroup] = replacementGroup;
				if (debug.Value) log.LogInfo("replacing " + keyGroup.id + " with " + replacementGroup.id + " and replaced " + replacedGroup.id);

				___groupsReplaced[keyForReplacedGroup] = replacedGroup;
				if (debug.Value) log.LogInfo("replaced " + keyForReplacedGroup.id + " with " + replacedGroup.id);

				/*List<GroupData> ___groupsReplacedKeys = new List<GroupData>(___groupsReplaced.Keys);
				foreach (GroupData gd in ___groupsReplacedKeys) {
					if (___groupsReplaced[gd].id == replacementGroup.id && gd.id != keyGroup.id) {
						___groupsReplaced[gd] = replacedGroup;
						log.LogInfo("replaced " + gd.id + " with " + replacedGroup.id);
					}
				}*/

			}
		}
	}
}

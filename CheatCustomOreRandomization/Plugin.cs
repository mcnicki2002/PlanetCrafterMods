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

		private static readonly int DataFormatVersion = 1;
		private static SaveState saveState;

		private static ConfigEntry<bool> enable;
		private static ConfigEntry<bool> debug;
		private static ConfigEntry<string> configOre;

		static ManualLogSource log;

		private void Awake() {
			// Plugin startup logic
			log = Logger;
			Instance = this;

			saveState = new SaveState(typeof(Plugin), DataFormatVersion);

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

			bool configExists = GetData(out Dictionary<string, string> oreConfig);

			if (!configExists) {
				Dictionary<string, string> newOreConfig = new Dictionary<string, string>();
				foreach (string cfgEntry in configOre.Value.Split(";")) {
					string[] cfgSplit = cfgEntry.Split(":");
					if (cfgSplit.Length != 2) { continue; }
					if (cfgSplit[0].Trim() == "Uranium") { cfgSplit[0] = "Uranim"; }
					if (cfgSplit[1].Trim() == "Uranium") { cfgSplit[1] = "Uranim"; }
					newOreConfig[cfgSplit[0].Trim()] = cfgSplit[1].Trim();
				}

				saveState.SetData(newOreConfig);
			}

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

		private static bool GetData(out Dictionary<string, string> stateData) {
			stateData = default;
			switch (saveState.GetDataFormatVersion(out int version)) {
				case SaveState.ERROR_CODE.SUCCESS:
					break;
				case SaveState.ERROR_CODE.NEWER_DATA_FORMAT:
					throw new Exception("Please update the mod " + PluginInfo.PLUGIN_NAME);
				case SaveState.ERROR_CODE.OLD_DATA_FORMAT:
					switch (version) {
						case 1: // Current version, nothing to convert
							break;
						default:
							return false;
					}
					break;
				case SaveState.ERROR_CODE.INVALID_JSON: // custom format didn't use json, so the deserialization would fail
					try {
						if (SaveState_v0.GetStateObject(SaveState_v0.GenerateId(typeof(Plugin)), out WorldObject stateObject)) {
							Dictionary<string, string> oldState = SaveState_v0.GetDictionaryData<string, string>(stateObject, StringIdentity, StringIdentity);
							saveState.SetData(oldState);
						}
					} catch {
						return false;
					}
					break;
				case SaveState.ERROR_CODE.STATE_OBJECT_MISSING:
					return false;
			}

			switch (saveState.GetData(out Dictionary<string, string> data)) {
				case SaveState.ERROR_CODE.SUCCESS:
					stateData = data;
					return true;
				default:
					return false;
			}
		}
	}
}

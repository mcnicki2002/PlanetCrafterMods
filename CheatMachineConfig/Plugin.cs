// Copyright 2025-2026 Nicolas Schäfer & Contributors
// Licensed under Apache License, Version 2.0

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SpaceCraft;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using UnityEngine;
using static UnityEngine.UIElements.TreeViewReorderableDragAndDropController;

namespace Nicki0.CheatMachineConfig {

	[BepInPlugin("Nicki0.theplanetcraftermods.CheatMachineConfig", "(Cheat) Machine Config", PluginInfo.PLUGIN_VERSION)]
	public class Plugin : BaseUnityPlugin {

		static ManualLogSource log;
		static Plugin Instance;

		public static ConfigEntry<bool> enableMod;
		public static ConfigEntry<string> persistentData;


		public static ConfigEntry<float> config_autocrafter_time;
		public static ConfigEntry<float> config_autocrafter_range;
		public static ConfigEntry<float> config_incubator_time;
		public static ConfigEntry<float> config_dnaManipulator_time;
		public static ConfigEntry<float> config_drone1_speed;
		public static ConfigEntry<float> config_drone2_speed;
		public static ConfigEntry<int> config_t1machineOptimizer_affectedMachineCount;
		public static ConfigEntry<float> config_t1machineOptimizer_range;
		public static ConfigEntry<int> config_t2machineOptimizer_affectedMachineCount;
		public static ConfigEntry<float> config_t2machineOptimizer_range;
		public static ConfigEntry<float> config_TradePlatform1_time;
		public static ConfigEntry<float> config_TradePlatform1_returnSpeed;
		public static ConfigEntry<float> config_InterplanetaryExchangePlatform1_time;
		public static ConfigEntry<float> config_InterplanetaryExchangePlatform1_returnSpeed;
		public static ConfigEntry<float> config_VehicleStation_time;

		// Should eventually be moved to a different mod that configures all group values
		public static ConfigEntry<int> config_TradePlatform1_invSize;
		public static ConfigEntry<int> config_InterplanetaryExchangePlatform1_invSize;

		private void Awake() {
			log = Logger;
			Instance = this;MachineRocket a;

			if (this.IsNewVersion(out Version v)) {
				if (v < new Version("1.0.9.0")) {
					if (File.Exists(Config.ConfigFilePath) && (File.ReadLines(Config.ConfigFilePath).FirstOrDefault(e => !string.IsNullOrEmpty(e)) ?? "").Contains("## Settings file was created by plugin (Cheat) Machine Config")) {
						File.Move(Config.ConfigFilePath, Config.ConfigFilePath + "_pre_1.0.9.0_" + DateTime.Now.ToString("yyyy-MM-ddThhmmss") + ".cfg");
						File.Create(Config.ConfigFilePath).Close();
						Config.Reload();
					}
				}
			}

			if (LibCommon.ModVersionCheck.Check(this, Logger.LogInfo, out bool hashError, out string repoURL)) {
				LibCommon.ModVersionCheck.NotifyUser(this, hashError, repoURL, Logger.LogInfo);
			}

			enableMod = Config.Bind<bool>("General", "enable", true, "Enable Mod");



			config_autocrafter_time = Config.Bind<float>("Auto-Crafter", "AutoCrafter_time", -1, "[Default: 5] Time to craft an item (in seconds)");
			config_autocrafter_range = Config.Bind<float>("Auto-Crafter", "AutoCrafter_range", -1, "[Default: 20] Range of auto crafter");
			config_incubator_time = Config.Bind<float>("Incubator", "Incubator_time", -1, "[Default: 1] Time to incubate an item (in minutes)");
			config_dnaManipulator_time = Config.Bind<float>("DNA_Manipulator", "DnaManipulator_time", -1, "[Default: 4] Time to dna-manipulate an item (in minutes)");
			config_drone1_speed = Config.Bind<float>("Drone", "T1Drone_SpeedMultiplier", 1f, "[Default: 1] Speed multiplier of drones");
			config_drone2_speed = Config.Bind<float>("Drone", "T2Drone_SpeedMultiplier", 1f, "[Default: 1] Speed multiplier of drones");
			config_t1machineOptimizer_affectedMachineCount = Config.Bind<int>("Machine_Optimizer", "T1MachineOptimizer_MachineCount", -1, "[Default: 5] Optimization Capacity / Max amount of machines per fuse");
			config_t1machineOptimizer_range = Config.Bind<float>("Machine_Optimizer", "T1MachineOptimizer_range", -1, "[Default: 120] Range of machine optimizer");
			config_t2machineOptimizer_affectedMachineCount = Config.Bind<int>("Machine_Optimizer", "T2MachineOptimizer_MachineCount", -1, "[Default: 8] Optimization Capacity / Max amount of machines per fuse");
			config_t2machineOptimizer_range = Config.Bind<float>("Machine_Optimizer", "T2MachineOptimizer_range", -1, "[Default: 250] Range of machine optimizer");
			config_TradePlatform1_time = Config.Bind<float>("RocketBackAndForth", "TradePlatform1_time", -1, "[Default: 600] Time to return of the trade rocket");
			config_TradePlatform1_returnSpeed = Config.Bind<float>("RocketBackAndForth", "TradePlatform1_langingSpeed", -1, "[Default: 20] Landing speed of the trade rocket (in m/s)");
			config_InterplanetaryExchangePlatform1_time = Config.Bind<float>("RocketBackAndForth", "InterplanetaryExchangePlatform1_time", -1, "[Default: 600] Time to return of the interplanetary exchange shuttle");
			config_InterplanetaryExchangePlatform1_returnSpeed = Config.Bind<float>("RocketBackAndForth", "InterplanetaryExchangePlatform_langingSpeed", -1, "[Default: 20] Landing speed of the shuttle (in m/s)");
			config_VehicleStation_time = Config.Bind<float>("VehicleStation", "VehicleStationCooldown_time", -1, "[Default: 600] Cooldown time of the vehicle station");

			config_TradePlatform1_invSize = Config.Bind<int>("RocketBackAndForth", "TradePlatform1_inventorySize", -1, "[Default: 25] Trade rocket inventory size");
			config_InterplanetaryExchangePlatform1_invSize = Config.Bind<int>("RocketBackAndForth", "InterplanetaryExchangePlatform1_inventorySize", -1, "[Default: 25] Interplanetary exchange shuttle inventory size");


			// Plugin startup logic

			if (!enableMod.Value) {
				log.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is DISABLED!");
				return;
			}



			Harmony.CreateAndPatchAll(typeof(Plugin));
			Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
		}

		private static IEnumerator ExecuteLater(Action toExecute) {
			yield return new WaitForEndOfFrame();
			toExecute.Invoke();
		}

		private static float? baseForwardSpeed_Drone1;
		private static float? baseDistanceMinToTarget_Drone1;
		private static float? baseRotationSpeed_Drone1;
		private static float? baseForwardSpeed_Drone2;
		private static float? baseDistanceMinToTarget_Drone2;
		private static float? baseRotationSpeed_Drone2;

		[HarmonyPrefix]
		[HarmonyPatch(typeof(StaticDataHandler), "LoadStaticData")]
		static void StaticDataHandler_LoadStaticData(List<GroupData> ___groupsData) {
			foreach (GroupData groupData in ___groupsData) {
				if (groupData == null) { continue; }
				GameObject associatedGameObject = groupData.associatedGameObject;
				if (associatedGameObject == null) { continue; }

				switch (groupData.id) {
					case "AutoCrafter1": {
							MachineAutoCrafter component = associatedGameObject.GetComponentInChildren<MachineAutoCrafter>();
							if (config_autocrafter_time.Value >= 0) component.craftEveryXSec = config_autocrafter_time.Value;
							if (config_autocrafter_range.Value >= 0) {
								component.range = config_autocrafter_range.Value;
								foreach (var rangeComp in component.transform.root.GetComponentsInChildren<ActionnableShowRange>()) {
									rangeComp.range = component.range;
								}
							}
							break;
						}
					case "Incubator1": {
							MachineGrowerIfLinkedGroup component = associatedGameObject.GetComponentInChildren<MachineGrowerIfLinkedGroup>();
							if (config_incubator_time.Value >= 0) component.timeToGrow = config_incubator_time.Value;
							break;
						}
					case "GeneticManipulator1": {
							MachineGrowerIfLinkedGroup component = associatedGameObject.GetComponentInChildren<MachineGrowerIfLinkedGroup>();
							if (config_dnaManipulator_time.Value >= 0) component.timeToGrow = config_dnaManipulator_time.Value;
							break;
						}
					case "Drone1": {
							Drone component = associatedGameObject.GetComponentInChildren<Drone>();
							baseForwardSpeed_Drone1 ??= component.forwardSpeed;
							baseDistanceMinToTarget_Drone1 ??= component.distanceMinToTarget;
							baseRotationSpeed_Drone1 ??= component.rotationSpeed;

							float multiplier = config_drone1_speed.Value;
							if (multiplier < 0 || multiplier == 1) break;
							component.forwardSpeed = multiplier * baseForwardSpeed_Drone1.Value;
							component.distanceMinToTarget = multiplier * baseDistanceMinToTarget_Drone1.Value;
							component.rotationSpeed = /*multiplier **/ baseRotationSpeed_Drone1.Value;
							break;
						}
					case "Drone2": {
							Drone component = associatedGameObject.GetComponentInChildren<Drone>();
							baseForwardSpeed_Drone2 ??= component.forwardSpeed;
							baseDistanceMinToTarget_Drone2 ??= component.distanceMinToTarget;
							baseRotationSpeed_Drone2 ??= component.rotationSpeed;

							float multiplier = config_drone2_speed.Value;
							if (multiplier < 0 || multiplier == 1) break;
							component.forwardSpeed = multiplier * baseForwardSpeed_Drone2.Value;
							component.distanceMinToTarget = multiplier * baseDistanceMinToTarget_Drone2.Value;
							component.rotationSpeed = /*multiplier **/ baseRotationSpeed_Drone2.Value;
							break;
						}
					case "Optimizer1": {
							MachineOptimizer component = associatedGameObject.GetComponentInChildren<MachineOptimizer>();
							if (config_t1machineOptimizer_range.Value >= 0) {
								component.range = config_t1machineOptimizer_range.Value;

								foreach (var rangeComp in component.transform.root.GetComponentsInChildren<ActionnableShowRange>()) {
									rangeComp.range = component.range;
								}
							}
							if (config_t1machineOptimizer_affectedMachineCount.Value >= 0) component.maxWorldObjectPerFuse = config_t1machineOptimizer_affectedMachineCount.Value;
							break;
						}
					case "Optimizer2": {
							MachineOptimizer component = associatedGameObject.GetComponentInChildren<MachineOptimizer>();
							if (config_t2machineOptimizer_range.Value >= 0) {
								component.range = config_t2machineOptimizer_range.Value;

								foreach (var rangeComp in component.transform.root.GetComponentsInChildren<ActionnableShowRange>()) {
									rangeComp.range = component.range;
								}
							}
							if (config_t2machineOptimizer_affectedMachineCount.Value >= 0) component.maxWorldObjectPerFuse = config_t2machineOptimizer_affectedMachineCount.Value;
							break;
						}
					case "TradePlatform1": {
							MachineRocketBackAndForthTrade component = associatedGameObject.GetComponentInChildren<MachineRocketBackAndForthTrade>();
							if (config_TradePlatform1_time.Value >= 0) component.updateGrowthEvery = config_TradePlatform1_time.Value / 100.0f;

							var componentLand = associatedGameObject.GetComponentInChildren<MachineRocketLand>();
							if (config_TradePlatform1_returnSpeed.Value >= 0) componentLand.speed = config_TradePlatform1_returnSpeed.Value;

							if (config_TradePlatform1_invSize.Value >= 0) groupData.inventorySize = config_TradePlatform1_invSize.Value;
							break;
						}
					case "InterplanetaryExchangePlatform1": {
							MachineRocketBackAndForthInterplanetaryExchange component = associatedGameObject.GetComponentInChildren<MachineRocketBackAndForthInterplanetaryExchange>();
							if (config_InterplanetaryExchangePlatform1_time.Value >= 0) component.updateGrowthEvery = config_InterplanetaryExchangePlatform1_time.Value / 100.0f;

							var componentLand = associatedGameObject.GetComponentInChildren<MachineRocketLand>();
							if (config_InterplanetaryExchangePlatform1_returnSpeed.Value >= 0) componentLand.speed = config_InterplanetaryExchangePlatform1_returnSpeed.Value;

							if (config_InterplanetaryExchangePlatform1_invSize.Value >= 0) groupData.inventorySize = config_InterplanetaryExchangePlatform1_invSize.Value;
							break;
						}
					default: {
							MachineGenerator componentMachineGenerator = associatedGameObject.GetComponentInChildren<MachineGenerator>();
							if (componentMachineGenerator != null && machineGenerator_times != null && machineGenerator_times.TryGetValue(groupData.id, out ConfigEntry<int> ceMachineGenerator)) {
								if (ceMachineGenerator.Value >= 0) componentMachineGenerator.spawnEveryXSec = ceMachineGenerator.Value;
							}
							MachineGrowerVegetationHarvestable componentMachineGrowerVegetationHarvestable = associatedGameObject.GetComponentInChildren<MachineGrowerVegetationHarvestable>();
							if (componentMachineGrowerVegetationHarvestable != null && machineGrowerVegetationHarvestable_times != null && machineGrowerVegetationHarvestable_times.TryGetValue(groupData.id, out ConfigEntry<float> ceMachineGrowerVH)) {
								if (ceMachineGrowerVH.Value >= 0) componentMachineGrowerVegetationHarvestable.growSpeed = ceMachineGrowerVH.Value;
							}
							MachineDisintegrator componentMachineDisintegrator = associatedGameObject.GetComponentInChildren<MachineDisintegrator>();
							if (componentMachineDisintegrator != null && machineDisintegrator_times != null && machineDisintegrator_times.TryGetValue(groupData.id, out ConfigEntry<int> ceMachineDisintegratorTime)) {
								if (ceMachineDisintegratorTime.Value >= 0) componentMachineDisintegrator.breakEveryXSec = ceMachineDisintegratorTime.Value;
							}
							if (componentMachineDisintegrator != null && machineDisintegrator_maxItemCount != null && machineDisintegrator_maxItemCount.TryGetValue(groupData.id, out ConfigEntry<int> ceMachineDisintegratorItemCount)) {
								if (ceMachineDisintegratorItemCount.Value >= 0) componentMachineDisintegrator.giveXIngredientsBack = ceMachineDisintegratorItemCount.Value;
							}
							break;
						}
				}

			}
		}



		static Dictionary<string, ConfigEntry<int>> machineGenerator_times = [];
		static Dictionary<string, ConfigEntry<float>> machineGrowerVegetationHarvestable_times = [];
		static Dictionary<string, ConfigEntry<int>> machineDisintegrator_times = [];
		static Dictionary<string, ConfigEntry<int>> machineDisintegrator_maxItemCount = [];
		[HarmonyPostfix]
		[HarmonyPriority(Priority.High)]
		[HarmonyPatch(typeof(Intro), "Start")]
		private static void Intro_Start() {
			StaticDataHandler sdh = Managers.GetManager<StaticDataHandler>();
			foreach (GroupData gd in sdh.staticAvailableObjects.groupsData) {
				if (gd == null) continue;
				if (gd.associatedGameObject == null) continue;
				string typeName = Regex.Replace(gd.id, @"[\d-]", string.Empty);

				MachineGenerator mg = gd.associatedGameObject.GetComponentInChildren<MachineGenerator>();
				if (mg != null) {
					machineGenerator_times.TryAdd(gd.id, Instance.Config.Bind<int>(typeName, gd.id + "_time", -1, "[Default: " + mg.spawnEveryXSec + "] Time to generate an item (in seconds)"));
				}
				MachineGrowerVegetationHarvestable mgvh = gd.associatedGameObject.GetComponentInChildren<MachineGrowerVegetationHarvestable>();
				if (mgvh != null) {
					machineGrowerVegetationHarvestable_times.TryAdd(gd.id, Instance.Config.Bind<float>(typeName, gd.id + "_growTime", -1, "[Default: " + mgvh.growSpeed + "] Grow speed"));
				}
				MachineDisintegrator machineDisintegrator = gd.associatedGameObject.GetComponentInChildren<MachineDisintegrator>();
				if (machineDisintegrator != null) {
					machineDisintegrator_times.TryAdd(gd.id, Instance.Config.Bind<int>(typeName, gd.id + "_time", -1, "[Default: " + machineDisintegrator.breakEveryXSec + "] Disintegration time (in seconds)"));
					machineDisintegrator_maxItemCount.TryAdd(gd.id, Instance.Config.Bind<int>(typeName, gd.id + "_maxItemCount", -1, $"[Default: {machineDisintegrator.giveXIngredientsBack}] Maximum amount of items that can be broken down by a {Localization.GetLocalizedString(GameConfig.localizationGroupNameId + gd.id)}"));
				}
			}
		}
		[HarmonyPostfix] // Directly setting it doesn't work with the command console mod, because it sets the value as postfix in SetInventoryRocketBackAndForth. The lower priority should make setting this still be possible.
		[HarmonyPriority(Priority.LowerThanNormal)]
		[HarmonyPatch(typeof(MachineRocketBackAndForth), nameof(MachineRocketBackAndForth.SetInventoryRocketBackAndForth))]
		static void MachineRocketBackAndForth_SetInventoryRocketBackAndForth(MachineRocketBackAndForth __instance, ref float ___updateGrowthEvery) {

			if (__instance is MachineRocketBackAndForthTrade) {
				if (config_TradePlatform1_time.Value >= 0) ___updateGrowthEvery = config_TradePlatform1_time.Value / 100.0f;
			} else if (__instance is MachineRocketBackAndForthInterplanetaryExchange) {
				if (config_InterplanetaryExchangePlatform1_time.Value >= 0) ___updateGrowthEvery = config_InterplanetaryExchangePlatform1_time.Value / 100.0f;
			}
		}

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(VehicleBackToGarage), "MoveVehicleToPositionServerRpc")]
		static IEnumerable<CodeInstruction> Transpiler_VehicleBackToGarage_MoveVehicleToPositionServerRpc(IEnumerable<CodeInstruction> instructions) {
			List<CodeInstruction> codeInstructions = new List<CodeInstruction>(instructions);
			foreach (CodeInstruction instruction in codeInstructions) {
				if (instruction.opcode == OpCodes.Ldc_R4 && instruction.operand.Equals(600.0f)) {
					instruction.opcode = OpCodes.Call;
					instruction.operand = AccessTools.Method(typeof(Plugin), nameof(Plugin.VehicleStationCooldown));
					log.LogInfo("Patched VBTG_MVTPSR");
				}
			}
			return codeInstructions.AsEnumerable<CodeInstruction>();
		}
		public static float VehicleStationCooldown() {
			return config_VehicleStation_time.Value >= 0 ? config_VehicleStation_time.Value : 600;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(MachineRocketBackAndForth), nameof(MachineRocketBackAndForth.SetInventoryRocketBackAndForth))]
		public static void MachineRocketBackAndForth_SetInventoryRocketBackAndForth(MachineRocketBackAndForth __instance, Inventory inventory) {
			if (inventory == null) return;
			if (__instance is MachineRocketBackAndForthTrade) {
				if (config_TradePlatform1_invSize.Value >= 0) inventory.SetSize(config_TradePlatform1_invSize.Value);
			} else if (__instance is MachineRocketBackAndForthInterplanetaryExchange) {
				if (config_InterplanetaryExchangePlatform1_invSize.Value >= 0) inventory.SetSize(config_InterplanetaryExchangePlatform1_invSize.Value);
			}
		}
	}
}

/*
t2recycler_time = Config.Bind<int>("Config_Recycler", "T2Recycler_time", 0, "[Default: 45] Time to recycle an item (in seconds)");
t1orebreaker_time = Config.Bind<int>("Config_OreCrusher", "T1OreCrusher_time", 0, "[Default: 130] Time to break an item (in seconds)");
t2orebreaker_time = Config.Bind<int>("Config_OreCrusher", "T2OreCrusher_time", 0, "[Default: 90] Time to break an item (in seconds)");
t3orebreaker_time = Config.Bind<int>("Config_OreCrusher", "T3OreCrusher_time", 0, "[Default: 70] Time to break an item (in seconds)");
t3orebreaker_itemCount = Config.Bind<int>("Config_OreCrusher", "T3OreCrusher_extractItemCount", 0, "[Default: 5] Max item count that can be broken down");
t1detoxificationmachine_time = Config.Bind<int>("Config_DetoxificationMachine", "T1DetoxificationMachine_time", 0, "[Default: 45] Time to detox an item (in seconds)");
t2detoxificationmachine_time = Config.Bind<int>("Config_DetoxificationMachine", "T2DetoxificationMachine_time", 0, "[Default: 30] Time to detox an item (in seconds)");
t3detoxificationmachine_time = Config.Bind<int>("Config_DetoxificationMachine", "T3DetoxificationMachine_time", 0, "[Default: 15] Time to detox an item (in seconds)");
t3detoxificationmachine_itemCount = Config.Bind<int>("Config_DetoxificationMachine", "T3DetoxificationMachine_extractItemCount", 0, "[Default: 2] Max item count that can be broken down");
*/

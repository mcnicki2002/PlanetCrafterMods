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

namespace Nicki0.CheatMachineConfig {

	[BepInPlugin("Nicki0.theplanetcraftermods.CheatMachineConfig", "(Cheat) Machine Config", PluginInfo.PLUGIN_VERSION)]
	public class Plugin : BaseUnityPlugin {

		static ManualLogSource log;
		static Plugin Instance;

		public static ConfigEntry<bool> enableMod;
		public static ConfigEntry<string> persistentData;


		public static ConfigEntry<float> autocrafter_time;
		public static ConfigEntry<float> autocrafter_range;
		public static ConfigEntry<float> incubator_time;
		public static ConfigEntry<float> dnaManipulator_time;
		public static ConfigEntry<float> drone1_speed;
		public static ConfigEntry<float> drone2_speed;
		public static ConfigEntry<int> t1machineOptimizer_affectedMachineCount;
		public static ConfigEntry<float> t1machineOptimizer_range;
		public static ConfigEntry<int> t2machineOptimizer_affectedMachineCount;
		public static ConfigEntry<float> t2machineOptimizer_range;
		public static ConfigEntry<float> TradePlatform1_time;
		public static ConfigEntry<float> InterplanetaryExchangePlatform1_time;
		public static ConfigEntry<float> VehicleStation_time;

		private void Awake() {
			log = Logger;
			Instance = this;

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



			autocrafter_time = Config.Bind<float>("Auto-Crafter", "AutoCrafter_time", -1, "[Default: 5] Time to craft an item (in seconds)");
			autocrafter_range = Config.Bind<float>("Auto-Crafter", "AutoCrafter_range", -1, "[Default: 20] Range of auto crafter");
			incubator_time = Config.Bind<float>("Incubator", "Incubator_time", -1, "[Default: 1] Time to incubate an item (in minutes)");
			dnaManipulator_time = Config.Bind<float>("DNA_Manipulator", "DnaManipulator_time", -1, "[Default: 4] Time to dna-manipulate an item (in minutes)");
			drone1_speed = Config.Bind<float>("Drone", "T1Drone_SpeedMultiplier", 1f, "[Default: 1] Speed multiplier of drones");
			drone2_speed = Config.Bind<float>("Drone", "T2Drone_SpeedMultiplier", 1f, "[Default: 1] Speed multiplier of drones");
			t1machineOptimizer_affectedMachineCount = Config.Bind<int>("Machine_Optimizer", "T1MachineOptimizer_MachineCount", -1, "[Default: 5] Optimization Capacity / Max amount of machines per fuse");
			t1machineOptimizer_range = Config.Bind<float>("Machine_Optimizer", "T1MachineOptimizer_range", -1, "[Default: 120] Range of machine optimizer");
			t2machineOptimizer_affectedMachineCount = Config.Bind<int>("Machine_Optimizer", "T2MachineOptimizer_MachineCount", -1, "[Default: 8] Optimization Capacity / Max amount of machines per fuse");
			t2machineOptimizer_range = Config.Bind<float>("Machine_Optimizer", "T2MachineOptimizer_range", -1, "[Default: 250] Range of machine optimizer");
			TradePlatform1_time = Config.Bind<float>("RocketBackAndForth", "TradePlatform1_time", -1, "[Default: 600] Time to return of the trade rocket");
			InterplanetaryExchangePlatform1_time = Config.Bind<float>("RocketBackAndForth", "InterplanetaryExchangePlatform1_time", -1, "[Default: 600] Time to return of the interplanetary exchange shuttle");
			VehicleStation_time = Config.Bind<float>("VehicleStation", "VehicleStationCooldown_time", -1, "[Default: 600] Cooldown time of the vehicle station");



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

		private static float? autoCrafter_baseRange;
		private static Vector3? autoCrafter_HorizontalRingSize;
		private static Vector3? autoCrafter_VerticalRingSize;

		private static float? optimizer1_baseRange;
		private static Vector3? optimizer1_HorizontalRingSize;
		private static float? optimizer2_baseRange;
		private static Vector3? optimizer2_HorizontalRingSize;

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
							if (autocrafter_time.Value >= 0) component.craftEveryXSec = autocrafter_time.Value;
							if (autocrafter_range.Value >= 0) {
								autoCrafter_baseRange ??= component.range;
								component.range = autocrafter_range.Value;
								Transform autoCrafter_HorizontalRingTransform = associatedGameObject.transform.Find("RangeViewer");
								autoCrafter_HorizontalRingSize ??= autoCrafter_HorizontalRingTransform.localScale;
								Vector3 newScaleHor = (component.range / autoCrafter_baseRange.Value) * autoCrafter_HorizontalRingSize.Value;
								newScaleHor.y = 1;
								autoCrafter_HorizontalRingTransform.localScale = newScaleHor;

								Transform autoCrafter_VerticalRingTransform = associatedGameObject.transform.Find("RangeViewer (1)");
								autoCrafter_VerticalRingSize ??= autoCrafter_VerticalRingTransform.localScale;
								Vector3 newScaleVert = (component.range / autoCrafter_baseRange.Value) * autoCrafter_HorizontalRingSize.Value;
								newScaleVert.y = 1;
								autoCrafter_VerticalRingTransform.localScale = newScaleVert;
							}
							break;
						}
					case "Incubator1": {
							MachineGrowerIfLinkedGroup component = associatedGameObject.GetComponentInChildren<MachineGrowerIfLinkedGroup>();
							if (incubator_time.Value >= 0) component.timeToGrow = incubator_time.Value;
							break;
						}
					case "GeneticManipulator1": {
							MachineGrowerIfLinkedGroup component = associatedGameObject.GetComponentInChildren<MachineGrowerIfLinkedGroup>();
							if (dnaManipulator_time.Value >= 0) component.timeToGrow = dnaManipulator_time.Value;
							break;
						}
					case "Drone1": {
							Drone component = associatedGameObject.GetComponentInChildren<Drone>();
							baseForwardSpeed_Drone1 ??= component.forwardSpeed;
							baseDistanceMinToTarget_Drone1 ??= component.distanceMinToTarget;
							baseRotationSpeed_Drone1 ??= component.rotationSpeed;

							float multiplier = drone1_speed.Value;
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

							float multiplier = drone2_speed.Value;
							if (multiplier < 0 || multiplier == 1) break;
							component.forwardSpeed = multiplier * baseForwardSpeed_Drone2.Value;
							component.distanceMinToTarget = multiplier * baseDistanceMinToTarget_Drone2.Value;
							component.rotationSpeed = /*multiplier **/ baseRotationSpeed_Drone2.Value;
							break;
						}
					case "Optimizer1": {
							MachineOptimizer component = associatedGameObject.GetComponentInChildren<MachineOptimizer>();
							if (t1machineOptimizer_range.Value >= 0) {
								optimizer1_baseRange ??= component.range;
								component.range = t1machineOptimizer_range.Value;

								Transform transform = associatedGameObject.transform.Find("RangeViewer");
								optimizer1_HorizontalRingSize ??= transform.localScale;
								Vector3 newScale = (component.range / optimizer1_baseRange.Value) * optimizer1_HorizontalRingSize.Value;
								newScale.y = 1;
								transform.localScale = newScale;
							}
							if (t1machineOptimizer_affectedMachineCount.Value >= 0) component.maxWorldObjectPerFuse = t1machineOptimizer_affectedMachineCount.Value;
							break;
						}
					case "Optimizer2": {
							MachineOptimizer component = associatedGameObject.GetComponentInChildren<MachineOptimizer>();
							if (t2machineOptimizer_range.Value >= 0) {
								optimizer2_baseRange ??= component.range;
								component.range = t2machineOptimizer_range.Value;

								Transform transform = associatedGameObject.transform.Find("RangeViewer");
								optimizer2_HorizontalRingSize ??= transform.localScale;
								Vector3 newScale = (component.range / optimizer2_baseRange.Value) * optimizer2_HorizontalRingSize.Value;
								newScale.y = 1;
								transform.localScale = newScale;
							}
							if (t2machineOptimizer_affectedMachineCount.Value >= 0) component.maxWorldObjectPerFuse = t2machineOptimizer_affectedMachineCount.Value;
							break;
						}
					case "TradePlatform1": {
							MachineRocketBackAndForthTrade component = associatedGameObject.GetComponentInChildren<MachineRocketBackAndForthTrade>();
							if (TradePlatform1_time.Value >= 0) component.updateGrowthEvery = TradePlatform1_time.Value / 100.0f;
							break;
						}
					case "InterplanetaryExchangePlatform1": {
							MachineRocketBackAndForthInterplanetaryExchange component = associatedGameObject.GetComponentInChildren<MachineRocketBackAndForthInterplanetaryExchange>();
							if (t2machineOptimizer_affectedMachineCount.Value >= 0) component.updateGrowthEvery = InterplanetaryExchangePlatform1_time.Value / 100.0f;
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
				if (TradePlatform1_time.Value >= 0) ___updateGrowthEvery = TradePlatform1_time.Value / 100.0f;
			} else if (__instance is MachineRocketBackAndForthInterplanetaryExchange) {
				if (t2machineOptimizer_affectedMachineCount.Value >= 0) ___updateGrowthEvery = InterplanetaryExchangePlatform1_time.Value / 100.0f;
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
			return VehicleStation_time.Value >= 0 ? VehicleStation_time.Value : 600;
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

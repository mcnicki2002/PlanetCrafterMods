// Copyright 2025-2025 Nicolas Schäfer & Contributors
// Licensed under Apache License, Version 2.0

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SpaceCraft;
using System;
using UnityEngine;

namespace Nicki0.CheatMachineConfig {

	[BepInPlugin("Nicki0.theplanetcraftermods.CheatMachineConfig", "(Cheat) Machine Config", PluginInfo.PLUGIN_VERSION)]
	public class Plugin : BaseUnityPlugin {

		static ManualLogSource log;

		public static ConfigEntry<bool> enableMod;

		public static ConfigEntry<int> t2recycler_time;
		public static ConfigEntry<int> t1orebreaker_time;
		public static ConfigEntry<int> t2orebreaker_time;
		public static ConfigEntry<int> t3orebreaker_time;
		public static ConfigEntry<float> autocrafter_time;
		public static ConfigEntry<float> autocrafter_range;
		public static ConfigEntry<float> incubator_time;
		public static ConfigEntry<float> dnaManipulator_time;
		public static ConfigEntry<float> drone_speed;
		public static ConfigEntry<int> t1machineOptimizer_affectedMachineCount;
		public static ConfigEntry<float> t1machineOptimizer_range;
		public static ConfigEntry<int> t2machineOptimizer_affectedMachineCount;
		public static ConfigEntry<float> t2machineOptimizer_range;

		private void Awake() {
			log = Logger;

			enableMod = Config.Bind<bool>("General", "enable", true, "Enable Mod");

			t2recycler_time = Config.Bind<int>("Config_Recycler", "T2Recycler_time", 45, "Time to recycle an item (in seconds)");
			t1orebreaker_time = Config.Bind<int>("Config_OreCrusher", "T1OreCrusher_time", 130, "Time to break an item (in seconds)");
			t2orebreaker_time = Config.Bind<int>("Config_OreCrusher", "T2OreCrusher_time", 90, "Time to break an item (in seconds)");
			t3orebreaker_time = Config.Bind<int>("Config_OreCrusher", "T3OreCrusher_time", 70, "Time to break an item (in seconds)");
			autocrafter_time = Config.Bind<float>("Config_AutoCrafter", "AutoCrafter_time", 5f, "Time to craft an item (in seconds)");
			autocrafter_range = Config.Bind<float>("Config_AutoCrafter", "AutoCrafter_range", 20f, "Range of auto crafter");
			incubator_time = Config.Bind<float>("Config_Incubator", "Incubator_time", 1f, "Time to incubate an item (in minutes)");
			dnaManipulator_time = Config.Bind<float>("Config_DNA-Manipulator", "DnaManipulator_time", 4f, "Time to dna-manipulate an item (in minutes)");
			drone_speed = Config.Bind<float>("Config_Drone", "Drone_SpeedMultiplier", 1f, "Speed multiplier of drones");
			t1machineOptimizer_affectedMachineCount = Config.Bind<int>("Config_MachineOptimizer", "T1MachineOptimizer_MachineCount", 5, "Optimization Capacity / Max amount of machines per fuse");
			t1machineOptimizer_range = Config.Bind<float>("Config_MachineOptimizer", "T1MachineOptimizer_range", 120, "Range of machine optimizer");
			t2machineOptimizer_affectedMachineCount = Config.Bind<int>("Config_MachineOptimizer", "T2MachineOptimizer_MachineCount", 8, "Optimization Capacity / Max amount of machines per fuse");
			t2machineOptimizer_range = Config.Bind<float>("Config_MachineOptimizer", "T2MachineOptimizer_range", 250, "Range of machine optimizer");

			// Plugin startup logic

			if (!enableMod.Value) {
				log.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is DISABLED!");
				return;
			}

			Harmony.CreateAndPatchAll(typeof(Plugin));
			Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
		}


		[HarmonyPrefix]
		[HarmonyPatch(typeof(MachineDisintegrator), nameof(MachineDisintegrator.SetDisintegratorInventory))]
		public static void MachineDisintegrator_SetDisintegratorInventory(Inventory inventory, ref int ___breakEveryXSec) {
			WorldObject wo = WorldObjectsHandler.Instance.GetWorldObjectForInventory(inventory);
			if (t2recycler_time.Value != 0) if (wo.GetGroup().GetId() == "RecyclingMachine2") ___breakEveryXSec = t2recycler_time.Value;
			if (t1orebreaker_time.Value != 0) if (wo.GetGroup().GetId() == "OreBreaker1") ___breakEveryXSec = t1orebreaker_time.Value;
			if (t2orebreaker_time.Value != 0) if (wo.GetGroup().GetId() == "OreBreaker2") ___breakEveryXSec = t2orebreaker_time.Value;
			if (t3orebreaker_time.Value != 0) if (wo.GetGroup().GetId() == "OreBreaker3") ___breakEveryXSec = t3orebreaker_time.Value;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(MachineAutoCrafter), "Awake")]
		public static void MachineAutoCrafter_Awake(ref float ___craftEveryXSec, ref float ___range) {
			if (autocrafter_time.Value != 0) ___craftEveryXSec = autocrafter_time.Value;
			if (autocrafter_range.Value != 0) ___range = autocrafter_range.Value;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(MachineGrowerIfLinkedGroup), "Awake")]
		public static void MachineGrowerIfLinkedGroup_Awake(ref float ___timeToGrow) {
			if (Math.Abs(___timeToGrow - 1f) < 0.01) {
				if (incubator_time.Value != 0) ___timeToGrow = incubator_time.Value;
			} else if (Math.Abs(___timeToGrow - 4f) < 0.01) {
				if (dnaManipulator_time.Value != 0) ___timeToGrow = dnaManipulator_time.Value;
			}
		}

		private static bool enabledDroneFix = true;
		private static bool baseInitialized = false;
		private static float baseForwardSpeed;
		private static float baseDistanceMinToTarget;
		private static float baseRotationSpeed;
		[HarmonyPrefix]
		[HarmonyPatch(typeof(Drone), "Awake")]
		public static void Drone_Awake(ref float ___forwardSpeed, ref float ___distanceMinToTarget, ref float ___rotationSpeed) {

			if (!baseInitialized) {
				baseForwardSpeed = ___forwardSpeed;
				baseDistanceMinToTarget = ___distanceMinToTarget;
				baseRotationSpeed = ___rotationSpeed;
				baseInitialized = true;
			}

			if (drone_speed.Value == 1f) {
				enabledDroneFix = false;
				return;
			}

			float multiplier = drone_speed.Value;
			___forwardSpeed = multiplier * baseForwardSpeed;
			___distanceMinToTarget = multiplier * baseDistanceMinToTarget;
			___rotationSpeed = multiplier * baseRotationSpeed;
		}

		// "look rotation viewing vector is zero"-fix
		/*[HarmonyPrefix] // old. Getting transform each time was marked as inefficient by performance profiler
		[HarmonyPatch(typeof(Drone), "MoveToTarget")]
		public static bool Drone_MoveToTarget(ref Vector3 targetPosition, ref GameObject ____droneRoot, float ___forwardSpeed, float ___rotationSpeed) {
			if (!enabledDroneFix) return true;
			targetPosition += Vector3.up * 2f;
			____droneRoot.transform.Translate(0f, 0f, Time.deltaTime * ___forwardSpeed);
			if ((double)(targetPosition - ____droneRoot.transform.position).sqrMagnitude > float.Epsilon) {
				____droneRoot.transform.rotation = Quaternion.Slerp(____droneRoot.transform.rotation, Quaternion.LookRotation(targetPosition - ____droneRoot.transform.position), ___rotationSpeed * Time.deltaTime);
			}
			return false;
		}*/
		[HarmonyPrefix]
		[HarmonyPatch(typeof(Drone), "MoveToTarget")]
		public static bool Drone_MoveToTarget(ref Vector3 targetPosition, ref GameObject ____droneRoot, float ___forwardSpeed, float ___rotationSpeed) {
			targetPosition += Vector3.up * 2f;
			Transform droneTransform = ____droneRoot.transform;
			droneTransform.Translate(0f, 0f, Time.deltaTime * ___forwardSpeed);
			Vector3 targetPositionDifference = targetPosition - droneTransform.position;
			if ((double)(targetPositionDifference).sqrMagnitude > float.Epsilon) {
				droneTransform.rotation = Quaternion.Slerp(droneTransform.rotation, Quaternion.LookRotation(targetPositionDifference), ___rotationSpeed * Time.deltaTime);
			}
			return false;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(MachineOptimizer), nameof(MachineOptimizer.SetOptimizerInventory))]
		public static void MachineOptimizer_SetOptimizerInventory(Inventory inventory, ref int ___maxWorldObjectPerFuse, ref float ___range) {
			WorldObject wo = WorldObjectsHandler.Instance.GetWorldObjectForInventory(inventory);
			if (wo.GetGroup().GetId() == "Optimizer1") {
				if (t1machineOptimizer_affectedMachineCount.Value != 0) ___maxWorldObjectPerFuse = t1machineOptimizer_affectedMachineCount.Value;
				if (t1machineOptimizer_range.Value != 0) ___range = t1machineOptimizer_range.Value;

			}
			if (wo.GetGroup().GetId() == "Optimizer2") {
				if (t2machineOptimizer_affectedMachineCount.Value != 0) ___maxWorldObjectPerFuse = t2machineOptimizer_affectedMachineCount.Value;
				if (t2machineOptimizer_range.Value != 0) ___range = t2machineOptimizer_range.Value;
			}
		}
	}
}

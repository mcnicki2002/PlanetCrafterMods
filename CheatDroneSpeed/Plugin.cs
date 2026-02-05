// Copyright 2025-2026 Nicolas Schäfer & Contributors
// Licensed under Apache License, Version 2.0

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SpaceCraft;
using UnityEngine;

namespace Nicki0.CheatDroneSpeed {
	[BepInPlugin("Nicki0.theplanetcraftermods.CheatDroneSpeed", "(Cheat) Drone Speed", PluginInfo.PLUGIN_VERSION)]
	public class Plugin : BaseUnityPlugin {

		private static ManualLogSource log;

		private void Awake() {
			// Plugin startup logic
			log = Logger;

			droneSpeedMultiplier = Config.Bind<float>("General", "droneSpeedMultiplier", 1.0f, "Multiplier for drone speed");

			Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
			Harmony.CreateAndPatchAll(typeof(Plugin));
		}

		private static bool baseInitialized = false;
		private static float baseForwardSpeed;
		private static float baseDistanceMinToTarget;
		private static float baseRotationSpeed;
		private static float baseForwardSpeedIntervalModifier;

		public static ConfigEntry<float> droneSpeedMultiplier;

		[HarmonyPrefix]
		[HarmonyPatch(typeof(Drone), "Awake")]
		public static void Drone_Awake(ref float ___forwardSpeed, ref float ___distanceMinToTarget, ref float ___rotationSpeed, ref float ___forwardSpeedIntervalModifier) {

			if (!baseInitialized) {
				baseForwardSpeed = ___forwardSpeed;
				baseDistanceMinToTarget = ___distanceMinToTarget;
				baseRotationSpeed = ___rotationSpeed;
				baseForwardSpeedIntervalModifier = ___forwardSpeedIntervalModifier;
				baseInitialized = true;
			}

			float multiplier = droneSpeedMultiplier.Value;
			___forwardSpeed = multiplier * baseForwardSpeed;
			___distanceMinToTarget = multiplier * baseDistanceMinToTarget;
			___rotationSpeed = /*Mathf.Sqrt(multiplier) * */baseRotationSpeed;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(LogisticManager), nameof(LogisticManager.AddDroneToFleet))]
		private static void LogisticManager_AddRoneToFleet(Drone drone) {
			return; // TODO HÄÄÄÄÄÄÄÄÄÄÄ???????????????
			drone.forwardSpeed = droneSpeedMultiplier.Value * baseForwardSpeed;
			drone.distanceMinToTarget = droneSpeedMultiplier.Value * baseDistanceMinToTarget;
			drone.rotationSpeed = droneSpeedMultiplier.Value * baseRotationSpeed;
		}

		// "look rotation viewing vector is zero"-fix
		/*[HarmonyPrefix]
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
		}*/
	}
}
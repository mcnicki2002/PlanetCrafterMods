// Copyright 2025-2025 Nicolas Schäfer & Contributors
// Licensed under Apache License, Version 2.0

using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using SpaceCraft;
using System;
using UnityEngine;

namespace CheatDroneSpeed
{
	[BepInPlugin("Nicki0.theplanetcraftermods.CheatDroneSpeed", "(Cheat) Drone Speed", PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            // Plugin startup logic
			
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
			___rotationSpeed = multiplier * baseRotationSpeed;
		}
		
		// "look rotation viewing vector is zero"-fix
		[HarmonyPrefix]
		[HarmonyPatch(typeof(Drone), "MoveToTarget")]
		public static bool Drone_MoveToTarget(ref Vector3 targetPosition, ref GameObject ____droneRoot, float ___forwardSpeed, float ___rotationSpeed) {
			targetPosition += Vector3.up * 2f;
			____droneRoot.transform.Translate(0f, 0f, Time.deltaTime * ___forwardSpeed);
			if ((double)(targetPosition - ____droneRoot.transform.position).sqrMagnitude > float.Epsilon) {
				____droneRoot.transform.rotation = Quaternion.Slerp(____droneRoot.transform.rotation, Quaternion.LookRotation(targetPosition - ____droneRoot.transform.position), ___rotationSpeed * Time.deltaTime);
			}
			return false;
		}
    }
}
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;

using HarmonyLib;
using SpaceCraft;

using TMPro;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using Unity.Mathematics;
using Unity.Netcode;

using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEngine.SceneManagement;

namespace CheatMachineConfig {
	
	[BepInPlugin("nicki0.theplanetcraftermods.CheatMachineConfig", "(Cheat) Machine Config", PluginInfo.PLUGIN_VERSION)]
	public class Plugin : BaseUnityPlugin {
		
		static ManualLogSource log;
		
		
		public static ConfigEntry<int> t2recycler_time;
		public static ConfigEntry<int> t1orebreaker_time;
		public static ConfigEntry<int> t2orebreaker_time;
		public static ConfigEntry<int> t3orebreaker_time;
		public static ConfigEntry<float> autocrafter_time;
		public static ConfigEntry<float> autocrafter_range;
		public static ConfigEntry<float> incubator_time;
		public static ConfigEntry<float> dnaManipulator_time;
		
		private void Awake() {
			log = Logger;
			
			t2recycler_time = Config.Bind<int>("Config_T2Recycler", "T2Recycler_time", 45, "Time to recycle an item (in seconds)");
			t1orebreaker_time = Config.Bind<int>("Config_T1OreCrusher", "T1OreCrusher_time", 130, "Time to break an item (in seconds)");
			t2orebreaker_time = Config.Bind<int>("Config_T2OreCrusher", "T2OreCrusher_time", 90, "Time to break an item (in seconds)");
			t3orebreaker_time = Config.Bind<int>("Config_T3OreCrusher", "T3OreCrusher_time", 70, "Time to break an item (in seconds)");
			autocrafter_time = Config.Bind<float>("Config_AutoCrafter", "AutoCrafter_time", 5f, "Time to craft an item (in seconds)");
			autocrafter_range = Config.Bind<float>("Config_AutoCrafter", "AutoCrafter_range", 20f, "Range of auto crafter");
			incubator_time = Config.Bind<float>("Config_Incubator", "Incubator_time", 1f, "Time to incubate an item (in minutes)");
			dnaManipulator_time = Config.Bind<float>("Config_DNA-Manipulator", "DnaManipulator_time", 4f, "Time to dna-manipulate an item (in minutes)");
			
			// Plugin startup logic
			Harmony.CreateAndPatchAll(typeof(Plugin));
			Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
		}
		
		
		[HarmonyPrefix]
		[HarmonyPatch(typeof(MachineDisintegrator), nameof(MachineDisintegrator.SetDisintegratorInventory))]
		public static void MachineDisintegrator_SetDisintegratorInventory(Inventory inventory, ref int ___breakEveryXSec) {
			WorldObject wo = WorldObjectsHandler.Instance.GetWorldObjectForInventory(inventory);
			if (wo.GetGroup().GetId() == "RecyclingMachine2") ___breakEveryXSec = t2recycler_time.Value;
			if (wo.GetGroup().GetId() == "OreBreaker1") ___breakEveryXSec = t1orebreaker_time.Value;
			if (wo.GetGroup().GetId() == "OreBreaker2") ___breakEveryXSec = t1orebreaker_time.Value;
			if (wo.GetGroup().GetId() == "OreBreaker3") ___breakEveryXSec = t1orebreaker_time.Value;
		}
		
		[HarmonyPrefix]
		[HarmonyPatch(typeof(MachineAutoCrafter), "Awake")]
		public static void MachineAutoCrafter_Awake(ref float ___craftEveryXSec, ref float ___range) {
			___craftEveryXSec = autocrafter_time.Value;
			___range = autocrafter_range.Value;
		}
		
		[HarmonyPrefix]
		[HarmonyPatch(typeof(MachineGrowerIfLinkedGroup), "Awake")]
		public static void MachineGrowerIfLinkedGroup_Awake(ref float ___timeToGrow) {
			if (Math.Abs(___timeToGrow - 1f) < 0.01) ___timeToGrow = incubator_time.Value;
			else if (Math.Abs(___timeToGrow - 4f) < 0.01) ___timeToGrow = dnaManipulator_time.Value;
		}
	}
}

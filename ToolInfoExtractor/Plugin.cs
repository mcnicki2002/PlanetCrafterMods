// Copyright 2025-2026 Nicolas Schäfer & Contributors
// Licensed under Apache License, Version 2.0

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;
using SpaceCraft;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Nicki0.ToolInfoExtractor {

	[BepInPlugin("Nicki0.theplanetcraftermods.ToolInfoExtractor", "(Tool) Info Extractor", PluginInfo.PLUGIN_VERSION)]
	public class Plugin : BaseUnityPlugin {

		private static ManualLogSource log;
		private static Plugin Instance;

		public static ConfigEntry<string> config_outputPath;

		static bool didAlreadyRun = false; // to prevent NRE when GOs are destroyed

		private void Awake() {
			// Plugin startup logic
			log = Logger;
			Instance = this;


			config_outputPath = Config.Bind<string>("General", "outputPath", "", "Export Directory");
			

			Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
			Harmony.CreateAndPatchAll(typeof(Plugin));
		}

		public class GroupData_JSONABLE {
			public GroupData_JSONABLE(GroupData gd) {
				GroupDataType = gd.GetType().Name;

				id = gd.id;
				associatedGameObject = gd.associatedGameObject?.name;
				icon = gd.icon?.name;
				recipeIngredients = gd.recipeIngredients?.Select(e => e.id).ToList();
				hideInCrafter = gd.hideInCrafter;
				unlockingWorldUnit = gd.unlockingWorldUnit.ToString();
				unlockingValue = gd.unlockingValue;
				terraformStageUnlock = gd.terraformStageUnlock?.id;
				unlockInPlanets = gd.unlockInPlanets?.Select(e => e.id).ToList();
				lootRecipeOnDeconstruct = gd.lootRecipeOnDeconstruct;
				tradeCategory = gd.tradeCategory.ToString();
				tradeValue = gd.tradeValue;
				inventorySize = gd.inventorySize;
				secondaryInventoriesSize = gd.secondaryInventoriesSize;
				logisticInterplanetaryType = gd.logisticInterplanetaryType.ToString();
			}

			public string GroupDataType;

			public string id;
			public string associatedGameObject;
			public string icon;
			public List<string> recipeIngredients;
			public bool hideInCrafter;
			public string unlockingWorldUnit;
			public float unlockingValue;
			public string terraformStageUnlock;
			public List<string> unlockInPlanets;
			public string planetUsageType;
			public bool lootRecipeOnDeconstruct;
			public string tradeCategory;
			public int tradeValue;
			public int inventorySize;
			public List<int> secondaryInventoriesSize;
			public string logisticInterplanetaryType;
		}
		public class GroupDataConstructible_JSONABLE : GroupData_JSONABLE {
			public GroupDataConstructible_JSONABLE(GroupDataConstructible gdc) : base(gdc) {
				unitGenerationOxygen = gdc.unitGenerationOxygen;
				unitGenerationPressure = gdc.unitGenerationPressure;
				unitGenerationHeat = gdc.unitGenerationHeat;
				unitGenerationEnergy = gdc.unitGenerationEnergy;
				unitGenerationPlants = gdc.unitGenerationPlants;
				unitGenerationInsects = gdc.unitGenerationInsects;
				unitGenerationAnimals = gdc.unitGenerationAnimals;
				unitGenerationPurification = gdc.unitGenerationPurification;
				nextTierGroup = gdc.nextTierGroup?.id;
				rotationFixed = gdc.rotationFixed;
				terraStageRequirements = gdc.terraStageRequirements?.Select(e => e.name).ToArray();
				notAllowedPlanetsRequirement = gdc.notAllowedPlanetsRequirement?.Select(e => e.id).ToList();
				notAllowedPlanetsRequirementTextId = gdc.notAllowedPlanetsRequirementTextId;
				groupCategory = gdc.groupCategory.ToString();
				worlUnitMultiplied = gdc.worlUnitMultiplied.ToString();
			}
			public float unitGenerationOxygen;
			public float unitGenerationPressure;
			public float unitGenerationHeat;
			public float unitGenerationEnergy;
			public float unitGenerationPlants;
			public float unitGenerationInsects;
			public float unitGenerationAnimals;
			public float unitGenerationPurification;
			public string nextTierGroup;
			public bool rotationFixed;
			public string[] terraStageRequirements;
			public List<string> notAllowedPlanetsRequirement;
			public string notAllowedPlanetsRequirementTextId;
			public string groupCategory;
			public string worlUnitMultiplied;
		}
		public class GroupDataItem_JSONABLE : GroupData_JSONABLE {
			public GroupDataItem_JSONABLE(GroupDataItem gdi) : base(gdi) {
				value = gdi.value;
				craftableInList = gdi.craftableInList?.Select(e => e.ToString()).ToList();
				equipableType = gdi.equipableType.ToString();
				usableType = gdi.usableType.ToString();
				itemCategory = gdi.itemCategory.ToString();
				itemSubCategory = gdi.itemSubCategory.ToString();
				growableGroup = gdi.growableGroup?.id;
				unlocksGroup = gdi.unlocksGroup?.id;
				effectOnPlayer = gdi.effectOnPlayer?.GetName();
				chanceToSpawn = gdi.chanceToSpawn;
				cantBeDestroyed = gdi.cantBeDestroyed;
				cantBeRecycled = gdi.cantBeRecycled;
				craftedInWorld = gdi.craftedInWorld;
				canBePickedUpFromWorldByDrones = gdi.canBePickedUpFromWorldByDrones;
				displayInLogisticType = gdi.displayInLogisticType.ToString();
				unitMultiplierOxygen = gdi.unitMultiplierOxygen;
				unitMultiplierPressure = gdi.unitMultiplierPressure;
				unitMultiplierHeat = gdi.unitMultiplierHeat;
				unitMultiplierEnergy = gdi.unitMultiplierEnergy;
				unitMultiplierPlants = gdi.unitMultiplierPlants;
				unitMultiplierInsects = gdi.unitMultiplierInsects;
				unitMultiplierAnimals = gdi.unitMultiplierAnimals;
				unitMultiplierPurification = gdi.unitMultiplierPurification;
			}
			public int value;
			public List<string> craftableInList;
			public string equipableType;
			public string usableType;
			public string itemCategory;
			public string itemSubCategory;
			public string growableGroup;
			public string unlocksGroup;
			public string effectOnPlayer;
			public float chanceToSpawn;
			public bool cantBeDestroyed;
			public bool cantBeRecycled;
			public bool craftedInWorld;
			public bool canBePickedUpFromWorldByDrones;
			public string displayInLogisticType;
			public float unitMultiplierOxygen;
			public float unitMultiplierPressure;
			public float unitMultiplierHeat;
			public float unitMultiplierEnergy;
			public float unitMultiplierPlants;
			public float unitMultiplierInsects;
			public float unitMultiplierAnimals;
			public float unitMultiplierPurification;
		}

		[HarmonyPrefix]
		[HarmonyPriority(Priority.First)]
		[HarmonyPatch(typeof(StaticDataHandler), nameof(StaticDataHandler.LoadStaticData))]
		static void StaticDataHandler_LoadStaticData(List<GroupData> ___groupsData) {
			if (didAlreadyRun) return;
			didAlreadyRun = true;

			string dir = string.IsNullOrEmpty(config_outputPath.Value) ? Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) : config_outputPath.Value;

			Directory.CreateDirectory(dir);

			Dictionary<string, GroupData_JSONABLE> data = ___groupsData.Select(e => (GroupData_JSONABLE)(e is GroupDataItem gdi ? new GroupDataItem_JSONABLE(gdi) : new GroupDataConstructible_JSONABLE((GroupDataConstructible)e))).ToDictionary(e => e.id);

			File.AppendAllText(Path.Combine(dir, "ExportedGroupData.json"), JsonConvert.SerializeObject(data, Formatting.Indented));
			log.LogInfo($"Data exported to {dir}");
		}
	}
}

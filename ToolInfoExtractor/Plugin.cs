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
using System.Text.RegularExpressions;
using UnityEngine;

namespace Nicki0.ToolInfoExtractor {

	[BepInPlugin("Nicki0.theplanetcraftermods.ToolInfoExtractor", "(Tool) Info Extractor", PluginInfo.PLUGIN_VERSION)]
	public class Plugin : BaseUnityPlugin {

		private static ManualLogSource log;
		private static Plugin Instance;

		public static ConfigEntry<bool> config_enabled;
		public static ConfigEntry<string> config_outputPath;

		static bool didAlreadyRun = false; // to prevent NRE when GOs are destroyed

		private void Awake() {
			// Plugin startup logic
			log = Logger;
			Instance = this;

			config_enabled = Config.Bind<bool>("General", "enabled", false, "Enable mod");
			config_outputPath = Config.Bind<string>("General", "outputPath", "", "Export Directory");
			

			Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
			Harmony.CreateAndPatchAll(typeof(Plugin));
		}

		public class GroupData_JSONABLE {
			public GroupData_JSONABLE(GroupData gd) {
				GroupDataType = gd.GetType().Name;
				LocalizationNameEnglish = Localization.GetLocalizedString(GameConfig.localizationGroupNameId + gd.id);
				LocalizationDescriptionEnglish = Localization.GetLocalizedString(GameConfig.localizationGroupDescriptionId + gd.id);
				Scripts = gd.associatedGameObject == null ? null : new Scripts_JSONABLE(gd);

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
			public string LocalizationNameEnglish;
			public string LocalizationDescriptionEnglish;
			public Scripts_JSONABLE Scripts;

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

		public class Scripts_JSONABLE {
			public Scripts_JSONABLE(GroupData gd) {
				GameObject go = gd.associatedGameObject;
				if (go == null) return;
				if (go.TryGetComponent<MachineAutoCrafter>(out var mac)) {
					this.MachineAutoCrafter = new MachineAutoCrafter_JSONABLE(mac);
					itemsPerSecond = 1.0f / mac.craftEveryXSec;
				}
				if (go.TryGetComponent<MachineGenerator>(out var machineGenerator)) {
					this.MachineGenerator = new MachineGenerator_JSONABLE(machineGenerator);
					itemsPerSecond = 1.0f / (machineGenerator.spawnEveryXSec);
				}
				if (go.TryGetComponent<MachineGrowerVegetationHarvestable>(out var mgvh)) {
					this.MachineGrowerVegetationHarvestable = new MachineGrowerVegetationHarvestable_JSONABLE(mgvh);
					itemsPerSecond = gd.secondaryInventoriesSize[0] / (100.0f * (1.0f - mgvh.minGrowth) / mgvh.growSpeed);
				}
				if (go.TryGetComponent<MachineDisintegrator>(out var machineDisintegrator)) {
					this.MachineDisintegrator = new MachineDisintegrator_JSONABLE(machineDisintegrator);
					itemsPerSecond = 1.0f / machineDisintegrator.breakEveryXSec;
				}
				if (go.TryGetComponent<MachineRocketBackAndForthInterplanetaryExchange>(out var mrbafie)) {
					this.MachineRocketBackAndForthInterplanetaryExchange = new MachineRocketBackAndForthInterplanetaryExchange_JSONABLE(mrbafie);
					itemsPerSecond = gd.inventorySize / (100.0f * mrbafie.updateGrowthEvery);
				}
				if (go.TryGetComponent<MachineRocketBackAndForthTrade>(out var mrbaft)) {
					this.MachineRocketBackAndForthTrade = new MachineRocketBackAndForthTrade_JSONABLE(mrbaft);
					itemsPerSecond = gd.inventorySize / (100.0f * mrbaft.updateGrowthEvery); // TODO check if everything calced right
				}
			}
			public float itemsPerSecond { get; set; }
			public MachineAutoCrafter_JSONABLE MachineAutoCrafter { get; set; }
			public MachineGenerator_JSONABLE MachineGenerator { get; set; }
			public MachineGrowerVegetationHarvestable_JSONABLE MachineGrowerVegetationHarvestable { get; set; }
			public MachineDisintegrator_JSONABLE MachineDisintegrator { get; set; }
			public MachineRocketBackAndForthInterplanetaryExchange_JSONABLE MachineRocketBackAndForthInterplanetaryExchange { get; set; }
			public MachineRocketBackAndForthTrade_JSONABLE MachineRocketBackAndForthTrade { get; set; }
		}

		public class MachineAutoCrafter_JSONABLE {
			public MachineAutoCrafter_JSONABLE(MachineAutoCrafter script) {
				this.range = script.range;
				this.craftEveryXSec = script.craftEveryXSec;
			}
			public float range;
			public float craftEveryXSec;
		}
		public class MachineGenerator_JSONABLE {
			public MachineGenerator_JSONABLE(MachineGenerator script) {
				this.spawnEveryXSec = script.spawnEveryXSec;
				this.groupDatas = script.groupDatas.Select(e => e.id).ToList();
				this.miningRays = script.miningRays;
				this.oreAllowedToMine = script.oreAllowedToMine.Select(e => e.ToString()).ToList(); // get names!!!
				this.setGroupsDataViaLinkedGroup = script.setGroupsDataViaLinkedGroup;
				this.updateMiningRaysWhenChangingTerraStage = script.updateMiningRaysWhenChangingTerraStage;
				this.terraStageNameInPlanetData = script.terraStageNameInPlanetData;
				this.groupDatasTerraStage = script.groupDatasTerraStage.Select(e => e.id).ToList();
				this.generateWhenVeinIsEmpty = script.generateWhenVeinIsEmpty;
				this.onlyUseFirstVeinDetected = script.onlyUseFirstVeinDetected;
				this._terraStage = script._terraStage;
				//this._speedMultiplier = script._speedMultiplier; // probably set on the fly??? TODO check that
				this._miningTerraStage = script._miningTerraStage;
			}
			public int spawnEveryXSec;
			public List<string> groupDatas;
			public int miningRays;
			public List<string> oreAllowedToMine;
			public bool setGroupsDataViaLinkedGroup;
			public bool updateMiningRaysWhenChangingTerraStage;
			public string terraStageNameInPlanetData;
			public List<string> groupDatasTerraStage;
			public bool generateWhenVeinIsEmpty;
			public bool onlyUseFirstVeinDetected;
			public TerraformStage _terraStage;
			// public float _speedMultiplier = 1f;
			public TerraformStage _miningTerraStage;
		}
		public class MachineGrowerBase_JSONABLE {
			public MachineGrowerBase_JSONABLE(MachineGrowerBase script) {
				this.growSpeed = script.growSpeed;
			}
			public float growSpeed;
		}
		public class MachineGrowerVegetationBase_JSONABLE : MachineGrowerBase_JSONABLE {
			public MachineGrowerVegetationBase_JSONABLE(MachineGrowerVegetationBase script) : base(script) {
				this.growthUpdateInterval = script.growthUpdateInterval;
				this.minRadius = script.minRadius;
				this.radius = script.radius;
				this.spawnOnThis = script.spawnOnThis?.name;
				this.allowedLayers = script.allowedLayers;
				this.thingsToGrow = script.thingsToGrow.Select(e => e.name).ToList();
				this.modelToOverride = script.modelToOverride?.name;
				this.downValue = script.downValue;
				this.minGrowth = script.minGrowth;
			}
			public float growthUpdateInterval = 1f;
			public float minRadius;
			public float radius;
			public string spawnOnThis;
			public int allowedLayers = -1;
			public List<string> thingsToGrow;
			public string modelToOverride;
			public float downValue = 0.1f;
			public float minGrowth;
		}
		public class MachineGrowerVegetationHarvestable_JSONABLE : MachineGrowerVegetationBase_JSONABLE {
			public MachineGrowerVegetationHarvestable_JSONABLE(MachineGrowerVegetationHarvestable script) : base(script) {

			}
		}

		public class MachineDisintegrator_JSONABLE {
			public MachineDisintegrator_JSONABLE(MachineDisintegrator script) {
				this.breakEveryXSec = script.breakEveryXSec;
				this.giveXIngredientsBack = script.giveXIngredientsBack;
			}
			public int breakEveryXSec;
			public int giveXIngredientsBack;
		}

		public class MachineOptimizer_JSONABLE {
			public MachineOptimizer_JSONABLE(MachineOptimizer script) {
				this.range = script.range;
				this.maxWorldObjectPerFuse = script.maxWorldObjectPerFuse;
			}
			public float range = 15f;
			public int maxWorldObjectPerFuse = 5;
		}

		public class MachineRocketBackAndForth_JSONABLE {
			public MachineRocketBackAndForth_JSONABLE(MachineRocketBackAndForth script) {
				this.updateGrowthEvery = script.updateGrowthEvery;
				this._hideAfter = script._hideAfter;
				this._yAxisOfSky = script._yAxisOfSky;
			}
			public float updateGrowthEvery;
			private float _hideAfter = 40f;
			private float _yAxisOfSky = 500f;
		}
		public class MachineRocketBackAndForthInterplanetaryExchange_JSONABLE : MachineRocketBackAndForth_JSONABLE {
			public MachineRocketBackAndForthInterplanetaryExchange_JSONABLE(MachineRocketBackAndForthInterplanetaryExchange script) : base(script) {

			}
		}
		public class MachineRocketBackAndForthTrade_JSONABLE : MachineRocketBackAndForth_JSONABLE {
			public MachineRocketBackAndForthTrade_JSONABLE(MachineRocketBackAndForthTrade script) : base(script) {

			}
		}

		public class ActionGroupSelector_JSONABLE {
			public ActionGroupSelector_JSONABLE(ActionGroupSelector script) {
				this.isAutoCrafter = script.isAutoCrafter;
				this.isOreExtractor = script.isOreExtractor;
				this.isInterplanetaryDepot = script.isInterplanetaryDepot;
				this.oreList = script.oreList.Select(e => e.id).ToList();
				this._textHoverId = script._textHoverId;
				this._gamepadHint = script._gamepadHint;
			}
			public bool isAutoCrafter;
			public bool isOreExtractor;
			public bool isInterplanetaryDepot;
			public List<string> oreList;
			public string _textHoverId;
			public string _gamepadHint;
		}


		[HarmonyPrefix]
		[HarmonyPriority(Priority.First)]
		[HarmonyPatch(typeof(StaticDataHandler), nameof(StaticDataHandler.LoadStaticData))]
		static void StaticDataHandler_LoadStaticData(List<GroupData> ___groupsData) {
			if (!config_enabled.Value) { return; }
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

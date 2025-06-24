// Copyright 2025-2025 Nicolas Schäfer & Contributors
// Licensed under Apache License, Version 2.0

using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using SpaceCraft;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Nicki0.ItemMoreFuses {
	[BepInPlugin("Nicki0.theplanetcraftermods.ItemMoreFuses", "(Item) More Fuses", PluginInfo.PLUGIN_VERSION)]
	public class Plugin : BaseUnityPlugin {
		class ItemConfig {
			public string gId;
			public int count;
			public int recipeQuantity;
			public int baseEfficiencyMultiplierValue;
			public ItemConfig(string pGId) {
				gId = pGId;
			}
		}
		class FuseItemConfig : ItemConfig {
			public FuseItemConfig(string pGId) : base(pGId) {
				base.count = 5;
				base.recipeQuantity = 9;
				base.baseEfficiencyMultiplierValue = 10;
			}
		}
		class RocketItemConfig : ItemConfig {
			public RocketItemConfig(string pGId) : base(pGId) {
				base.count = 2;
				base.recipeQuantity = 0;
				base.baseEfficiencyMultiplierValue = 1000;
			}
		}
		//static readonly List<string> fuses = new List<string> {"FuseProduction2","FuseProduction3","FuseProduction4","FuseProduction5","FuseEnergy2","FuseEnergy3","FuseEnergy4","FuseEnergy5","FuseOxygen2","FuseOxygen3","FuseOxygen4","FuseOxygen5","FuseHeat2","FuseHeat3","FuseHeat4","FuseHeat5","FusePressure2","FusePressure3","FusePressure4","FusePressure5","FusePlants2","FusePlants3","FusePlants4","FusePlants5", "FuseTradeRocketsSpeed2", "FuseTradeRocketsSpeed3", "FuseTradeRocketsSpeed4", "FuseTradeRocketsSpeed5", "FuseGrowth2", "FuseGrowth3", "FuseGrowth4", "FuseGrowth5", "FuseInsects2", "FuseInsects3", "FuseInsects4", "FuseInsects5", "FuseAnimals2", "FuseAnimals3", "FuseAnimals4", "FuseAnimals5"};
		static readonly List<ItemConfig> fuseType = new List<ItemConfig> {
			new FuseItemConfig("FuseProduction"),
			new FuseItemConfig("FuseEnergy"),
			new FuseItemConfig("FuseOxygen"),
			new FuseItemConfig("FuseHeat"),
			new FuseItemConfig("FusePressure"),
			new FuseItemConfig("FusePlants"),
			new FuseItemConfig("FuseTradeRocketsSpeed"),
			new FuseItemConfig("FuseGrowth"),
			new FuseItemConfig("FuseInsects"),
			new FuseItemConfig("FuseAnimals") { count = 7 }
		};
		static readonly List<ItemConfig> rocketType = new List<ItemConfig> {
			new RocketItemConfig("RocketOxygen1"),
			new RocketItemConfig("RocketHeat1"),
			new RocketItemConfig("RocketHeat2"),
			new RocketItemConfig("RocketPressure1"),
			new RocketItemConfig("RocketPressure2"),
			new RocketItemConfig("RocketBiomass1"),
			new RocketItemConfig("RocketBiomass2"),
			new RocketItemConfig("RocketInsects1"),
			new RocketItemConfig("RocketInsects2"),
			new RocketItemConfig("RocketAnimals1")
		};



		static readonly string prefix = "Nicki0ModItem_";
		static readonly string prefixFuse = prefix + "Fuse-";
		static readonly string prefixRocket = prefix + "Rocket-";

		static string dir;

		static ManualLogSource log;

		public void Awake() {
			Logger.LogInfo($"Plugin is enabled.");

			log = Logger;

			Assembly me = Assembly.GetExecutingAssembly();
			dir = Path.GetDirectoryName(me.Location);

			Harmony.CreateAndPatchAll(typeof(Plugin));

			Logger.LogInfo("Plugin is loaded!");
		}

		static string GetNameFuse(string pName, int pLevel, bool addPrefix = true) {
			return ((pLevel <= 1 || !addPrefix ? "" : prefixFuse) + pName + pLevel);
		}

		static GroupDataItem GetFuseGroupDataItem(List<GroupData> ___groupsData, ItemConfig itemConfig, int pLevel) {
			// Extract infos from item name strings
			string fuseName = itemConfig.gId;
			int fuseId = pLevel;



			// Create / Config Item
			GroupDataItem groupDataItem = (GroupDataItem)Instantiate(___groupsData.Find((GroupData data) => data.id == GetNameFuse(fuseName, 1)));
			groupDataItem.name = GetNameFuse(fuseName, fuseId);
			groupDataItem.id = GetNameFuse(fuseName, fuseId);

			// Apply Multiplier dependent of type of effect
			int efficiencyMultiplier = (int)Math.Pow(itemConfig.baseEfficiencyMultiplierValue, fuseId - 1);
			if (fuseName.Contains("Production") || fuseName.Contains("Trade") || fuseName.Contains("Growth")) {
				groupDataItem.value *= efficiencyMultiplier;
			} else if (fuseName.Contains("Energy")) {
				groupDataItem.unitMultiplierEnergy *= efficiencyMultiplier;
			} else if (fuseName.Contains("Oxygen")) {
				groupDataItem.unitMultiplierOxygen *= efficiencyMultiplier;
			} else if (fuseName.Contains("Heat")) {
				groupDataItem.unitMultiplierHeat *= efficiencyMultiplier;
			} else if (fuseName.Contains("Pressure")) {
				groupDataItem.unitMultiplierPressure *= efficiencyMultiplier;
			} else if (fuseName.Contains("Plants")) {
				groupDataItem.unitMultiplierPlants *= efficiencyMultiplier;
			} else if (fuseName.Contains("Insect")) {
				groupDataItem.unitMultiplierInsects *= efficiencyMultiplier;
			} else if (fuseName.Contains("Animal")) {
				groupDataItem.unitMultiplierAnimals *= efficiencyMultiplier;
			}

			groupDataItem.terraformStageUnlock = null;
			groupDataItem.unlockingWorldUnit = DataConfig.WorldUnitType.Terraformation;
			groupDataItem.unlockingValue = (float)Math.Pow(1000, fuseId + 1); // T2: GTi, T3: TTi, T4: PTi, T5: ETi, T6: ZTi
			groupDataItem.tradeValue *= (int)Math.Pow(itemConfig.recipeQuantity, fuseId - 1);
			groupDataItem.tradeCategory = ((groupDataItem.tradeValue > 0) && (fuseId >= 5) && (groupDataItem.tradeCategory == DataConfig.TradeCategory.Null || groupDataItem.tradeCategory == DataConfig.TradeCategory.tier1)) ? DataConfig.TradeCategory.tier1 : DataConfig.TradeCategory.Null;
			if (fuseId <= 3 && !groupDataItem.craftableInList.Contains(DataConfig.CraftableIn.CraftStationT3)) groupDataItem.craftableInList.Add(DataConfig.CraftableIn.CraftStationT3);
			if (!groupDataItem.craftableInList.Contains(DataConfig.CraftableIn.CraftQuartzT1)) groupDataItem.craftableInList.Add(DataConfig.CraftableIn.CraftQuartzT1);
			groupDataItem.recipeIngredients = new List<GroupDataItem> { };
			for (int i = 0; i < itemConfig.recipeQuantity; i++) {
				groupDataItem.recipeIngredients.Add(Instantiate((GroupDataItem)___groupsData.Find((GroupData data) => data.id == GetNameFuse(fuseName, fuseId - 1))));
			}

			groupDataItem.associatedGameObject = Instantiate(groupDataItem.associatedGameObject);
			groupDataItem.associatedGameObject.name = GetNameFuse(fuseName, fuseId, addPrefix: false);


			string iconPath = Path.Combine(dir, GetNameFuse(fuseName, fuseId, addPrefix: false) + ".png");
			if (File.Exists(iconPath)) {
				byte[] array = File.ReadAllBytes(iconPath);
				Texture2D texture2D = new Texture2D(1, 1);
				texture2D.LoadImage(array);
				groupDataItem.icon = Sprite.Create(texture2D, new Rect(0f, 0f, (float)texture2D.width, (float)texture2D.height), new Vector2(0f, 0f));
			}

			return groupDataItem;
		}

		static string GetNameRocket(string pName, int pLevel, bool addPrefix = true) {
			return ((pLevel <= 1 || !addPrefix ? "" : prefixRocket) + pName + (pLevel <= 1 ? "" : ("_" + pLevel)));
		}
		static GroupDataItem GetRocketGroupDataItem(List<GroupData> ___groupsData, ItemConfig itemConfig, int pLevel) {
			// Extract infos from item name strings
			string rocketName = itemConfig.gId;
			int rocketLvl = pLevel;



			// Create / Config Item
			GroupDataItem groupDataItem = (GroupDataItem)Instantiate(___groupsData.Find((GroupData data) => data.id == GetNameRocket(rocketName, 1)));
			groupDataItem.name = GetNameRocket(rocketName, rocketLvl);
			groupDataItem.id = GetNameRocket(rocketName, rocketLvl);

			// Apply Multiplier dependent of type of effect
			int efficiencyMultiplier = (int)Math.Pow(itemConfig.baseEfficiencyMultiplierValue, rocketLvl - 1);

			groupDataItem.unitMultiplierOxygen *= efficiencyMultiplier;
			groupDataItem.unitMultiplierHeat *= efficiencyMultiplier;
			groupDataItem.unitMultiplierPressure *= efficiencyMultiplier;
			groupDataItem.unitMultiplierPlants *= efficiencyMultiplier;
			groupDataItem.unitMultiplierInsects *= efficiencyMultiplier;
			groupDataItem.unitMultiplierAnimals *= efficiencyMultiplier;


			//groupDataItem.terraformStageUnlock = null;
			//groupDataItem.unlockingWorldUnit = DataConfig.WorldUnitType.Terraformation;
			//groupDataItem.unlockingValue = (float) Math.Pow(1000, rocketLvl + 1); // T2: GTi, T3: TTi, T4: PTi, T5: ETi
			//groupDataItem.tradeValue *= (int) Math.Pow(9,rocketLvl - 1);
			//groupDataItem.tradeCategory = ((groupDataItem.tradeValue > 0) && (rocketLvl >= 5) && (groupDataItem.tradeCategory == DataConfig.TradeCategory.Null || groupDataItem.tradeCategory == DataConfig.TradeCategory.tier1)) ? DataConfig.TradeCategory.tier1 : DataConfig.TradeCategory.Null;

			groupDataItem.craftedInWorld = false;
			groupDataItem.craftableInList.Clear();
			groupDataItem.recipeIngredients = new List<GroupDataItem> { };
			/*GroupDataItem lastRocket = Instantiate((GroupDataItem)___groupsData.Find((GroupData data) => data.id == GetNameRocket(rocketName, rocketLvl - 1)));
			for (int i = 0; i < quantity; i++) {
				groupDataItem.recipeIngredients.Add(lastRocket);
			}*/

			//groupDataItem.associatedGameObject = Instantiate(groupDataItem.associatedGameObject);
			//groupDataItem.associatedGameObject.name = GetNameRocket(rocketName, rocketLvl, addPrefix: false);


			string iconPath = Path.Combine(dir, GetNameRocket(rocketName, rocketLvl, addPrefix: false) + ".png");
			if (File.Exists(iconPath)) {
				byte[] array = File.ReadAllBytes(iconPath);
				Texture2D texture2D = new Texture2D(1, 1);
				texture2D.LoadImage(array);
				groupDataItem.icon = Sprite.Create(texture2D, new Rect(0f, 0f, (float)texture2D.width, (float)texture2D.height), new Vector2(0f, 0f));
			}

			return groupDataItem;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(UiWindowRockets), nameof(UiWindowRockets.OnOpen))]
		private static void UiWindowRockets_OnOpen(List<GroupData> ___rocketsGenerationGroups) {
			foreach (GroupData rocket in addedRocketGroupData)
				if (!___rocketsGenerationGroups.Contains(rocket)) ___rocketsGenerationGroups.Add(rocket);
		}

		private static List<GroupData> addedRocketGroupData = new List<GroupData>();

		[HarmonyPrefix]
		[HarmonyPatch(typeof(StaticDataHandler), "LoadStaticData")]
		private static void StaticDataHandler_LoadStaticData(List<GroupData> ___groupsData) {
			for (var i = ___groupsData.Count - 1; i >= 0; i--) {
				var groupData = ___groupsData[i];
				if (groupData == null || (groupData.associatedGameObject == null && groupData.id.StartsWith(prefixFuse))) {
					___groupsData.RemoveAt(i);
				}
			}

			var existingGroups = ___groupsData.Select(gd => gd.id).ToHashSet();

			foreach (ItemConfig fuseConfig in fuseType) {
				string text = fuseConfig.gId;
				for (int lvl = 2; lvl <= fuseConfig.count; lvl++) {
					var fuseGroupDataItem = GetFuseGroupDataItem(___groupsData, fuseConfig, lvl);
					if (!existingGroups.Contains(fuseGroupDataItem.id)) {
						___groupsData.Add(fuseGroupDataItem);
						log.LogInfo("Added " + GetNameFuse(text, lvl));
					}
				}
			}
			foreach (ItemConfig rocketConfig in rocketType) {
				string text = rocketConfig.gId;
				for (int lvl = 2; lvl <= rocketConfig.count; lvl++) {
					GroupDataItem rocketGroupDataItem = GetRocketGroupDataItem(___groupsData, rocketConfig, lvl);
					if (!existingGroups.Contains(rocketGroupDataItem.id)) {
						addedRocketGroupData.Add(rocketGroupDataItem);
						___groupsData.Add(rocketGroupDataItem);
						log.LogInfo("Added " + GetNameRocket(text, lvl));
					}
				}
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(Localization), "LoadLocalization")]
		private static void Localization_LoadLocalization(Dictionary<string, Dictionary<string, string>> ___localizationDictionary, string ___currentLangage) {
			if (string.IsNullOrEmpty(___currentLangage)) return;
			if (___localizationDictionary.TryGetValue(/*"english"*/___currentLangage, out var dictionary)) {
				if (dictionary.ContainsKey("GROUP_NAME_Nicki0ModItem_Fuse-FuseProduction2")) {
					return;
				}

				foreach (ItemConfig fuseConfig in fuseType) {
					string text = fuseConfig.gId;
					for (int lvl = 2; lvl <= fuseConfig.count; lvl++) {
						dictionary["GROUP_NAME_" + GetNameFuse(text, lvl)] = dictionary[GameConfig.localizationGroupNameId + text + "1"] + " T" + lvl;//text.Substring(4) + " multiplier fuse T" + lvl;
						dictionary["GROUP_DESC_" + GetNameFuse(text, lvl)] = ((int)Math.Pow(fuseConfig.baseEfficiencyMultiplierValue, lvl - 1)) + " times as efficient. (" + ((int)Math.Pow(fuseConfig.recipeQuantity, lvl - 1)) + ")";
					}
				}
				foreach (ItemConfig rocketConfig in rocketType) {
					string text = rocketConfig.gId;
					for (int lvl = 2; lvl <= rocketConfig.count; lvl++) {
						dictionary["GROUP_NAME_" + GetNameRocket(text, lvl)] = dictionary[GameConfig.localizationGroupNameId + text] + " Compressed (" + ((int)Math.Pow(rocketConfig.baseEfficiencyMultiplierValue, lvl - 1)) + ")";//text.Substring(4) + " multiplier fuse T" + lvl;
						dictionary["GROUP_DESC_" + GetNameRocket(text, lvl)] = ((int)Math.Pow(rocketConfig.baseEfficiencyMultiplierValue, lvl - 1)) + " times as efficient";
					}
				}
			}
		}
	}
}

// Copyright 2025-2025 Nicolas Schäfer & Contributors
// Licensed under Apache License, Version 2.0

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SpaceCraft;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;


namespace Nicki0.QoLAutoLogistics {

	[BepInPlugin("Nicki0.theplanetcraftermods.QoLAutoLogistics", "(QoL) Auto-Logistics", PluginInfo.PLUGIN_VERSION)]
	public class Plugin : BaseUnityPlugin {

		/*	TODO
		 *	- Localization for all texts
		 *	
		 *	- Multiplayer compatibility
		 *	
		 *	Info: Group -> GameObject -> get component ActionOpenable
		 *	
		 *	
		 *	
		 *	TO TEST
		 *	
		 *	
		 *	
		 *	BUGS
		 *	
		 */

		static ManualLogSource log;

		static AccessTools.FieldRef<CanvasPinedRecipes, List<Group>> field_CanvasPinedRecipes_groupsAdded;
		static AccessTools.FieldRef<CanvasPinedRecipes, List<InformationDisplayer>> field_CanvasPinedRecipes_informationDisplayers;
		static AccessTools.FieldRef<UiWindowContainer, Inventory> field_UiWindowContainer__inventoryRight;
		static AccessTools.FieldRef<PopupsHandler, List<PopupData>> field_PopupsHandler_popupsToPop;
		static AccessTools.FieldRef<Inventory, HashSet<Group>> field_Inventory__unauthorizedGroups;
		static FieldInfo field_WorldObjectText_proxy;
		static ConstructorInfo constructor_Group;
		static MethodInfo method_CanvasPinedRecipes_RemovePinedRecipeAtIndex;
		static MethodInfo method_MachineGenerator_SetMiningRayGeneration;

		public static ConfigEntry<bool> enableMod;
		public static ConfigEntry<bool> enableDebug;
		public static ConfigEntry<bool> enableNotification;
		public static ConfigEntry<bool> allowAnyValue;
		public static ConfigEntry<bool> enablePotentialBuggedFeatures;

		public static ConfigEntry<bool> clearOutputOnInputChange;
		public static ConfigEntry<bool> allowLongNames;
		public static ConfigEntry<string> logisticGroupSynonymesList;
		public static ConfigEntry<string> logisticGroupNamedLists;
		public static ConfigEntry<string> priorityNamesList;
		public static ConfigEntry<bool> enableGeneratorAddLogistics;
		public static ConfigEntry<bool> copyLogisticsPerGroup;
		public static ConfigEntry<Key> copyLogisticsKey;
		public static ConfigEntry<Key> pasteLogisticsKey;
		public static ConfigEntry<bool> updateSupplyAll;
		public static ConfigEntry<bool> logisticMenuIgnoreLockingConditions;
		public static ConfigEntry<string> logisticMenuAdditionalGroups;
		public static ConfigEntry<bool> deliveryDontDeliverFromProductionToDestructor;
		public static ConfigEntry<string> deliveryDontDeliverFromProductionToDestructorGroups;
		public static ConfigEntry<bool> deliveryDontDeliverSpawnedObjectsToDestructor;
		public static ConfigEntry<Key> addContainedItemsToLogisticsModifierKey;
		public static ConfigEntry<bool> addContainedItemsToLogisticsClearList;
		public static ConfigEntry<bool> setContainerNameWhenSelectingLogistics;

		private static readonly List<string> staticSynonymesList = new List<string>() {
			"Al:Aluminium",
			"Si:Silicon",
			"P:Phosphorus",
			"S:Sulfur",
			"Ti:Titanium",
			"Mg:Magnesium",
			"Fe:Iron",
			"Co:Cobalt",
			"Se:Selenium",
			"W:Minable-Tungsten",
			"Os:Osmium",
			"Ir:Iridium",
			"U:Uranim"
		};

		private static Sprite SpriteCopy;
		private static Sprite SpritePaste;

		private static System.Drawing.Font LogisticsFont;

		void Awake() {
			// Plugin startup logic
			log = Logger;

			// Transfer old config file
			String pathToConfigFolder = Paths.ConfigPath;
			if (File.Exists(pathToConfigFolder + "\\autoAddLogistic.cfg")) {
				File.Copy(pathToConfigFolder + "\\autoAddLogistic.cfg", pathToConfigFolder + "\\Nicki0.theplanetcraftermods.QoLAutoLogistics.cfg", true);
				File.Move(pathToConfigFolder + "\\autoAddLogistic.cfg", pathToConfigFolder + "\\autoAddLogistic.cfg_backup_" + DateTime.Now.ToString("yyyy-MM-ddThhmmss"));
				Config.Reload();
				log.LogInfo("Old config transfered");
			}

			field_CanvasPinedRecipes_groupsAdded = AccessTools.FieldRefAccess<CanvasPinedRecipes, List<Group>>("groupsAdded");
			field_CanvasPinedRecipes_informationDisplayers = AccessTools.FieldRefAccess<CanvasPinedRecipes, List<InformationDisplayer>>("informationDisplayers");
			field_UiWindowContainer__inventoryRight = AccessTools.FieldRefAccess<UiWindowContainer, Inventory>("_inventoryRight");
			field_PopupsHandler_popupsToPop = AccessTools.FieldRefAccess<PopupsHandler, List<PopupData>>("popupsToPop");
			field_Inventory__unauthorizedGroups = AccessTools.FieldRefAccess<Inventory, HashSet<Group>>("_unauthorizedGroups");
			field_WorldObjectText_proxy = AccessTools.Field(typeof(WorldObjectText), "_proxy");
			constructor_Group = AccessTools.DeclaredConstructor(typeof(Group), new Type[] { typeof(GroupData) });
			method_CanvasPinedRecipes_RemovePinedRecipeAtIndex = AccessTools.Method(typeof(CanvasPinedRecipes), "RemovePinnedRecipeAtIndex");
			method_MachineGenerator_SetMiningRayGeneration = AccessTools.Method(typeof(MachineGenerator), "SetMiningRayGeneration");


			enableMod = Config.Bind<bool>(".Config_General", "enableMod", true, "Enable mod");
			enableDebug = Config.Bind<bool>(".Config_General", "enableDebug", false, "Enable debug messages");
			enableNotification = Config.Bind<bool>(".Config_General", "enableNotification", true, "Send a notification if an item group wasn't found or the logistics settings are copied/pasted.");
			allowAnyValue = Config.Bind<bool>(".Config_General", "allowAnyValue", false, "Allows priority below lowest (-3) and above 5, demanding any item group/type and select unavailable items to extract. Those values are not officially supported by the game. Be carefull.");
			enablePotentialBuggedFeatures = Config.Bind<bool>(".Config_General", "enablePotentialBuggedFeatures", false, "Be careful! This enables features that could be incompatible with the game and should only be used if you have backups! This e.g. enables logistics on outdoor farms, but also on the bugged extraction rocket!");

			clearOutputOnInputChange = Config.Bind<bool>("Config_OreBreaker", "clearOutputOnInputChange", true, "Clear output-inventory supply list if other item is selected in ore crusher's, recycler T2's or Detoxification Machine's input-inventory's demand list. Only one item can be selected as demand input.");

			allowLongNames = Config.Bind<bool>("Config_LogisticsByText", "allowLongNames", false, "Allows 125 characters in container name to make setting logistics by text actually usefull. Long text will show outside of the text field.");
			logisticGroupSynonymesList = Config.Bind<string>("Config_LogisticsByText", "synonymes", "N2:NitrogenCapsule1,O2:OxygenCapsule1,CH4:MethanCapsule1,H2O:WaterBottle1,Water:WaterBottle1", "List of synonymes");//Fertilizer T1:Fertilizer1,Fertilizer T2:Fertilizer2,Fertilizer T3:Fertilizer3,Mutagen T1:Mutagen1,Mutagen T2:Mutagen2,Mutagen T3:Mutagen3,Mutagen T4:Mutagen4,Drone T1:Drone1,Drone T2:Drone2,Animal food T1:AnimalFood1,Animal food T2:AnimalFood2,Animal food T3:AnimalFood3,Rocket Engine T1:RocketReactor,Rocket Engine T2:RocketReactor2,Azurae:Butterfly1Larvae,Leani:Butterfly2Larvae,Fensea:Butterfly3Larvae,Galaxe:Butterfly4Larvae,Abstreus:Butterfly5Larvae,Empalio:Butterfly6Larvae,Penga:Butterfly7Larvae,Chevrone:Butterfly8Larvae,Aemel:Butterfly9Larvae,Liux:Butterfly10Larvae,Nere:Butterfly11Larvae,Lorpen:Butterfly12Larvae,Fiorente:Butterfly13Larvae,Alben:Butterfly14Larvae,Futura:Butterfly15Larvae,Imeo:Butterfly16Larvae,Serena:Butterfly17Larvae,Golden Butterfly:Butterfly18Larvae,Faleria:Butterfly19Larvae,Oesbe:Butterfly20Larvae,Provios:Fish1Eggs,Vilnus:Fish2Eggs,Gerrero:Fish3Eggs,Khrom:Fish4Eggs,Ulani:Fish5Eggs,Aelera:Fish6Eggs,Tegede:Fish7Eggs,Ecaru:Fish8Eggs,Buyu:Fish9Eggs,Tiloo:Fish10Eggs,Golden Fish:Fish11Eggs,Velkia:Fish12Eggs,Galbea:Fish13Eggs,Stabu:Fish14Eggs,Atabu:Fish15Eggs,Generic Frog:Frog1Eggs,Huli:Frog2Eggs,Felicianna:Frog3Eggs,Strabo:Frog4Eggs,Trajuu:Frog5Eggs,Aiolus:Frog6Eggs,Afae:Frog7Eggs,Cillus:Frog8Eggs,Amedo:Frog9Eggs,Kenjoss:Frog10Eggs,Lavaum:Frog11Eggs,Leglus:Frog12Eggs,Jumi:Frog13Eggs,Seren:Frog14Eggs,Acuzzi:Frog15Eggs,Golden Frog:FrogGoldEggs,Common:LarvaeBase1,Uncommon:LarvaeBase2,Rare:LarvaeBase3,Bee:Bee1Larvae,Lirma:Seed0,Shanga:Seed1,Pestera:Seed2,Nulna:Seed3,Tuska:Seed4,Orema:Seed5,Volnus:Seed6,Snepea:Seed7Humble,Brelea:Seed8Humble,Seleus:Seed9Humble,Furteo:Seed10Humble,Humblea:Seed11Humble,Golden Seed:SeedGold,Iterra:Tree0Seed,Linifolia:Tree1Seed,Aleatus:Tree2Seed,Cernea:Tree3Seed,Elegea:Tree4Seed,Humelora:Tree5Seed,Aemora:Tree6Seed,Pleom:Tree7Seed,Soleus:Tree8Seed,Shreox:Tree9Seed,Rosea:Tree10Seed,Lillia:Tree11Seed,Prunea:Tree12Seed,Ruberu:Tree13Seed,Malissea:Tree14Seed,Redwo:Tree15Seed,Pamelia:Tree16Seed
			logisticGroupNamedLists = Config.Bind<string>("Config_LogisticsByText", "lists", "quartz:quartz>,gas:N2+O2+CH4,basic ores:Fe+Si+Co+Mg+Ti+S,t1 ores:Al+Ir+Se,t2 ores:U+Os+Super Alloy+Zeolite+Obsidian+Blazar Quartz,fuses:multiplier fuse>", "List of groups. Put the list name into an inventory's text field to demand all item groups listed. Format: [name]:[group1]+[group2]...");
			priorityNamesList = Config.Bind<string>("Config_LogisticsByText", "priorityGroups", "override:3,storage:2,s:2,backup:1,AC:0,overflow:-1,tradeAC:-1,rocket:-2,genOverflow:-2,trash:-3", "Names for priorities");//override:5,storage:4,s:4,backup:3,AC:2,overflow:1,pulsar:1,rocket:0,genOverflow:-2,trash:-3

			enableGeneratorAddLogistics = Config.Bind<bool>("Config_GeneratorLogistics", "enableAddLogistics", true, "Enable automatic supply of generated items in machines when build. For example, Bee hives automatically supply honey and bee larva. Shredders are automatically set to the lowest priority.");

			copyLogisticsPerGroup = Config.Bind<bool>("Config_Copy", "copyLogisticsPerGroup", true, "Not recommended to disable. Copy unique logistic menu settings per group. Disabling might lead to unexpected results.");
			copyLogisticsKey = Config.Bind<Key>("Config_Copy", "keyCopy", Key.C, "Hold this key while closing the logistics menu to copy the inventory logistics settings.");
			pasteLogisticsKey = Config.Bind<Key>("Config_Copy", "keyPaste", Key.V, "Hold this key while opening an inventory to paste the inventory logistics settings.");

			updateSupplyAll = Config.Bind<bool>("Config_UpdateSupplyAll", "updateSupplyAll", false, "Will update the 'supply all' item groups when new items are available, e.g. after an update. Only logistic settings that don't demand any group are updated.");

			logisticMenuIgnoreLockingConditions = Config.Bind<bool>("Config_ShowLogisticsItemGroups", "ignoreLockingConditions", false, "Ignore lock condition and show groups from additionalGroups.");
			logisticMenuAdditionalGroups = Config.Bind<string>("Config_ShowLogisticsItemGroups", "additionalGroups", "CookCocoaSeed,CookWheatSeed", "Additional item groups to show in the logistics menu if ignoreLockingConditions=true. allowAnyValue is ignored by this setting. Requires restart to apply.");

			deliveryDontDeliverFromProductionToDestructor = Config.Bind<bool>("Config_DroneDelivery", "dontDeliverFromProductionToShredder", false, "Prevent that drones deliver from [dontDeliverToShredderFromMachines] to shredders (Example: Iron from T3 Ore Extractors isn't delivered to shredders demanding Iron).");
			deliveryDontDeliverFromProductionToDestructorGroups = Config.Bind<string>("Config_DroneDelivery", "dontDeliverToShredderFromMachines", "OreExtractor3,HarvestingRobot1,AutoCrafter1,Incubator1,GeneticManipulator1,PlanetaryDeliveryDepot1,InterplanetaryExchangePlatform1,SilkGenerator,TradePlatform1,WaterCollector1,WaterCollector2,Biodome2,GasExtractor2", "Machines that drones won't deliver items from to shredders.");
			deliveryDontDeliverSpawnedObjectsToDestructor = Config.Bind<bool>("Config_DroneDelivery", "dontDeliverSpawnedObjectsToShredder", false, "Prevent that drones deliver fruits (and any other item that drones collect from the floor) to shredders.");

			addContainedItemsToLogisticsModifierKey = Config.Bind<Key>("Config_AddContainedGroups", "addContainedGroupsModifierKey", Key.LeftCtrl, "Hold this key and press the supply or demand selection button in the logistics selector to supply or demand all item groups contained in the inventory.");
			addContainedItemsToLogisticsClearList = Config.Bind<bool>("Config_AddContainedGroups", "clearListWhenAddingContainedGroups", true, "Clear supply/demand list when item groups that are contained in the inventory are added to the logistic settings.");

			setContainerNameWhenSelectingLogistics = Config.Bind<bool>("Config_TextByLogistics", "enableSetDemandAsText", true, "Set the container name to the demanded groups");

			SpriteCopy = IconData.CreateSprite(IconData.ImageCopy, IconData.ImageCopy_Width, IconData.ImageCopy_Height);
			SpritePaste = IconData.CreateSprite(IconData.ImagePaste, IconData.ImagePaste_Width, IconData.ImagePaste_Height);

			LogisticsFont = new System.Drawing.Font(System.Drawing.SystemFonts.DefaultFont.OriginalFontName, 28);

			Harmony.CreateAndPatchAll(typeof(Plugin));
			Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
		}

		public static void OnModConfigChanged(ConfigEntryBase _) {
			deliveryDontDeliverFromList_StableHashCodes = null;

			ReloadConfig();
		}
		private static void ReloadConfig() {
		}

		private static bool pasteLogisticsIntoNewlyBuild = false;
		public void Update() {
			if (!enableMod.Value) { return; }

			if (Keyboard.current[pasteLogisticsKey.Value].wasPressedThisFrame && Keyboard.current.ctrlKey.IsPressed() && Keyboard.current.shiftKey.IsPressed()) {
				pasteLogisticsIntoNewlyBuild = !pasteLogisticsIntoNewlyBuild;
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(BaseHudHandler), "UpdateHud")]
		private static void Post_BaseHudHandler_UpdateHud(BaseHudHandler __instance) {
			if (!enableMod.Value) { return; }

			if (pasteLogisticsIntoNewlyBuild) {
				__instance.textPositionDecoration.text += " - Paste Logistics";
			}
		}

		// --- Set Logistics on e.g. Ore Extractors --->
		[HarmonyPrefix]
		[HarmonyPatch(typeof(UiWindowGroupSelector), "OnGroupSelected")]
		public static void UiWindowGroupSelector_OnGroupSelected(ref Inventory ____inventoryRight, Group group) {
			if (!enableMod.Value) return;

			____inventoryRight.GetLogisticEntity().ClearSupplyGroups();
			if (group != null) {
				____inventoryRight.GetLogisticEntity().AddSupplyGroup(group);
			}
			if (Keyboard.current[copyLogisticsKey.Value].isPressed) {
				CreateCopyLogistics(____inventoryRight.GetLogisticEntity(), ____inventoryRight, group);
			}
			InventoriesHandler.Instance.UpdateLogisticEntity(____inventoryRight);
			return;
		}
		// <--- Set Logistics on e.g. Ore Extractors ---

		// --- Set Logistics on Ore Crusher --->
		[HarmonyPrefix]
		[HarmonyPatch(typeof(LogisticSelector), "OnGroupDemandSelected")]
		public static void LogisticSelector_OnGroupDemandSelected(Group group, Inventory ____inventory) {
			if (!enableMod.Value) return;

			WorldObject wo = WorldObjectsHandler.Instance.GetWorldObjectForInventory(____inventory);
			if (wo == null) {
				if (enableDebug.Value) log.LogWarning("WorldObject for Inventory " + ____inventory.GetId() + " not found!");
				return;
			}

			string woId = wo.GetGroup().GetId();
			if (woId.Contains("OreBreaker") || woId.Contains("RecyclingMachine2") || woId.Contains("DetoxificationMachine")) {
				Inventory outputInventory = InventoriesHandler.Instance.GetInventoryById(wo.GetSecondaryInventoriesId().First());

				LogisticEntity logisticEntity = outputInventory.GetLogisticEntity();

				if (clearOutputOnInputChange.Value) {
					____inventory.GetLogisticEntity().ClearDemandGroups();
					logisticEntity.ClearSupplyGroups();
				}

				foreach (Group ingredients in group.GetRecipe().GetIngredientsGroupInRecipe()) {
					logisticEntity.AddSupplyGroup(ingredients);
				}
				InventoriesHandler.Instance.UpdateLogisticEntity(outputInventory);
			}
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(LogisticSelector), nameof(LogisticSelector.OnClearDemand))]
		public static void LogisticSelector_OnClearDemand(Inventory ____inventory) {
			if (!enableMod.Value) return;

			ClearDemand(____inventory);
		}
		[HarmonyPrefix]
		[HarmonyPatch(typeof(LogisticSelector), "OnRemoveDemandGroup")]
		public static void LogisticSelector_OnRemoveDemandGroup(Inventory ____inventory) {
			if (!enableMod.Value) return;

			ClearDemand(____inventory);
		}
		private static void ClearDemand(Inventory inv) {
			if (!enableMod.Value) return;
			if (!clearOutputOnInputChange.Value) return;

			WorldObject wo = WorldObjectsHandler.Instance.GetWorldObjectForInventory(inv);

			if (wo == null) {
				if (enableDebug.Value) log.LogWarning("WorldObject for Inventory " + inv.GetId() + " not found!");
				return;
			}
			string woId = wo.GetGroup().id;
			if (woId.Contains("OreBreaker") || woId.Contains("RecyclingMachine2") || woId.Contains("DetoxificationMachine")) {
				Inventory outputInventory = InventoriesHandler.Instance.GetInventoryById(wo.GetSecondaryInventoriesId().First());
				outputInventory.GetLogisticEntity().ClearSupplyGroups();

				InventoriesHandler.Instance.UpdateLogisticEntity(outputInventory);
			}
		}
		// <--- Set Logistics on Ore Crusher ---

		// --- Set Logistics on generating machines e.g. bee hive --->
		[HarmonyPostfix]
		[HarmonyPatch(typeof(MachineGenerator), nameof(MachineGenerator.SetGeneratorInventory))]
		public static void MachineGenerator_SetGeneratorInventory(ref Inventory inventory, MachineGenerator __instance) {
			if (!enableMod.Value) return;
			if (!enableGeneratorAddLogistics.Value) return;

			if (!WorldObjectsHandler.Instance.GetHasInitiatedAllObjects()) return;
			if (__instance.setGroupsDataViaLinkedGroup) return; // e.g. OreExtractor3, GasExtractor2, HarvestingRobot1

			method_MachineGenerator_SetMiningRayGeneration.Invoke(__instance, []);//__instance.SetMiningRayGeneration();
			List<GroupData> list = new List<GroupData>(__instance.groupDatas);
			if (__instance.groupDatasTerraStage != null) list.AddRange(__instance.groupDatasTerraStage);

			foreach (GroupData groupData in list) {
				inventory.GetLogisticEntity().AddSupplyGroup(GroupsHandler.GetGroupViaId(groupData.id));
			}

			if (list.Count > 0) {// means that supply groups were changed
				InventoriesHandler.Instance.UpdateLogisticEntity(inventory);
			}
		}
		// <--- Set Logistics on generating machines e.g. bee hive ---

		// --- Set Logistic priority on shredder to -3 on build --->
		[HarmonyPrefix]
		[HarmonyPatch(typeof(MachineDestructInventoryIfFull), nameof(MachineDestructInventoryIfFull.SetDestructInventoryInventory))]
		public static void MachineDestructInventoryIfFull_SetDestructInventoryInventory(ref Inventory inventory) {
			if (!enableMod.Value) return;
			if (!enableGeneratorAddLogistics.Value) return;

			if (!WorldObjectsHandler.Instance.GetHasInitiatedAllObjects()) return;

			inventory.GetLogisticEntity().SetPriority(-3);

			InventoriesHandler.Instance.UpdateLogisticEntity(inventory);
		}
		// <--- Set Logistic priority on shredder to -3 on build ---

		// --- Update supply all --->
		private static DateTime lastUpdatedTimeStamp = new DateTime();
		[HarmonyPrefix]
		[HarmonyPatch(typeof(InventoryAssociated), nameof(InventoryAssociated.SetInventory))]
		public static void InventoryAssociated_SetInventory(Inventory inventory) {
			if (!enableMod.Value) return;

			if (WorldObjectsHandler.Instance.GetHasInitiatedAllObjects()) return;
			if (!updateSupplyAll.Value) return;

			if (inventory == null) return;

			LogisticEntity logisticEntity = inventory.GetLogisticEntity();
			if (logisticEntity.GetSupplyGroups().Count > 140 && logisticEntity.GetDemandGroups().Count == 0) {
				bool hasAddedGroup = false;

				HashSet<Group> logisticSupplyGroups = logisticEntity.GetSupplyGroups();

				foreach (Group g in Managers.GetManager<LogisticManager>().GetItemsToDisplayForLogistics(true)) {
					if (logisticSupplyGroups.Contains(g)) continue;
					hasAddedGroup = true;
					logisticEntity.AddSupplyGroup(g);
				}

				if (hasAddedGroup) {
					if (enableDebug.Value) log.LogInfo("supply all for inventory: " + inventory.GetId());

					if (enableNotification.Value) {
						if ((DateTime.Now - lastUpdatedTimeStamp).TotalSeconds > 30) {
							lastUpdatedTimeStamp = DateTime.Now;
							SendNotification("Updated all 'Supply all' logistic settings", timeShown: 5f); // [missing translation]
						}
					}
				}
			}
		}
		// <--- Update supply all ---

		// --- Show Logistic Menu Items --->
		[HarmonyPrefix]
		[HarmonyPatch(typeof(LogisticManager), nameof(LogisticManager.GetItemsToDisplayForLogistics))]
		public static void Prefix_LogisticManager_GetItemsToDisplayForLogistics(ref bool ignoreLockingConditions) {
			if (!enableMod.Value) return;

			if (logisticMenuIgnoreLockingConditions.Value) {
				ignoreLockingConditions = true;
			}
		}
		[HarmonyPrefix]
		[HarmonyPatch(typeof(StaticDataHandler), "LoadStaticData")]
		private static void StaticDataHandler_LoadStaticData(List<GroupData> ___groupsData) {
			if (!enableMod.Value) return;

			if (!logisticMenuIgnoreLockingConditions.Value) return;

			List<string> allGroupsToShow = logisticMenuAdditionalGroups.Value.Split(',').Select(e => e.Trim()).ToList();
			foreach (GroupData gd in ___groupsData) {
				if (gd is GroupDataItem gdi && allGroupsToShow.Any(e => e == gd.id)) {
					gdi.displayInLogisticType = DataConfig.LogisticDisplayType.Display;
				}
			}
		}
		// <--- Show Logistic Menu Items ---

		// --- Add items in inventory to logistics --->
		[HarmonyPrefix]
		[HarmonyPatch(typeof(GroupSelector), nameof(GroupSelector.OnOpenList))]
		public static bool GroupSelector_OnOpenList(GroupSelector __instance) {
			if (!enableMod.Value) return true;

			if (Keyboard.current[addContainedItemsToLogisticsModifierKey.Value].isPressed) {
				if (__instance.groupSelectedEvent == null) { return true; }

				WindowsHandler windowsHandler = Managers.GetManager<WindowsHandler>();
				if (windowsHandler?.GetOpenedUi() != DataConfig.UiType.Container) { return true; }
				UiWindowContainer uiWindow = (UiWindowContainer)windowsHandler?.GetWindowViaUiId(DataConfig.UiType.Container);
				if (uiWindow == null) { return true; }
				Inventory containerInventory = field_UiWindowContainer__inventoryRight(uiWindow);
				if (containerInventory == null) { return true; }

				if (addContainedItemsToLogisticsClearList.Value) {
					if (__instance.name.StartsWith("GroupSelectorDemand", StringComparison.InvariantCulture)) {
						containerInventory.GetLogisticEntity().ClearDemandGroups();
						InventoriesHandler.Instance.UpdateLogisticEntity(containerInventory);
					} else if (__instance.name.StartsWith("GroupSelectorSupply", StringComparison.InvariantCulture)) {
						containerInventory.GetLogisticEntity().ClearSupplyGroups();
						InventoriesHandler.Instance.UpdateLogisticEntity(containerInventory);
					}
				}
				IEnumerable<Group> groupEnumerator = containerInventory.GetInsideWorldObjects().GroupBy(e => e.GetGroup().stableHashCode).Select(l => l.First()).Select(e => e.GetGroup());
				if (!allowAnyValue.Value) {
					groupEnumerator = groupEnumerator.Where(g => g.GetGroupData() is GroupDataItem).Where(g => ((GroupDataItem)g.GetGroupData()).displayInLogisticType == DataConfig.LogisticDisplayType.Display);
				}
				foreach (Group group in groupEnumerator) {
					__instance.groupDisplayer.SetGroupAndUpdateDisplay(group, false, true, false, false);
					__instance.groupSelectedEvent(group);
				}
				SetTextForInventory(containerInventory);
				InventoriesHandler.Instance.UpdateLogisticEntity(containerInventory); // although that should already happen in __instance.groupSelectedEvent(group)
				return false;
			}
			return true;
		}
		// <--- Add items in inventory to logistics ---

		// --- Add Name to logistic entities --->
		[HarmonyPrefix]
		[HarmonyPatch(typeof(UiWindowTextInput), nameof(UiWindowTextInput.OnClose))]
		public static void UiWindowTextInput_OnClose(ref WorldObjectText ___worldObjectText, ref TMP_InputField ___inputField) {
			if (!enableMod.Value) return;

			if (___worldObjectText == null) return;
			TextProxy textProxy = (TextProxy)field_WorldObjectText_proxy.GetValue(___worldObjectText);
			WorldObject worldObject = textProxy.GetComponent<WorldObjectAssociated>()?.GetWorldObject();
			if (worldObject == null) return;
			if (!worldObject.HasLinkedInventory()) return;
			Inventory inventory = InventoriesHandler.Instance.GetInventoryById(worldObject.GetLinkedInventoryId());
			SetLogisticsByName(ref inventory, ___inputField.text);
		}

		static List<Group> allGroups;
		private static readonly List<string> LogisticByName_CommentSymbols = new List<string>() { ".", "#", "//" };
		private static void SetLogisticsByName(ref Inventory pInventory, string pText) {
			allGroups = GroupsHandler.GetAllGroups();

			LogisticEntity logisticEntity = pInventory.GetLogisticEntity();
			string textModified = pText.Trim();
			if (string.IsNullOrEmpty(textModified)) {
				logisticEntity.ClearDemandGroups();
				logisticEntity.SetPriority(0);
			}

			// Ignore name (comment)
			foreach (string commentSymbol in LogisticByName_CommentSymbols) {
				if (textModified.StartsWith(commentSymbol)) return;
			}

			// Supply instead
			bool SupplyInstead = textModified.StartsWith("!");
			if (SupplyInstead) textModified = textModified.Substring(1);

			// Split id list and priority
			string[] textComponents = textModified.Split(new[] { '+', ':' });
			if (textComponents.Count() == 2) {
				textModified = textComponents[0].Trim();
				string priorityString = textComponents[1].Trim();

				// Add default logistic priority names
				List<string> priorityNamesListWithDefaultNames = priorityNamesList.Value.Split(',').ToList();
				priorityNamesListWithDefaultNames.AddRange(Enumerable.Range(-3, 7).Select(x => Localization.GetLocalizedString("Ui_Logistics_Priority" + x) + ":" + x));
				// Convert priority names to priority values
				foreach (string pair in priorityNamesListWithDefaultNames) {
					string[] splitPair = pair.Trim().Split(':');
					if (splitPair.Count() != 2) continue;
					if (string.Compare(priorityString, splitPair[0].Trim(), StringComparison.OrdinalIgnoreCase) == 0) priorityString = splitPair[1].Trim();
				}

				if (int.TryParse(priorityString, out int priority)) {
					if (allowAnyValue.Value || (-3 <= priority && priority <= 5)) {
						logisticEntity.SetPriority(priority);
					}
				}
			}

			// for each id, translate and find group case insensitive. If successful, add to demand group
			List<string> logisticGroupSynonymesListSplit = logisticGroupSynonymesList.Value.Split(',').ToList();
			logisticGroupSynonymesListSplit.AddRange(staticSynonymesList);

			Dictionary<string, List<string>> namedLists = new Dictionary<string, List<string>>();
			foreach (string s in logisticGroupNamedLists.Value.Split(',')) {
				string[] splitNameAndList = s.Trim().Split(':');
				if (splitNameAndList.Count() != 2) continue;

				namedLists[splitNameAndList[0].Trim().ToLower()] = splitNameAndList[1].Split('+').Select(e => e.Trim()).ToList();
			}

			List<Group> foundGroups = new List<Group>();
			foreach (string possibleIdElement in textModified.Split(',')) {
				string possibleId = possibleIdElement.Trim();

				List<Group> foundGroupsFromString = new List<Group>();

				if (string.IsNullOrEmpty(possibleId)) continue;

				// Allow all
				if (string.Compare(possibleId, "all", StringComparison.OrdinalIgnoreCase) == 0
					|| string.Compare(possibleId, Localization.GetLocalizedString("Ui_Logistics_AllGroups").Trim(), StringComparison.OrdinalIgnoreCase) == 0) {
					foundGroups.AddRange(allGroups.Where(g => g.GetGroupData() is GroupDataItem).Where(g => ((GroupDataItem)g.GetGroupData()).displayInLogisticType == DataConfig.LogisticDisplayType.Display));
					break;
				}

				// Get all groups from name
				if (namedLists.TryGetValue(possibleId.ToLower(), out List<string> listIDs)) {
					foreach (string idElement in listIDs) {
						foundGroupsFromString.AddRange(GetGroupsFromString(idElement, logisticGroupSynonymesListSplit));
					}
				} else {
					foundGroupsFromString.AddRange(GetGroupsFromString(possibleId, logisticGroupSynonymesListSplit));
				}

				foundGroups.AddRange(foundGroupsFromString);

				if (foundGroupsFromString.Count == 0 && enableNotification.Value) SendNotification("No group found: " + possibleIdElement); // [missing translation]
			}
			if (foundGroups.Count() > 0) {// clear demand groups only if a group is found (Compatibility to other/older lockers)
				if (SupplyInstead) {
					logisticEntity.ClearSupplyGroups();
				} else {
					logisticEntity.ClearDemandGroups();
				}
			}

			if (SupplyInstead) {
				foreach (Group group in foundGroups) {
					logisticEntity.AddSupplyGroup(group);
				}
			} else {
				foreach (Group group in foundGroups) {
					LogisticSelector_OnGroupDemandSelected(group, pInventory);

					logisticEntity.AddDemandGroup(group);
				}
			}

			InventoriesHandler.Instance.UpdateLogisticEntity(pInventory);
		}

		private static List<Group> GetGroupsFromString(string possibleId, List<string> logisticGroupSynonymesListSplit) {
			List<Group> foundGroups = new List<Group>();

			bool subset = possibleId.EndsWith(">");
			if (subset) possibleId = possibleId.Remove(possibleId.Length - 1);

			// subset exclusion
			string[] splitIDsForSubsetExclusion = possibleId.Split('>');
			List<string> substringsToExclude = new List<string>();
			if (splitIDsForSubsetExclusion.Count() > 1) {
				possibleId = splitIDsForSubsetExclusion[0];
				foreach (string groupToExcludeString in splitIDsForSubsetExclusion.Skip(1)) {
					substringsToExclude.Add(groupToExcludeString);
				}
			}

			// Convert synonyme name to name
			foreach (string pair in logisticGroupSynonymesListSplit) {
				string[] splitPair = pair.Trim().Split(':');
				if (splitPair.Count() != 2) continue;

				if (!subset && string.Compare(possibleId, splitPair[0].Trim(), StringComparison.OrdinalIgnoreCase) == 0) {
					possibleId = splitPair[1].Trim();
					break;
				}
			}

			// translate name to id
			if (!subset && !string.IsNullOrEmpty(possibleId) && untranslate.TryGetValue(possibleId.ToLower(), out string id)) {
				if (string.IsNullOrEmpty(id)) return foundGroups;
				possibleId = id;
			}

			// find id in allGroups case insensitive
			foreach (Group group in allGroups) {
				if (group == null || group.GetId() == null) continue;
				if (
						(
							!subset
							&&
							string.Compare(possibleId, group.GetId(), StringComparison.OrdinalIgnoreCase) == 0
						) // compares full string
						||
						(
							subset
							&&
							(
								group.GetId().IndexOf(possibleId, StringComparison.OrdinalIgnoreCase) >= 0
								||
								(Readable.GetGroupName(group) ?? "").IndexOf(possibleId, StringComparison.OrdinalIgnoreCase) >= 0
							)
						) // finds subset (if string ends with >)
					) {
					if (allowAnyValue.Value || ((group.GetGroupData() is GroupDataItem gdi) && (gdi.displayInLogisticType == DataConfig.LogisticDisplayType.Display))) {

						//foundGroups.Add(group);
						bool addGroup = true;
						foreach (string subS in substringsToExclude) {
							if (group.GetId().IndexOf(subS, StringComparison.OrdinalIgnoreCase) >= 0 || (Readable.GetGroupName(group) ?? "").IndexOf(subS, StringComparison.OrdinalIgnoreCase) >= 0) {
								addGroup = false;
							}
						}
						if (addGroup) foundGroups.Add(group);

						if (!subset) break;
					}
				}
			}

			return foundGroups;
		}

		static Dictionary<string, string> untranslate = new Dictionary<string, string>();
		static string currentlyLoadedLanguage = "";
		[HarmonyPostfix]
		[HarmonyPatch(typeof(Localization), "LoadLocalization")]
		public static void Localization_LoadLocalization(Dictionary<string, Dictionary<string, string>> ___localizationDictionary, string ___currentLangage) {
			if (!enableMod.Value) return;

			if (currentlyLoadedLanguage == ___currentLangage && untranslate.Count != 0) return;
			untranslate.Clear();
			if (!___localizationDictionary.TryGetValue(___currentLangage, out Dictionary<string, string> translateCurrentLanguage)) return;
			foreach (string key in translateCurrentLanguage.Keys) {
				if (translateCurrentLanguage.TryGetValue(key, out string translation) && key.Contains(GameConfig.localizationGroupNameId)) {
					untranslate[translation.ToLower()] = key.Replace(GameConfig.localizationGroupNameId, "");
				}
			}
			currentlyLoadedLanguage = ___currentLangage;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(UiWindowTextInput), nameof(UiWindowTextInput.SetTextWorldObject))]
		public static void UiWindowTextInput_SetTextWorldObject(TMP_InputField ___inputField) {
			if (!enableMod.Value) return;
			if (!allowLongNames.Value) return;

			if (___inputField.characterLimit < 125) {
				___inputField.characterLimit = 125; // Increase character limit. Buffer is only 128 Bytes: 2 bytes for size and \0 terminated
			}
		}
		// <--- Add Name to logistic entities ---

		// --- Logistics to String --->
		private static string LogisticsToString(LogisticEntity entity, int count = int.MaxValue) {
			return string.Join(", ", entity.GetDemandGroups().Select(group => (Readable.GetGroupName(group) ?? group.GetId())).Take(count).ToArray()) + (" +" + entity.GetPriority());
		}
		private static string PriorityToNames(int prio) {
			IEnumerable<IEnumerable<string>> prioNamesSplit =
			priorityNamesList.Value.Split(',').Select(n => n.Trim().Split(':').Select(e => e.Trim())).Where(e => e.Count() == 2).Reverse();
			Dictionary<int, string> prioToNames = new Dictionary<int, string>();
			foreach (IEnumerable<string> e in prioNamesSplit) {
				if (int.TryParse(e.ElementAt(1), out int parsed)) {
					prioToNames[parsed] = e.ElementAt(0);
				}
			}
			return prioToNames.TryGetValue(prio, out string name) ? name : prio.ToString();
		}
		// <--- Logistics to String ---

		// --- Copy Logistics --->
		private class LogisticsSettingsAttrib {
			public HashSet<Group> demandGroups = new HashSet<Group>();
			public HashSet<Group> supplyGroups = new HashSet<Group>();
			public int priority;
			public int setting = 0;
			public Group groupSelected = null;
			public int linkedPlanet = 0;
			public string text = "";
		}
		private static Group defaultCopiedLogisticsGroup = null;
		private static Dictionary<Group, LogisticsSettingsAttrib> copiedLogistics = new Dictionary<Group, LogisticsSettingsAttrib>();
		static bool GetCopiedLogistics(Group group, out LogisticsSettingsAttrib lsa) {
			defaultCopiedLogisticsGroup ??= GroupsHandler.GetGroupViaId("Container1");
			if (!copyLogisticsPerGroup.Value) group = defaultCopiedLogisticsGroup;
			return copiedLogistics.TryGetValue(group, out lsa);
		}

		static void AddLogisticsSettingsCopy(Group _group, LogisticsSettingsAttrib lsa) {
			defaultCopiedLogisticsGroup ??= GroupsHandler.GetGroupViaId("Container1");
			if (!copyLogisticsPerGroup.Value) _group = defaultCopiedLogisticsGroup;

			CanvasPinedRecipes cpr = Managers.GetManager<CanvasPinedRecipes>();

			int num = field_CanvasPinedRecipes_groupsAdded(cpr).IndexOf(_group);
			if (num != -1) {
				method_CanvasPinedRecipes_RemovePinedRecipeAtIndex.Invoke(cpr, new object[] { num });
			}
			// build pin entry
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(cpr.informationDisplayerGameObject, cpr.grid.transform);
			InformationDisplayer component = gameObject.GetComponent<InformationDisplayer>();
			LoadDefaultSprites(component);
			component.SetDisplay("", _group.GetImage(), DataConfig.UiInformationsType.OutInventory);
			component.iconContainer.sprite = GetSprite(SpriteType.logistic);
			component.SetGroupList(InfoGroupList(lsa), false);
			gameObject.GetComponentInChildren<GroupList>().SetGridCellSize(new Vector2(42f, 42f), new Vector2(1f, 1f));
			EventsHelpers.AddTriggerEvent(gameObject, EventTriggerType.PointerClick, new Action<EventTriggerCallbackData>(delegate (EventTriggerCallbackData eventTriggerCallbackData) {
				CanvasPinedRecipes cpr = Managers.GetManager<CanvasPinedRecipes>();
				int num = field_CanvasPinedRecipes_groupsAdded(cpr).IndexOf(eventTriggerCallbackData.group);
				method_CanvasPinedRecipes_RemovePinedRecipeAtIndex.Invoke(cpr, new object[] { num });
				copiedLogistics.Remove(eventTriggerCallbackData.group);
			}), new EventTriggerCallbackData(_group));
			field_CanvasPinedRecipes_groupsAdded(cpr).Add(_group);
			field_CanvasPinedRecipes_informationDisplayers(cpr).Add(component);
			//this.OnInventoryModified(null, false);

			copiedLogistics[_group] = lsa;
		}
		// load temporary/default sprites if logistic selector wasn't opened yet.
		enum SpriteType { logistic, demand, supply, supplyAll, setting, Planet, Message }
		static readonly Dictionary<SpriteType, Sprite> sprites = new Dictionary<SpriteType, Sprite>();
		private static readonly Dictionary<SpriteType, Sprite> defaultSprites = new Dictionary<SpriteType, Sprite>();
		private static Sprite GetSprite(SpriteType st) {
			if (sprites.ContainsKey(st)) return sprites[st];
			LoadSprites();
			if (sprites.ContainsKey(st)) return sprites[st];

			if (defaultSprites.ContainsKey(st)) return defaultSprites[st];
			if (enableDebug.Value) log.LogWarning("Sprite not found");
			return Sprite.Create(Texture2D.blackTexture, new Rect(0f, 0f, 4, 4), new Vector2(0f, 0f));
		}
		private static void LoadSprites() {
			if (!sprites.ContainsKey(SpriteType.setting) && defaultSprites.ContainsKey(SpriteType.setting)) sprites[SpriteType.setting] = defaultSprites[SpriteType.setting];

			Transform lsTransform = UnityEngine.Object.FindFirstObjectByType<LogisticSelector>().transform;
			if (!sprites.ContainsKey(SpriteType.logistic)) {
				Transform tObj = lsTransform.Find("Button/OpenButton"); // "MainScene/BaseStack/UI/WindowsHandler/[UiWindowContainer,UiWindowGenetics,UiWindowDNAExtractor,UiWindowGroupSelector]/ContainerInventoryContainer/InventoryDisplayer(Clone)/IconsContainer/LogisticSelector/Button/OpenButton"
				if (tObj != null) sprites[SpriteType.logistic] = tObj.GetComponent<Image>().sprite;
			}
			if (!sprites.ContainsKey(SpriteType.demand)) {
				Transform tObj = lsTransform.Find("Window/ListDemand/GroupSelectorDemand/GroupDisplayer (1)/Background");
				if (tObj != null) sprites[SpriteType.demand] = tObj.GetComponent<Image>().sprite;
			}
			if (!sprites.ContainsKey(SpriteType.supply)) {
				Transform tObj = lsTransform.Find("Window/ListSupply/GroupSelectorSupply/GroupDisplayer (1)/Background");
				if (tObj != null) sprites[SpriteType.supply] = tObj.GetComponent<Image>().sprite;
			}
			if (!sprites.ContainsKey(SpriteType.supplyAll)) {
				Transform tObj = lsTransform.Find("Window/ListSupply/SupplyAll");
				if (tObj != null) sprites[SpriteType.supplyAll] = tObj.GetComponent<Image>().sprite;
			}
			if (!sprites.ContainsKey(SpriteType.Planet)) {
				GameObject obj = GameObject.Find("MainScene/BaseStack/UI/WindowsHandler/UiWindowInterplanetaryExhange/Container/ContentRocketOnSite/RightContent/SelectedPlanet/PlanetIcon");
				if (obj != null) sprites[SpriteType.Planet] = obj.GetComponent<Image>().sprite;
			}
			if (!sprites.ContainsKey(SpriteType.Message)) {
				Sprite messageSprite = GroupsHandler.GetGroupViaId("ScreenMessage").GetAssociatedGameObject().GetComponentsInChildren<Image>().Select(e => e.sprite).Where(e => e != null).Where(e => e.name == "Icon_Message3").First();
				if (messageSprite != null) sprites[SpriteType.Message] = messageSprite;
			}
		}
		private static void LoadDefaultSprites(InformationDisplayer id) {
			if (!defaultSprites.ContainsKey(SpriteType.logistic)) defaultSprites[SpriteType.logistic] = id.spriteOutInventory;
			if (!defaultSprites.ContainsKey(SpriteType.demand)) defaultSprites[SpriteType.demand] = id.spriteOutInventory;
			if (!defaultSprites.ContainsKey(SpriteType.supply)) defaultSprites[SpriteType.supply] = id.spriteInInventory;
			if (!defaultSprites.ContainsKey(SpriteType.setting)) defaultSprites[SpriteType.setting] = id.spriteTutorial;
			if (!defaultSprites.ContainsKey(SpriteType.supplyAll)) defaultSprites[SpriteType.supplyAll] = id.spriteTutorial;
			if (!defaultSprites.ContainsKey(SpriteType.Planet)) defaultSprites[SpriteType.Planet] = id.spriteTutorial;
			if (!defaultSprites.ContainsKey(SpriteType.Message)) defaultSprites[SpriteType.Message] = id.spriteTutorial;
		}
		// build info-groups
		private static List<Group> InfoGroupList(LogisticsSettingsAttrib lsa) {
			List<Group> ingredientsGroupInRecipe = new List<Group>();

			if (lsa.groupSelected != null) {
				ingredientsGroupInRecipe.Add(GroupWithCustomIcon("Produced Group: " + lsa.groupSelected.GetId(), lsa.groupSelected.GetImage())); // [missing translation]
				return ingredientsGroupInRecipe;
			}

			if (lsa.demandGroups.Count > 0) {
				ingredientsGroupInRecipe.Add(GroupWithCustomIcon(Localization.GetLocalizedString("Logistic_menu_demand"), GetSprite(SpriteType.demand)));
				ingredientsGroupInRecipe.AddRange(lsa.demandGroups.ToList());
			}
			if (lsa.supplyGroups.Count > 140) {
				ingredientsGroupInRecipe.Add(GroupWithCustomIcon(Localization.GetLocalizedString("Logistic_menu_supply") + ": " + Localization.GetLocalizedString("Ui_Logistics_AllGroups"), GetSprite(SpriteType.supplyAll)));
			} else if (lsa.supplyGroups.Count > 0) {
				ingredientsGroupInRecipe.Add(GroupWithCustomIcon(Localization.GetLocalizedString("Logistic_menu_supply"), GetSprite(SpriteType.supply)));
				ingredientsGroupInRecipe.AddRange(lsa.supplyGroups.ToList());
			}
			Texture2D textureWithString = DrawText(lsa.priority.ToString(), LogisticsFont, System.Drawing.Color.White, System.Drawing.Color.Transparent);
			ingredientsGroupInRecipe.Add(GroupWithCustomIcon(Localization.GetLocalizedString("Logistic_menu_priority") + ": " + ((lsa.priority < -3 || lsa.priority > 3) ? lsa.priority : Localization.GetLocalizedString("Ui_Logistics_Priority" + lsa.priority)), Sprite.Create(textureWithString, new Rect(0.0f, 0.0f, textureWithString.width, textureWithString.height), new Vector2(0.5f, 0.5f), 100.0f)));
			if (lsa.setting > 0) ingredientsGroupInRecipe.Add(GroupWithCustomIcon(Localization.GetLocalizedString("Ui_settings_title") + ": " + Localization.GetLocalizedString("Ui_settings_on")/*"Auto launch: active"*/, GetSprite(SpriteType.setting)));
			if (lsa.linkedPlanet != 0) {
				PlanetData linkedPlanetData = Managers.GetManager<PlanetLoader>().planetList.GetPlanetFromIdHash(lsa.linkedPlanet);
				ingredientsGroupInRecipe.Add(GroupWithCustomIcon(Localization.GetLocalizedString("UI_InterplanetaryExchange_selectedPlanet") + " " + (linkedPlanetData != null ? Readable.GetPlanetLabel(linkedPlanetData) : ""), GetSprite(SpriteType.Planet)));
			}
			if (!string.IsNullOrEmpty(lsa.text)) {
				ingredientsGroupInRecipe.Add(GroupWithCustomIcon(Localization.GetLocalizedString("UI_change_text") + ": " + lsa.text, GetSprite(SpriteType.Message)));
			}

			return ingredientsGroupInRecipe;
		}
		private static Group GroupWithCustomIcon(string name, Sprite sprite) {
			GroupData iconGroupData = (GroupData)ScriptableObject.CreateInstance(typeof(GroupData));
			iconGroupData.icon = sprite;
			iconGroupData.id = name;
			//Probably not needed, but might create nullrefexceptions if not initialized:
			iconGroupData.recipeIngredients = new List<GroupDataItem>();
			iconGroupData.hideInCrafter = true;
			iconGroupData.unlockingWorldUnit = DataConfig.WorldUnitType.Null;
			iconGroupData.unlockingValue = 0f;
			iconGroupData.unlockInPlanets = new List<PlanetData>();
			iconGroupData.planetUsageType = DataConfig.GroupPlanetUsageType.CanBeUsedOnAllPlanets;
			iconGroupData.lootRecipeOnDeconstruct = false;
			iconGroupData.tradeCategory = DataConfig.TradeCategory.Null;
			iconGroupData.tradeValue = 0;
			iconGroupData.inventorySize = 0;
			iconGroupData.secondaryInventoriesSize = new List<int>();
			//iconGroupData.associatedGameObject : GameObject
			//iconGroupData.terraformStageUnlock : TerraformStage
			return (Group)constructor_Group.Invoke(new object[] { iconGroupData });
		}
		private static Texture2D DrawText(String text, System.Drawing.Font font, System.Drawing.Color textColor, System.Drawing.Color backColor) {
			// https://stackoverflow.com/questions/2070365/how-to-generate-an-image-from-text-on-fly-at-runtime
			System.Drawing.Bitmap img = new System.Drawing.Bitmap(1, 1);
			System.Drawing.Graphics drawing = System.Drawing.Graphics.FromImage(img);
			System.Drawing.SizeF textSize = drawing.MeasureString(text, font);
			img.Dispose();
			drawing.Dispose();
			img = new System.Drawing.Bitmap((int)textSize.Width, (int)textSize.Height);
			drawing = System.Drawing.Graphics.FromImage(img);
			drawing.Clear(backColor);
			System.Drawing.Brush textBrush = new System.Drawing.SolidBrush(textColor);
			drawing.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
			drawing.DrawString(text, font, textBrush, 0, 0);
			drawing.Save();
			textBrush.Dispose();
			drawing.Dispose();

			// https://stackoverflow.com/questions/40482700/convert-a-bitmap-to-a-texture2d-in-unity
			MemoryStream ms = new MemoryStream();
			img.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
			var buffer = new byte[ms.Length];
			ms.Position = 0;
			ms.Read(buffer, 0, buffer.Length);
			Texture2D t = new Texture2D(1, 1);
			t.LoadImage(buffer);

			return t;
		}

		// Copy logistics when closing the menu
		[HarmonyPostfix]
		[HarmonyPatch(typeof(LogisticSelector), nameof(LogisticSelector.OnCloseLogisticSelector))]
		public static void LogisticSelector_OnCloseLogisticSelector(LogisticEntity ____logisticEntity, Inventory ____inventory) {
			if (!enableMod.Value) return;

			if (Keyboard.current[copyLogisticsKey.Value].isPressed) {
				CreateCopyLogistics(____logisticEntity, ____inventory);
			}
		}
		private static void CreateCopyLogistics(LogisticEntity logisticEntity, Inventory inventory, Group selectedGroup = null, bool sendNotification = true) {
			WorldObject worldObject = WorldObjectsHandler.Instance.GetWorldObjectForInventory(inventory);
			if (worldObject == null) {
				if (enableDebug.Value) log.LogWarning("WorldObject for Inventory " + inventory.GetId() + " not found!");
				return;
			}
			Group group = worldObject.GetGroup();

			LogisticsSettingsAttrib lsa = new LogisticsSettingsAttrib();
			lsa.priority = logisticEntity.GetPriority();
			lsa.demandGroups.UnionWith(logisticEntity.GetDemandGroups());
			lsa.supplyGroups.UnionWith(logisticEntity.GetSupplyGroups());
			lsa.setting = worldObject.GetSetting();
			lsa.groupSelected = selectedGroup;
			lsa.linkedPlanet = linkedPlanetWhenOpened; // Set in ActionOpenable_OpenInventories
			lsa.text = logisticEntity?.GetWorldObject()?.GetText() ?? "";

			AddLogisticsSettingsCopy(group, lsa);
			if (enableNotification.Value && sendNotification) SendNotification("Copied logistics", SpriteCopy); // [missing translation]
		}
		// Paste logistics settings for demand, supply, priority and settings (auto-launch / auto-shredding)
		private static int linkedPlanetWhenOpened = 0;
		private static ActionOpenable PasteButton_LastOpened_ActionOpenable;
		[HarmonyPrefix]
		[HarmonyPatch(typeof(ActionOpenable), "OpenInventories")]
		public static void ActionOpenable_OpenInventories(ActionOpenable __instance, Inventory objectInventory, WorldObject worldObject, ref bool __state) {
			if (!enableMod.Value) return;

			// prepare copy --->
			LinkedPlanetProxy lpp = __instance.GetComponentInParent<LinkedPlanetProxy>();
			if (lpp != null) {
				linkedPlanetWhenOpened = lpp.GetLinkedPlanet();
			} else {
				linkedPlanetWhenOpened = 0;
			}
			// <--- prepare copy

			// --- prepare paste with buttons --->
			PasteButton_LastOpened_ActionOpenable = __instance;
			// <--- prepare paste with buttons ---

			// --- show logistics on inventories that have it disabled --->
			__state = __instance.hideLogisticsButton;
			__instance.hideLogisticsButton &= !allowAnyValue.Value || !enablePotentialBuggedFeatures.Value;
			// <--- show logistics on inventories that have it disabled ---

			if (Keyboard.current[pasteLogisticsKey.Value].isPressed && (allowAnyValue.Value || !__instance.hideLogisticsButton)) {
				PasteLogisticsContainer(__instance, objectInventory, worldObject);
			}
		}
		private static void PasteLogisticsContainer(ActionOpenable actionOpenable, Inventory objectInventory, WorldObject worldObject, bool sendNotification = true) {
			Group group = worldObject.GetGroup();
			LogisticEntity logisticEntity = objectInventory.GetLogisticEntity();
			if (logisticEntity == null) { // TODO Find out if -> why it can be null.
				if (enableDebug.Value) {
					log.LogWarning("objectInventory.GetLogisticEntity() is null");
					SendNotification("logistics is null");
				}
				return;
			}
			if (GetCopiedLogistics(group, out LogisticsSettingsAttrib lsa)) {
				logisticEntity.SetPriority(lsa.priority);
				if (lsa.demandGroups.Count > 0) foreach (Group g in lsa.demandGroups) LogisticSelector_OnGroupDemandSelected(g, objectInventory);

				IEnumerable<Group> demandGroupsToAdd = lsa.demandGroups;
				IEnumerable<Group> supplyGroupsToAdd = lsa.supplyGroups;
				if (!allowAnyValue.Value) {
					demandGroupsToAdd = lsa.demandGroups.Where(g => g.GetGroupData() is GroupDataItem).Where(g => ((GroupDataItem)g.GetGroupData()).displayInLogisticType == DataConfig.LogisticDisplayType.Display);
					supplyGroupsToAdd = lsa.supplyGroups.Where(g => g.GetGroupData() is GroupDataItem).Where(g => ((GroupDataItem)g.GetGroupData()).displayInLogisticType == DataConfig.LogisticDisplayType.Display);
					HashSet<Group> authorizedGroups = objectInventory.GetAuthorizedGroups();
					if (authorizedGroups != null && authorizedGroups.Count > 0) {
						demandGroupsToAdd = demandGroupsToAdd.Where(authorizedGroups.Contains);
						supplyGroupsToAdd = supplyGroupsToAdd.Where(authorizedGroups.Contains);
					}
				}
				logisticEntity.GetDemandGroups().Clear();
				logisticEntity.GetDemandGroups().UnionWith(demandGroupsToAdd); // must run after LogisticSelector_OnGroupDemandSelected
				logisticEntity.GetSupplyGroups().Clear();
				logisticEntity.GetSupplyGroups().UnionWith(supplyGroupsToAdd);
				SettingProxy sp = actionOpenable.GetComponentInParent<SettingProxy>();
				sp?.SetSetting(lsa.setting);
				MachineRocketBackAndForthInterplanetaryExchange exchangeRocket = actionOpenable.GetComponentInParent<MachineRocketBackAndForthInterplanetaryExchange>();
				exchangeRocket?.SetLinkedPlanet(Managers.GetManager<PlanetLoader>().planetList.GetPlanetFromIdHash(lsa.linkedPlanet));
				if (!string.IsNullOrEmpty(lsa.text)) worldObject.GetGameObject()?.GetComponent<TextProxy>()?.SetText(lsa.text);

				if (enableNotification.Value && sendNotification) SendNotification("Inserted logistics", SpritePaste); // [missing translation]
				InventoriesHandler.Instance.UpdateLogisticEntity(objectInventory);
			}
		}
		// Paste group selection (ore extractor T3, gas extractor T2, Harvester, delivery depot)
		private static void SetSelectedGroupFromCopy(UiWindowGroupSelector __instance, Inventory ____inventoryRight, List<GroupData> allowedGroups = null) {
			WorldObject worldObject = WorldObjectsHandler.Instance.GetWorldObjectForInventory(____inventoryRight);
			if (worldObject == null) {
				if (enableDebug.Value) log.LogWarning("WorldObject for Inventory " + ____inventoryRight.GetId() + " not found!");
				return;
			}

			Group group = worldObject.GetGroup();
			if (GetCopiedLogistics(group, out LogisticsSettingsAttrib lsa)) { // Check if --- add logistics when placing a WO --- needs adaptions when changing!
				if ((!copyLogisticsPerGroup.Value) && (lsa.groupSelected == null)) { // Note: Can't clear selected group when copyLogisticsPerGroup is false
					return;
				}
				if (!allowAnyValue.Value && lsa.groupSelected != null && allowedGroups != null && !allowedGroups.Contains(lsa.groupSelected.GetGroupData())) {
					if (enableNotification.Value) SendNotification("Group not selectable here"); // [missing translation]
					return;
				}
				AccessTools.Method(typeof(UiWindowGroupSelector), "OnGroupSelected").Invoke(__instance, new object[] { lsa.groupSelected }); // Invoked here because it's also patched!

				if (enableNotification.Value) SendNotification("Set selected Group", SpritePaste); // [missing translation]
			}
		}
		[HarmonyPrefix] // OnOpen or SetGroupSelectorWorldObject not possible because ActionGroupSelector.OpenInventories calls SetInventories after them
		[HarmonyPatch(typeof(UiWindowGroupSelector), nameof(UiWindowGroupSelector.OnOpenAutoCrafter))]
		public static void UiWindowGroupSelector_OnOpenAutoCrafter(UiWindowGroupSelector __instance, Inventory ____inventoryRight) {
			if (!enableMod.Value) return;

			if (!Keyboard.current[pasteLogisticsKey.Value].isPressed) return;

			SetSelectedGroupFromCopy(__instance, ____inventoryRight);
		}
		[HarmonyPrefix]
		[HarmonyPatch(typeof(UiWindowGroupSelector), nameof(UiWindowGroupSelector.OnOpenOreExtractor))]
		public static void UiWindowGroupSelector_OnOpenOreExtractor(UiWindowGroupSelector __instance, Inventory ____inventoryRight, List<GroupData> groupsData) {
			if (!enableMod.Value) return;

			if (!Keyboard.current[pasteLogisticsKey.Value].isPressed) return;

			List<GroupData> availableGroups = new List<GroupData>();
			availableGroups.AddRange(groupsData);
			availableGroups.AddRange(__instance.groupSelector.GetAddedGroups().Select(e => e.GetGroupData()));
			SetSelectedGroupFromCopy(__instance, ____inventoryRight, availableGroups);
		}
		[HarmonyPrefix]
		[HarmonyPatch(typeof(UiWindowGroupSelector), nameof(UiWindowGroupSelector.OnOpenInterplanetaryDepot))]
		public static void UiWindowGroupSelector_OnOpenInterplanetaryDepot(UiWindowGroupSelector __instance, Inventory ____inventoryRight) {
			if (!enableMod.Value) return;

			if (!Keyboard.current[pasteLogisticsKey.Value].isPressed) return;

			SetSelectedGroupFromCopy(__instance, ____inventoryRight);
		}

		// Fix if group item has less ingredients than there are logistic Icons displayed
		[HarmonyPrefix]
		[HarmonyPatch(typeof(GroupList), "SetGroupsAvailability")]
		public static void GroupList_SetGroupsAvailability(List<bool> _availableStatus, List<GroupDisplayer> ___groupsDisplayer) {
			if (!enableMod.Value) return;

			if (_availableStatus.Count != ___groupsDisplayer.Count) for (int i = 0; i < _availableStatus.Count; i++) _availableStatus[0] = true;
			while (_availableStatus.Count < ___groupsDisplayer.Count) _availableStatus.Add(true);
		}

		// Copy/Paste Buttons
		private static GameObject ButtonCopy;
		private static GameObject ButtonPaste;
		[HarmonyPostfix]
		[HarmonyPatch(typeof(LogisticSelector), nameof(LogisticSelector.OpenLogisticSelector))]
		private static void LogisticSelector_OpenLogisticSelector(LogisticSelector __instance, LogisticEntity ____logisticEntity, Inventory ____inventory) {
			if (!enableMod.Value) return;

			WindowsHandler windowsHandler = Managers.GetManager<WindowsHandler>();
			if (windowsHandler?.GetOpenedUi() == DataConfig.UiType.GroupSelector) { return; } // Currently it isn't possible to copy/paste ACs, ore extractors etc.

			if (__instance.gameObject.transform.Find("ButtonCopy") == null) {
				ButtonCopy = CreateButton(__instance, "ButtonCopy", new Vector3(130, 210, 0), SpriteCopy);
				ButtonCopy.GetComponent<Button>().onClick.AddListener(delegate () {
					CreateCopyLogistics(____logisticEntity, ____inventory, sendNotification: false);
				});
			} else {
				ButtonCopy.SetActive(true);
			}
			if (__instance.gameObject.transform.Find("ButtonPaste") == null) {
				ButtonPaste = CreateButton(__instance, "ButtonPaste", new Vector3(180, 210, 0), SpritePaste);
				ButtonPaste.GetComponent<Button>().onClick.AddListener(delegate () {
					WorldObject containerWorldObject = WorldObjectsHandler.Instance.GetWorldObjectForInventory(____inventory);
					if (containerWorldObject == null) {
						if (enableDebug.Value) log.LogWarning("WorldObject for Inventory " + ____inventory.GetId() + " not found!");
						return;
					}
					PasteLogisticsContainer(PasteButton_LastOpened_ActionOpenable, ____inventory, containerWorldObject, sendNotification: false);
				});
			} else {
				ButtonPaste.SetActive(true);
			}
		}
		[HarmonyPostfix]
		[HarmonyPatch(typeof(LogisticSelector), nameof(LogisticSelector.OnCloseLogisticSelector))]
		private static void LogisticSelector_OnCloseLogisticSelector(LogisticSelector __instance) {
			if (!enableMod.Value) return;

			if (ButtonCopy != null) ButtonCopy.SetActive(false);
			if (ButtonPaste != null) ButtonPaste.SetActive(false);
		}
		private static GameObject CreateButton(LogisticSelector __instance, string name, Vector3 pos, Sprite image) {
			TMP_DefaultControls.Resources resourcesA = new TMP_DefaultControls.Resources();
			GameObject button = TMP_DefaultControls.CreateButton(resourcesA);

			button.transform.SetParent(__instance.transform, false);
			button.name = name;
			button.transform.localPosition = pos;
			button.GetComponent<RectTransform>().sizeDelta = new Vector2(25, 25);
			button.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "";
			button.AddComponent<EventHoverIncrease>().SetHoverGroupEvent();
			button.GetComponent<Image>().sprite = image;

			GameObject backgroundImageGameObjectA = new GameObject("BackgroundHexagonImage");
			backgroundImageGameObjectA.transform.SetParent(button.transform, false);
			backgroundImageGameObjectA.transform.localScale = new Vector3(0.4f, 1.15f * 0.4f, 1f * 0.4f);
			backgroundImageGameObjectA.AddComponent<Image>().sprite = GameObject.Find("MainScene/BaseStack/UI/WindowsHandler/UiWindowInterplanetaryExhange/Container/CloseUiButton").GetComponentInChildren<Image>().sprite;

			button.SetActive(true);

			return button;
		}
		// <--- Copy Logistics ---

		// --- Don't deliver from production to destructor --->
		private static int stableHashCode_Destructor1 = -1;
		private static HashSet<int> deliveryDontDeliverFromList_StableHashCodes;
		private static void ReloadConfig_DeliveryDontDeliverFromProductionToDestructor() {
			(deliveryDontDeliverFromList_StableHashCodes ??= new HashSet<int>()).Clear();

			stableHashCode_Destructor1 = GroupsHandler.GetGroupViaId("Destructor1").stableHashCode;

			List<string> logisticGroupSynonymesListSplit = logisticGroupSynonymesList.Value.Split(',').ToList();
			logisticGroupSynonymesListSplit.AddRange(staticSynonymesList);
			allGroups = GroupsHandler.GetAllGroups(); // needed for GetGroupsFromString([...])
			foreach (string groupString in deliveryDontDeliverFromProductionToDestructorGroups.Value.Split(',').Select(e => e.Trim())) {
				foreach (Group g in GetGroupsFromString(groupString, logisticGroupSynonymesListSplit)) {
					deliveryDontDeliverFromList_StableHashCodes.Add(g.stableHashCode);
				}
			}
		}
		[HarmonyPrefix]
		[HarmonyPatch(typeof(LogisticManager), "CreateNewTaskForWorldObject")]
		private static bool LogisticManager_CreateNewTaskForWorldObject(Inventory supplyInventory, Inventory demandInventory, ref LogisticTask __result) {
			if (!enableMod.Value) { return true; }
			if (!deliveryDontDeliverFromProductionToDestructor.Value) { return true; }

			if (deliveryDontDeliverFromList_StableHashCodes == null) { ReloadConfig_DeliveryDontDeliverFromProductionToDestructor(); }

			int demandingWorldObjectGroupHash = demandInventory.GetLogisticEntity().GetWorldObject()?.GetGroup().stableHashCode ?? 0;
			if (demandingWorldObjectGroupHash != stableHashCode_Destructor1) { return true; }
			int supplyingWorldObjectGroupHash = supplyInventory.GetLogisticEntity().GetWorldObject()?.GetGroup().stableHashCode ?? 0;
			if (deliveryDontDeliverFromList_StableHashCodes.Contains(supplyingWorldObjectGroupHash)) {
				__result = null;
				return false;
			}
			return true;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(LogisticManager), "CreateNewTaskForWorldObjectForSpawnedObject")]
		private static bool LogisticManager_CreateNewTaskForWorldObjectForSpawnedObject(Inventory demandInventory, ref LogisticTask __result) {
			if (!enableMod.Value) { return true; }
			if (!deliveryDontDeliverSpawnedObjectsToDestructor.Value) { return true; }

			if (stableHashCode_Destructor1 == 0) { stableHashCode_Destructor1 = GroupsHandler.GetGroupViaId("Destructor1").stableHashCode; }
			int demandingWorldObjectGroupHash = demandInventory.GetLogisticEntity().GetWorldObject()?.GetGroup().stableHashCode ?? 0;

			if (demandingWorldObjectGroupHash == stableHashCode_Destructor1) {
				__result = null;
				return false;
			}
			return true;
		}
		// <--- Don't deliver from production to destructor ---

		// --- show all groups in logistics selector --->
		[HarmonyPrefix]
		[HarmonyPatch(typeof(LogisticManager), nameof(LogisticManager.GetItemsToDisplayForLogistics))]
		private static void LogisticManager_GetItemsToDisplayForLogistics(ref HashSet<Group> authorizedGroups) {
			if (!enableMod.Value) { return; }

			if (!allowAnyValue.Value) { return; }
			if (authorizedGroups == null) { return; }
			authorizedGroups = new HashSet<Group>();
		}
		// <--- show all groups in logistics selector ---


		// --- show logistics on inventories that have it disabled --->
		[HarmonyPostfix]
		[HarmonyPatch(typeof(ActionOpenable), "OpenInventories")]
		private static void Post_ActionOpenable_OpenInventory(ActionOpenable __instance, ref bool __state) {
			if (!enableMod.Value) { return; }

			__instance.hideLogisticsButton = __state;
		}
		// <--- show logistics on inventories that have it disabled ---

		// --- add logistics when placing a WO --->
		[HarmonyPostfix]
		[HarmonyPatch(typeof(WorldObjectsHandler), nameof(WorldObjectsHandler.InstantiateWorldObject))]
		private static void Post_WorldObjectsHandler_InstantiateWorldObjecct(WorldObject worldObject) {
			if (!enableMod.Value) { return; }

			if (!pasteLogisticsIntoNewlyBuild) { return; }
			if (!WorldObjectsHandler.Instance.GetHasInitiatedAllObjects()) return; // So objects that are placed during creation / loading aren't affected

			int inventoryId = worldObject.GetLinkedInventoryId();
			if (inventoryId <= 0 || inventoryId >= 9999999 /*<- taken from InventoriesHandler.GetBiggestInventoryId, should be border to scene invs*/) {
				return;
			}

			Inventory inventory = InventoriesHandler.Instance.GetInventoryById(inventoryId);

			ActionOpenable actionOpenable = worldObject.GetGameObject().GetComponent<ActionOpenable>();
			if (actionOpenable == null) {
				// It's pure luck that the game finds the correct ActionOpenable for dual inventory objects!
				actionOpenable = worldObject.GetGameObject().GetComponentInChildren<ActionOpenable>();
			}
			if (actionOpenable != null) {
				if (actionOpenable.hideLogisticsButton && !enablePotentialBuggedFeatures.Value) {
					if (enableDebug.Value) SendNotification("Logistics is disabled for this machine"); // [missing translation]
					return;
				}
				PasteLogisticsContainer(actionOpenable, inventory, worldObject, sendNotification: true);
			} else if (worldObject.GetGameObject().GetComponentInChildren<ActionGroupSelector>() != null) { // e.g. for ore extractor
				if (!allowAnyValue.Value) {
					if (enableNotification.Value) SendNotification("Select group manually"); // [missing translation]
					return;
				}
				if (GetCopiedLogistics(worldObject.GetGroup(), out LogisticsSettingsAttrib lsa)) {
					if ((!copyLogisticsPerGroup.Value) && (lsa.groupSelected == null)) { // Note: Can't clear selected group when copyLogisticsPerGroup is false
						return;
					}

					LinkedGroupsProxy linkedGroupsProxy = worldObject.GetGameObject()?.GetComponent<LinkedGroupsProxy>();
					if (linkedGroupsProxy != null) {
						linkedGroupsProxy.SetLinkedGroups(lsa.groupSelected != null ? [lsa.groupSelected] : []);
						UiWindowGroupSelector_OnGroupSelected(ref inventory, lsa.groupSelected);
						if (enableNotification.Value) SendNotification("Set selected Group", SpritePaste); // [missing translation]
					}
				}
			}
		}
		// <--- add logistics when placing a WO ---

		// --- Set Text for selected logistics -->
		private static void SetTextForInventory(Inventory inventory) {
			if (!enableMod.Value) { return; }
			if (!setContainerNameWhenSelectingLogistics.Value) { return; }

			if (inventory == null) return;
			WorldObject worldObject = inventory.GetLogisticEntity()?.GetWorldObject(); //WorldObjectsHandler.Instance.GetWorldObjectForInventory(inventory)
			GameObject go = worldObject?.GetGameObject();
			if (go == null) { return; }

			TextProxy tp = go.GetComponentInChildren<TextProxy>();
			string text = tp?.GetText();
			if (text == null) { return; }

			foreach (string commentSymbol in LogisticByName_CommentSymbols) {
				if (text.StartsWith(commentSymbol)) return;
			}

			tp.SetText(LogisticsToString(inventory.GetLogisticEntity(), 5));
		}
		[HarmonyPostfix]
		[HarmonyPatch(typeof(LogisticSelector), "OnGroupDemandSelected")]
		private static void Post_LogisticSelector_OnGroupDemandSelected(Inventory ____inventory) {
			SetTextForInventory(____inventory);
		}
		[HarmonyPostfix]
		[HarmonyPatch(typeof(LogisticSelector), nameof(LogisticSelector.OnClearDemand))]
		private static void Post_LogisticSelector_OnClearDemand(Inventory ____inventory) {
			SetTextForInventory(____inventory);
		}
		[HarmonyPostfix]
		[HarmonyPatch(typeof(LogisticSelector), nameof(LogisticSelector.OnSupplyAll))]
		private static void Post_LogisticSelector_OnSupplyAll(Inventory ____inventory) {
			SetTextForInventory(____inventory);
		}
		// <--- Set Text for selected logistics ---

		private static void SendNotification(string text, Sprite sprite = null, float timeShown = 2f) {
			field_PopupsHandler_popupsToPop(Managers.GetManager<PopupsHandler>()).Add(new PopupData((sprite ?? Sprite.Create(Texture2D.blackTexture, new Rect(0.0f, 0.0f, 4, 4), new Vector2(0.5f, 0.5f), 100.0f)), text, timeShown, true));
		}

		private static void PrintTime() {
			Console.WriteLine("Local date and time: {0}", DateTime.Now.ToString(new CultureInfo("de-DE")));
			Console.WriteLine(new System.Diagnostics.StackTrace(true).ToString());
		}
	}
}

using SpaceCraft;

using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Globalization;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

using TMPro;


namespace autoAddLogistic {
	
    [BepInPlugin(PluginInfo.PLUGIN_GUID, "(QoL) Auto Add Logistic", PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin {
		
		/* TODO
			Localization for all texts
			
			Multiplayer compatibility
		*/
		
		static ManualLogSource log;
		
		static AccessTools.FieldRef<CanvasPinedRecipes, List<Group>> field_CanvasPinedRecipes_groupsAdded;
		static AccessTools.FieldRef<CanvasPinedRecipes, List<InformationDisplayer>> field_CanvasPinedRecipes_informationDisplayers;
		static AccessTools.FieldRef<PopupsHandler, List<PopupData>> field_PopupsHandler_popupsToPop;
		static FieldInfo field_WorldObjectText_proxy;
		static ConstructorInfo constructor_Group;
		static MethodInfo method_CanvasPinedRecipes_RemovePinedRecipeAtIndex;
		
		public static ConfigEntry<bool> enableMod;
		public static ConfigEntry<bool> enableDebug;
		public static ConfigEntry<bool> enableNotification;
		public static ConfigEntry<bool> allowAnyValue;
		
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
		
		private static List<string> staticSynonymesList = new List<string>() {
			"Al:Aluminium",
			"Si:Silicon",
			"P:Phosphorus",
			"S:Sulfur",
			"Ti:Titanium",
			"Mg:Magnesium",
			"Fe:Iron",
			"Co:Cobalt",
			"Se:Selenium",
			"Os:Osmium",
			"Ir:Iridium",
			"U:Uranim"
		};
		
        void Awake() {
            // Plugin startup logic
			log = Logger;
			
			field_CanvasPinedRecipes_groupsAdded = AccessTools.FieldRefAccess<CanvasPinedRecipes, List<Group>>("groupsAdded");
			field_CanvasPinedRecipes_informationDisplayers = AccessTools.FieldRefAccess<CanvasPinedRecipes, List<InformationDisplayer>>("informationDisplayers");
			field_PopupsHandler_popupsToPop = AccessTools.FieldRefAccess<PopupsHandler, List<PopupData>>("popupsToPop");
			field_WorldObjectText_proxy = AccessTools.Field(typeof(WorldObjectText), "_proxy");
			constructor_Group = AccessTools.DeclaredConstructor(typeof(Group), new Type[]{typeof(GroupData)});
			method_CanvasPinedRecipes_RemovePinedRecipeAtIndex = AccessTools.Method(typeof(CanvasPinedRecipes), "RemovePinnedRecipeAtIndex");
			
			enableMod = Config.Bind<bool>(".Config_General", "enableMod", true, "Enable mod");
			enableDebug = Config.Bind<bool>(".Config_General", "enableDebug", false, "Enable debug messages");
			enableNotification = Config.Bind<bool>(".Config_General", "enableNotification", true, "Send a notification if an item group wasn't found or the logistics settings are copied/pasted.");
			allowAnyValue = Config.Bind<bool>(".Config_General", "allowAnyValue", false, "Allows priority below lowest (-3) and above 5, demanding any item group/type and select unavailable items to extract. Those values are not officially supported by the game. Be carefull.");
			
			clearOutputOnInputChange = Config.Bind<bool>("Config_OreBreaker", "clearOutputOnInputChange", true, "Clear output-inventory supply list if other item is selected in ore crusher's or recycler T2's input-inventory's demand list. Only one item can be selected as demand input.");
			allowLongNames = Config.Bind<bool>("Config_LogisticsByText", "allowLongNames", false, "Allows 1000 characters in container name to make setting logistics by text actually usefull. Long text will show outside of the text field.");
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
			
			Harmony.CreateAndPatchAll(typeof(Plugin));
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
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
			CreateCopyLogistics(____inventoryRight.GetLogisticEntity(), ____inventoryRight, group);
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
			
			string woId = wo.GetGroup().id;
			if (woId.Contains("OreBreaker") || woId.Contains("RecyclingMachine2")) {
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
		public static void LogisticSelector_OnClearDemand(ref Inventory ____inventory) {
			if (!enableMod.Value) return;
			
			clearDemand(ref ____inventory);
		}
		[HarmonyPrefix]
		[HarmonyPatch(typeof(LogisticSelector), "OnRemoveDemandGroup")]
		public static void LogisticSelector_OnRemoveDemandGroup(ref Inventory ____inventory) {
			if (!enableMod.Value) return;
			
			clearDemand(ref ____inventory);
		}
		private static void clearDemand(ref Inventory inv) {
			if (!enableMod.Value) return;
			if (!clearOutputOnInputChange.Value) return;
			
			WorldObject wo = WorldObjectsHandler.Instance.GetWorldObjectForInventory(inv);
			string woId = wo.GetGroup().id;
			if (woId.Contains("OreBreaker") || woId.Contains("RecyclingMachine2")) {
				Inventory outputInventory = InventoriesHandler.Instance.GetInventoryById(wo.GetSecondaryInventoriesId().First());
				outputInventory.GetLogisticEntity().ClearSupplyGroups();
				
				InventoriesHandler.Instance.UpdateLogisticEntity(outputInventory);
			}
		}
		// <--- Set Logistics on Ore Crusher ---
		
		// --- Set Logistics on generating machines e.g. bee hive --->
		[HarmonyPostfix]
		[HarmonyPatch(typeof(MachineGenerator), nameof(MachineGenerator.SetGeneratorInventory))]
		public static void MachineGenerator_SetGeneratorInventory(ref Inventory inventory, ref MachineGenerator __instance, WorldObject ____worldObject) {
			if (!enableMod.Value) return;
			if (!enableGeneratorAddLogistics.Value) return;
			
			if (!WorldObjectsHandler.Instance.GetHasInitiatedAllObjects()) return;
			if (__instance.setGroupsDataViaLinkedGroup) return; // e.g. OreExtractor3, GasExtractor2, HarvestingRobot1
			
			if (__instance.groupDatas.Count > 0) {
				List<GroupData> list = new List<GroupData>(__instance.groupDatas);
				if (__instance.groupDatasTerraStage.Count > 0) {
					list.AddRange(__instance.groupDatasTerraStage);
				}
				foreach (GroupData groupData in list) {
					inventory.GetLogisticEntity().AddSupplyGroup(GroupsHandler.GetGroupViaId(groupData.id));
				}
				
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
		public static void Prefix_LogisticManager_GetItemsToDisplayForLogistics(ref bool ignoreLockingConditions, ref List<GroupItem> __result) {
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
				if (gd is GroupDataItem && allGroupsToShow.Any(e => e == gd.id)) {
					((GroupDataItem)gd).displayInLogisticType = DataConfig.LogisticDisplayType.Display;
				}
			}
		}
		// <--- Show Logistic Menu Items ---
		
		// --- Add Name to logistic entities --->
		[HarmonyPrefix]
		[HarmonyPatch(typeof(UiWindowTextInput), nameof(UiWindowTextInput.OnClose))]
		public static void UiWindowTextInput_OnClose(ref WorldObjectText ___worldObjectText, ref TMP_InputField ___inputField, ref UiWindowTextInput __instance) {
			if (!enableMod.Value) return;
			
			TextProxy textProxy = (TextProxy)field_WorldObjectText_proxy.GetValue(___worldObjectText);
			WorldObject worldObject = textProxy.GetComponent<WorldObjectAssociated>().GetWorldObject();
			if (!worldObject.HasLinkedInventory()) return;
			Inventory inventory = InventoriesHandler.Instance.GetInventoryById(worldObject.GetLinkedInventoryId());
			SetLogisticsByName(ref inventory, ___inputField.text);
		}
		
		static List<Group> allGroups;
		private static void SetLogisticsByName(ref Inventory pInventory, string pText) {
			allGroups = GroupsHandler.GetAllGroups();
			
			LogisticEntity logisticEntity = pInventory.GetLogisticEntity();
			string textModified = pText;
			if (string.IsNullOrEmpty(textModified)) {
				logisticEntity.ClearDemandGroups();
				logisticEntity.SetPriority(0);
			}
			// Split id list and priority
			string[] textComponents = textModified.Split(new[]{'+', ':'});
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
					foundGroups.AddRange(allGroups.Where(g => g.GetGroupData() is GroupDataItem).Where(g => ((GroupDataItem) g.GetGroupData()).displayInLogisticType == DataConfig.LogisticDisplayType.Display));
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
				logisticEntity.ClearDemandGroups();
			}
			foreach (Group group in foundGroups) {
				logisticEntity.AddDemandGroup(group);
			}
			
			InventoriesHandler.Instance.UpdateLogisticEntity(pInventory);
		}
		
		private static List<Group> GetGroupsFromString(string possibleId, List<string> logisticGroupSynonymesListSplit) {
			List<Group> foundGroups = new List<Group>();
			
			bool subset = possibleId.EndsWith(">");
			if (subset) possibleId = possibleId.Remove(possibleId.Length - 1);
			
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
					if (allowAnyValue.Value || ((group.GetGroupData() is GroupDataItem) && (((GroupDataItem) group.GetGroupData()).displayInLogisticType == DataConfig.LogisticDisplayType.Display))) {
						foundGroups.Add(group);
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
		public static void Localization_LoadLocalization(ref Dictionary<string, Dictionary<string, string>> ___localizationDictionary, ref string ___currentLangage) {
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
		public static void UiWindowTextInput_SetTextWorldObject(ref TMP_InputField ___inputField) {
			if (!enableMod.Value) return;
			if (!allowLongNames.Value) return;
			
			___inputField.characterLimit = 1000; // Increase character limit
		}
		// <--- Add Name to logistic entities ---
		
		// --- Logistics to String --->
		private static string LogisticsToString(LogisticEntity entity) {
			return string.Join(",", entity.GetDemandGroups().Select(group => (Readable.GetGroupName(group) ?? group.GetId())).ToArray()) + (" +" + entity.GetPriority());
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
		}
		private static Group defaultCopiedLogisticsGroup = null;
		private static Dictionary<Group, LogisticsSettingsAttrib> copiedLogistics = new Dictionary<Group, LogisticsSettingsAttrib>();
		static bool GetCopiedLogistics(Group group, out LogisticsSettingsAttrib lsa) {
			if (defaultCopiedLogisticsGroup == null) defaultCopiedLogisticsGroup = GroupsHandler.GetGroupViaId("Container1");
			if (!copyLogisticsPerGroup.Value) group = defaultCopiedLogisticsGroup;
			return copiedLogistics.TryGetValue(group, out lsa);
		}
		
		static void AddLogisticsSettingsCopy(Group _group, LogisticsSettingsAttrib lsa) {
			if (defaultCopiedLogisticsGroup == null) defaultCopiedLogisticsGroup = GroupsHandler.GetGroupViaId("Container1");
			if (!copyLogisticsPerGroup.Value) _group = defaultCopiedLogisticsGroup;
			
			CanvasPinedRecipes cpr = Managers.GetManager<CanvasPinedRecipes>();
			
			int num = field_CanvasPinedRecipes_groupsAdded(cpr).IndexOf(_group);
			if (num != -1) {
				method_CanvasPinedRecipes_RemovePinedRecipeAtIndex.Invoke(cpr, new object[]{num});
			}
			// build pin entry
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(cpr.informationDisplayerGameObject, cpr.grid.transform);
			InformationDisplayer component = gameObject.GetComponent<InformationDisplayer>();
			LoadDefaultSprites(component);
			component.SetDisplay("", _group.GetImage(), DataConfig.UiInformationsType.OutInventory);
			component.iconContainer.sprite = GetSprite(SpriteType.logistic);
			component.SetGroupList(InfoGroupList(lsa), false);
			gameObject.GetComponentInChildren<GroupList>().SetGridCellSize(new Vector2(42f, 42f), new Vector2(1f, 1f));
			EventsHelpers.AddTriggerEvent(gameObject, EventTriggerType.PointerClick, new Action<EventTriggerCallbackData>(delegate(EventTriggerCallbackData eventTriggerCallbackData){
				CanvasPinedRecipes cpr = Managers.GetManager<CanvasPinedRecipes>();
				int num = field_CanvasPinedRecipes_groupsAdded(cpr).IndexOf(eventTriggerCallbackData.group);
				method_CanvasPinedRecipes_RemovePinedRecipeAtIndex.Invoke(cpr, new object[]{num});
				copiedLogistics.Remove(eventTriggerCallbackData.group);
			}), new EventTriggerCallbackData(_group));
			field_CanvasPinedRecipes_groupsAdded(cpr).Add(_group);
			field_CanvasPinedRecipes_informationDisplayers(cpr).Add(component);
			//this.OnInventoryModified(null, false);
			
			copiedLogistics[_group] = lsa;
		}
		// load temporary/default sprites if logistic selector wasn't opened yet.
		enum SpriteType { logistic, demand, supply, supplyAll, setting, Planet }
		static Dictionary<SpriteType, Sprite> sprites = new Dictionary<SpriteType, Sprite>();
		private static Dictionary<SpriteType, Sprite> defaultSprites = new Dictionary<SpriteType, Sprite>();
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
		}
		private static void LoadDefaultSprites(InformationDisplayer id) {
			if (!defaultSprites.ContainsKey(SpriteType.logistic)) defaultSprites[SpriteType.logistic] = id.spriteOutInventory;
			if (!defaultSprites.ContainsKey(SpriteType.demand)) defaultSprites[SpriteType.demand] = id.spriteOutInventory;
			if (!defaultSprites.ContainsKey(SpriteType.supply)) defaultSprites[SpriteType.supply] = id.spriteInInventory;
			if (!defaultSprites.ContainsKey(SpriteType.setting)) defaultSprites[SpriteType.setting] = id.spriteTutorial;
			if (!defaultSprites.ContainsKey(SpriteType.supplyAll)) defaultSprites[SpriteType.supplyAll] = id.spriteTutorial;
			if (!defaultSprites.ContainsKey(SpriteType.Planet)) defaultSprites[SpriteType.Planet] = id.spriteTutorial;
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
			Texture2D textureWithString = DrawText(lsa.priority.ToString(), System.Drawing.SystemFonts.DefaultFont, System.Drawing.Color.White, System.Drawing.Color.Transparent);
			ingredientsGroupInRecipe.Add(GroupWithCustomIcon(Localization.GetLocalizedString("Logistic_menu_priority") + ": " + ((lsa.priority < -3 || lsa.priority > 3) ? lsa.priority : Localization.GetLocalizedString("Ui_Logistics_Priority" + lsa.priority)), Sprite.Create(textureWithString, new Rect(0.0f, 0.0f, textureWithString.width, textureWithString.height), new Vector2(0.5f, 0.5f), 100.0f)));
			if (lsa.setting > 0) ingredientsGroupInRecipe.Add(GroupWithCustomIcon(Localization.GetLocalizedString("Ui_settings_title") + ": " + Localization.GetLocalizedString("Ui_settings_on")/*"Auto launch: active"*/, GetSprite(SpriteType.setting)));
			if (lsa.linkedPlanet != 0) {
				PlanetData linkedPlanetData = Managers.GetManager<PlanetLoader>().planetList.GetPlanetFromIdHash(lsa.linkedPlanet);
				ingredientsGroupInRecipe.Add(GroupWithCustomIcon(Localization.GetLocalizedString("UI_InterplanetaryExchange_selectedPlanet") + " " + (linkedPlanetData != null ? Readable.GetPlanetLabel(linkedPlanetData) : ""), GetSprite(SpriteType.Planet)));
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
			return (Group)constructor_Group.Invoke(new object[]{iconGroupData});
		}
		private static Texture2D DrawText(String text, System.Drawing.Font font, System.Drawing.Color textColor, System.Drawing.Color backColor) {
			// https://stackoverflow.com/questions/2070365/how-to-generate-an-image-from-text-on-fly-at-runtime
			System.Drawing.Bitmap img = new System.Drawing.Bitmap(1, 1);
			System.Drawing.Graphics drawing = System.Drawing.Graphics.FromImage(img);
			System.Drawing.SizeF textSize = drawing.MeasureString(text, font);
			img.Dispose();
			drawing.Dispose();
			img = new System.Drawing.Bitmap((int) textSize.Width, (int)textSize.Height);
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
			
			CreateCopyLogistics(____logisticEntity, ____inventory);
		}
		private static void CreateCopyLogistics(LogisticEntity logisticEntity, Inventory inventory, Group selectedGroup = null) {
			if (Keyboard.current[copyLogisticsKey.Value].isPressed) {
				WorldObject worldObject = WorldObjectsHandler.Instance.GetWorldObjectForInventory(inventory);
				Group group = worldObject.GetGroup();
				
				LogisticsSettingsAttrib lsa = new LogisticsSettingsAttrib();
				lsa.priority = logisticEntity.GetPriority();
				lsa.demandGroups.UnionWith(logisticEntity.GetDemandGroups());
				lsa.supplyGroups.UnionWith(logisticEntity.GetSupplyGroups());
				lsa.setting = worldObject.GetSetting();
				lsa.groupSelected = selectedGroup;
				lsa.linkedPlanet = linkedPlanetWhenOpened; // Set in ActionOpenable_OpenInventories
				
				AddLogisticsSettingsCopy(group, lsa);
				if (enableNotification.Value) SendNotification("Copied logistics", GetSprite(SpriteType.supply)); // [missing translation]
			}
		}
		// Paste logistics settings for demand, supply, priority and settings (auto-launch / auto-shredding)
		private static int linkedPlanetWhenOpened = 0;
		[HarmonyPrefix]
		[HarmonyPatch(typeof(ActionOpenable), "OpenInventories")]
		public static void ActionOpenable_OpenInventories(ActionOpenable __instance, Inventory objectInventory, WorldObject worldObject) {
			if (!enableMod.Value) return;
			
			// prepare copy --->
			LinkedPlanetProxy lpp = __instance.GetComponentInParent<LinkedPlanetProxy>();
			if (lpp != null) {
				linkedPlanetWhenOpened = lpp.GetLinkedPlanet();
			} else {
				linkedPlanetWhenOpened = 0;
			}
			// <--- prepare copy
			
			if (Keyboard.current[pasteLogisticsKey.Value].isPressed) {
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
					
					
					logisticEntity.GetDemandGroups().Clear();
					logisticEntity.GetDemandGroups().UnionWith(
							allowAnyValue.Value ?
								lsa.demandGroups :
								lsa.demandGroups.Where(g => g.GetGroupData() is GroupDataItem).Where(g => ((GroupDataItem) g.GetGroupData()).displayInLogisticType == DataConfig.LogisticDisplayType.Display)
							); // must run after LogisticSelector_OnGroupDemandSelected
					logisticEntity.GetSupplyGroups().Clear();
					logisticEntity.GetSupplyGroups().UnionWith(
							allowAnyValue.Value ?
								lsa.supplyGroups :
								lsa.supplyGroups.Where(g => g.GetGroupData() is GroupDataItem).Where(g => ((GroupDataItem) g.GetGroupData()).displayInLogisticType == DataConfig.LogisticDisplayType.Display)
							);
					/*logisticEntity.SetDemandGroups(
							allowAnyValue.Value ?
								new HashSet<Group>(lsa.demandGroups) :
								new HashSet<Group>(lsa.demandGroups.Where(g => g.GetGroupData() is GroupDataItem).Where(g => ((GroupDataItem) g.GetGroupData()).displayInLogisticType == DataConfig.LogisticDisplayType.Display))
							); // must run after LogisticSelector_OnGroupDemandSelected
					logisticEntity.SetSupplyGroups(
							allowAnyValue.Value ?
								new HashSet<Group>(lsa.supplyGroups) :
								new HashSet<Group>(lsa.supplyGroups.Where(g => g.GetGroupData() is GroupDataItem).Where(g => ((GroupDataItem) g.GetGroupData()).displayInLogisticType == DataConfig.LogisticDisplayType.Display))
							);*/
					SettingProxy sp = __instance.GetComponentInParent<SettingProxy>();
					if (sp != null) sp.SetSetting(lsa.setting);
					MachineRocketBackAndForthInterplanetaryExchange exchangeRocket = __instance.GetComponentInParent<MachineRocketBackAndForthInterplanetaryExchange>();
					if (exchangeRocket != null) exchangeRocket.SetLinkedPlanet(Managers.GetManager<PlanetLoader>().planetList.GetPlanetFromIdHash(lsa.linkedPlanet));
					if (enableNotification.Value) SendNotification("Inserted logistics", GetSprite(SpriteType.demand)); // [missing translation]
					InventoriesHandler.Instance.UpdateLogisticEntity(objectInventory);
				}
			}
		}
		// Paste group selection (ore extractor T3, gas extractor T2, Harvester, delivery depot)
		private static void SetSelectedGroupFromCopy(UiWindowGroupSelector __instance, Inventory ____inventoryRight, List<GroupData> allowedGroups = null) {
			if (!enableMod.Value) return;
			
			if (!Keyboard.current[pasteLogisticsKey.Value].isPressed) return;
			
			Group group = WorldObjectsHandler.Instance.GetWorldObjectForInventory(____inventoryRight).GetGroup();
			if (GetCopiedLogistics(group, out LogisticsSettingsAttrib lsa)) {
				if ((!copyLogisticsPerGroup.Value) && (lsa.groupSelected == null)) { // Note: Can't clear selected group when copyLogisticsPerGroup is false
					return;
				}
				if (!allowAnyValue.Value && lsa.groupSelected != null && allowedGroups != null && !allowedGroups.Contains(lsa.groupSelected.GetGroupData())) {
					if (enableNotification.Value) SendNotification("Group not selectable here"); // [missing translation]
					return;
				}
				AccessTools.Method(typeof(UiWindowGroupSelector), "OnGroupSelected").Invoke(__instance, new object[]{lsa.groupSelected});
				if (enableNotification.Value) SendNotification("Set selected Group", GetSprite(SpriteType.demand)); // [missing translation]
			}
		}
		[HarmonyPrefix] // OnOpen or SetGroupSelectorWorldObject not possible because ActionGroupSelector.OpenInventories calls SetInventories after them
		[HarmonyPatch(typeof(UiWindowGroupSelector), nameof(UiWindowGroupSelector.OnOpenAutoCrafter))]
		public static void UiWindowGroupSelector_OnOpenAutoCrafter(UiWindowGroupSelector __instance, Inventory ____inventoryRight) {
			SetSelectedGroupFromCopy(__instance, ____inventoryRight);
		}
		[HarmonyPrefix]
		[HarmonyPatch(typeof(UiWindowGroupSelector), nameof(UiWindowGroupSelector.OnOpenOreExtractor))]
		public static void UiWindowGroupSelector_OnOpenOreExtractor(UiWindowGroupSelector __instance, Inventory ____inventoryRight, List<GroupData> groupsData) {
			List<GroupData> availableGroups = new List<GroupData>();
			availableGroups.AddRange(groupsData);
			availableGroups.AddRange(__instance.groupSelector.GetAddedGroups().Select(e => e.GetGroupData()));
			SetSelectedGroupFromCopy(__instance, ____inventoryRight, availableGroups);
		}
		[HarmonyPrefix]
		[HarmonyPatch(typeof(UiWindowGroupSelector), nameof(UiWindowGroupSelector.OnOpenInterplanetaryDepot))]
		public static void UiWindowGroupSelector_OnOpenInterplanetaryDepot(UiWindowGroupSelector __instance, Inventory ____inventoryRight) {
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
		// <--- Copy Logistics ---
		
		private static void SendNotification(string text, Sprite sprite = null, float timeShown = 2f) {
			field_PopupsHandler_popupsToPop(Managers.GetManager<PopupsHandler>()).Add(new PopupData(((sprite != null) ? sprite : Sprite.Create(Texture2D.blackTexture, new Rect(0.0f, 0.0f, 4, 4), new Vector2(0.5f, 0.5f), 100.0f)), text, timeShown, true));
		}
		
		private static void printTime() {
			Console.WriteLine("Local date and time: {0}", DateTime.Now.ToString(new CultureInfo("de-DE")));
			Console.WriteLine(new System.Diagnostics.StackTrace(true).ToString());
		}
	}
}

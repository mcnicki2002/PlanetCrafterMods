// Copyright 2025-2025 Nicolas Schäfer & Contributors
// Licensed under Apache License, Version 2.0

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SpaceCraft;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Nicki0.FeatPortalTeleport {

	[BepInPlugin("Nicki0.theplanetcraftermods.FeatPortalTeleport", "(Feat) Portal Travel", PluginInfo.PLUGIN_VERSION)]
	public class Plugin : BaseUnityPlugin {

		/*
			TODO:
		*/




		static ManualLogSource log;

		public static ConfigEntry<bool> configEnableDebug;
		public static ConfigEntry<bool> configRequireCost;
		public static ConfigEntry<bool> configRequireFullTerraformation;
		public static ConfigEntry<bool> configDeletePortalsFromMoonsWhenModIsLost;

		static MethodInfo method_PlanetNetworkLoader_SwitchToPlanetClientRpc;
		static AccessTools.FieldRef<Recipe, List<Group>> field_Recipe_recipe;
		static AccessTools.FieldRef<WorldInstanceHandler, List<MachinePortalGenerator>> field_WorldInstanceHandler__allMachinePortalGenerator;
		static AccessTools.FieldRef<PopupsHandler, List<PopupData>> field_PopupsHandler_popupsToPop;

		private void Awake() {
			log = Logger;

			configRequireCost = Config.Bind<bool>("General", "requireCost", false, "Opening the Portal to another planet costs one Fusion Energy Cell");
			configRequireFullTerraformation = Config.Bind<bool>("General", "requireFullTerraformation", true, "Requires the source and destination planet to be terraformed to stage \"Complete\"");
			configEnableDebug = Config.Bind<bool>("Debug", "enableDebug", false, "Enable debug messages");
			configDeletePortalsFromMoonsWhenModIsLost = Config.Bind<bool>("Debug", "deleteMoonPortals", true, "Savety mechanism. Portals on Moons will be deleted if the mod doesn't get loaded, as they aren't constructable on moons in the base game.");

			method_PlanetNetworkLoader_SwitchToPlanetClientRpc = AccessTools.Method(typeof(PlanetNetworkLoader), "SwitchToPlanetClientRpc");
			field_Recipe_recipe = AccessTools.FieldRefAccess<Recipe, List<Group>>("_ingredientsGroups");
			field_WorldInstanceHandler__allMachinePortalGenerator = AccessTools.FieldRefAccess<WorldInstanceHandler, List<MachinePortalGenerator>>("_allMachinePortalGenerator");
			field_PopupsHandler_popupsToPop = AccessTools.FieldRefAccess<PopupsHandler, List<PopupData>>("popupsToPop");

			// Plugin startup logic
			Harmony.CreateAndPatchAll(typeof(Plugin));
			Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
		}

		private static int planetToTeleportToHash = 0;

		static GameObject buttonTabProceduralInstance = null;
		static GameObject buttonTabPortalTravel = null;
		[HarmonyPostfix]
		[HarmonyPatch(typeof(UiWindowPortalGenerator), "Start")]
		static void UiWindowPortalGenerator_Start(UiWindowPortalGenerator __instance) {
			buttonTabProceduralInstance = CreateButton(__instance, "ButtonProceduralInstance", new Vector3(110, 880, 0), "MainScene/BaseStack/UI/WindowsHandler/UiWindowInterplanetaryExhange/Container/ContentRocketOnSite/RightContent/SelectedPlanet/PlanetIcon"); //(100, 860, 0)
			buttonTabPortalTravel = CreateButton(__instance, "ButtonPortalTravel", new Vector3(110, 780, 0), "MainScene/BaseStack/UI/WindowsHandler/UiWindowInterplanetaryExhange/Container/Title/Image"); //(100, 680, 0)
																																																		   // Top Button
			buttonTabProceduralInstance.GetComponent<Button>().onClick.AddListener(delegate () {
				if (Managers.GetManager<WorldInstanceHandler>().GetOpenedWorldInstanceData() != null) {
					return;
				}

				bool onExcludedPlanet = planetsExcludingPortalGenerator.Contains(Managers.GetManager<PlanetLoader>().GetCurrentPlanetData()?.GetPlanetHash() ?? 0);
				foreach (Transform transform in __instance.transform.Find("Container/UiPortalList/InstancesList/Grid")) {
					if (transform.name == "UiWorldInstanceLine(Clone)") {
						if (!onExcludedPlanet) {
							transform.gameObject.SetActive(true);
						}
					} else if (transform.name.Contains("UiWorldInstanceSelector_PlanetTravel")) {
						transform.gameObject.SetActive(false);
					}
				}

				SetUiVisibility(true, __instance);
				__instance.btnScan.SetActive(true);
			});

			// Bottom Button
			buttonTabPortalTravel.GetComponent<Button>().onClick.AddListener(delegate () {
				if (Managers.GetManager<WorldInstanceHandler>().GetOpenedWorldInstanceData() != null) {
					return;
				}

				bool foundPlanetInstanceSelector = false;
				foreach (Transform transform in __instance.transform.Find("Container/UiPortalList/InstancesList/Grid")) {
					if (transform.name == "UiWorldInstanceLine(Clone)") {
						transform.gameObject.SetActive(false);
					} else if (transform.name.Contains("UiWorldInstanceSelector_PlanetTravel")) {
						transform.gameObject.SetActive(true);
						foundPlanetInstanceSelector = true;
					}
				}
				if (!foundPlanetInstanceSelector) AddPortals(__instance);

				SetUiVisibility(false, __instance);
				__instance.btnScan.SetActive(false);
			});
		}
		private static GameObject CreateButton(UiWindowPortalGenerator __instance, string name, Vector3 pos, string imagePath) {
			TMP_DefaultControls.Resources resourcesA = new TMP_DefaultControls.Resources();
			GameObject button = TMP_DefaultControls.CreateButton(resourcesA);

			button.transform.SetParent(__instance.transform, false);
			button.name = name;
			button.transform.position = pos;
			button.GetComponent<RectTransform>().sizeDelta = new Vector2(60, 60);
			button.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "";
			button.AddComponent<EventHoverIncrease>().SetHoverGroupEvent();
			button.GetComponent<Image>().sprite = GameObject.Find(imagePath).GetComponent<Image>().sprite;

			GameObject backgroundImageGameObjectA = new GameObject("BackgroundHexagonImage");
			backgroundImageGameObjectA.transform.SetParent(button.transform, false);
			backgroundImageGameObjectA.transform.localScale = new Vector3(1f * 0.85f, 1.15f * 0.85f, 1f * 0.85f)/*(1.1f, 1.28f, 1.1f)*/;
			backgroundImageGameObjectA.AddComponent<Image>().sprite = GameObject.Find("MainScene/BaseStack/UI/WindowsHandler/UiWindowInterplanetaryExhange/Container/CloseUiButton").GetComponentInChildren<Image>().sprite;

			button.SetActive(true);

			return button;
		}

		private static string originalTitle = "";
		private static void SetUiVisibility(bool active, UiWindowPortalGenerator __instance) {
			__instance.transform.Find("Container/UiPortalList/InstancesList/CategoryTitles/RarityLevel").gameObject.SetActive(active);
			__instance.transform.Find("Container/UiPortalList/InstancesList/CategoryTitles/DifficultyLevel").gameObject.SetActive(active);
			__instance.transform.Find("Container/UiPortalList/InstancesList/CategoryTitles/Recipe").gameObject.SetActive(active || configRequireCost.Value);
			TMPro.TextMeshProUGUI tmp = __instance.transform.Find("Container/Title").GetComponent<TMPro.TextMeshProUGUI>();
			if (string.IsNullOrEmpty(originalTitle)) originalTitle = tmp.text;
			tmp.text = active ? originalTitle : "Portal travel";
		}
		[HarmonyPostfix]
		[HarmonyPatch(typeof(UiWindowPortalGenerator), nameof(UiWindowPortalGenerator.OnOpen))]
		static void UiWindowPortalGenerator_OnOpen(UiWindowPortalGenerator __instance) {
			__instance.StartCoroutine(hideButtonBOnOpen());
			if (Managers.GetManager<WorldInstanceHandler>().GetOpenedWorldInstanceData() != null) {
				if (buttonTabPortalTravel != null) buttonTabPortalTravel.SetActive(false);
			}

			SetUiVisibility(true, __instance);
		}
		private static IEnumerator hideButtonBOnOpen() { // Hide buttonTabPortalTravel because on first load, buttonTabPortalTravel == null
			yield return new WaitForSeconds(0.01f);
			if (Managers.GetManager<WorldInstanceHandler>().GetOpenedWorldInstanceData() != null) {
				if (buttonTabPortalTravel != null) buttonTabPortalTravel.SetActive(false);
			}
		}
		[HarmonyPostfix]
		[HarmonyPatch(typeof(UiWindowPortalGenerator), nameof(UiWindowPortalGenerator.ShowOpenedInstance))]
		static void UiWindowPortalGenerator_ShowOpenedInstance() {
			if (buttonTabPortalTravel != null) buttonTabPortalTravel.SetActive(false);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(UiWindowPortalGenerator), "ShowUiWindows")]
		static void UiWindowPortalGenerator_ShowUiWindows(UiWindowPortalGenerator __instance) {
			if (buttonTabProceduralInstance != null && buttonTabPortalTravel != null) {
				buttonTabProceduralInstance.SetActive(!__instance.uiOnScanning.activeSelf);
				buttonTabPortalTravel.SetActive(!__instance.uiOnScanning.activeSelf);
			}
			if (planetsExcludingPortalGenerator.Contains(Managers.GetManager<PlanetLoader>().GetCurrentPlanetData()?.GetPlanetHash() ?? 0)) {
				foreach (Transform transform in __instance.transform.Find("Container/UiPortalList/InstancesList/Grid")) {
					if (transform.name == "UiWorldInstanceLine(Clone)") {
						transform.gameObject.SetActive(false);
						SetUiVisibility(false, __instance);
					}
				}
			}
		}

		private static GameObject gameObjectForOpeningPortals;
		static void AddPortals(UiWindowPortalGenerator __instance) {
			PlayerMainController pmc = Managers.GetManager<PlayersManager>().GetActivePlayerController();
			WorldUnitsHandler wuh = Managers.GetManager<WorldUnitsHandler>();
			Group groupPortalGenerator = GroupsHandler.GetGroupViaId("PortalGenerator1");

			bool worldInstanceSelectorAdded = false;

			bool isCurrentPlanetTerraformed = false;
			if (configRequireFullTerraformation.Value) {
				PlanetData pd = Managers.GetManager<PlanetLoader>().GetCurrentPlanetData();
				if (wuh.AreUnitsInited(pd.GetPlanetId())) {
					List<TerraformStage> terraformStages = pd.GetPlanetTerraformationStages().Where(stage => stage.GetTerraId() == "Complete").ToList();
					if (terraformStages.Count() > 0) {
						isCurrentPlanetTerraformed = wuh.GetUnit(DataConfig.WorldUnitType.Terraformation, pd.GetPlanetId()).GetValue() >= terraformStages.First().GetStageStartValue();
					} else {
						if (configEnableDebug.Value) log.LogInfo(pd.GetPlanetId() + " Does not have a \"Complete\" stage");
					}
				} else if (configEnableDebug.Value) log.LogInfo(pd.GetPlanetId() + " Units not initialized");
			} else {
				isCurrentPlanetTerraformed = true;
			}


			foreach (PlanetData pd in Managers.GetManager<PlanetLoader>().planetList.GetPlanetList()) {
				if (!isCurrentPlanetTerraformed) break;

				if (pd.GetPlanetId() == Managers.GetManager<PlanetLoader>().GetCurrentPlanetData().GetPlanetId()) continue;

				if (!wuh.AreUnitsInited(pd.GetPlanetId())) {
					if (configEnableDebug.Value) log.LogInfo(pd.GetPlanetId() + " Units not initialized");
					continue;
				}

				double completeTi = 0;

				if (configRequireFullTerraformation.Value) {
					List<TerraformStage> terraformStages = pd.GetPlanetTerraformationStages().Where(stage => stage.GetTerraId() == "Complete").ToList();
					if (terraformStages.Count() > 0) {
						completeTi = terraformStages.First().GetStageStartValue();
					} else {
						if (configEnableDebug.Value) log.LogInfo(pd.GetPlanetId() + " Does not have a \"Complete\" stage");
					}
				}

				if (wuh.GetUnit(DataConfig.WorldUnitType.Terraformation, pd.GetPlanetId()).GetValue() < completeTi) continue;

				WorldObject woPortalGenerator = WorldObjectsHandler.Instance.GetFirstWorldObjectOfGroup(groupPortalGenerator, pd.GetPlanetHash());
				if (woPortalGenerator == null) continue;



				GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(__instance.uiWorldInstanceSelector, __instance.gridForInstances.transform);
				gameObject.name = "UiWorldInstanceSelector_PlanetTravel";
				Recipe recipe = new Recipe(new List<GroupDataItem>());
				if (configRequireCost.Value) field_Recipe_recipe(recipe).Add(GroupsHandler.GetGroupViaId("FusionEnergyCell"));
				List<bool> list = pmc.GetPlayerBackpack().GetInventory().ItemsContainsStatus(recipe.GetIngredientsGroupInRecipe());
				gameObject.GetComponent<UiWorldInstanceSelector>().SetValues(
							new WorldInstanceData(pd.GetPlanetId(), pd.GetPlanetHash(), 0, recipe, 0, 0),
							recipe.GetIngredientsGroupInRecipe(),
							list,
							new Action<UiWorldInstanceSelector>(delegate (UiWorldInstanceSelector uiWorldInstanceSelector) {

								InventoriesHandler.Instance.RemoveItemsFromInventory(
										uiWorldInstanceSelector.GetAssociatedWorldInstanceData().GetRecipe().GetIngredientsGroupInRecipe(),
										Managers.GetManager<PlayersManager>().GetActivePlayerController().GetPlayerBackpack().GetInventory(),
										true, true, null);

								planetToTeleportToHash = pd.GetPlanetHash();

								List<MachinePortalGenerator> allMachinePortalGenerators = field_WorldInstanceHandler__allMachinePortalGenerator(Managers.GetManager<WorldInstanceHandler>());
								foreach (MachinePortalGenerator mpg in allMachinePortalGenerators) {
									if (!mpg.gameObject.activeInHierarchy) continue;
									if (gameObjectForOpeningPortals == null) gameObjectForOpeningPortals = new GameObject();
									mpg.OpenPortal(null, gameObjectForOpeningPortals, false);
									portalCreatedByMod = true;
								}

								Managers.GetManager<WindowsHandler>().CloseAllWindows();
							}), false);

				worldInstanceSelectorAdded = true;
			}
			if (!worldInstanceSelectorAdded) {
				GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(__instance.uiWorldInstanceSelector, __instance.gridForInstances.transform);
				gameObject.name = "UiWorldInstanceSelector_PlanetTravelInfo";
				Recipe recipe = new Recipe(new List<GroupDataItem>());
				List<bool> list = new List<bool>() { };
				gameObject.GetComponent<UiWorldInstanceSelector>().SetValues(new WorldInstanceData("PlanetTravelInfo", -10, 0, recipe, 0, 0), recipe.GetIngredientsGroupInRecipe(), list, new Action<UiWorldInstanceSelector>(delegate (UiWorldInstanceSelector uiWorldInstanceSelector) { }), false);

				gameObject.transform.Find("ContentContainer/Name").GetComponent<RectTransform>().sizeDelta = new Vector2(1000, 80);
				gameObject.GetComponent<UiWorldInstanceSelector>().buttonOpen.transform.position = new Vector3(10000, 0, 0);
				gameObject.GetComponent<UiWorldInstanceSelector>().buttonClose.transform.position = new Vector3(10000, 0, 0);
			}
		}

		private static bool portalCreatedByMod = false;
		[HarmonyPrefix]
		[HarmonyPatch(typeof(UiWindowPortalGenerator), nameof(UiWindowPortalGenerator.OnOpenInstance))]
		private static void UiWindowPortalGenerator_OnOpenInstance() {
			portalCreatedByMod = false;
		}

		private static bool semaphoreActive = false;
		[HarmonyPrefix]
		[HarmonyPatch(typeof(PlanetLoader), "HandleDataAfterLoad")]
		private static void PlanetLoader_HandleDataAfterLoad() {
			if (semaphoreActive) {
				Managers.GetManager<SavedDataHandler>().DecrementSaveLock();// semaphore unlock saving
				semaphoreActive = false;
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(WorldInstanceData), nameof(WorldInstanceData.GetSeedLabel))]
		private static void WorldInstanceData_GetSeedLabel(int ____seed, ref string ____seedLabel, ref string __result) {
			if (____seed == -10) {
				string labelString = Localization.GetLocalizedString("PortalTeleport_InfoNoPlanetsAvailable") ?? "";
				____seedLabel = labelString;
				__result = labelString.ToString();
				return;
			}

			PlanetData pd = Managers.GetManager<PlanetLoader>().planetList.GetPlanetFromIdHash(____seed);
			if (pd == null) return;
			____seedLabel = pd.GetPlanetId();
			__result = pd.GetPlanetId();
		}

		// prevent difficulty and rarity strings from displaying
		[HarmonyPostfix]
		[HarmonyPatch(typeof(UiWorldInstanceSelector), nameof(UiWorldInstanceSelector.SetValues))]
		private static void UiWorldInstanceSelector_SetValues(WorldInstanceData worldInstanceData, TextMeshProUGUI ___difficulty, TextMeshProUGUI ___rarity) {
			PlanetData pd = Managers.GetManager<PlanetLoader>().planetList.GetPlanetFromIdHash(worldInstanceData.GetSeed());
			if (pd == null && worldInstanceData.GetSeed() != -10) return;
			___difficulty.text = "";
			___rarity.text = "";
		}

		[HarmonyPrefix] // do not execute WorldInstanceHandler.SetWorldInstanceActive(true)
		[HarmonyPatch(typeof(MachinePortal), "GoInsidePortal")]
		private static void Pre_MachinePortal_GoInsidePortal(ref bool ____enterPortal, ref bool __state) {
			__state = ____enterPortal;
			if (portalCreatedByMod) ____enterPortal = false;
		}
		[HarmonyPostfix]
		[HarmonyPatch(typeof(MachinePortal), "GoInsidePortal")]
		private static void Post_MachinePortal_GoInsidePortal(ref bool ____enterPortal, ref bool __state) {
			____enterPortal = __state;
		}
		[HarmonyPrefix]
		[HarmonyPatch(typeof(MachinePortal), "GoToFinalPosition")]
		private static void MachinePortal_GoToFinalPosition(ref bool ____playerInTunel, InputActionReference[] ___inputsToDisableInTunnel) {
			if (portalCreatedByMod && ____playerInTunel) {
				____playerInTunel = false; // also prevents the execution of GoToFinalPosition instead of return false;
				MachineBeaconUpdater.HideBeacons = false;
				InputActionReference[] array = ___inputsToDisableInTunnel;
				for (int i = 0; i < array.Length; i++) {
					array[i].action.Enable();
				}

				List<MachinePortalGenerator> allMachinePortalGenerators = field_WorldInstanceHandler__allMachinePortalGenerator(Managers.GetManager<WorldInstanceHandler>());
				foreach (MachinePortalGenerator mpg in allMachinePortalGenerators) {
					if (!mpg.gameObject.activeInHierarchy) continue;
					mpg.ClosePortal();
				}

				//Managers.GetManager<SavedDataHandler>().DecrementSaveLock();
				// ^v will lift each other, but for testing, keeping both: top from game code, bottom from own planet switch
				//Managers.GetManager<SavedDataHandler>().IncrementSaveLock(); // semaphore lock saving
				semaphoreActive = true;
				PlanetData pd = Managers.GetManager<PlanetLoader>().planetList.GetPlanetFromIdHash(planetToTeleportToHash);
				if (pd != null) PlanetNetworkLoader.Instance.SwitchToPlanet(pd);
			}
		}



		// --- Prevent the creation and deletion of capsules --->
		static bool disablePlanetListGetPlanetIndex = false;
		static int? planetIndexFromGetPlanetIndex = null;
		[HarmonyPrefix]
		[HarmonyPatch(typeof(PlanetNetworkLoader), "SwitchToPlanetServerRpc")]
		private static void Pre_PlanetNetworkLoader_SwitchToPlanetServerRpc() {
			if (portalCreatedByMod) {
				disablePlanetListGetPlanetIndex = true;
			}
		}
		[HarmonyPostfix]
		[HarmonyPatch(typeof(PlanetList), nameof(PlanetList.GetPlanetIndex))]
		private static void PlanetList_GetPlanetIndex(ref int __result) {
			if (disablePlanetListGetPlanetIndex) {
				planetIndexFromGetPlanetIndex = __result;
				__result = -1; // return result as if planet doesn't exist
			}
		}
		[HarmonyPostfix]
		[HarmonyPatch(typeof(PlanetNetworkLoader), "SwitchToPlanetServerRpc")]
		private static void Post_PlanetNetworkLoader_SwitchToPlanetServerRpc(int planetHash, PlanetNetworkLoader __instance, ref NetworkVariable<short> ____planetIndex) {
			disablePlanetListGetPlanetIndex = false;
			if (planetIndexFromGetPlanetIndex != null) {
				____planetIndex.Value = (short)planetIndexFromGetPlanetIndex;
				planetIndexFromGetPlanetIndex = null;

				Vector3 pos = Vector3.zero;
				Quaternion rot = Quaternion.identity;

				PlanetData pd = Managers.GetManager<PlanetLoader>().planetList.GetPlanetFromIdHash(planetHash);

				WorldObject woPortalGenerator = WorldObjectsHandler.Instance.GetFirstWorldObjectOfGroup(GroupsHandler.GetGroupViaId("PortalGenerator1"), pd.GetPlanetHash());
				if (woPortalGenerator != null) { // should never be null as the portal should only show if there is a portal generator on the planet
					pos = woPortalGenerator.GetPosition();
					rot = woPortalGenerator.GetRotation();
				}
				// ClientRpc has to be manually invoked as it isn't executed because the planetIndex returns -1 (see PlanetList.GetPlanetIndex patch)
				//this.SwitchToPlanetClientRpc(____planetIndex.Value, vector + new Vector3(0f, 1f, 0f), (int)quaternion.eulerAngles.y);
				method_PlanetNetworkLoader_SwitchToPlanetClientRpc.Invoke(__instance, new object[] { ____planetIndex.Value, pos + new Vector3(0, 7, 0), (int)rot.eulerAngles.y + 90 });

				portalCreatedByMod = false;
			}
		}
		// <--- Prevent the creation and deletion of capsules ---

		// --- Activate Portal everywhere --->
		private static HashSet<int> planetsExcludingPortalGenerator = new HashSet<int>();
		[HarmonyPrefix]
		[HarmonyPatch(typeof(StaticDataHandler), "LoadStaticData")]
		static void StaticDataHandler_LoadStaticData(ref List<GroupData> ___groupsData) {
			GroupDataConstructible portalGroupDataContructible = ___groupsData.Find(e => e.id == "PortalGenerator1") as GroupDataConstructible;
			if (portalGroupDataContructible == null) {
				if (configEnableDebug.Value) log.LogError("PortalGenerator1 not found");
				return;
			}
			foreach (PlanetData pd in portalGroupDataContructible.notAllowedPlanetsRequirement) planetsExcludingPortalGenerator.Add(pd.GetPlanetHash());
			portalGroupDataContructible.notAllowedPlanetsRequirement = new List<PlanetData>();
		}
		[HarmonyPrefix]
		[HarmonyPatch(typeof(UiWindowPortalGenerator), "SelectFirstButtonInGrid")]
		static void UiWindowPortalGenerator_SelectFirstButtonInGrid(UiWindowPortalGenerator __instance) {
			if (planetsExcludingPortalGenerator.Contains(Managers.GetManager<PlanetLoader>().GetCurrentPlanetData()?.GetPlanetHash() ?? 0)) {
				foreach (Transform transform in __instance.transform.Find("Container/UiPortalList/InstancesList/Grid")) {
					if (transform.name == "UiWorldInstanceLine(Clone)") {
						transform.gameObject.SetActive(false);
					}
				}
			}
		}
		[HarmonyPostfix]
		[HarmonyPatch(typeof(UiWindowPortalGenerator), "OnChoicesUpdated")]
		static void UiWindowPortalGenerator_OnChoicesUpdated(UiWindowPortalGenerator __instance) {
			if (planetsExcludingPortalGenerator.Contains(Managers.GetManager<PlanetLoader>().GetCurrentPlanetData()?.GetPlanetHash() ?? 0)) {
				field_PopupsHandler_popupsToPop(Managers.GetManager<PopupsHandler>()).Add(new PopupData(
						Sprite.Create(Texture2D.blackTexture, new Rect(0.0f, 0.0f, 4, 4), new Vector2(0.5f, 0.5f), 100.0f),
						Localization.GetLocalizedString("Ui_Alert_buildConstraint_portalsOnMoons"),
						5f,
						true));
			}

			SetUiVisibility(true, __instance);
		}
		[HarmonyPrefix]
		[HarmonyPatch(typeof(UiWindowPortalGenerator), "CreateMapMarker")]
		static bool UiWindowPortalGenerator_CreateMapMarker(UiWindowPortalGenerator __instance) {
			if (planetsExcludingPortalGenerator.Contains(Managers.GetManager<PlanetLoader>().GetCurrentPlanetData()?.GetPlanetHash() ?? 0)) {
				return false;
			}
			return true;
		}
		// <--- Activate Portal everywhere ---

		// --- prevent loading of portals on moons --->
		[HarmonyPostfix]
		[HarmonyPatch(typeof(JsonablesHelper), "WorldObjectToJsonable")]
		static void JsonablesHelper_WorldObjectToJsonable(JsonableWorldObject __result) {
			if (!planetsExcludingPortalGenerator.Contains(__result.planet)) return;
			if (__result.gId != "PortalGenerator1") return;
			if (!configDeletePortalsFromMoonsWhenModIsLost.Value) return;
			__result.gId = "PortalGenerator1_OnMoon";
		}
		[HarmonyPostfix]
		[HarmonyPatch(typeof(JSONExport), nameof(JSONExport.LoadFromJson))]
		static void JSONExport_LoadFromJson(List<JsonableWorldObject> ____worldObjects) {
			foreach (JsonableWorldObject jwo in ____worldObjects) {
				if (jwo.gId == "PortalGenerator1_OnMoon") jwo.gId = "PortalGenerator1";
			}

		}
		// <--- prevent loading of portals on moons ---

		[HarmonyPostfix]
		[HarmonyPatch(typeof(Localization), "GetLocalizedString")]
		private static void Localization_GetLocalizedString(string stringCode, ref string __result) {
			if (stringCode == "PortalTeleport_InfoNoPlanetsAvailable") {
				__result = "Travel requirements not met. Source and destination planet need a portal" + (configRequireFullTerraformation.Value ? " and a portal travel license (issued by " + Localization.GetLocalizedString("MessageSender_GalacticTribunal") + " at full terraformation)" : "") + ".";
				return;
			}
			if (stringCode == "UI_portals_Instructions1") {
				__result = __result + ". Portal travel requires a portal on source and destination planet and no other portal may be open.";
				return;
			}
		}
	}
}

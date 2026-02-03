// Copyright 2025-2026 Nicolas Schäfer & Contributors
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
using static UnityEngine.ParticleSystem;

namespace Nicki0.FeatPortalTeleport {

	[BepInPlugin("Nicki0.theplanetcraftermods.FeatPortalTeleport", "(Feat) Portal Travel", PluginInfo.PLUGIN_VERSION)]
	public class Plugin : BaseUnityPlugin {

		/*
		 *	TODO:
		 *	
		 *	
		 *	BUGS:
		 *	
		 *	Clients can close all portals by closing the procedural instance
		 */
		private static bool enableKeepPortalOpen = true;
		private static bool enableColorPortals = true;

		static Plugin Instance;
		static ManualLogSource log;

		private static readonly int DataFormatVersion = 1;
		private static SaveState saveState;

		public static ConfigEntry<bool> configEnableDebug;
		public static ConfigEntry<bool> configRequireCost;
		public static ConfigEntry<string> configItemsCost;
		public static ConfigEntry<bool> configRequireFullTerraformation;
		public static ConfigEntry<bool> configDisableOtherRequirements;
		public static ConfigEntry<bool> configDeletePortalsFromMoonsWhenModIsLost;

		public static ConfigEntry<bool> configKeepPortalsOpen;
		public static ConfigEntry<bool> configSetColorPortals;
		public static ConfigEntry<string> configColorPortalsColors;
		public static ConfigEntry<float> configTimeInPortal;
		public static ConfigEntry<bool> configEnableStrudel;

		static MethodInfo method_PlanetNetworkLoader_SwitchToPlanetClientRpc;
		static MethodInfo method_MachinePortalGenerator_SetParticles;
		static MethodInfo method_UiWindowPortalGenerator_SelectFirstButtonInGrid;
		static AccessTools.FieldRef<Recipe, List<Group>> field_Recipe_recipe;
		static AccessTools.FieldRef<WorldInstanceHandler, List<MachinePortalGenerator>> field_WorldInstanceHandler__allMachinePortalGenerator;
		static AccessTools.FieldRef<PopupsHandler, List<PopupData>> field_PopupsHandler_popupsToPop;


		private void Awake() {
			log = Logger;
			Instance = this;

			if (LibCommon.ModVersionCheck.Check(this, Logger.LogInfo)) {
				LibCommon.ModVersionCheck.NotifyUser(this, Logger.LogInfo);
			}

			configRequireCost = Config.Bind<bool>("General", "requireCost", false, "Opening the Portal to another planet costs one Fusion Energy Cell");
			configItemsCost = Config.Bind<string>("General", "costItems", "FusionEnergyCell", "Cost to open a portal (Comma separated list of item IDs)");
			configRequireFullTerraformation = Config.Bind<bool>("General", "requireFullTerraformation", true, "Requires the source and destination planet to be terraformed to stage \"Complete\"");
			configDisableOtherRequirements = Config.Bind<bool>("General", "disableOtherRequirements", false, "Disables other requirements, e.g. minimum Terraformation / Purification requirements.");
			configEnableDebug = Config.Bind<bool>("Debug", "enableDebug", false, "Enable debug messages");
			configDeletePortalsFromMoonsWhenModIsLost = Config.Bind<bool>("Debug", "deleteMoonPortals", true, "Savety mechanism. Portals on Moons will be deleted if the mod doesn't get loaded, as they aren't constructable on moons in the base game.");

			configKeepPortalsOpen = Config.Bind<bool>("General", "keepPortalsOpen", true, "Keep portal open instead of closing them after traveling");
			configSetColorPortals = Config.Bind<bool>("Color", "activateColoredPortals", true, "Opened Portals are colored depending on their destination. Only works if keepPortalsOpen = true");
			configColorPortalsColors = Config.Bind<string>("Color", "portalDestinationColors", "Prime: 200, 55, 0, 1; Humble: 30, 30, 25, 1; Selenea: 32, 80, 16, 1; Aqualis: 0, 100, 100, 1; Toxicity: 192, 192, 0, 1", "Color of a portal connected to a planet (RGB/RGBA)");
			configEnableStrudel = Config.Bind<bool>("Color", "coneShapedPortals", true, "Activate cone shape of the portal");
			configTimeInPortal = Config.Bind<float>("General", "timeInPortal", 0, "Time in seconds for how long the player remains inside the portal animation. Default time: 5s. Set to 0 to use default time.");

			enableKeepPortalOpen = configKeepPortalsOpen.Value;
			enableColorPortals = configSetColorPortals.Value;
			if (enableKeepPortalOpen) { SetColorConfig(); }

			method_PlanetNetworkLoader_SwitchToPlanetClientRpc = AccessTools.Method(typeof(PlanetNetworkLoader), "SwitchToPlanetClientRpc");
			method_MachinePortalGenerator_SetParticles = AccessTools.Method(typeof(MachinePortalGenerator), "SetParticles");
			method_UiWindowPortalGenerator_SelectFirstButtonInGrid = AccessTools.Method(typeof(UiWindowPortalGenerator), "SelectFirstButtonInGrid");
			field_Recipe_recipe = AccessTools.FieldRefAccess<Recipe, List<Group>>("_ingredientsGroups");
			field_WorldInstanceHandler__allMachinePortalGenerator = AccessTools.FieldRefAccess<WorldInstanceHandler, List<MachinePortalGenerator>>("_allMachinePortalGenerator");
			field_PopupsHandler_popupsToPop = AccessTools.FieldRefAccess<PopupsHandler, List<PopupData>>("popupsToPop");

			saveState = new SaveState(typeof(Plugin), DataFormatVersion);

			// Plugin startup logic
			Harmony.CreateAndPatchAll(typeof(Plugin));
			Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
		}

		public static void OnModConfigChanged(ConfigEntryBase _) {
			if (enableKeepPortalOpen) { SetColorConfig(); }
		}

		private static Dictionary<int, int> woidsToPlanetidhashstate = null;
		private static Dictionary<int, int> GetWoIdsToPlanetIdHashes() {
			if (woidsToPlanetidhashstate != null) { return woidsToPlanetidhashstate; }

			switch (saveState.GetDataFormatVersion(out int version)) {
				case SaveState.ERROR_CODE.SUCCESS:
					break;
				case SaveState.ERROR_CODE.NEWER_DATA_FORMAT:
					throw new Exception("Please update the mod " + PluginInfo.PLUGIN_NAME);
				case SaveState.ERROR_CODE.OLD_DATA_FORMAT:
					switch (version) {
						case 1: // Current version, nothing to convert
							break;
						default:
							log.LogError("Unknown data format! Resetting data.");
							saveState.SetData(new Dictionary<int, int>());
							return new Dictionary<int, int>();
					}
					break;
				case SaveState.ERROR_CODE.INVALID_JSON: // custom format didn't use json, so the deserialization would fail
					try {
						if (SaveState_v0.GetStateObject(SaveState_v0.GenerateId(typeof(Plugin)), out WorldObject stateObject)) {
							Dictionary<int, int> oldState = SaveState_v0.GetDictionaryData<int, int>(stateObject, Int32.Parse, Int32.Parse);
							saveState.SetData(oldState);
						}
					} catch {
						log.LogError("Can't convert data format! Resetting data.");
						saveState.SetData(new Dictionary<int, int>());
					}
					break;
				case SaveState.ERROR_CODE.STATE_OBJECT_MISSING:
					saveState.SetData(new Dictionary<int, int>());
					return new Dictionary<int, int>();
			}

			switch (saveState.GetData(out Dictionary<int, int> data)) {
				case SaveState.ERROR_CODE.SUCCESS:
					woidsToPlanetidhashstate = data;
					return woidsToPlanetidhashstate;
				default:
					log.LogError("Can't load data! Resetting data.");
					saveState.SetData(new Dictionary<int, int>());
					return new Dictionary<int, int>();
			}
		}
		private static void SetWoIdsToPlanetIdHashes(Dictionary<int, int> newState) {
			if (saveState.SetData(newState)) {
				woidsToPlanetidhashstate = null;
			} else {
				throw new Exception("Couldn't obtain woIdsToPlanetId state object");
			}
		}

		private static int planetToTeleportToHash = 0;

		static GameObject buttonTabProceduralInstance = null;
		static GameObject buttonTabPortalTravel = null;
		[HarmonyPostfix]
		[HarmonyPatch(typeof(UiWindowPortalGenerator), "Start")]
		static void UiWindowPortalGenerator_Start(UiWindowPortalGenerator __instance) {
			buttonTabProceduralInstance = CreateButton(__instance, "ButtonProceduralInstance", new Vector3(-850, 340, 0), "MainScene/BaseStack/UI/WindowsHandler/UiWindowInterplanetaryExhange/Container/ContentRocketOnSite/RightContent/SelectedPlanet/PlanetIcon");
			buttonTabPortalTravel = CreateButton(__instance, "ButtonPortalTravel", new Vector3(-850, 240, 0), "MainScene/BaseStack/UI/WindowsHandler/UiWindowInterplanetaryExhange/Container/Title/Image");

			// Top Button
			buttonTabProceduralInstance.GetComponent<Button>().onClick.AddListener(delegate () {
				if (!enableKeepPortalOpen) {
					if (Managers.GetManager<WorldInstanceHandler>().GetOpenedWorldInstanceData() != null) {
						return;
					}
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

				if (enableKeepPortalOpen) {
					Dictionary<int, int> woIdToPlanet = GetWoIdsToPlanetIdHashes();
					if (woIdToPlanet.TryGetValue(PortalToWoId(lastMachinePortalGeneratorInteractedWith.machinePortal), out int planetHash)) {
						ClosePortal(lastMachinePortalGeneratorInteractedWith);
						if (Managers.GetManager<WorldInstanceHandler>().GetOpenedWorldInstanceData() != null) {
							OpenPortal(lastMachinePortalGeneratorInteractedWith);
						}

						woIdToPlanet.Remove(PortalToWoId(lastMachinePortalGeneratorInteractedWith.machinePortal));
						SetWoIdsToPlanetIdHashes(woIdToPlanet);
					}
				}
			});

			// Bottom Button
			buttonTabPortalTravel.GetComponent<Button>().onClick.AddListener(delegate () {
				if (!enableKeepPortalOpen) {
					if (Managers.GetManager<WorldInstanceHandler>().GetOpenedWorldInstanceData() != null) {
						return;
					}
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
			button.transform.localPosition = pos;
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

			if (!enableKeepPortalOpen) {
				tmp.text = active ? originalTitle : "Portal travel";
			} else {
				string destinationText = "";

				if (IsLastMachinePortalGeneratorInteractedWithValid() && GetWoIdsToPlanetIdHashes().TryGetValue(PortalToWoId(lastMachinePortalGeneratorInteractedWith.machinePortal), out int planetHash)) {
					destinationText = " - Destination: " + Readable.GetPlanetLabel(Managers.GetManager<PlanetLoader>().planetList.GetPlanetFromIdHash(planetHash));
				}

				tmp.text = active ? originalTitle : ("Portal travel" + destinationText);

				// Move '?' button to the right, away from the:
				__instance.transform.Find("Container/UiPortalList/UIInfosHover").localPosition = new Vector3(828, -339, 0);

				// Change Grid layout to support more planets in the UI
				Transform gridObjectTransform = __instance.transform.Find("Container/UiPortalList/InstancesList/Grid");
				int destinationCount = 0;
				foreach (Transform transform in gridObjectTransform) {
					if (transform.name == "UiWorldInstanceSelector_PlanetTravel" && transform.gameObject.activeSelf) destinationCount++;
				}
				GridLayoutGroup grid = gridObjectTransform.GetComponent<GridLayoutGroup>();

				if (active) {
					grid.cellSize = new Vector2(1500, 85);
				} else {
					int newWidth = 1500;
					int localNamePositionY = 0; // x = -730, x = -355, x = -230 for 1, 2 or 3 columns
					Vector3 localButtonPositon = new Vector3(573, 0, 0); // Default local x position as of v1.611
					Vector3 localMaterialContainerPosition = new Vector3(-256, -39, 0);
					if (destinationCount > 3) {
						newWidth = 750;
						localButtonPositon.x = 198; // Alligned in a way that switching between the views doesn't change the button positon on the right
						localMaterialContainerPosition.x = 64;
					}
					if (destinationCount > 6) {
						newWidth = 500;
						localButtonPositon.x = 100;
						if (configRequireCost.Value) {
							localNamePositionY = 24;
							localMaterialContainerPosition.x = 16;
							localMaterialContainerPosition.y = -51;
						}
					}
					grid.cellSize = new Vector2(newWidth, 85);
					foreach (Transform transform in gridObjectTransform) {
						if (transform.name == "UiWorldInstanceSelector_PlanetTravel" && transform.gameObject.activeSelf) {
							transform.Find("ContentContainer/ButtonOpen").localPosition = localButtonPositon;
							transform.Find("ContentContainer/GroupList").localPosition = localMaterialContainerPosition;
							// The transform of the "Name" GameObject isn't centered and the cellSize change messes with the anchor positions or pivot position, therefore the localPosition changes...
							Transform nameTransform = transform.Find("ContentContainer/Name");
							nameTransform.localPosition = new Vector3(nameTransform.localPosition.x, localNamePositionY, nameTransform.localPosition.z);
						}
					}
				}

				// Change between planet view and system view in the top central screen
				RawImage view = __instance.gameObject.GetComponentInChildren<RawImage>(true);
				if (active) {
					SpaceViewHandler SpaceVH = Managers.GetManager<SpaceViewHandler>();
					view.texture = SpaceVH.GetComponentInChildren<Camera>(true).targetTexture;
					view.uvRect = new Rect(0.3f, 0.5f, 0.5f, 0.17f); // default values as of v1.611
				} else {
					SystemViewHandler SolarVH = Managers.GetManager<SystemViewHandler>();
					SolarVH.ResetCameraPosition();
					SolarVH.SetVisibiltity(true, 90);
					view.texture = SolarVH.GetComponentInChildren<Camera>(true).targetTexture;
					view.uvRect = new Rect(0.2f, 0.44f, 0.6f, 0.13f); // centers and unstretches the view
				}

				// Hide location id labels when system view is active
				foreach (Transform transform in __instance.transform.Find("Container/UiPortalList/SpaceView/IconsContainer")) {
					transform.gameObject.SetActive(active);
				}

				// Make selectable for controller
				Instance.StartCoroutine((IEnumerator)method_UiWindowPortalGenerator_SelectFirstButtonInGrid.Invoke(__instance, []));
				// Select procedural instance buttons when no button in the grid is available (e.g. when opening the PortalGenerator UI on Aqualis)
				Instance.StartCoroutine(ExecuteLater(delegate () {
					if (GamepadConfig.Instance.GetIsUsingController()) {
						Selectable componentInChildren = __instance.gridForInstances.GetComponentInChildren<Selectable>();
						if (componentInChildren == null) {
							componentInChildren = (active ? buttonTabProceduralInstance : buttonTabPortalTravel).GetComponentInChildren<Selectable>();
						}
						if (componentInChildren != null) {
							componentInChildren.Select();
							__instance.gamepadSelectButton.SetActive(value: true);
						}
					}
				}, 2));
			}
		}

		// Reset changes to System View Camera
		[HarmonyPostfix]
		[HarmonyPatch(typeof(UiWindowPortalGenerator), nameof(UiWindowPortalGenerator.OnClose))]
		static void UiWindowPortalGenerator_OnClose() {
			Managers.GetManager<SystemViewHandler>().SetVisibiltity(false);
		}

		private static Action hideButtonTabPortalTravelOnOpen;
		[HarmonyPostfix]
		[HarmonyPatch(typeof(UiWindowPortalGenerator), nameof(UiWindowPortalGenerator.OnOpen))]
		static void UiWindowPortalGenerator_OnOpen(UiWindowPortalGenerator __instance) {
			if (!enableKeepPortalOpen) {
				hideButtonTabPortalTravelOnOpen ??= delegate () {
					if (Managers.GetManager<WorldInstanceHandler>().GetOpenedWorldInstanceData() != null) {
						buttonTabPortalTravel.SetActive(false);
					}
				};

				if (buttonTabPortalTravel == null) {// Hide buttonTabPortalTravel because on first load, buttonTabPortalTravel == null
					Instance.StartCoroutine(ExecuteLater(hideButtonTabPortalTravelOnOpen));
				} else {
					hideButtonTabPortalTravelOnOpen();
				}
			}

			SetUiVisibility(true, __instance);
		}
		private static IEnumerator ExecuteLater(Action toExecute, int waitFrames = 1) {
			//yield return new WaitForSeconds(0.01f);
			for (int i = 0; i < waitFrames; i++) yield return new WaitForEndOfFrame();
			toExecute.Invoke();
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(UiWindowPortalGenerator), nameof(UiWindowPortalGenerator.ShowOpenedInstance))]
		static void UiWindowPortalGenerator_ShowOpenedInstance() {
			if (!enableKeepPortalOpen) {
				if (buttonTabPortalTravel != null) buttonTabPortalTravel.SetActive(false);
			}
		}
		[HarmonyPostfix]
		[HarmonyPatch(typeof(UiWindowPortalGenerator), "ShowUiWindows")]
		static void UiWindowPortalGenerator_ShowUiWindows(UiWindowPortalGenerator __instance, GameObject containerToShow) {
			if (enableKeepPortalOpen) {
				if (!IsLastMachinePortalGeneratorInteractedWithValid()) { // For when the UI is opened with M in the portal wreck
					if (buttonTabPortalTravel != null) {
						buttonTabPortalTravel.SetActive(false);
					} else {
						Instance.StartCoroutine(ExecuteLater(delegate () { if (buttonTabPortalTravel != null) buttonTabPortalTravel.SetActive(false); }));
					}
					if (buttonTabProceduralInstance != null) {
						buttonTabProceduralInstance.SetActive(false);
					} else {
						Instance.StartCoroutine(ExecuteLater(delegate () { if (buttonTabProceduralInstance != null) buttonTabProceduralInstance.SetActive(false); }));
					}

					return;
				}
				if ((containerToShow == __instance.uiPortalsList) && GetWoIdsToPlanetIdHashes().TryGetValue(PortalToWoId(lastMachinePortalGeneratorInteractedWith.machinePortal), out int planetHash)) {
					Instance.StartCoroutine(ExecuteLater(() => buttonTabPortalTravel.GetComponent<Button>().onClick.Invoke()));
				}
			}

			if (buttonTabProceduralInstance != null && buttonTabPortalTravel != null) {
				buttonTabProceduralInstance.SetActive(!__instance.uiOnScanning.activeSelf);
				buttonTabPortalTravel.SetActive(!__instance.uiOnScanning.activeSelf);
			}
			if (planetsExcludingPortalGenerator.Contains(Managers.GetManager<PlanetLoader>().GetCurrentPlanetData()?.GetPlanetHash() ?? 0)) {
				foreach (Transform transform in __instance.transform.Find("Container/UiPortalList/InstancesList/Grid")) {
					if (transform.name == "UiWorldInstanceLine(Clone)") {
						transform.gameObject.SetActive(false);
						if (!enableKeepPortalOpen) SetUiVisibility(false, __instance); // No clue why this even was here in the first place
					}
				}
			}
		}

		private static GameObject gameObjectForOpeningPortals_DontKeepPortalOpen;
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
				if (configRequireCost.Value) {
					foreach (string groupStringRaw in configItemsCost.Value.Split(",")) {
						Group costGroup = GroupsHandler.GetGroupViaId(groupStringRaw.Trim());
						if (costGroup != null) {
							field_Recipe_recipe(recipe).Add(costGroup);
						}
					}
				}
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

							if (enableKeepPortalOpen) {
								OpenPortal(lastMachinePortalGeneratorInteractedWith);

								Dictionary<int, int> woIdToPlanet = GetWoIdsToPlanetIdHashes();
								woIdToPlanet[PortalToWoId(lastMachinePortalGeneratorInteractedWith.machinePortal)] = pd.GetPlanetHash();
								SetWoIdsToPlanetIdHashes(woIdToPlanet);
							} else {
								List<MachinePortalGenerator> allMachinePortalGenerators = field_WorldInstanceHandler__allMachinePortalGenerator(Managers.GetManager<WorldInstanceHandler>());
								foreach (MachinePortalGenerator mpg in allMachinePortalGenerators) {
									if (!mpg.gameObject.activeInHierarchy) continue;
									if (gameObjectForOpeningPortals_DontKeepPortalOpen == null) gameObjectForOpeningPortals_DontKeepPortalOpen = new GameObject();
									mpg.OpenPortal(null, gameObjectForOpeningPortals_DontKeepPortalOpen, false);
									portalCreatedByMod = true;
								}
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

				//gameObject.GetComponent<UiWorldInstanceSelector>().buttonOpen.transform.position = new Vector3(10000, 0, 0);
				//gameObject.GetComponent<UiWorldInstanceSelector>().buttonClose.transform.position = new Vector3(10000, 0, 0);
				gameObject.GetComponent<UiWorldInstanceSelector>().buttonOpen.SetActive(false); // Setting inactive (instead of to x=10000) such that the controller can't select it.
				gameObject.GetComponent<UiWorldInstanceSelector>().buttonClose.SetActive(false);// --- " ---
			}
		}

		static MachinePortalGenerator lastMachinePortalGeneratorInteractedWith = null;
		private static bool IsLastMachinePortalGeneratorInteractedWithValid() {
			return lastMachinePortalGeneratorInteractedWith != null && (lastMachinePortalGeneratorInteractedWith.gameObject.transform.position - Managers.GetManager<PlayersManager>().GetActivePlayerController().transform.position).magnitude < 20;
		}
		[HarmonyPrefix]
		[HarmonyPatch(typeof(ActionUsePortalGenerator), nameof(ActionUsePortalGenerator.OnAction))]
		static void ActionUsePortalGenerator_OnAction(ActionUsePortalGenerator __instance) {
			if (enableKeepPortalOpen) {
				lastMachinePortalGeneratorInteractedWith = __instance.transform.root.GetComponent<MachinePortalGenerator>();
			}
		}

		static int PortalToWoId(MachinePortal portal) {
			WorldObjectAssociated woa = portal.transform.parent.GetComponent<WorldObjectAssociated>();
			if (woa == null) return -1; // <- E.g. when the portal is the ReturnPortal in the procedural instance

			if (woa.GetWorldObject() == null) { // E.g. for a client in a multiplayer session before the proxy set it; also probably for all construction ghosts
				if (woa.TryGetComponent<ConstructibleGhost>(out _)) return -1; // Calling GetWorldObjectDetails in unity 6000.3 could result in an exception as the m_NetworkManager could be null
				woa.GetComponent<WorldObjectAssociatedProxy>().GetWorldObjectDetails(delegate (WorldObject wo) { }); // <- Maybe it could help with multiplayer???
				return -1;
			}

			return woa.GetWorldObject().GetId();
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(WorldObjectsHandler), nameof(WorldObjectsHandler.InstantiateWorldObject))]
		static void WorldObjectsHandler_InstantiateWorldObject(WorldObject worldObject, GameObject __result) {
			if (!enableKeepPortalOpen) return;

			if (worldObject.GetGroup().GetId() != "PortalGenerator1") { return; }
			MachinePortalGenerator mpg = __result.GetComponent<MachinePortalGenerator>();

			if (GetWoIdsToPlanetIdHashes().ContainsKey(worldObject.GetId())) {
				OpenPortal(mpg);
			}
		}

		/*[HarmonyPrefix]
		[HarmonyPatch(typeof(SaveFilesSelector), nameof(SaveFilesSelector.SelectedSaveFile))]
		static void SaveFilesSelector_SelectedSaveFile() {
			if (!enableKeepPortalOpen) return;

			
		}*/

		[HarmonyPrefix]
		[HarmonyPatch(typeof(SavedDataHandler), nameof(SavedDataHandler.SetSaveFileName))]
		static void SavedDataHandler_SetSaveFileName() {
			if (!enableKeepPortalOpen) return;

			// reset state when loading a save file
			woidsToPlanetidhashstate = null;
		}

		static bool closePortalCalledFromOnCloseInstance = false;
		[HarmonyPrefix]
		[HarmonyPatch(typeof(UiWindowPortalGenerator), nameof(UiWindowPortalGenerator.OnCloseInstance))]
		static void Pre_WorldInstanceHandler_Awake() { closePortalCalledFromOnCloseInstance = true; }
		[HarmonyPrefix]
		[HarmonyPatch(typeof(MachinePortalGenerator), nameof(MachinePortalGenerator.ClosePortal))]
		static bool MachinePortalGenerator_ClosePortal(MachinePortalGenerator __instance, GameObject ____currentReturnPortal) {
			if (!enableKeepPortalOpen) return true;

			if (!closePortalCalledFromOnCloseInstance) return true;

			if (GetWoIdsToPlanetIdHashes().ContainsKey(PortalToWoId(__instance.machinePortal)) && __instance.gameObject.activeInHierarchy) {
				if (____currentReturnPortal != null) {
					/* If the portal generator with the return portal has an interplanetary portal open when clicking the close instance button, 
					 * then the return portal must be destroyed, because otherwise there are multiple return portals when opening a new procedural instance. */
					Destroy(____currentReturnPortal);
				}
				return false;
			}

			return true;
		}
		[HarmonyPostfix]
		[HarmonyPatch(typeof(UiWindowPortalGenerator), nameof(UiWindowPortalGenerator.OnCloseInstance))]
		static void Post_WorldInstanceHandler_Awake() { closePortalCalledFromOnCloseInstance = false; }


		private static bool portalCreatedByMod = false;
		[HarmonyPrefix]
		[HarmonyPatch(typeof(UiWindowPortalGenerator), nameof(UiWindowPortalGenerator.OnOpenInstance))]
		private static void UiWindowPortalGenerator_OnOpenInstance() {
			portalCreatedByMod = false;
		}

		private static bool semaphoreActive = false;
		[HarmonyPrefix]
		[HarmonyPatch(typeof(PlanetLoader), "HandleDataAfterLoad")]
		private static void Pre_PlanetLoader_HandleDataAfterLoad() {
			if (semaphoreActive) {
				Managers.GetManager<SavedDataHandler>().DecrementSaveLock();// semaphore unlock saving
				semaphoreActive = false;
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(PlanetLoader), "HandleDataAfterLoad")]
		private static void Post_PlanetLoader_HandleDataAfterLoad() {
			if (!enableKeepPortalOpen) return;

			Dictionary<int, int> woIdToPlanet = GetWoIdsToPlanetIdHashes();

			foreach (MachinePortal mp in FindObjectsByType<MachinePortal>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
				if (mp == null) continue;

				MachinePortalGenerator mpg = mp.transform.parent.GetComponent<MachinePortalGenerator>();
				if (mpg == null) continue; // <- (Return) Portal in procedural instance doesn't have a MachinePortalGenerator in it's parent

				if (mpg.gameObject.activeInHierarchy && woIdToPlanet.ContainsKey(PortalToWoId(mp))) {
					OpenPortal(mpg);
				}
			}

			// clean stateObject
			Dictionary<int, int> newWoIdToPlanetDict = new Dictionary<int, int>();
			foreach (int woid in WorldObjectsHandler.Instance.GetAllWorldObjectOfGroup(GroupsHandler.GetGroupViaId("PortalGenerator1")).Select(e => e.GetId())) {
				if (woIdToPlanet.TryGetValue(woid, out int planetId)) {
					newWoIdToPlanetDict[woid] = planetId;
				}
			}
			SetWoIdsToPlanetIdHashes(newWoIdToPlanetDict);
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
			string localizedName = Readable.GetPlanetLabel(pd);
			if (string.IsNullOrEmpty(localizedName)) localizedName = pd.GetPlanetId();
			____seedLabel = localizedName + "";
			__result = localizedName + "";
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

		struct GoInsidePortalState {
			public bool enterPortal;
			public bool tunnelChanged;
			public GameObject portalTunnel;
		}
		[HarmonyPrefix] // do not execute WorldInstanceHandler.SetWorldInstanceActive(true)
		[HarmonyPatch(typeof(MachinePortal), "GoInsidePortal")]
		private static bool Pre_MachinePortal_GoInsidePortal(ref bool ____enterPortal, ref GoInsidePortalState __state, ref MachinePortal __instance, ref float ____timeInTunnel) {
			if (enableKeepPortalOpen) {
				int woid = PortalToWoId(__instance);
				if (woid != -1) {
					if (GetWoIdsToPlanetIdHashes().TryGetValue(woid, out int planetHash)) {
						// check if portal still exists
						if (WorldObjectsHandler.Instance.GetFirstWorldObjectOfGroup(GroupsHandler.GetGroupViaId("PortalGenerator1"), planetHash) == null) {
							field_PopupsHandler_popupsToPop(Managers.GetManager<PopupsHandler>()).Add(new PopupData(
								Sprite.Create(Texture2D.blackTexture, new Rect(0.0f, 0.0f, 4, 4), new Vector2(0.5f, 0.5f), 100.0f),
								"No portal on destination planet",
								5f,
								true));
							ClosePortal(__instance.transform.root.GetComponent<MachinePortalGenerator>());

							Dictionary<int, int> woIdsToPlanetIdHashes = GetWoIdsToPlanetIdHashes();
							woIdsToPlanetIdHashes.Remove(woid);
							SetWoIdsToPlanetIdHashes(woIdsToPlanetIdHashes);

							return false;
						}

						portalCreatedByMod = true;
						planetToTeleportToHash = planetHash;
					}
				}
			}

			__state.enterPortal = ____enterPortal;
			__state.tunnelChanged = false; // init value
			if (portalCreatedByMod) {
				if (configTimeInPortal.Value >= 0.001f) {
					____timeInTunnel = configTimeInPortal.Value;
				}

				____enterPortal = false;

				__state.portalTunnel = Managers.GetManager<VisualsResourcesHandler>().GetPortalTunnelGameObject();
				SetPortalTunnelColor();
				__state.tunnelChanged = true;
			}
			return true;
		}
		[HarmonyPostfix]
		[HarmonyPatch(typeof(MachinePortal), "GoInsidePortal")]
		private static void Post_MachinePortal_GoInsidePortal(ref bool ____enterPortal, ref GoInsidePortalState __state) {
			____enterPortal = __state.enterPortal;

			if (__state.tunnelChanged) {
				Destroy(Managers.GetManager<VisualsResourcesHandler>().portalTunnel, configTimeInPortal.Value);
				Managers.GetManager<VisualsResourcesHandler>().portalTunnel = __state.portalTunnel;
			}
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

				// possibly for closing portals on the other planet when traveling??? No real idea why this exists or is important...
				List<MachinePortalGenerator> allMachinePortalGenerators = field_WorldInstanceHandler__allMachinePortalGenerator(Managers.GetManager<WorldInstanceHandler>());
				foreach (MachinePortalGenerator mpg in allMachinePortalGenerators) {
					if (!mpg.gameObject.activeInHierarchy) continue;
					if (enableKeepPortalOpen) {
						ClosePortal(mpg);
					} else {
						mpg.ClosePortal();
					}
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

				if (enableKeepPortalOpen) {
					WorldObject woPortalGenerator = null;

					foreach (WorldObject portalWoOnDestinationPlanet in WorldObjectsHandler.Instance.GetAllWorldObjectOfGroup(GroupsHandler.GetGroupViaId("PortalGenerator1"), pd.GetPlanetHash())) {
						woPortalGenerator ??= portalWoOnDestinationPlanet;
						if (GetWoIdsToPlanetIdHashes().TryGetValue(portalWoOnDestinationPlanet.GetId(), out int destinationPlanetHash)) {
							if (destinationPlanetHash == Managers.GetManager<PlanetLoader>().GetCurrentPlanetData()?.GetPlanetHash()) {
								woPortalGenerator = portalWoOnDestinationPlanet;
								break;
							}
						}
					}
					pos = woPortalGenerator.GetPosition();
					rot = woPortalGenerator.GetRotation();
				} else {
					WorldObject woPortalGenerator = WorldObjectsHandler.Instance.GetFirstWorldObjectOfGroup(GroupsHandler.GetGroupViaId("PortalGenerator1"), pd.GetPlanetHash());
					if (woPortalGenerator != null) { // should never be null as the portal should only show if there is a portal generator on the planet
						pos = woPortalGenerator.GetPosition();
						rot = woPortalGenerator.GetRotation();
					}
				}

				// ClientRpc has to be manually invoked as it isn't executed because the planetIndex returns -1 (see PlanetList.GetPlanetIndex patch)
				//this.SwitchToPlanetClientRpc(____planetIndex.Value, vector + new Vector3(0f, 1f, 0f), (int)quaternion.eulerAngles.y);
				method_PlanetNetworkLoader_SwitchToPlanetClientRpc.Invoke(__instance, new object[] { ____planetIndex.Value, pos + new Vector3(0, 7, 0) + rot * (-1.17f * Vector3.forward + 0.29f * Vector3.right), (int)rot.eulerAngles.y + 90 });

				portalCreatedByMod = false;
			}
		}
		// <--- Prevent the creation and deletion of capsules ---

		// --- Activate Portal everywhere --->
		private static HashSet<int> planetsExcludingPortalGenerator = new HashSet<int>();
		[HarmonyPrefix]
		[HarmonyPriority(Priority.VeryLow)] // So notAllowedPlanetsRequirement is changed after e.g. space station planet is added
		[HarmonyPatch(typeof(StaticDataHandler), "LoadStaticData")]
		static void StaticDataHandler_LoadStaticData(ref List<GroupData> ___groupsData) {
			GroupDataConstructible portalGroupDataContructible = ___groupsData.Find(e => e.id == "PortalGenerator1") as GroupDataConstructible;
			if (portalGroupDataContructible == null) {
				if (configEnableDebug.Value) log.LogError("PortalGenerator1 not found");
				return;
			}
			foreach (PlanetData pd in portalGroupDataContructible.notAllowedPlanetsRequirement) planetsExcludingPortalGenerator.Add(pd.GetPlanetHash());
			portalGroupDataContructible.notAllowedPlanetsRequirement = new List<PlanetData>();
			if (configDisableOtherRequirements.Value) portalGroupDataContructible.terraStageRequirements = [];
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

		// --- Color Portals --->
		/*
		 *	{ -1140328421, new Color(200, 55, 0, 1)}, // Prime
		 *	{ -486276833, new Color(30, 30, 25, 1)}, // Humble
		 *	{ -1016990411, new Color(32, 80, 16, 1)}, // Selenea
		 *	{ -1291310150, new Color(0, 100, 100, 1)} // Aqualis
		 */
		private static void SetColorConfig() {
			PlanetColor.Clear();
			foreach (string planetSplit in ("SpaceStation: -5, -5, -5, 1; Toxicity: 192, 192, 0, 1; " + configColorPortalsColors.Value).Split(';')) {
				string[] attributeSplit = planetSplit.Split(':');
				if (attributeSplit.Length != 2) continue;
				string planet = attributeSplit[0].Trim();
				string[] colorParams = attributeSplit[1].Split(",");

				float[] colorParamsFloat = new float[colorParams.Length];
				bool successfulConversion = true;
				for (int i = 0; i < colorParams.Length; i++) {
					if (!float.TryParse(colorParams[i].Trim(), out colorParamsFloat[i])) {
						successfulConversion = false;
					}
				}
				if (successfulConversion) {
					if (colorParamsFloat.Length == 4) {
						PlanetColor[planet.GetStableHashCode()] = new Color(colorParamsFloat[0], colorParamsFloat[1], colorParamsFloat[2], colorParamsFloat[3]);
					} else if (colorParamsFloat.Length == 3) {
						PlanetColor[planet.GetStableHashCode()] = new Color(colorParamsFloat[0], colorParamsFloat[1], colorParamsFloat[2]);
					}
				}
			}
		}
		private static readonly Dictionary<int, Color> PlanetColor = new Dictionary<int, Color>();
		private static Color DefaultPortalColor = new Color(767, 112, 568, 1);
		[HarmonyPrefix]
		[HarmonyPatch(typeof(MachinePortalGenerator), "SetParticles")]
		static void MachinePortalGenerator_SetParticles(List<ParticleSystem> ___particlesOnOpen, MachinePortalGenerator __instance, ref bool playParticles) {
			if (!enableKeepPortalOpen) { return; }
			if (!enableColorPortals) { return; }

			foreach (ParticleSystem particle in ___particlesOnOpen) {
				ParticleSystemRenderer particleCircles = particle.GetComponent<ParticleSystemRenderer>();
				Instance.StartCoroutine(ExecuteLater(delegate () {

					if (__instance.machinePortal == null || __instance.machinePortal != null && PortalToWoId(__instance.machinePortal) == -1) {
						SetMaterialColor(particleCircles, Color.clear);
						return;
					}
					if (GetWoIdsToPlanetIdHashes().TryGetValue(PortalToWoId(__instance.machinePortal), out int planetIdHash)) {
						if (PlanetColor.TryGetValue(planetIdHash, out Color planetColor)) {
							SetMaterialColor(particleCircles, planetColor);
							return;
						}
					} else {
						SetMaterialColor(particleCircles, DefaultPortalColor);
					}
				}));
			}
			if (playParticles) { // Color is only changed in next frame, so particles are started only after that
				playParticles = false;
				Instance.StartCoroutine(ExecuteLater(delegate () {
					foreach (ParticleSystem particleSystem in ___particlesOnOpen) {

						particleSystem.Play();
					}
				}, 1));
			}
		}
		private static void SetMaterialColor(ParticleSystemRenderer renderer, Color color) {
			/*
			 * _EmissionColor -> ring particles
			 * _TintColor -> dot particles
			 */
			if (!renderer.sharedMaterial.GetName().EndsWith(")")) { // "(Clone)" or "(Instance)"(<-from UnityExplorer). This means that the object has been instantiated and configured already, which is done here
				if (configEnableStrudel.Value && renderer.sharedMaterial.HasColor("_EmissionColor")) {
					ParticleSystem.MainModule mm = renderer.GetComponent<ParticleSystem>().main;
					mm.startSpeed = 1;
					renderer.transform.position -= renderer.transform.forward * 4;
				}

				renderer.sharedMaterial = Instantiate(renderer.sharedMaterial); // Can't set the color on the original material, as the color would be the same everywhere
			}

			// Prevent switching back to default color when portal is closed
			if (!renderer.transform.GetComponentInParent<MachinePortalGenerator>()?.machinePortal?.gameObject.activeSelf ?? false) return;

			if (renderer.sharedMaterial.HasColor("_EmissionColor")) renderer.sharedMaterial.SetColor("_EmissionColor", color);
			if (renderer.sharedMaterial.HasColor("_TintColor")) {
				ParticleSystem dotParticles = renderer.GetComponent<ParticleSystem>();

				if (color != DefaultPortalColor) {
					if (color.r >= 0 || color.g >= 0 || color.b >= 0) {

						renderer.sharedMaterial.SetColor("_TintColor", NormalizeAndScaleColor(color, 0.4f));

						ParticleSystem.ColorOverLifetimeModule dotParticlesColorOL = dotParticles.colorOverLifetime;
						dotParticlesColorOL.enabled = false;
					} else { // don't use dot particles on black portals
						ParticleSystem.MainModule dotParticlesMM = dotParticles.main;
						dotParticlesMM.startColor = Color.clear;
					}
				}

				if (configEnableStrudel.Value) { // only show when opening the portal if the strudel is enabled
					ParticleSystem.MainModule dotParticlesMM = dotParticles.main;
					dotParticlesMM.loop = false;
				}
			}
		}
		private static Color NormalizeAndScaleColor(Color color, float scale = 1) {
			float largestValue = Math.Max(Math.Max(Math.Abs(color.r), Math.Abs(color.g)), Math.Abs(color.b)) / scale;
			return new Color(color.r / largestValue, color.g / largestValue, color.b / largestValue, color.a);
		}
		// <--- Color Portals ---
		// --- Color Portal Tunnel --->
		private static void SetPortalTunnelColor() {
			GameObject configuredPortalTunnel = Instantiate(Managers.GetManager<VisualsResourcesHandler>().GetPortalTunnelGameObject());
			Managers.GetManager<VisualsResourcesHandler>().portalTunnel = configuredPortalTunnel;

			if (!PlanetColor.TryGetValue(Managers.GetManager<PlanetLoader>().GetCurrentPlanetData().GetPlanetHash(), out Color sourcePlanetColor)) {
				sourcePlanetColor = DefaultPortalColor;
			}
			if (!PlanetColor.TryGetValue(planetToTeleportToHash, out Color destinationPlanetColor)) {
				destinationPlanetColor = DefaultPortalColor;
			}

			foreach (ParticleSystemRenderer renderer in configuredPortalTunnel.transform.root.gameObject.GetComponentsInChildren<ParticleSystemRenderer>()) {
				if (renderer.gameObject.name.Contains("ParticleComplex")) {
					ParticleSystem particle = renderer.GetComponent<ParticleSystem>();
					ParticleSystem.ColorOverLifetimeModule coltm = particle.colorOverLifetime;
					Gradient gradient = new Gradient();
					gradient.SetKeys(
						new GradientColorKey[] {
							new GradientColorKey(NormalizeAndScaleColor(destinationPlanetColor), 0.7f),
							new GradientColorKey(Color.white, 0.8f),
							new GradientColorKey(NormalizeAndScaleColor(sourcePlanetColor), 0.9f)
						},
						new GradientAlphaKey[] { new GradientAlphaKey(1, 0) }
						);
					coltm.color = new MinMaxGradient(gradient);
				}
				if (renderer.gameObject.name.Contains("FloorDust")) {
					ParticleSystem.MainModule mm = renderer.GetComponent<ParticleSystem>().main;
					mm.startColor = NormalizeAndScaleColor(sourcePlanetColor, 0.2f);

					ParticleSystem.MinMaxGradient color_mmg = mm.startColor;
					color_mmg.mode = ParticleSystemGradientMode.Color;
				}
			}
		}
		// <--- Color Portal Tunnel ---



		private static void OpenPortal(MachinePortalGenerator mpg) {
			mpg.machinePortal.gameObject.SetActive(true);
			method_MachinePortalGenerator_SetParticles.Invoke(mpg, new object[] { true });
			mpg.soundOnOpening.Play();
			mpg.soundIsOpen.Play();
		}
		private static void ClosePortal(MachinePortalGenerator mpg) {
			mpg.machinePortal.Close();
			mpg.machinePortal.gameObject.SetActive(false);
			method_MachinePortalGenerator_SetParticles.Invoke(mpg, new object[] { false });
			mpg.soundIsOpen.Stop();
			mpg.soundOnClose.Play();
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(Localization), "GetLocalizedString")]
		private static void Localization_GetLocalizedString(string stringCode, ref string __result) {
			if (stringCode == "PortalTeleport_InfoNoPlanetsAvailable") {
				__result = "Travel requirements not met. Source and destination planet need a portal" + (configRequireFullTerraformation.Value ? " and a portal travel license (issued by " + Localization.GetLocalizedString("MessageSender_GalacticTribunal") + " at full terraformation)" : "") + ".";
				return;
			}
			if (stringCode == "UI_portals_Instructions1") {
				if (enableKeepPortalOpen) {
					__result = __result + ". Portal travel requires a portal on source and destination planet. Select the 'Long range wrecks' tab to close the portal.";
				} else {
					__result = __result + ". Portal travel requires a portal on source and destination planet and no other portal may be open.";
				}
				return;
			}
		}
	}
}

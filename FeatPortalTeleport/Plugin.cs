﻿// Copyright 2025-2025 Nicolas Schäfer & Contributors
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
	[BepInDependency("Nicki0.theplanetcraftermods.SpaceStationPlanet", BepInDependency.DependencyFlags.SoftDependency)] // So StaticDataHandler_LoadStaticData changes notAllowedPlanetsRequirement after space station planet is added
	public class Plugin : BaseUnityPlugin {

		/*
		 *	TODO:
		 *	
		 *	
		 *	BUGS:
		 */
		private static bool enableKeepPortalOpen = true;
		private static bool enableColorPortals = true;

		static Plugin Instance;
		static ManualLogSource log;

		public static ConfigEntry<bool> configEnableDebug;
		public static ConfigEntry<bool> configRequireCost;
		public static ConfigEntry<bool> configRequireFullTerraformation;
		public static ConfigEntry<bool> configDeletePortalsFromMoonsWhenModIsLost;

		public static ConfigEntry<bool> configKeepPortalsOpen;
		public static ConfigEntry<bool> configSetColorPortals;
		public static ConfigEntry<string> configColorPortalsColors;
		public static ConfigEntry<float> configTimeInPortal;

		static MethodInfo method_PlanetNetworkLoader_SwitchToPlanetClientRpc;
		static MethodInfo method_MachinePortalGenerator_SetParticles;
		static AccessTools.FieldRef<Recipe, List<Group>> field_Recipe_recipe;
		static AccessTools.FieldRef<WorldInstanceHandler, List<MachinePortalGenerator>> field_WorldInstanceHandler__allMachinePortalGenerator;
		static AccessTools.FieldRef<PopupsHandler, List<PopupData>> field_PopupsHandler_popupsToPop;


		private void Awake() {
			log = Logger;
			Instance = this;

			configRequireCost = Config.Bind<bool>("General", "requireCost", false, "Opening the Portal to another planet costs one Fusion Energy Cell");
			configRequireFullTerraformation = Config.Bind<bool>("General", "requireFullTerraformation", true, "Requires the source and destination planet to be terraformed to stage \"Complete\"");
			configEnableDebug = Config.Bind<bool>("Debug", "enableDebug", false, "Enable debug messages");
			configDeletePortalsFromMoonsWhenModIsLost = Config.Bind<bool>("Debug", "deleteMoonPortals", true, "Savety mechanism. Portals on Moons will be deleted if the mod doesn't get loaded, as they aren't constructable on moons in the base game.");

			configKeepPortalsOpen = Config.Bind<bool>("General", "keepPortalsOpen", true, "Keep portal open instead of closing them after traveling");
			configSetColorPortals = Config.Bind<bool>("Color", "activateColoredPortals", true, "Opened Portals are colored depending on their destination. Only works if keepPortalsOpen = true");
			configColorPortalsColors = Config.Bind<string>("Color", "portalDestinationColors", "Prime: 200, 55, 0, 1; Humble: 30, 30, 25, 1; Selenea: 32, 80, 16, 1; Aqualis: 0, 100, 100, 1", "Color of a portal connected to a planet (RGB/RGBA)");
			configTimeInPortal = Config.Bind<float>("General", "timeInPortal", 0, "Time in seconds for how long the player remains inside the portal animation. Default time: 5s. Set to 0 to use default time.");

			enableKeepPortalOpen = configKeepPortalsOpen.Value;
			enableColorPortals = configSetColorPortals.Value;
			if (enableKeepPortalOpen) { SetColorConfig(); }

			method_PlanetNetworkLoader_SwitchToPlanetClientRpc = AccessTools.Method(typeof(PlanetNetworkLoader), "SwitchToPlanetClientRpc");
			method_MachinePortalGenerator_SetParticles = AccessTools.Method(typeof(MachinePortalGenerator), "SetParticles");
			field_Recipe_recipe = AccessTools.FieldRefAccess<Recipe, List<Group>>("_ingredientsGroups");
			field_WorldInstanceHandler__allMachinePortalGenerator = AccessTools.FieldRefAccess<WorldInstanceHandler, List<MachinePortalGenerator>>("_allMachinePortalGenerator");
			field_PopupsHandler_popupsToPop = AccessTools.FieldRefAccess<PopupsHandler, List<PopupData>>("popupsToPop");

			// Plugin startup logic
			Harmony.CreateAndPatchAll(typeof(Plugin));
			Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
		}

		public static void OnModConfigChanged(ConfigEntryBase _) {
			if (enableKeepPortalOpen) { SetColorConfig(); }
		}

		private static readonly int StateObjectId = SaveState.GenerateId(typeof(Plugin));
		private static WorldObject woidsToPlanetidhash_StateObject = null;
		private static Dictionary<int, int> woidsToPlanetidhashstate = null;
		private static Dictionary<int, int> GetWoIdsToPlanetIdHashes() {
			if (woidsToPlanetidhashstate != null) { return woidsToPlanetidhashstate; }
			if (SaveState.GetAndCreateStateObject(StateObjectId, out WorldObject stateObject)) {
				woidsToPlanetidhash_StateObject = stateObject;
				woidsToPlanetidhashstate = SaveState.GetDictionaryData<int, int>(stateObject, Int32.Parse, Int32.Parse);
				return woidsToPlanetidhashstate;
			}
			throw new Exception("Couldn't obtain woIdsToPlanetId state object");
		}
		private static void SetWoIdsToPlanetIdHashes(Dictionary<int, int> newState) {
			if (woidsToPlanetidhash_StateObject == null) {
				if (SaveState.GetAndCreateStateObject(StateObjectId, out WorldObject stateObject)) {
					woidsToPlanetidhash_StateObject = stateObject;
				}
			}
			if (woidsToPlanetidhash_StateObject != null) {
				SaveState.SetDictionaryData<int, int>(woidsToPlanetidhash_StateObject, newState);
			} else {
				throw new Exception("Couldn't obtain woIdsToPlanetId state object");
			}
			woidsToPlanetidhashstate = null;
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
					if (woIdToPlanet.TryGetValue(portalToWoId[lastMachinePortalGeneratorInteractedWith.machinePortal], out int planetHash)) {
						ClosePortal(lastMachinePortalGeneratorInteractedWith);
						if (Managers.GetManager<WorldInstanceHandler>().GetOpenedWorldInstanceData() != null) {
							OpenPortal(lastMachinePortalGeneratorInteractedWith);
						}

						woIdToPlanet.Remove(portalToWoId[lastMachinePortalGeneratorInteractedWith.machinePortal]);
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

			if (!enableKeepPortalOpen) {
				tmp.text = active ? originalTitle : "Portal travel";
			} else {
				string destinationText = "";

				if (IsLastMachinePortalGeneratorInteractedWithValid() && GetWoIdsToPlanetIdHashes().TryGetValue(portalToWoId[lastMachinePortalGeneratorInteractedWith.machinePortal], out int planetHash)) {
					destinationText = " - Destination: " + Readable.GetPlanetLabel(Managers.GetManager<PlanetLoader>().planetList.GetPlanetFromIdHash(planetHash));
				}

				tmp.text = active ? originalTitle : ("Portal travel" + destinationText);
			}
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
		private static IEnumerator ExecuteLater(Action toExecute) {
			//yield return new WaitForSeconds(0.01f);
			yield return new WaitForEndOfFrame();
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
				if ((containerToShow == __instance.uiPortalsList) && GetWoIdsToPlanetIdHashes().TryGetValue(portalToWoId[lastMachinePortalGeneratorInteractedWith.machinePortal], out int planetHash)) {
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

							if (enableKeepPortalOpen) {
								OpenPortal(lastMachinePortalGeneratorInteractedWith);

								Dictionary<int, int> woIdToPlanet = GetWoIdsToPlanetIdHashes();
								woIdToPlanet[portalToWoId[lastMachinePortalGeneratorInteractedWith.machinePortal]] = pd.GetPlanetHash();
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
				gameObject.GetComponent<UiWorldInstanceSelector>().buttonOpen.transform.position = new Vector3(10000, 0, 0);
				gameObject.GetComponent<UiWorldInstanceSelector>().buttonClose.transform.position = new Vector3(10000, 0, 0);
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

		static Dictionary<MachinePortal, int> portalToWoId = new Dictionary<MachinePortal, int>();
		[HarmonyPostfix]
		[HarmonyPatch(typeof(WorldObjectsHandler), nameof(WorldObjectsHandler.InstantiateWorldObject))]
		static void WorldObjectsHandler_InstantiateWorldObject(WorldObject worldObject, GameObject __result) {
			if (!enableKeepPortalOpen) return;

			if (worldObject.GetGroup().GetId() != "PortalGenerator1") { return; }
			MachinePortalGenerator mpg = __result.GetComponent<MachinePortalGenerator>();
			portalToWoId[mpg.machinePortal] = worldObject.GetId();

			if (GetWoIdsToPlanetIdHashes().ContainsKey(worldObject.GetId())) {
				OpenPortal(mpg);
			}
		}
		[HarmonyPrefix] // Clear dictionary portalToWoId to prevent errors when reloading / loading another world
		[HarmonyPatch(typeof(SaveFilesSelector), nameof(SaveFilesSelector.SelectedSaveFile))]
		static void SaveFilesSelector_SelectedSaveFile() {
			if (!enableKeepPortalOpen) return;
			portalToWoId.Clear();
		}

		static bool closePortalCalledFromOnCloseInstance = false;
		[HarmonyPrefix]
		[HarmonyPatch(typeof(UiWindowPortalGenerator), nameof(UiWindowPortalGenerator.OnCloseInstance))]
		static void Pre_WorldInstanceHandler_Awake() { closePortalCalledFromOnCloseInstance = true; }
		[HarmonyPrefix]
		[HarmonyPatch(typeof(MachinePortalGenerator), nameof(MachinePortalGenerator.ClosePortal))]
		static bool MachinePortalGenerator_ClosePortal(MachinePortalGenerator __instance) {
			if (!enableKeepPortalOpen) return true;

			if (!closePortalCalledFromOnCloseInstance) return true;

			if (GetWoIdsToPlanetIdHashes().ContainsKey(portalToWoId[__instance.machinePortal]) && __instance.gameObject.activeInHierarchy) {
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

			foreach (MachinePortal mp in portalToWoId.Keys) {
				if (mp == null) continue;

				MachinePortalGenerator mpg = mp.transform.root.GetComponent<MachinePortalGenerator>();

				if (mpg.gameObject.activeInHierarchy && woIdToPlanet.ContainsKey(portalToWoId[mp])) {
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
		private static bool Pre_MachinePortal_GoInsidePortal(ref bool ____enterPortal, ref bool __state, ref MachinePortal __instance, ref float ____timeInTunnel) {
			if (enableKeepPortalOpen) {
				if (portalToWoId.TryGetValue(__instance, out int woid)) {
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


			__state = ____enterPortal;
			if (portalCreatedByMod) {
				if (configTimeInPortal.Value >= 0.001f) {
					____timeInTunnel = configTimeInPortal.Value;
				}

				____enterPortal = false;
			}
			return true;
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

		// --- Color Portals --->
		/*
		 *	{ -1140328421, new Color(200, 55, 0, 1)}, // Prime
		 *	{ -486276833, new Color(30, 30, 25, 1)}, // Humble
		 *	{ -1016990411, new Color(32, 80, 16, 1)}, // Selenea
		 *	{ -1291310150, new Color(0, 100, 100, 1)} // Aqualis
		 */
		private static void SetColorConfig() {
			PlanetColor.Clear();
			foreach (string planetSplit in ("SpaceStation: -5, -5, -5, 1; " + configColorPortalsColors.Value).Split(';')) {
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
		private static bool DefaultPortalColorSet = true;
		private static Color DefaultPortalColor = new Color(767, 112, 568, 1);
		[HarmonyPrefix]
		[HarmonyPatch(typeof(MachinePortalGenerator), "SetParticles")]
		static void MachinePortalGenerator_SetParticles(List<ParticleSystem> ___particlesOnOpen, MachinePortalGenerator __instance) {
			if (!enableKeepPortalOpen) { return; }
			if (!enableColorPortals) { return; }

			foreach (ParticleSystem particle in ___particlesOnOpen) {
				ParticleSystemRenderer particleCircles = particle.GetComponent<ParticleSystemRenderer>();
				Instance.StartCoroutine(ExecuteLater(delegate () {
					if (!portalToWoId.ContainsKey(__instance.machinePortal)) {
						SetMaterialColor(particleCircles, Color.clear);
						return;
					}
					if (GetWoIdsToPlanetIdHashes().TryGetValue(portalToWoId[__instance.machinePortal], out int planetIdHash)) {
						if (PlanetColor.TryGetValue(planetIdHash, out Color planetColor)) {
							SetMaterialColor(particleCircles, planetColor);
							return;
						}
					}
					if (DefaultPortalColorSet) {
						SetMaterialColor(particleCircles, DefaultPortalColor, true);
					}
				}));
			}
		}
		private static void SetMaterialColor(ParticleSystemRenderer renderer, Color color, bool useDefaultColor = false) {
			if (!renderer.sharedMaterial.HasColor("_EmissionColor")) {
				ParticleSystem.MainModule mm = renderer.GetComponent<ParticleSystem>().main;
				mm.startColor = Color.clear;
			}

			if (!renderer.sharedMaterial.GetName().EndsWith(")")) { // "(Clone)" or "(Instance)"(<-from UnityExplorer)
				ParticleSystem.MainModule mm = renderer.GetComponent<ParticleSystem>().main;
				mm.startSpeed = 1;

				renderer.transform.position -= renderer.transform.forward * 4;
				renderer.sharedMaterial = Instantiate(renderer.sharedMaterial);
			}
			if (!useDefaultColor || DefaultPortalColorSet) {
				renderer.sharedMaterial.SetColor("_EmissionColor", useDefaultColor ? DefaultPortalColor : color);
			}
		}
		// <--- Color Portals ---



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

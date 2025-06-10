using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;

using HarmonyLib;
using SpaceCraft;

//using EVP; // not found, maybe missing assembly
using TMPro;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Unity.Mathematics;
using Unity.Netcode;

using System;
using System.IO;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Diagnostics;

using UnityEngine.Networking;

namespace FeatPortalTeleport {
	
    [BepInPlugin("Nicki0.theplanetcraftermods.FeatPortalTeleport", PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin {
		
		/*
			TODO:
			do portals have to be closed when teleporting???
			
			
		*/
		
		
		static ManualLogSource log;
		
        private void Awake() {
			log = Logger;
			
            // Plugin startup logic
			Harmony.CreateAndPatchAll(typeof(Plugin));
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }
		
		private static int planetToTeleportToHash = 0;
		
		static GameObject buttonA = null;
		static GameObject buttonB = null;
		[HarmonyPostfix]
        [HarmonyPatch(typeof(UiWindowPortalGenerator), "Start")]
        static void UiWindowPortalGenerator_Start(UiWindowPortalGenerator __instance) {
			buttonA = CreateButton(__instance, "ButtonProceduralInstance", new Vector3(110, 880, 0), "MainScene/BaseStack/UI/WindowsHandler/UiWindowInterplanetaryExhange/Container/ContentRocketOnSite/RightContent/SelectedPlanet/PlanetIcon"); //(100, 860, 0)
			buttonB = CreateButton(__instance, "ButtonPortalTravel", new Vector3(110, 780, 0), "MainScene/BaseStack/UI/WindowsHandler/UiWindowInterplanetaryExhange/Container/Title/Image"); //(100, 680, 0)
			// Top Button
			buttonA.GetComponent<Button>().onClick.AddListener(delegate() {
				if (Managers.GetManager<WorldInstanceHandler>().GetOpenedWorldInstanceData() != null) {
					return;
				}
				
				foreach (Transform transform in __instance.transform.Find("Container/UiPortalList/InstancesList/Grid")) {
					if (transform.name ==  "UiWorldInstanceLine(Clone)") {
						transform.gameObject.SetActive(true);
					} else if (transform.name ==  "UiWorldInstanceSelector_PlanetTravel") {
						transform.gameObject.SetActive(false);
					}
				}
				
				__instance.btnScan.SetActive(true);
			});
			
			// Bottom Button
			buttonB.GetComponent<Button>().onClick.AddListener(delegate() {
				if (Managers.GetManager<WorldInstanceHandler>().GetOpenedWorldInstanceData() != null) {
					return;
				}
				
				bool foundPlanetInstanceSelector = false;
				foreach (Transform transform in __instance.transform.Find("Container/UiPortalList/InstancesList/Grid")) {
					if (transform.name ==  "UiWorldInstanceLine(Clone)") {
						transform.gameObject.SetActive(false);
					} else if (transform.name ==  "UiWorldInstanceSelector_PlanetTravel") {
						transform.gameObject.SetActive(true);
						foundPlanetInstanceSelector = true;
					}
				}
				if (!foundPlanetInstanceSelector) AddPortals(__instance);
				
				__instance.btnScan.SetActive(false);
			});
			return;
			
			// Top Button
			TMP_DefaultControls.Resources resourcesA = new TMP_DefaultControls.Resources();
			buttonA = TMP_DefaultControls.CreateButton(resourcesA);
			
			buttonA.name = "ButtonProceduralInstance";
			buttonA.transform.SetParent(__instance.transform, false);
			buttonA.transform.position = new Vector3(110, 880, 0)/*(100, 860, 0)*/;
			buttonA.GetComponent<RectTransform>().sizeDelta = new Vector2(60, 60);
			buttonA.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "";
			buttonA.AddComponent<EventHoverIncrease>().SetHoverGroupEvent();
			buttonA.GetComponent<Image>().sprite = GameObject.Find("MainScene/BaseStack/UI/WindowsHandler/UiWindowInterplanetaryExhange/Container/ContentRocketOnSite/RightContent/SelectedPlanet/PlanetIcon").GetComponent<Image>().sprite;
			buttonA.GetComponent<Button>().onClick.AddListener(delegate() {
				log.LogInfo("buttonA pressed");
				//AddPortals(__instance);
				//__instance.transform.Find("Container/UiPortalList/ScanButton/ScanDestinationsBtn").gameObject.SetActive(true);
				
				if (Managers.GetManager<WorldInstanceHandler>().GetOpenedWorldInstanceData() != null) {
					return;
				}
				
				__instance.btnScan.SetActive(true);
				
				//foreach (GameObject go in listAllAddedInstanceSelectorGOs) go.SetActive(false);
				
				foreach (Transform transform in __instance.transform.Find("Container/UiPortalList/InstancesList/Grid")) {
					if (transform.name ==  "UiWorldInstanceLine(Clone)") {
						transform.gameObject.SetActive(true);
					} else if (transform.name ==  "UiWorldInstanceSelector_PlanetTravel") {
						transform.gameObject.SetActive(false);
					}
					//log.LogInfo(transform + " : " + transform.name);//"UiWorldInstanceLine(Clone)"
				}
				
				//WorldInstanceHandler.InstanceChoices? currentChoices = Managers.GetManager<WorldInstanceHandler>().GetCurrentChoices();
				//if (currentChoices != null) __instance.StartCoroutine(__instance.DisplayInstances(currentChoices.Value));
				
				
			});
			
			GameObject backgroundImageGameObjectA = new GameObject("BackgroundHexagonImage");
			backgroundImageGameObjectA.transform.SetParent(buttonA.transform, false);
			backgroundImageGameObjectA.transform.localScale = new Vector3(1f*0.85f, 1.15f*0.85f, 1f*0.85f)/*(1.1f, 1.28f, 1.1f)*/;
			//backgroundImageGameObjectA.AddComponent<EventHoverIncrease>().SetHoverGroupEvent();
			Image backgroundImageA = backgroundImageGameObjectA.AddComponent<Image>();
			backgroundImageA.sprite = GameObject.Find("MainScene/BaseStack/UI/WindowsHandler/UiWindowInterplanetaryExhange/Container/CloseUiButton").GetComponentInChildren<Image>().sprite;//obj.GetComponent<Image>().sprite;
			
			buttonA.SetActive(true);
			
			
			
			
			
			// Bottom Button
			TMP_DefaultControls.Resources resourcesB = new TMP_DefaultControls.Resources();
			buttonB = TMP_DefaultControls.CreateButton(resourcesB);
			
			buttonB.name = "ButtonPortalTravel";
			buttonB.transform.SetParent(__instance.transform, false);
			buttonB.transform.position = new Vector3(110, 780, 0)/*(100, 680, 0)*/;
			buttonB.GetComponent<RectTransform>().sizeDelta = new Vector2(60, 60);
			buttonB.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "";
			buttonB.AddComponent<EventHoverIncrease>().SetHoverGroupEvent();
			buttonB.GetComponent<Image>().sprite = GameObject.Find("MainScene/BaseStack/UI/WindowsHandler/UiWindowInterplanetaryExhange/Container/Title/Image").GetComponent<Image>().sprite;
			buttonB.GetComponent<Button>().onClick.AddListener(delegate() {
				log.LogInfo("buttonB pressed");
				
				if (Managers.GetManager<WorldInstanceHandler>().GetOpenedWorldInstanceData() != null) {
					return;
				}
				
				bool foundPlanetInstanceSelector = false;
				foreach (Transform transform in __instance.transform.Find("Container/UiPortalList/InstancesList/Grid")) {
					if (transform.name ==  "UiWorldInstanceLine(Clone)") {
						transform.gameObject.SetActive(false);
					} else if (transform.name ==  "UiWorldInstanceSelector_PlanetTravel") {
						transform.gameObject.SetActive(true);
						foundPlanetInstanceSelector = true;
					}
				}
				if (!foundPlanetInstanceSelector) AddPortals(__instance);
				
				
				__instance.btnScan.SetActive(false);
			});
			
			GameObject backgroundImageGameObjectB = new GameObject("BackgroundHexagonImage");
			backgroundImageGameObjectB.transform.SetParent(buttonB.transform, false);
			backgroundImageGameObjectB.transform.localScale = new Vector3(1f*0.83f, 1.15f*0.83f, 1f*0.83f)/*(1.1f, 1.28f, 1.1f)*/;
			//backgroundImageGameObjectB.AddComponent<EventHoverIncrease>().SetHoverGroupEvent();
			Image backgroundImageB = backgroundImageGameObjectB.AddComponent<Image>();
			backgroundImageB.sprite = GameObject.Find("MainScene/BaseStack/UI/WindowsHandler/UiWindowInterplanetaryExhange/Container/CloseUiButton").GetComponent<Image>().sprite;//obj.GetComponent<Image>().sprite;
			
			buttonB.SetActive(true);
			
			
			//AddPortals(__instance);
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
			backgroundImageGameObjectA.transform.localScale = new Vector3(1f*0.85f, 1.15f*0.85f, 1f*0.85f)/*(1.1f, 1.28f, 1.1f)*/;
			backgroundImageGameObjectA.AddComponent<Image>().sprite = GameObject.Find("MainScene/BaseStack/UI/WindowsHandler/UiWindowInterplanetaryExhange/Container/CloseUiButton").GetComponentInChildren<Image>().sprite;
			
			button.SetActive(true);
			
			return button;
		}
		[HarmonyPostfix]
        [HarmonyPatch(typeof(UiWindowPortalGenerator), nameof(UiWindowPortalGenerator.OnOpen))]
        static void UiWindowPortalGenerator_OnOpen(UiWindowPortalGenerator __instance) {
			__instance.StartCoroutine(hideButtonBOnOpen());
			if (Managers.GetManager<WorldInstanceHandler>().GetOpenedWorldInstanceData() != null) {
				if (buttonB != null) buttonB.SetActive(false);
			}
        }
		private static IEnumerator hideButtonBOnOpen() { // Hide buttonB because on first load, buttonB == null
			yield return new WaitForSeconds(0.01f);
			if (Managers.GetManager<WorldInstanceHandler>().GetOpenedWorldInstanceData() != null) {
				if (buttonB != null) buttonB.SetActive(false);
			}
			yield break;
		}
		[HarmonyPostfix]
        [HarmonyPatch(typeof(UiWindowPortalGenerator), nameof(UiWindowPortalGenerator.ShowOpenedInstance))]
        static void UiWindowPortalGenerator_ShowOpenedInstance() {
			if (buttonB != null) buttonB.SetActive(false);
        }
		[HarmonyPostfix]
        [HarmonyPatch(typeof(UiWindowPortalGenerator), nameof(UiWindowPortalGenerator.OnCloseInstance))]
        static void UiWindowPortalGenerator_OnCloseInstance() {
			if (buttonB != null) buttonB.SetActive(true);
        }
		
		[HarmonyPostfix]
        [HarmonyPatch(typeof(UiWindowPortalGenerator), "ShowUiWindows")]
        static void UiWindowPortalGenerator_ShowUiWindows(UiWindowPortalGenerator __instance) {
			if (buttonA != null && buttonB != null) {
				buttonA.SetActive(!__instance.uiOnScanning.activeSelf);
				buttonB.SetActive(!__instance.uiOnScanning.activeSelf);
			}
        }
		
		
		static void AddPortals(UiWindowPortalGenerator __instance) {
			PlayerMainController pmc = Managers.GetManager<PlayersManager>().GetActivePlayerController();
			WorldUnitsHandler wuh = Managers.GetManager<WorldUnitsHandler>();
			Group groupPortalGenerator = GroupsHandler.GetGroupViaId("PortalGenerator1");
			foreach (PlanetData pd in Managers.GetManager<PlanetLoader>().planetList.GetPlanetList()) {
				if (pd.GetPlanetId() == Managers.GetManager<PlanetLoader>().GetCurrentPlanetData().GetPlanetId()) continue;
				
				if (!wuh.AreUnitsInited(pd.GetPlanetId())) {
					log.LogInfo(pd.GetPlanetId() + " Units not initialized"); 
					continue;
				}
				
				double completeTi = 0;
				
				List<TerraformStage> terraformStages = pd.GetPlanetTerraformationStages().Where(stage => stage.GetTerraId() == "Complete").ToList();
				if (terraformStages.Count() > 0) {
					completeTi = terraformStages.First().GetStageStartValue();
				} else {
					log.LogInfo(pd.GetPlanetId() + " Does not have a \"Complete\" stage");
				}
				
				if (wuh.GetUnit(DataConfig.WorldUnitType.Terraformation, pd.GetPlanetId()).GetValue() < completeTi) continue;
				
				WorldObject woPortalGenerator = WorldObjectsHandler.Instance.GetFirstWorldObjectOfGroup(groupPortalGenerator, pd.GetPlanetHash());
				if (woPortalGenerator == null) continue;
				
				
				
				GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(__instance.uiWorldInstanceSelector, __instance.gridForInstances.transform);
				gameObject.name = "UiWorldInstanceSelector_PlanetTravel";
				Recipe recipe = new Recipe(new List<GroupDataItem>());
				//AccessTools.FieldRefAccess<Recipe, List<Group>>(recipe, "_ingredientsGroups").Add(GroupsHandler.GetGroupViaId("Iron"));
				List<bool> list = pmc.GetPlayerBackpack().GetInventory().ItemsContainsStatus(recipe.GetIngredientsGroupInRecipe());
				gameObject.GetComponent<UiWorldInstanceSelector>().SetValues(
							new WorldInstanceData(pd.GetPlanetId(), pd.GetPlanetHash(), 0, recipe, 0, 0), 
							recipe.GetIngredientsGroupInRecipe(), 
							list, 
							new Action<UiWorldInstanceSelector>(delegate(UiWorldInstanceSelector uiWorldInstanceSelector) {
					
					planetToTeleportToHash = pd.GetPlanetHash();
					
					List<MachinePortalGenerator> allMachinePortalGenerators = AccessTools.FieldRefAccess<WorldInstanceHandler, List<MachinePortalGenerator>>(Managers.GetManager<WorldInstanceHandler>(), "_allMachinePortalGenerator");
					foreach (MachinePortalGenerator mpg in allMachinePortalGenerators) {
						if (!mpg.gameObject.activeInHierarchy) continue;
						GameObject go = new GameObject();
						mpg.OpenPortal(null, go, false);
						portalCreatedByMod = true;
					}
					
					Managers.GetManager<WindowsHandler>().CloseAllWindows();
				}), false);
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
		
		[HarmonyPrefix]
        [HarmonyPatch(typeof(WorldInstanceData), nameof(WorldInstanceData.GetSeedLabel))]
        private static bool WorldInstanceData_GetSeedLabel(int ____seed, ref string __result) {
			PlanetData pd = Managers.GetManager<PlanetLoader>().planetList.GetPlanetFromIdHash(____seed);
			if (pd == null) return true;
			__result = pd.GetPlanetId();
			return false;
		}
		
		// prevent difficulty and rarity strings from displaying
		[HarmonyPostfix]
        [HarmonyPatch(typeof(UiWorldInstanceSelector), nameof(UiWorldInstanceSelector.SetValues))]
        private static void UiWorldInstanceSelector_SetValues(WorldInstanceData worldInstanceData, TextMeshProUGUI ___difficulty, TextMeshProUGUI ___rarity) {
			PlanetData pd = Managers.GetManager<PlanetLoader>().planetList.GetPlanetFromIdHash(worldInstanceData.GetSeed());
			if (pd == null) return;
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
        private static bool MachinePortal_GoToFinalPosition(ref bool ____playerInTunel, InputActionReference[] ___inputsToDisableInTunnel) {
			if (!portalCreatedByMod) return true;
			
			if (____playerInTunel) {
				____playerInTunel = false;
				MachineBeaconUpdater.HideBeacons = false;
				InputActionReference[] array = ___inputsToDisableInTunnel;
				for (int i = 0; i < array.Length; i++) {
					array[i].action.Enable();
				}
				Managers.GetManager<SavedDataHandler>().DecrementSaveLock();
				
				// ^v will lift each other, but for testing, keeping both: top from game code, bottom from own planet switch
				
				Managers.GetManager<SavedDataHandler>().IncrementSaveLock(); // semaphore lock saving
				semaphoreActive = true;
				PlanetData pd = Managers.GetManager<PlanetLoader>().planetList.GetPlanetFromIdHash(planetToTeleportToHash);
				if (pd != null) PlanetNetworkLoader.Instance.SwitchToPlanet(pd);
			}
			return false;
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
				AccessTools.Method(typeof(PlanetNetworkLoader), "SwitchToPlanetClientRpc").Invoke(__instance, new object[] {____planetIndex.Value, pos + new Vector3(0, 7, 0), (int)rot.eulerAngles.y + 90});
			}
		}
		// <--- Prevent the creation and deletion of capsules ---
		
		/*
		// Token: 0x06000D51 RID: 3409 RVA: 0x00056A00 File Offset: 0x00054C00
		private void OnTriggerEnter(Collider other)
		{
			PlayerMainController component = other.gameObject.GetComponent<PlayerMainController>();
			if (component == null || component != Managers.GetManager<PlayersManager>().GetActivePlayerController())
			{
				return;
			}
			Vector3 positionToTeleport = this._positionToTeleport;
			this.GoInsidePortal();
		}

		// Token: 0x06000D52 RID: 3410 RVA: 0x00056A44 File Offset: 0x00054C44
		private void GoInsidePortal()
		{
			GameObject portalTunnelGameObject = Managers.GetManager<VisualsResourcesHandler>().GetPortalTunnelGameObject();
			PlayerMainController activePlayerController = Managers.GetManager<PlayersManager>().GetActivePlayerController();
			GameObject gameObject = Object.Instantiate<GameObject>(portalTunnelGameObject, null);
			gameObject.transform.position = new Vector3(1000f, 1000f);
			activePlayerController.SetPlayerPlacement(gameObject.transform.position, gameObject.transform.rotation, true);
			Managers.GetManager<SavedDataHandler>().IncrementSaveLock();
			InputActionReference[] array = this.inputsToDisableInTunnel;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].action.Disable();
			}
			base.Invoke("GoToFinalPosition", this._timeInTunnel);
			Object.Destroy(gameObject, this._timeInTunnel);
			MachineBeaconUpdater.HideBeacons = true;
			if (this._enterPortal)
			{
				Managers.GetManager<WorldInstanceHandler>().SetWorldInstanceActive(true);
			}
			this._playerInTunel = true;
		}*/
    }
}

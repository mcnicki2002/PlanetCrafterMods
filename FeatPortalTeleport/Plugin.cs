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
			UI
			Create the portal
			only teleport when the correct portal is set
		*/
		
		
		static ManualLogSource log;
		
        private void Awake() {
			log = Logger;
			
            // Plugin startup logic
			Harmony.CreateAndPatchAll(typeof(Plugin));
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }
		
		private static int planetToTeleportToHash = 0;
		class PortalDestination {
			public int destinationPlanet = 0;
		}
		[HarmonyPostfix]
        [HarmonyPatch(typeof(UiWindowPortalGenerator), "OnOpen")]
        static void UiWindowPortalGenerator_OnOpen(UiWindowPortalGenerator __instance) {
			AddPortals(__instance);
        }
		[HarmonyPostfix]
        [HarmonyPatch(typeof(UiWindowPortalGenerator), "SelectFirstButtonInGrid")]
        static void UiWindowPortalGenerator_SelectFirstButtonInGrid(UiWindowPortalGenerator __instance) {
			AddPortals(__instance);
        }
		static void AddPortals(UiWindowPortalGenerator __instance) {
			PlayerMainController pmc = Managers.GetManager<PlayersManager>().GetActivePlayerController();
			WorldUnitsHandler wuh = Managers.GetManager<WorldUnitsHandler>();
			foreach (PlanetData pd in Managers.GetManager<PlanetLoader>().planetList.GetPlanetList()) {
				if (pd.GetPlanetId() == Managers.GetManager<PlanetLoader>().GetCurrentPlanetData().GetPlanetId()) continue;
				
				if (!wuh.AreUnitsInited(pd.GetPlanetId())) {
					log.LogInfo(pd.GetPlanetId() + " Units not initialized"); 
					continue;
				}
				
				List<TerraformStage> terraformStages = pd.GetPlanetTerraformationStages().Where(stage => stage.GetTerraId() == "Complete").ToList();
				if (terraformStages.Count() < 1) {
					log.LogInfo(pd.GetPlanetId() + " Does not have a \"Complete\" stage");
					continue;
				
				}
				
				double completeTi = terraformStages.First().GetStageStartValue();
				if (wuh.GetUnit(DataConfig.WorldUnitType.Terraformation, pd.GetPlanetId()).GetValue() < completeTi) continue;
				
				GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(__instance.uiWorldInstanceSelector, __instance.gridForInstances.transform);
				Recipe recipe = new Recipe(new List<GroupDataItem>());
				//AccessTools.FieldRefAccess<Recipe, List<Group>>(recipe, "_ingredientsGroups").Add(GroupsHandler.GetGroupViaId("Iron"));
				List<bool> list = pmc.GetPlayerBackpack().GetInventory().ItemsContainsStatus(recipe.GetIngredientsGroupInRecipe());
				gameObject.GetComponent<UiWorldInstanceSelector>().SetValues(new WorldInstanceData(pd.GetPlanetId(),pd.GetPlanetHash(),0,recipe,0,0), recipe.GetIngredientsGroupInRecipe(), list, new Action<UiWorldInstanceSelector>(delegate(UiWorldInstanceSelector uiWorldInstanceSelector) {
					//Managers.GetManager<SavedDataHandler>().IncrementSaveLock(); // semaphore lock saving
					//semaphoreActive = true;
					//PlanetNetworkLoader.Instance.SwitchToPlanet(pd);
					
					planetToTeleportToHash = pd.GetPlanetHash();
					
					//foreach (Component c in __instance.GetComponentsInChildren(typeof(Component))) log.LogInfo(c);
					//MachinePortalGenerator mpg = __instance.GetComponentInParent<MachinePortalGenerator>();
					//if (mpg != null) mpg.machinePortal.gameObject.SetActive(true);//mpg.OpenPortal(null, null, false);
					
					log.LogInfo("bla");
					List<MachinePortalGenerator> allMachinePortalGenerators = AccessTools.FieldRefAccess<WorldInstanceHandler, List<MachinePortalGenerator>>(Managers.GetManager<WorldInstanceHandler>(), "_allMachinePortalGenerator");
					foreach (MachinePortalGenerator mpg in allMachinePortalGenerators) {
						log.LogInfo("bli");
						//mpg.machinePortal.gameObject.SetActive(true);
						GameObject go = new GameObject();
						mpg.OpenPortal(null, go, false);
					}
					
					Managers.GetManager<WindowsHandler>().CloseAllWindows();
				}), false);
			}
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
		
		[HarmonyPostfix]
        [HarmonyPatch(typeof(UiWorldInstanceSelector), nameof(UiWorldInstanceSelector.SetValues))]
        private static void UiWorldInstanceSelector_SetValues(WorldInstanceData worldInstanceData, TextMeshProUGUI ___difficulty, TextMeshProUGUI ___rarity) {
			PlanetData pd = Managers.GetManager<PlanetLoader>().planetList.GetPlanetFromIdHash(worldInstanceData.GetSeed());
			if (pd == null) return;
			___difficulty.text = "";
			___rarity.text = "";
		}
		
		[HarmonyPrefix]
        [HarmonyPatch(typeof(MachinePortal), "GoInsidePortal")]
        private static void Pre_MachinePortal_GoInsidePortal(ref bool ____enterPortal, ref bool __state) {
			__state = ____enterPortal;
			____enterPortal = false;
		}
		[HarmonyPostfix]
        [HarmonyPatch(typeof(MachinePortal), "GoInsidePortal")]
        private static void Post_MachinePortal_GoInsidePortal(ref bool ____enterPortal, ref bool __state) {
			____enterPortal = __state;
		}
		[HarmonyPrefix]
        [HarmonyPatch(typeof(MachinePortal), "GoToFinalPosition")]
        private static bool MachinePortal_GoToFinalPosition(ref bool ____playerInTunel, InputActionReference[] ___inputsToDisableInTunnel) {
			
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

// Copyright 2025-2025 Nicolas Schäfer & Contributors
// Licensed under Apache License, Version 2.0

using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using SpaceCraft;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nicki0.FeatSpaceStationPlanet {

	[BepInPlugin("Nicki0.theplanetcraftermods.SpaceStationPlanet", "(Feat) Space Station", PluginInfo.PLUGIN_VERSION)]
	public class Plugin : BaseUnityPlugin {
		/*
		 *	TODO:
		 *	- Map Background should be black, not dust-orange
		 *	- disable larvae spawning
		 *	First fall quite extreme... bug in TPC: game does not reset fall speed while standing.
		 *	
		 *	Long-term:
		 *	
		 */

		private static Plugin Instance;

		static ManualLogSource log;

		static MethodInfo method_Asteroid_SetFxStatuts;

		static readonly string planetName = "SpaceStation";
		static readonly string planetSceneName = "SpaceStation_EmptyScene";
		static readonly Vector3 planetPosition = new Vector3(2000, 200, 2000);
		static readonly Dictionary<string, string[]> planetNameLocalized = new() {
			{ "english", new string[] { "Space Station", "Terraform Space! Or at least create a livable environment inside your space station." } }
		};

		public void Awake() {
			log = Logger;
			Instance = this;

			method_Asteroid_SetFxStatuts = AccessTools.Method(typeof(Asteroid), "SetFxStatuts");

			Harmony.CreateAndPatchAll(typeof(Plugin));
			Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
		}



		[HarmonyPrefix]
		[HarmonyPatch(typeof(PlanetList), "InitPlanetList")]
		public static void PlanetList_InitPlanetList(ref PlanetData[] ____planets) {
			foreach (PlanetData pd in ____planets) if (pd.id == planetName) return;


			foreach (MeteoEventData med in Array.Find(____planets, e => e.id == "Prime").meteoEvents) {
				List<GroupDataItem> meteorMaterialGroups = med?.asteroidEventData?.asteroidGameObject?.GetComponent<Asteroid>()?.groupsSelected;
				if (meteorMaterialGroups == null) continue;
				foreach (GroupDataItem gdi in meteorMaterialGroups) {
					asteroidOresForHarvestingRobot.Add(gdi);
				}
			}


			List<PlanetData> newPlanetList = new List<PlanetData>();
			foreach (PlanetData pd in ____planets) {
				newPlanetList.Add(pd);
				log.LogInfo(pd.id + " : " + pd.steamAppId);
			}

			PlanetData newPlanet = PlanetData.Instantiate(Array.Find(____planets, e => e.id == "Selenea"));
			DontDestroyOnLoad(newPlanet); // Taken from FlatLands mod by akarnokd
			newPlanetList.Add(newPlanet);

			log.LogInfo(____planets[1].steamAppId);

			newPlanet.id = planetName;
			newPlanet.name = planetName;
			newPlanet.steamAppId = 0;
			newPlanet.sceneName = planetSceneName;
			newPlanet.showInBuild = true;
			newPlanet.startingPlanet = false;



			TerraformStage completeStage = Instantiate(newPlanet.allTerraStages.Last());
			newPlanet.allTerraStages.RemoveRange(1, newPlanet.allTerraStages.Count - 1);
			newPlanet.allTerraStages.Add(completeStage);
			AccessTools.FieldRefAccess<TerraformStage, double>(completeStage, "startValue") = 1000100000000;


			TerraformStage neverStage = ScriptableObject.CreateInstance<TerraformStage>();//new TerraformStage();
			AccessTools.FieldRefAccess<TerraformStage, string>(neverStage, "id") = "Never";
			AccessTools.FieldRefAccess<TerraformStage, DataConfig.WorldUnitType>(neverStage, "unitType") = DataConfig.WorldUnitType.Terraformation;
			AccessTools.FieldRefAccess<TerraformStage, double>(neverStage, "startValue") = double.MaxValue;
			AccessTools.FieldRefAccess<TerraformStage, Sprite>(neverStage, "icon") = Sprite.Create(Texture2D.blackTexture, new Rect(0.0f, 0.0f, 4, 4), new Vector2(0f, 0f), 100.0f);

			newPlanet.skyChangeTerraStage = neverStage;
			newPlanet.startCloudsTerraStage = neverStage;
			newPlanet.fullCloudsTerraStage = neverStage;
			newPlanet.startMossTerraStage = neverStage;
			newPlanet.endMossTerraStage = neverStage;
			newPlanet.startBreathMoreStage = neverStage;
			newPlanet.noNeedForOxygenStage = neverStage;

			EnvironmentVolumeVariables envEmpty = Instantiate(newPlanet.envDataStart);
			envEmpty.fogColor = Color.black;

			newPlanet.envDataStart = envEmpty;
			newPlanet.envDataEnd = envEmpty;
			newPlanet.envDataNight = envEmpty;


			newPlanet.layersToMoss.Clear();
			newPlanet.mossPotentialColors.Clear();
			//newPlanet.meteoEvents.Clear();
			newPlanet.evolutionners.Clear();
			newPlanet.disableMusicsSectorsLimitations = true;
			newPlanet.availableGeneticTraits.Clear();
			newPlanet.tutorialSteps.Clear();
			newPlanet.spawnPositions = new PlanetData.SpawnPositions[] {
				new PlanetData.SpawnPositions {
					id = "Standard",
					positions = new List<PositionAndRotation>() {
						new PositionAndRotation(
							planetPosition,
							Quaternion.identity
						)
					}
				}
			};
			newPlanet.availableStoryEvents.Clear();
			newPlanet.manualStoryEvents.Clear();


			//RenderSettings.skybox = new Material(Material.GetDefaultParticleMaterial());
			//RenderSettings.skybox.color = Color.black;
			//RenderSettings.skybox.mainTexture = Texture2D.blackTexture;
			//RenderSettings.skybox.SetFloat("_Exposure",100);

			____planets = newPlanetList.ToArray();
		}

		static AssetBundle bundle;
		[HarmonyPrefix]
		[HarmonyPatch(typeof(PlanetLoader), "LoadScene")]
		public static void PlanetLoader_LoadScene(PlanetData planetToLoad) {
			if (planetToLoad.id == planetName) {
				//RenderSettings.skybox = new Material(Material.GetDefaultParticleMaterial());
				//RenderSettings.skybox.color = Color.black;

				if (!SceneManager.GetSceneByName(planetSceneName).IsValid()) {
					//Scene newScene = SceneManager.CreateScene(planetSceneName); // For creating an empty map/scene

					// --- Load Custom Map ---
					if (bundle == null) {
						bundle ??= AssetBundle.LoadFromFile(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/spacestationmap");
					}

					string[] scenePath = bundle.GetAllScenePaths();
					Scene newScene = SceneManager.GetSceneByPath(scenePath[0]);
					Instance.StartCoroutine(AddScripts());

				}
			}
		}

		private static IEnumerator AddScripts() {
			WaitForSeconds wait = new WaitForSeconds(1);
			int counter = 60;
			while (GameObject.Find("World/OreVeins/Ores/MagnetarQuartz") == null && --counter > 0) {
				yield return wait;
			}

			GameObject goOreMagnetarQuartz = GameObject.Find("World/OreVeins/Ores/MagnetarQuartz");
			MachineGenerationGroupVein mggvMagnetarQuartz = goOreMagnetarQuartz.AddComponent<MachineGenerationGroupVein>();
			mggvMagnetarQuartz.SetGroups([GroupsHandler.GetGroupViaId("MagnetarQuartz").GetGroupData()]);
			mggvMagnetarQuartz.oreVeinIdentifer = DataConfig.OreVeinIdentifer.tier2;

			GameObject goOreAlgae = GameObject.Find("World/OreVeins/Ores/Algae");
			MachineGenerationGroupVein mggvAlgae = goOreAlgae.AddComponent<MachineGenerationGroupVein>();
			mggvAlgae.SetGroups([GroupsHandler.GetGroupViaId("Algae1Seed").GetGroupData()]);
			mggvAlgae.oreVeinIdentifer = DataConfig.OreVeinIdentifer.tier2;

			GameObject goOreIce = GameObject.Find("World/OreVeins/Ores/Ice");
			MachineGenerationGroupVein mggvIce = goOreIce.AddComponent<MachineGenerationGroupVein>();
			mggvIce.SetGroups([GroupsHandler.GetGroupViaId("ice").GetGroupData()]);
			mggvIce.oreVeinIdentifer = DataConfig.OreVeinIdentifer.tier1;

			GameObject goTerrain = GameObject.Find("World/Terrains/TerrainAsteroids");
			HomemadeTag ht = goTerrain.AddComponent<HomemadeTag>();
			ht.homemadeTag = DataConfig.HomemadeTag.SurfaceTerrain;
		}

		/*[HarmonyPrefix]
				[HarmonyPatch(typeof(PlanetLoader), "HandleDataAfterLoad")]
				static void PlanetLoader_HandleDataAfterLoad(PlanetData ____selectedPlanet){
			TerrainData terrainData = new TerrainData();
			//GameObject terrain = new Terrain();
			
			int res = terrainData.heightmapResolution;
			log.LogInfo(res);
						float[,] heights = terrainData.GetHeights(0, 0, res, res);
						for (int i = 0; i < res; i++)
						{
								for (int j = 0; j < res; j++)
								{
										heights[i, j] = i*j;//(Math.Abs(i-res/2) + Math.Abs(j-res/2))/(res/10);
								}
						}
			heights[1, 1] = 100;
						terrainData.SetHeights(0, 0, heights);
			
			//terrainData.size = new Vector3(1000, 100, 1000);
			
			GameObject go = Terrain.CreateTerrainGameObject(terrainData);
			go.SetActive(true);
		}*/

		[HarmonyPrefix]
		[HarmonyPatch(typeof(PlanetUnlocker), nameof(PlanetUnlocker.LoadUnlockedPlanets))]
		static void PlanetUnlocker_LoadUnlockedPlanets() {// From Akarnokd "FlatLands"
														  // make sure planetlist is initialized
			Managers.GetManager<PlanetLoader>().planetList.GetPlanetList(true);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(Localization), "LoadLocalization")]
		private static void Localization_LoadLocalization(Dictionary<string, Dictionary<string, string>> ___localizationDictionary) {
			foreach (KeyValuePair<string, string[]> translation in planetNameLocalized) {
				if (___localizationDictionary.TryGetValue(translation.Key, out var dictionary)) {
					dictionary["Planet_" + planetName] = translation.Value[0];
					dictionary["Planet_Desc_" + planetName] = translation.Value[1];
				}
			}
		}


		/*[HarmonyPrefix]
		[HarmonyPatch(typeof(ConstraintOnSurfaces), "Start")]
		private static void ConstraintOnSurfaces_Start(ConstraintOnSurfaces __instance) {
			
			if (!__instance.allowedTaggedSurfaces.Contains(DataConfig.HomemadeTag.SurfaceFloor)) __instance.allowedTaggedSurfaces.Add(DataConfig.HomemadeTag.SurfaceFloor);
			if (!__instance.allowedTaggedSurfaces.Contains(DataConfig.HomemadeTag.SurfaceGrid)) __instance.allowedTaggedSurfaces.Add(DataConfig.HomemadeTag.SurfaceGrid);
		}*/

		private static List<GroupData> asteroidOresForHarvestingRobot = new List<GroupData>();
		[HarmonyPrefix]
		[HarmonyPatch(typeof(ActionGroupSelector), "OpenInventories")]
		private static void ActionGroupSelector_OpenInventories(WorldObject worldObject, List<GroupData> ___oreList) {
			if (worldObject?.GetGroup()?.GetId() == "HarvestingRobot1" &&
				worldObject?.GetPlanetHash() == planetName.GetStableHashCode()
			) {
				if (asteroidOresForHarvestingRobot.Count == 0) {
					log.LogWarning("asteroid list is empty!!!");
				}
				___oreList.AddRange(asteroidOresForHarvestingRobot);
			}
		}

		private static List<string> lockedGroupsOnNewPlanet = new List<string>() {
			/*"Drill0",
			"Drill1",
			"Drill2",
			"Drill3",
			"Drill4",*/
			"WindTurbine1",
			"PortalGenerator1",
			"WaterCollector1",
			"WaterCollector2",
			"GrassSpreader1",
			"SeedSpreader1",
			"SeedSpreader2",
			"AlgaeGenerator1",
			"AlgaeGenerator2",
			"TreeSpreader0",
			"TreeSpreader1",
			"TreeSpreader2",
			"ButterflyFarm1",
			"ButterflyFarm2",
			"ButterflyFarm3",
			"WaterLifeCollector1",
			"FishFarm1",
			"FishFarm2",
			"AmphibiansFarm1",
			"AnimalShelter1",
			"Ecosystem1"
		};
		private static Dictionary<string, List<DataConfig.HomemadeTag>> SurfaceConstraintsForGroupData = new Dictionary<string, List<DataConfig.HomemadeTag>>() {
			{ "Heater4", [DataConfig.HomemadeTag.SurfaceFloor]},
			{ "Heater5", [DataConfig.HomemadeTag.SurfaceFloor]},
			{ "Beehive1", [DataConfig.HomemadeTag.SurfaceFloor]},
			{ "Beehive2", [DataConfig.HomemadeTag.SurfaceFloor]},
			{ "OreBreaker1", [DataConfig.HomemadeTag.SurfaceFloor]},
			{ "OreBreaker2", [DataConfig.HomemadeTag.SurfaceFloor]},
			{ "OreBreaker3", [DataConfig.HomemadeTag.SurfaceFloor]},
			{ "DroneStation1", [DataConfig.HomemadeTag.SurfaceFloor]},
			{ "ComAntenna", [DataConfig.HomemadeTag.SurfaceFloor]},
			{ "VegetubeOutside1", [DataConfig.HomemadeTag.SurfaceFloor]},
			{ "Farm1", [DataConfig.HomemadeTag.SurfaceFloor]},
			{ "Farm2", [DataConfig.HomemadeTag.SurfaceFloor]},
			{ "FountainBig", [DataConfig.HomemadeTag.SurfaceFloor]},
			{ "TreePlanter", [DataConfig.HomemadeTag.SurfaceFloor]},
			{ "OutsideLamp1", [DataConfig.HomemadeTag.SurfaceFloor]},
			{ "EnergyGenerator4", [DataConfig.HomemadeTag.SurfaceFloor]},
			{ "EnergyGenerator5", [DataConfig.HomemadeTag.SurfaceFloor]}
		};
		[HarmonyPrefix]
		[HarmonyPatch(typeof(StaticDataHandler), "LoadStaticData")]
		static void StaticDataHandler_LoadStaticData(ref List<GroupData> ___groupsData) {

			Managers.GetManager<PlanetLoader>()?.planetList?.GetPlanetList(); // Load PlanetList.InitPlanetList and therefore the space station planet // Using '?' because new save file creation crashes here

			PlanetData newPlanetData = Managers.GetManager<PlanetLoader>()?.planetList?.GetPlanetFromId(planetName);
			if (newPlanetData == null) {
				log.LogError("StaticDataHandler.LoadStaticData did not find " + planetName);
				return;
			}

			foreach (GroupData groupData in ___groupsData) {
				if (groupData != null && groupData is GroupDataConstructible gdc) {
					if (lockedGroupsOnNewPlanet.Contains(groupData.id)) {
						gdc.notAllowedPlanetsRequirement ??= new List<PlanetData>();
						gdc.notAllowedPlanetsRequirement.Add(newPlanetData);
					}
					if (SurfaceConstraintsForGroupData.ContainsKey(groupData.id) && gdc.associatedGameObject != null) {
						ConstraintOnSurfaces groupDataCOS = gdc.associatedGameObject.GetComponent<ConstraintOnSurfaces>();
						if (groupDataCOS != null && groupDataCOS.allowedTaggedSurfaces != null) {
							foreach (DataConfig.HomemadeTag constraintTag in SurfaceConstraintsForGroupData[groupData.id]) {
								groupDataCOS.allowedTaggedSurfaces.Add(constraintTag);
							}
						}
					}
				}
			}
		}

		private static PlanetLoader planetLoader = null;
		private static bool IsOnPlanet() {
			if (planetLoader == null) {
				planetLoader = Managers.GetManager<PlanetLoader>();
			}
			return planetLoader.GetCurrentPlanetData()?.GetPlanetId() == planetName;
		}

		// --- Player Movement --->
		private static float lastJetpackFactor = 1;
		[HarmonyPrefix]
		[HarmonyPatch(typeof(PlayerMovable), nameof(PlayerMovable.UpdatePlayerMovement))]
		static void Pre_PlayerMovable_UpdatePlayerMovement(ref float[] __state, ref float ___PlayerWeight, float ___jetpackFactor, ref float ___JumpImpulse, int ___jumpStatusInAir) {
			__state = new float[] { ___PlayerWeight, ___JumpImpulse };
			lastJetpackFactor = ___jetpackFactor;

			if (IsOnPlanet()) {
				___PlayerWeight *= 0.1f;
				if (___jumpStatusInAir == 2) ___JumpImpulse *= 0.2f;
			}
		}
		[HarmonyPostfix]
		[HarmonyPatch(typeof(PlayerMovable), nameof(PlayerMovable.UpdatePlayerMovement))]
		static void Post_PlayerMovable_UpdatePlayerMovement(ref float[] __state, ref float ___PlayerWeight, ref float ___JumpImpulse) {
			___PlayerWeight = __state[0];
			___JumpImpulse = __state[1];
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(PlayerGroundRelation), nameof(PlayerGroundRelation.GetGroundDistance))]
		static bool PlayerGroundRelation_GetGroundDistance(ref float __result) {
			if (IsOnPlanet()) {
				__result = 3.5f + 2f * lastJetpackFactor - 0.1f;
				return false;
			}
			return true;
		}
		// <--- Player Movement ---

		// --- Meteors --->
		static readonly Vector3 meteorCenter = new Vector3(planetPosition.x, planetPosition.y + 1000, planetPosition.z);
		static readonly Vector3 meteorSize = new Vector3(500, 5, 500);
		[HarmonyPostfix]
		[HarmonyPatch(typeof(AsteroidsHandler), nameof(AsteroidsHandler.InitAsteroidsHandler))]
		static void AsteroidsHandler_InitAsteroidsHandler(List<Collider> ___spawnBoxes, List<Collider> ___authorizedPlaces) {
			if (IsOnPlanet()) {
				GameObject goSpawn = new GameObject();
				goSpawn.AddComponent<BoxCollider>();
				BoxCollider spawnCollider = goSpawn.GetComponent<BoxCollider>();
				//spawnCollider.enabled = false;
				spawnCollider.isTrigger = true;
				spawnCollider.providesContacts = false;
				spawnCollider.center = meteorCenter;
				spawnCollider.size = meteorSize;
				___spawnBoxes.Add(spawnCollider);

				log.LogInfo(spawnCollider.bounds.min + " :: " + spawnCollider.bounds.max);

				/*GameObject goPlace = new GameObject();
				goPlace.AddComponent<SphereCollider>();
				SphereCollider placeCollider = goPlace.GetComponent<SphereCollider>();
				//placeCollider.enabled = false;
				//placeCollider.isTrigger = true;
				//placeCollider.providesContacts = false;
				placeCollider.center = new Vector3(planetPosition.x, planetPosition.y, planetPosition.z);
				placeCollider.radius = 20;
				___authorizedPlaces.Add(placeCollider);*/
			}
		}


		[HarmonyPrefix]
		[HarmonyPatch(typeof(AsteroidsHandler), "IsInAuthorizedBounds")]
		static bool AsteroidsHandler_IsInAuthorizedBounds(ref bool __result, Vector3 position) {
			if (IsOnPlanet()) {
				__result = true;
				return false;
			}
			return true;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(Asteroid), "Start")]
		static bool Asteroid_Start(
					Asteroid __instance,
					ref Vector3 ____startPoint,
					ref Vector3 ____impactPoint,
					float ___maxLiveTime,
					List<GroupItem> ___associatedGroups
		) {
			if (!IsOnPlanet()) {
				return true;
			}
			bool hitSpot = Physics.Raycast(__instance.transform.position, __instance.transform.forward, out RaycastHit raycastHit, 10000f, ~LayerMask.GetMask(GameConfig.commonIgnoredAndWater));
			if (hitSpot) return true;

			____startPoint = __instance.transform.position;
			____impactPoint = __instance.transform.position + __instance.transform.forward * 4000;

			method_Asteroid_SetFxStatuts.Invoke(__instance, new object[] { __instance.fxContainerTail, true }); //this.SetFxStatuts(__instance.fxContainerTail, true);
			method_Asteroid_SetFxStatuts.Invoke(__instance, new object[] { __instance.fxContainerImpact, false }); //this.SetFxStatuts(__instance.fxContainerImpact, false);
			UnityEngine.Object.Destroy(__instance.gameObject, ___maxLiveTime);
			__instance.audioExplosion?.Stop();
			__instance.audioTrail?.Play();
			foreach (GroupDataItem groupDataItem in __instance.groupsSelected) {
				___associatedGroups.Add((GroupItem)GroupsHandler.GetGroupViaId(groupDataItem.id));
			}
			PlanetLoader manager = Managers.GetManager<PlanetLoader>();
			//manager.planetIsLoaded = (Action)Delegate.Combine(manager.planetIsLoaded, new Action(__instance.Destroy));
			manager.planetIsLoaded = (Action)Delegate.Combine(manager.planetIsLoaded, new Action(delegate () { UnityEngine.Object.Destroy(__instance.gameObject); }));
			return false;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(AsteroidEventData), nameof(AsteroidEventData.GetMaxAsteroidsTotal))]
		static void AsteroidEventData_GetMaxAsteroidsTotal(ref int __result) {
			if (IsOnPlanet()) {
				__result *= 10;
			}
		}
		[HarmonyPostfix]
		[HarmonyPatch(typeof(AsteroidEventData), nameof(AsteroidEventData.GetMaxAsteroidsSimultaneous))]
		static void AsteroidEventData_GetMaxAsteroidsSimultaneous(ref int __result) {
			if (IsOnPlanet()) {
				__result *= 10;
			}
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(MeteoHandler), "UpdateCurrentMeteoEvent")]
		static bool MeteoHandler_UpdateCurrentMeteoEvent() {
			if (IsOnPlanet()) {
				return false;
			}
			return true;
		}
		// <--- Meteors ---

		[HarmonyPostfix]
		[HarmonyPatch(typeof(GaugesConsumptionHandler), nameof(GaugesConsumptionHandler.GetOxygenConsumptionRate))]
		private static void GaugesConsumptionHandler_GetOxygenConsumptionRate(ref float __result) {
			if (IsOnPlanet()) {
				__result *= 5;
			}
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(PlayerCameraShake), nameof(PlayerCameraShake.SetShaking), new Type[] { typeof(bool), typeof(float), typeof(float) })]
		static void PlayerCameraShake_SetShaking(ref float _shakeValue) {
			if (IsOnPlanet()) {
				_shakeValue *= 0.2f;
			}
		}

		/*[HarmonyPostfix]
		[HarmonyPatch(typeof(SystemViewHandler), "Start")]
		private static void SystemViewHandler_Start(List<SpacePlanetView> ___spacePlanetViews) {
			SpacePlanetView spacePlanet;
			spacePlanet.InitPlanetSpaceView(planetName);
			___spacePlanetViews.Add(spacePlanet);
		}*/
		[HarmonyPrefix]
		[HarmonyPatch(typeof(SystemViewHandler), nameof(SystemViewHandler.ZoomOnPlanet))]
		static bool SystemViewHandler_ZoomOnPlanet(PlanetData planetData, SystemViewHandler __instance, int ___zoomValueOnPlanets, List<SpacePlanetView> ___spacePlanetViews) {
			if (planetData == null) return true;
			if (planetData.GetPlanetId() != planetName) return true;

			//foreach (SpacePlanetView spacePlanetView in ___spacePlanetViews) log.LogInfo(spacePlanetView.transform.position);

			Vector3 vector = new Vector3(-5049.94f, -4000.50f, 15.13f);//spacePlanetView.transform.position; //(-5049.94, -3994.29, 17.13)-3992.50
			vector += new Vector3((float)___zoomValueOnPlanets, 0f, 0f);
			log.LogInfo(vector + " is the space station zoom spot");
			AccessTools.Method(typeof(SystemViewHandler), "ActivateZoomTarget").Invoke(__instance, new object[] { vector });
			return false;
		}
		//[Error  :  HarmonyX] Failed to patch void SpaceCraft.SystemViewHandler::ZoomOnPlanet(SpaceCraft.PlanetData planetData): HarmonyLib.InvalidHarmonyPatchArgumentException: (static bool SpaceStationPlanet.Plugin::SystemViewHandler_Start(SpaceCraft.PlanetData planetData, SpaceCraft.SystemViewHandler __instance, int ___zoomValueOnPlanets, System.Collections.Generic.List<SpaceCraft.SpacePlanetView> ___spacePlanetViews)): Return type of pass through postfix static bool SpaceStationPlanet.Plugin::SystemViewHandler_Start(SpaceCraft.PlanetData planetData, SpaceCraft.SystemViewHandler __instance, int ___zoomValueOnPlanets, System.Collections.Generic.List<SpaceCraft.SpacePlanetView> ___spacePlanetViews) does not match type of its first parameter
		//at HarmonyLib.Public.Patching.HarmonyManipulator.WritePostfixes (HarmonyLib.Internal.Util.ILEmitter+Label returnLabel) [0x0035a] in <474744d65d8e460fa08cd5fd82b5d65f>:0
		//at HarmonyLib.Public.Patching.HarmonyManipulator.WriteImpl () [0x00234] in <474744d65d8e460fa08cd5fd82b5d65f>:0
		//[Error  : Unity Log] InvalidHarmonyPatchArgumentException: (static bool SpaceStationPlanet.Plugin::SystemViewHandler_Start(SpaceCraft.PlanetData planetData, SpaceCraft.SystemViewHandler __instance, int ___zoomValueOnPlanets, System.Collections.Generic.List<SpaceCraft.SpacePlanetView> ___spacePlanetViews)): Return type of pass through postfix static bool SpaceStationPlanet.Plugin::SystemViewHandler_Start(SpaceCraft.PlanetData planetData, SpaceCraft.SystemViewHandler __instance, int ___zoomValueOnPlanets, System.Collections.Generic.List<SpaceCraft.SpacePlanetView> ___spacePlanetViews) does not match type of its first parameter
	}
}

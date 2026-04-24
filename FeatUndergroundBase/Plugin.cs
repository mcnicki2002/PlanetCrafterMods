// Copyright 2025-2026 Nicolas Schäfer & Contributors
// Licensed under Apache License, Version 2.0

using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using SpaceCraft;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;

namespace Nicki0.FeatUndergroundBase {

	[BepInPlugin("Nicki0.theplanetcraftermods.FeatUndergroundBase", "(Feat) Underground Base", PluginInfo.PLUGIN_VERSION)]
	public class Plugin : BaseUnityPlugin {
		public static bool debugPrint = false;

		/*
		 * TODO
		 * 
		 * Rock texture on other domes
		 * 
		 * 
		 * Move bottom death barrier (in a better way)
		 * 
		 * BUG: rock shows in humble south-east cave, probably due to being below 0
		 * 
		 * 
		 */

		private static ManualLogSource log;
		private static Plugin Instance;

		public static readonly string LadderDownId = "Nicki0_LadderDown";
		public static readonly string LadderStartId = "Nicki0_LadderStart";

		public static readonly float SURFACE_HEIGHT = -120; // Humble's lower area is deep in the negative area

		private void Awake() {
			// Plugin startup logic
			log = Logger;
			Instance = this;

			MaterialsHelper.InitMaterialsHelper(Logger);

			if (LibCommon.ModVersionCheck.Check(this, Logger.LogInfo, out bool hashError, out string repoURL)) {
				LibCommon.ModVersionCheck.NotifyUser(this, hashError, repoURL, Logger.LogInfo);
			}

			Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
			Harmony.CreateAndPatchAll(typeof(Plugin));
		}

		private static IEnumerator ExecuteLater(Action toExecute, int waitFrames = 1) {
			for (int i = 0; i < waitFrames; i++) yield return new WaitForEndOfFrame();
			toExecute.Invoke();
		}

		static bool isInitialized = false;
		[HarmonyPrefix]
		[HarmonyPriority(Priority.Low)]
		[HarmonyPatch(typeof(StaticDataHandler), "LoadStaticData")]
		private static void StaticDataHandler_LoadStaticData(List<GroupData> ___groupsData) {

			if (new StackTrace(true).ToString().Contains("CreateNewFile", StringComparison.InvariantCultureIgnoreCase)) return;

			if (!isInitialized) {
				GameObject rock02 = ___groupsData.Find(e => e.id == "Biodome2").associatedGameObject.transform.Find("Biodome2/Rocks/Boulder_02").gameObject;
				GameObject rock10 = ___groupsData.Find(e => e.id == "Biodome2").associatedGameObject.transform.Find("Biodome2/Rocks/Boulder_10").gameObject;
				GameObject rock12 = ___groupsData.Find(e => e.id == "Biodome2").associatedGameObject.transform.Find("Biodome2/Rocks/Obstacle_12").gameObject;
				
				/*
				 * Biodome2/Biodome2/Rocks/Boulder_02
				 * localPos: -0.9418, 1.7855, -3.2127
				 * localRot: 89.3511, 90, 0
				 * scale: 3, 0.1, 3.5
				 */
				GameObject rockForWindow = Instantiate(rock02);
				rockForWindow.transform.SetParent(___groupsData.Find(e => e.id == "window").associatedGameObject.transform);
				rockForWindow.transform.localPosition = new Vector3(-0.9418f, 1.8219f, -3.1f);
				rockForWindow.transform.localRotation = Quaternion.Euler(89.3511f, 90, 359.7998f);
				rockForWindow.transform.localScale = new Vector3(2.75f, 0.1f, 3.6f);
				rockForWindow.AddComponent<DestroyIfGhost>();
				rockForWindow.AddComponent<Nicki0_DestroyIfAboveGround>();
				rockForWindow.AddComponent<Nicki0_HideWhenAgainstLivable>();

				/*
				 * Biodome2/Biodome2/Rocks/Boulder_02
				 * localPos: -0.83, 1.611, -3.0272
				 * localRot: 89.3517, 90, 0
				 * scale: 1.4, 0.1, 3.5
				 */
				GameObject rockForDoor = Instantiate(rock02);
				rockForDoor.transform.SetParent(___groupsData.Find(e => e.id == "door").associatedGameObject.transform);
				rockForDoor.transform.localPosition = new Vector3(-0.83f, 1.611f, -3.0272f);
				rockForDoor.transform.localRotation = Quaternion.Euler(89.3517f, 90, 0);
				rockForDoor.transform.localScale = new Vector3(1.5f, 0.1f, 3.5f);
				rockForDoor.AddComponent<DestroyIfGhost>();
				rockForDoor.AddComponent<Nicki0_DestroyIfAboveGround>();
				rockForDoor.AddComponent<Nicki0_HideWhenAgainstLivable>();

				/*
				 * Biodome2/Biodome2/Rocks/Boulder_10
				 * localPos: 2.8109f, 4.8545f, 3.3346f
				 * localRot: 0.2531f, 180, 179.4737f
				 * scale: 3.9782f, 0.1f, 2.16f
				 */
				GameObject rockForRoofWindow = Instantiate(rock10);
				foreach (var renderer in rockForRoofWindow.GetComponentsInChildren<Renderer>()) {
					renderer.material = Instantiate(renderer.material);
					renderer.material.SetFloat("_Terraformation", 0);
				}
				rockForRoofWindow.transform.SetParent(___groupsData.Find(e => e.id == "FloorGlass").associatedGameObject.transform);
				rockForRoofWindow.transform.localPosition = new Vector3(2.8109f, 4.8545f, 3.3346f);
				rockForRoofWindow.transform.localRotation = Quaternion.Euler(0.2531f, 180, 179.4737f);
				rockForRoofWindow.transform.localScale = new Vector3(3.9782f, 0.1f, 2.16f);
				rockForRoofWindow.AddComponent<DestroyIfGhost>();
				rockForRoofWindow.AddComponent<Nicki0_DestroyIfAboveGround>();
				rockForRoofWindow.AddComponent<Nicki0_HideWhenAgainstLivable>();
				/*
				 * Biodome2/Biodome2/Rocks/Boulder_10
				 * localPos: -1.0109f, 2.4455f, -3.1836f
				 * localRot: 0, 0, 270
				 * scale: 1.0873f, 0.1f, 2.04f
				 */
				GameObject rockForWallLab = Instantiate(rock10);
				rockForWallLab.transform.SetParent(Managers.GetManager<PanelsResources>().GetPanelGameObject(DataConfig.BuildPanelSubType.WallLab).transform);
				rockForWallLab.transform.localPosition = new Vector3(-1.0109f, 2.4455f, -3.1836f);
				rockForWallLab.transform.localRotation = Quaternion.Euler(0, 0, 270);
				rockForWallLab.transform.localScale = new Vector3(1.0873f, 0.1f, 2.04f);
				rockForWallLab.AddComponent<DestroyIfGhost>();
				rockForWallLab.AddComponent<Nicki0_DestroyIfAboveGround>();
				rockForWallLab.AddComponent<Nicki0_HideWhenAgainstLivable>();

				/*
				 * Biodome2/Biodome2/Rocks/Boulder_10
				 * localPos: 0.16f, -9.5473f, -0.3382f
				 * localRot: 0, 0, 0
				 * scale: 7.6836f, 0.1f, 3.4945f
				 */
				GameObject rockForMegadome = Instantiate(rock10);
				rockForMegadome.transform.SetParent(___groupsData.Find(e => e.id == "Megadome1").associatedGameObject.transform);
				rockForMegadome.transform.localPosition = new Vector3(0.16f, -9.5473f, -0.3382f);
				rockForMegadome.transform.localRotation = Quaternion.Euler(0, 0, 0);
				rockForMegadome.transform.localScale = new Vector3(7.6836f, 0.1f, 3.4945f);
				rockForMegadome.AddComponent<DestroyIfGhost>();
				rockForMegadome.AddComponent<Nicki0_DestroyIfAboveGround>();
				//rockForMegadome.AddComponent<Nicki0_HideWhenAgainstLivable>();
				/*
				 * From Left to Right:
				 * 1.9055, 2.7346, -3.7091
				 * 90, 8.4363, 0
				 * 1.2909, 0.1, 1.0364
				 * ---
				 * 1.1127, 2.4909, -3.5018
				 * 0.4949, 288.4479, 269.4908
				 * 1.7527, 0.01, 1.1309
				 * ---
				 * -0.9564, 2.5273, -2.5636
				 * 270.3466, 34.4343, 180.0003
				 * 1.5564, 0.1, 1.0655
				 * 
				 * 
				 * 
				 * -2.7091, 2.7528, -0.7673
				 * 90, 56.8692, 0
				 * 1.5928, 0.1, 1.0655
				 * ---
				 * -3.7018, 2.8109, 1.38
				 * 2.4919, 158.8116, 90.5803
				 * 1.7527, 0.01, 1.1309
				 * ---
				 * -3.9054, 2.6255, 2.0236
				 * 270.7078, 286.31, 337.4538
				 * 1.3854, 0.1, 1.0364
				 */
				(Vector3 localPos, Quaternion localRotation, Vector3 localScale)[] rockSettingsForPodAngle = [
					(new Vector3(1.9055f,	2.7346f, -3.7091f),	Quaternion.Euler(90,		8.4363f,	0),			new Vector3(1.2909f, 0.1f,	1.0364f)),
					(new Vector3(1.1127f,	2.4909f, -3.5018f),	Quaternion.Euler(0.4949f,	288.4479f,	269.4908f), new Vector3(1.7527f, 0.01f, 1.1309f)),
					(new Vector3(-0.9564f,	2.5273f, -2.5636f),	Quaternion.Euler(270.3466f, 34.4343f,	180.0003f), new Vector3(1.5564f, 0.1f,	1.0655f)),
					(new Vector3(-2.7091f,	2.7528f, -0.7673f),	Quaternion.Euler(90,		56.8692f,	0),			new Vector3(1.5928f, 0.1f,	1.0655f)),
					(new Vector3(-3.7018f,	2.8109f, 1.38f),	Quaternion.Euler(2.4919f,	158.8116f,	90.5803f),	new Vector3(1.7527f, 0.01f, 1.1309f)),
					(new Vector3(-3.9054f,	2.6255f, 2.0236f),	Quaternion.Euler(270.7078f, 286.31f,	337.4538f), new Vector3(1.3854f, 0.1f,	1.0364f))
					];
				GameObject podAngleGameObject = ___groupsData.Find(e => e.id == "podAngle").associatedGameObject;
				int livCompCornerCtr = 0;
				foreach (var setting in rockSettingsForPodAngle) {
					GameObject rockForPodAngle = Instantiate(rock10);
					rockForPodAngle.name = "rock10_" + (livCompCornerCtr++);
					rockForPodAngle.transform.SetParent(podAngleGameObject.transform);
					rockForPodAngle.transform.localPosition = setting.localPos;
					rockForPodAngle.transform.localRotation = setting.localRotation;
					rockForPodAngle.transform.localScale = setting.localScale;
					rockForPodAngle.AddComponent<DestroyIfGhost>();
					rockForPodAngle.AddComponent<Nicki0_DestroyIfAboveGround>();
					rockForPodAngle.AddComponent<Nicki0_HideWhenAgainstLivable>();
				}

				/*
				 * -0.8836f, 2.0291f, -3.1327f
				 * 0, 0, 270
				 * 2.5963f, 0.1f, 2.1563f
				 */
				GameObject rockForWallWaterLife = Instantiate(rock10);
				rockForWallWaterLife.transform.SetParent(Managers.GetManager<PanelsResources>().GetPanelGameObject(DataConfig.BuildPanelSubType.WallWaterLife).transform);
				rockForWallWaterLife.transform.localPosition = new Vector3(-0.8836f, 2.0291f, -3.1327f);
				rockForWallWaterLife.transform.localRotation = Quaternion.Euler(0, 0, 270);
				rockForWallWaterLife.transform.localScale = new Vector3(2.5963f, 0.1f, 2.1563f);
				rockForWallWaterLife.AddComponent<DestroyIfGhost>();
				rockForWallWaterLife.AddComponent<Nicki0_DestroyIfAboveGround>();
				rockForWallWaterLife.AddComponent<Nicki0_HideWhenAgainstLivable>();

				/* Biodome2(Clone)/Biodome2/Rocks/Obstacle_12
				 * 3.4709f, -0.98f, 3.5055f
				 * 0, 200.4151f, 0
				 * 2.6f, 0.1f, 2.7764f
				 * 
				 * 3.4709f, -0.0382f, 3.531f
				 * 0, 249.3241f, 180
				 * 2.7f, 0.1f, 2.7764f
				 */
				(Vector3 localPos, Quaternion localRotation, Vector3 localScale)[] rockSettingsForFloorAngleGlass = [
					(new Vector3(3.4709f, -0.98f, 3.5055f), Quaternion.Euler(0, 200.4151f, 0), new Vector3(2.6f, 0.1f, 2.7764f)),
					(new Vector3(3.4709f, -0.0382f, 3.531f), Quaternion.Euler(0, 249.3241f, 180), new Vector3(2.7f, 0.1f, 2.7764f))
					];
				GameObject floorAngleGlassGameObject = Managers.GetManager<PanelsResources>().GetPanelGameObject(DataConfig.BuildPanelSubType.FloorAngleGlass);
				foreach (var setting in rockSettingsForFloorAngleGlass) {
					GameObject rockForFloorAngleGlass = Instantiate(rock12);
					foreach (var renderer in rockForFloorAngleGlass.GetComponentsInChildren<Renderer>()) {
						renderer.material = Instantiate(renderer.material);
						renderer.material.SetFloat("_Terraformation", 0);
					}
					rockForFloorAngleGlass.transform.SetParent(floorAngleGlassGameObject.transform);
					rockForFloorAngleGlass.transform.localPosition = setting.localPos;
					rockForFloorAngleGlass.transform.localRotation = setting.localRotation;
					rockForFloorAngleGlass.transform.localScale = setting.localScale;
					rockForFloorAngleGlass.AddComponent<DestroyIfGhost>();
					rockForFloorAngleGlass.AddComponent<Nicki0_DestroyIfAboveGround>();
					rockForFloorAngleGlass.AddComponent<Nicki0_HideWhenAgainstLivable>();
				}


				//___groupsData.Find(e => e.id == "Biodome2").associatedGameObject.AddComponent<ConstraintAboveGround>();
				/*List<string> groupsToDisableBelowTheSurface = [
					"Pod9xB",
					"biodome",
					"Megadome1",
					"podAngle",
					//"Ladder", // <- prefix on if the ladder leads to an envirnoment with oxygen
					"Biodome2",
					"ButterflyDome1",
					"Aquarium2"
				];*/
				List<string> groupsToDisableBelowTheSurface = [
					"Biodome2",
					"ButterflyDome1",
					"Aquarium2"
				];
				foreach (string id in groupsToDisableBelowTheSurface) {
					___groupsData.Find(e => e.id == id).associatedGameObject.AddComponent<Nicki0_ConstraintAboveGround>();
				}

				List<string> groupsWithRockAsWindows = [
					"Pod9xB",
					"biodome", // <- glass on inside is also replaced...
					"Megadome1", // <- -"-
					//"podAngle"
				];
				//foreach (string id in groupsWithRockAsWindows) {
				//	___groupsData.Find(e => e.id == id).associatedGameObject.AddComponent<ChangeGlassToRockIfBelowGround>();
				//}
				___groupsData.Find(e => e.id == "biodome").associatedGameObject.transform.Find("Container").gameObject.AddComponent<Nicki0_ChangeGlassToRockIfBelowGround>().materialRange = (0, 58);
				___groupsData.Find(e => e.id == "Megadome1").associatedGameObject.transform.Find("City_Dome_Top").gameObject.AddComponent<Nicki0_ChangeGlassToRockIfBelowGround>().materialRange = (0, 58);
				___groupsData.Find(e => e.id == "Pod9xB").associatedGameObject.AddComponent<Nicki0_ChangeGlassToRockIfBelowGround>().materialRange = (0, 58);
				___groupsData.Find(e => e.id == "podAngle").associatedGameObject.AddComponent<Nicki0_ChangeGlassToRockIfBelowGround>().materialRange = (0, 58);

				isInitialized = true;
			}

			___groupsData.RemoveAll(e => e.id == LadderDownId || e.id == LadderStartId);

			GroupDataConstructible ladderGDC = ___groupsData.Find(e => e.id == "Ladder") as GroupDataConstructible;

			ConstructibleGhost cg = ladderGDC.associatedGameObject.AddComponent<ConstructibleGhost>();


			// ---------- downwardsLadder ---------->
			GroupDataConstructible downwardsLadder = Instantiate(ladderGDC);
			downwardsLadder.id = LadderDownId;
			downwardsLadder.associatedGameObject = Instantiate(downwardsLadder.associatedGameObject);
			downwardsLadder.associatedGameObject.transform.position = GameConfig.spaceLocation;
			downwardsLadder.recipeIngredients.Reverse();
			Texture2D textureLadderDown = new Texture2D(2, 2);
			ImageConversion.LoadImage(textureLadderDown, Properties.Resources.LadderDown);
			downwardsLadder.icon = Sprite.Create(textureLadderDown, new Rect(0.0f, 0.0f, textureLadderDown.width, textureLadderDown.height), new Vector2(0.5f, 0.5f), 100.0f);
			Destroy(downwardsLadder.associatedGameObject.GetComponent<ConstructibleGhost>());
			downwardsLadder.associatedGameObject.GetComponent<CapsuleCollider>().center += new Vector3(0, -6, 0);
			downwardsLadder.associatedGameObject.name = downwardsLadder.id;
			foreach (Transform t in downwardsLadder.associatedGameObject.transform) {
				t.localPosition += new Vector3(0, -6, 0);
			}
			___groupsData.Add(downwardsLadder);
			// <---------- downwardsLadder ----------

			// ---------- startLadder ---------->
			GroupDataConstructible startLadder = Instantiate(ladderGDC);
			startLadder.id = LadderStartId;
			startLadder.recipeIngredients.AddRange(___groupsData.Find(e => e.id == "pod").recipeIngredients);
			startLadder.associatedGameObject = Instantiate(startLadder.associatedGameObject);
			startLadder.associatedGameObject.transform.position = GameConfig.spaceLocation;
			Destroy(startLadder.associatedGameObject.GetComponent<ConstructibleGhost>());
			startLadder.associatedGameObject.name = startLadder.id;
			Texture2D textureStartLadder = new Texture2D(2, 2);
			ImageConversion.LoadImage(textureStartLadder, Properties.Resources.LadderStart);
			startLadder.icon = Sprite.Create(textureStartLadder, new Rect(0.0f, 0.0f, textureStartLadder.width, textureStartLadder.height), new Vector2(0.5f, 0.5f), 100.0f);

			startLadder.associatedGameObject.GetComponent<CapsuleCollider>().center += new Vector3(0, -5.5f, 0);
			startLadder.associatedGameObject.transform.Find("Container").localPosition += new Vector3(0, -5.5f, 0);
			startLadder.associatedGameObject.transform.Find("AudioActionDoor").localPosition += new Vector3(0, -5.5f, 0);
			startLadder.associatedGameObject.transform.Find("AudioActionLadderUp").localPosition += new Vector3(0, -5.5f, 0);
			startLadder.associatedGameObject.transform.Find("AudioActionLadderDown").localPosition += new Vector3(0, -5.5f, 0);
			Transform startLadder_TriggerDeconstruction = startLadder.associatedGameObject.transform.Find("TriggerDeconstruction");
			startLadder_TriggerDeconstruction.localPosition += new Vector3(0, -3, 0);
			BoxCollider triggerDeconstructionBoxCollider = startLadder_TriggerDeconstruction.GetComponent<BoxCollider>();
			triggerDeconstructionBoxCollider.size = new Vector3(triggerDeconstructionBoxCollider.size.x, 2.5f, triggerDeconstructionBoxCollider.size.z);
			startLadder.associatedGameObject.transform.Find("TriggerNotColliding").localPosition += Vector3.zero;
			startLadder.associatedGameObject.transform.Find("ColliderTop").localPosition += new Vector3(0, -5.5f, 0);
			startLadder.associatedGameObject.transform.Find("ArrowDirectionGhost").localPosition += Vector3.zero;

			startLadder.associatedGameObject.AddComponent<Nicki0_DeleteCollider>();
			GameObject startLadder_BottomMove = startLadder.associatedGameObject.transform.Find("Container/BottomMove").gameObject;
			startLadder_BottomMove.AddComponent<Nicki0_SetFixedHeight>();
			List<DataConfig.HomemadeTag> surfaceConstraints = startLadder.associatedGameObject.GetComponent<ConstraintOnSurfaces>().allowedTaggedSurfaces;
			for (int i = 0; i < surfaceConstraints.Count; i++) {
				if (surfaceConstraints[i] == DataConfig.HomemadeTag.SurfaceGrid) {
					surfaceConstraints[i] = DataConfig.HomemadeTag.SurfaceTerrain;
				}
			}
			startLadder_BottomMove.AddComponent<Nicki0_SpawnStartingRoom>();
			startLadder_BottomMove.AddComponent<Nicki0_MoveStartingRoomInGhost>();
			___groupsData.Add(startLadder);
			// <---------- startLadder ----------

			DestroyImmediate(cg);



			foreach (HomemadeTag tag in ___groupsData.Select(e => e.associatedGameObject).Where(e => e != null).SelectMany(e => e.GetComponentsInChildren<HomemadeTag>()).Where(e => e.GetHomemadeTag() == DataConfig.HomemadeTag.IsInsideLivable)) {
				if (!tag.TryGetComponent<Nicki0_ReMoveTerrain>(out _)) {
					tag.gameObject.AddComponent<Nicki0_ReMoveTerrain>();

					foreach (ConstraintNotColliding constraint in tag.transform.root.GetComponentsInChildren<ConstraintNotColliding>()) {
						constraint.collideWithTerrain = false;
					}
				}
			}
		}


		private static readonly string localizationLadderInsideRock = "Nicki0_FeatundergroundBase_LadderInsideRock";
		public static readonly string localizationCantBeBuildBelowSurface = "Nicki0_FeatundergroundBase_CompartmentInsideRock";

		[HarmonyPrefix]
		[HarmonyPatch(typeof(ActionMovePlayer), nameof(ActionMovePlayer.OnAction))]
		public static bool Prefix_ActionMovePlayer_OnAction(ActionMovePlayer __instance) {
			if (__instance.gameObject == null || !__instance.gameObject.transform.root.name.Contains("Ladder") || __instance.destination == null || __instance.destination.transform.position.y >= SURFACE_HEIGHT) return true;
			log.LogInfo($"checked height is {__instance.destination.transform.position}");
			foreach (Collider collider in Physics.OverlapSphere(__instance.destination.transform.position, 1)) { // radius 1 such that, when the destination is slightly outside the livable area, it still teleports
				foreach (HomemadeTag homemadeTag in collider.transform.GetComponentsInChildren<HomemadeTag>().Union(collider.transform.GetComponentsInParent<HomemadeTag>())) { // collider.transform.root.GetComponentsInChildren<HomemadeTag>()) {
					if (homemadeTag.GetHomemadeTag() == DataConfig.HomemadeTag.IsInsideLivable) {
						return true;
					}
				}
			}
			Managers.GetManager<BaseHudHandler>().DisplayCursorText(localizationLadderInsideRock, 4f, "", "");
			return false;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(Localization), "LoadLocalization")]
		static void Localization_LoadLocalization(Dictionary<string, Dictionary<string, string>> ___localizationDictionary) {
			if (___localizationDictionary.TryGetValue(Localization.GetDefaultLangageCode(), out Dictionary<string, string> dict)) {
				dict[localizationLadderInsideRock] = "Ladder leads to rock."; // "Ladder not usable"
				dict[localizationCantBeBuildBelowSurface] = "Not constructible below the terrain";

				dict[GameConfig.localizationGroupNameId + LadderDownId] = "Ladder down";
				dict[GameConfig.localizationGroupDescriptionId + LadderDownId] = "Ladder that leads one compartment below.";

				dict[GameConfig.localizationGroupNameId + LadderStartId] = "Underground base ladder";
				dict[GameConfig.localizationGroupDescriptionId + LadderStartId] = "Ladder to start an underground base.";
			}
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(ConstraintLookAtPlayer), "Start")]
		static bool ConstraintLookAtPlayer_Start() {
			return Managers.GetManager<PlayersManager>() != null && Managers.GetManager<PlayersManager>().GetActivePlayerController() != null;
		}

		[HarmonyPrefix]
		[HarmonyPriority(Priority.High)]
		[HarmonyPatch(typeof(WorldObjectsHandler), nameof(WorldObjectsHandler.CreateAndInstantiateWorldObject))]
		static void WorldObjectsHandler_CreateAndInstantiateWorldObject(ref Group group, ref Vector3 position) {
			if (group != null && group.id == LadderDownId) {
				group = GroupsHandler.GetGroupViaId("Ladder");
				position += new Vector3(0, -6, 0);
			}
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(ActionDeconstructible), nameof(ActionDeconstructible.OnAction))]
		static bool ActionDeconstructible_OnAction(ActionDeconstructible __instance) {
			if (Managers.GetManager<PlayersManager>().GetActivePlayerController().GetMultitool().GetState() != DataConfig.MultiToolState.Deconstruct) {
				return true;
			}
			if (!__instance.transform.root.gameObject.name.Contains(LadderStartId)) { return true; }

			WorldObjectAssociated woa = __instance.gameObject.transform.root.GetComponentInChildren<WorldObjectAssociated>();
			if (woa == null) { return true; }

			int pod_WOid = woa.GetWorldObject().GetLinkedWorldObject();
			if (pod_WOid != 0) {
				WorldObject pod_WO = WorldObjectsHandler.Instance.GetWorldObjectViaId(pod_WOid);
				if (pod_WO != null) {
					DeconstructStatus status = pod_WO.GetGameObject().GetComponentInChildren<ActionDeconstructible>().CheckDeconstructOnServer();
					if (status == DeconstructStatus.Ok) {
						pod_WO.GetGameObject().GetComponentInChildren<ActionDeconstructible>().OnAction();
						__instance.StartCoroutine(ExecuteLater(delegate () {
							if (deconstructionSoundTimer != null && deconstructionSoundTimer.IsRunning) {
								deconstructionSoundTimer.Stop();
							}
							deconstructionSoundTimer = Stopwatch.StartNew();
							__instance.OnAction();
						}));
					} else {
						pod_WO.GetGameObject().GetComponentInChildren<ActionDeconstructible>().DisplayStatus(status);
						return false;
					}
				} else {
					woa.GetWorldObject().SetLinkedWorldObject(0);
					return true;
				}
				/*
				 * TODO
				 * disable collider for deconstruct on LinkedWorldObject pods
				 */
			} else {
				return true;
			}


			return false;
		}

		private static Stopwatch deconstructionSoundTimer = null;
		[HarmonyPrefix]
		[HarmonyPatch(typeof(ActionDeconstructible), nameof(ActionDeconstructible.StartDeconstructAnim))]
		static bool ActionDeconstructible_StartDeconstructAnim() {
			if (deconstructionSoundTimer != null) {
				if (deconstructionSoundTimer.ElapsedMilliseconds < 500) {
					return false;
				} else {
					deconstructionSoundTimer.Stop();
					deconstructionSoundTimer = null;
				}
			}
			return true;
		}

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(PlayerDirectEnvironment), "OnTriggerEnter")]
		static IEnumerable<CodeInstruction> Transpiler_PlayerDirectEnvironment_OnTriggerEnter(IEnumerable<CodeInstruction> instructions) {
			List<CodeInstruction> codeInstructions = instructions.ToList();
			for (int i = 0; i < codeInstructions.Count; i++) {
				CodeInstruction instruction = codeInstructions[i];
				if (instruction.opcode == OpCodes.Ldc_I4_S && instruction.operand is SByte) {
					if (((SByte)instruction.operand) == (SByte)9) {
						codeInstructions[i].operand = (SByte)(-1);
					}
				}
			}
			return codeInstructions.AsEnumerable<CodeInstruction>();
		}

		// Otherwise, rocks show indoors
		[HarmonyPostfix]
		[HarmonyPatch(typeof(PerformancerDisableIfFarAway), "StartImpl")]
		static void PerformancerDisableIfFarAway_StartImpl(PerformancerDisableIfFarAway __instance, ref float ___disableIfPlayerIsFarerThan) {
			if (__instance.gameObject != null && __instance.gameObject.transform.root.GetComponentsInChildren<HomemadeTag>().Where(e => e.GetHomemadeTag() == DataConfig.HomemadeTag.IsInsideLivable).Any()) {
				___disableIfPlayerIsFarerThan = 200;
			}
		}

		// Fix NRE
		[HarmonyPrefix]
		[HarmonyPatch(typeof(ConstraintNotColliding), "Update")]
		static bool ConstraintNotColliding_Update(ConstraintNotColliding __instance) => __instance.GetComponent<Collider>() != null;
	}
	public class Nicki0_DestroyIfAboveGround : MonoBehaviour {
		public void Start() {
			//if (this.transform.position.y >= SURFACE_HEIGHT) Destroy(this.gameObject);

			Vector3 position = this.transform.position + (-2) * this.transform.up;
			if (Vector3.Angle(this.transform.up, Vector3.down) < 10) {
				position = this.transform.position + (2) * this.transform.up; // When the stone is on the top, then the raycast hits the glass itself, so the position is moved here.
			}

			bool terrainAbove = Physics.Raycast(position + new Vector3(0, 500, 0), Vector3.down, 500, LayerMask.GetMask(new string[] { GameConfig.layerTerrainName }));
			/*
			 * ignore:
			 * - water
			 * - ore vein
			 * - sectors
			 * 
			 */
			bool anythingBelow = false;
			RaycastHit[] hitsBelow = Physics.RaycastAll(position, Vector3.down, 500, ~LayerMask.GetMask(GameConfig.commonIgnoredAndWater.Union(new string[] { "Occlusion", GameConfig.layerToxicName, GameConfig.layerPlayerAndRoverExcludeName }).ToArray()));
			foreach (RaycastHit hit in hitsBelow) {
				if (hit.collider.GetComponent<MachineGenerationGroupVein>() == null &&
					hit.collider.transform.root.GetComponentInChildren<WorldObjectAssociated>() == null
					) {
					anythingBelow = true;
					if (Plugin.debugPrint) Console.WriteLine("Collider hit by underground glass cover rock raycase: " + string.Join("/", hit.collider.gameObject.GetComponentsInParent<Transform>().Select(t => t.name).Reverse().ToArray()));
				}
			}

			if (!(terrainAbove && !anythingBelow) && this.transform.position.y >= Plugin.SURFACE_HEIGHT) {
				Destroy(this.gameObject);
			}
		}
	}

	public class Nicki0_HideWhenAgainstLivable : MonoBehaviour {
		public void Start() {
			this.StartCoroutine(CheckIfInLivableRoutine());
		}
		private IEnumerator CheckIfInLivableRoutine() {


			bool isAgainstRock = !CheckIfInLivable();
			if (!this.transform.GetChild(0).gameObject.activeSelf && isAgainstRock) {
				foreach (Transform t in this.transform) {
					t.gameObject.SetActive(true);
				}
				this.GetComponent<Collider>().enabled = true;
			} else if (this.transform.GetChild(0).gameObject.activeSelf && !isAgainstRock) {
				foreach (Transform t in this.transform) {
					t.gameObject.SetActive(false);
				}
				this.GetComponent<Collider>().enabled = false;
			}

			yield return new WaitForSeconds((float)new System.Random().NextDouble() * 1);
			WaitForSeconds wait = new WaitForSeconds(1);
			WaitForSeconds waitLong = new WaitForSeconds(10);
			while (true) {
				if (this.gameObject == null) yield break;

				isAgainstRock = !CheckIfInLivable();
				if (!this.transform.GetChild(0).gameObject.activeSelf && isAgainstRock) {
					foreach (Transform t in this.transform) {
						t.gameObject.SetActive(true);
					}
					this.GetComponent<Collider>().enabled = true;
				} else if (this.transform.GetChild(0).gameObject.activeSelf && !isAgainstRock) {
					CheckIfInLivable();
					foreach (Transform t in this.transform) {
						t.gameObject.SetActive(false);
					}
					this.GetComponent<Collider>().enabled = false;
				}
				if ((Managers.GetManager<PlayersManager>().GetActivePlayerController().transform.position - this.transform.position).magnitude > 50) {
					yield return waitLong;
				} else {
					yield return wait;
				}
			}
		}
		private bool CheckIfInLivable() {
			foreach (Collider collider in Physics.OverlapSphere(this.transform.position + (-2) * this.transform.up, 1)) {
				foreach (HomemadeTag homemadeTag in collider.transform.GetComponentsInChildren<HomemadeTag>().Union(collider.transform.GetComponentsInParent<HomemadeTag>())) {
					if (homemadeTag.GetHomemadeTag() == DataConfig.HomemadeTag.IsInsideLivable) {
						return true;
					}
				}
			}
			return false;
		}
	}

	public class Nicki0_ConstraintAboveGround : BuildConstraint {
		public void Update() {
			base.isConstraintRespected = this.transform.position.y >= 0;

			if (!base.isConstraintRespected) {
				Managers.GetManager<BaseHudHandler>().DisplayCursorText(Plugin.localizationCantBeBuildBelowSurface, 0.5f, "", "");
			}
		}
	}

	public class Nicki0_ChangeGlassToRockIfBelowGround : MonoBehaviour {
		public static string rockMaterialOverwrite = "";
		public string rockMaterial = "Material_AW_01";
		public (int, int) materialRange = (0, -1);

		public void Start() {
			if (this.transform.position.y >= Plugin.SURFACE_HEIGHT) { return; }
			this.StartCoroutine(ChangeMagerialsLater());
		}
		public IEnumerator ChangeMagerialsLater() {
			while (this.transform.root.GetComponentInChildren<GhostFx>() != null) {
				yield return null;// new WaitForEndOfFrame();//null;
			}

			Dictionary<string, Material> materialDict = MaterialsHelper.GetMaterials(true);

			Material replacementMaterial = null;
			if (!string.IsNullOrEmpty(rockMaterialOverwrite)) {
				replacementMaterial = materialDict[rockMaterialOverwrite];
			} else if (materialRange.Item2 >= 0) {
				VehicleCheckSurroundingRocks vcsr = GroupsHandler.GetGroupViaId("VehicleTruck").GetAssociatedGameObject().GetComponentInChildren<VehicleCheckSurroundingRocks>(true);
				MaterialList lst = AccessTools.FieldRefAccess<VehicleCheckSurroundingRocks, MaterialList>(vcsr, "_materialsToFilter");
				//int randomizedMaterial = new System.Random().Next(Math.Max(0, materialRange.Item1), Math.Min(lst.materials.Length, materialRange.Item2));
				int minIndex = Math.Max(0, materialRange.Item1);
				int maxIndex = Math.Min(lst.materials.Length, materialRange.Item2);
				int randomizedMaterial = (((int)this.transform.position.sqrMagnitude) % (maxIndex - minIndex)) + minIndex;
				replacementMaterial = lst.materials[randomizedMaterial];
			} else {
				replacementMaterial = materialDict[rockMaterial];
			}

			foreach (Renderer renderer in this.GetComponentsInChildren<Renderer>(true)) {
				if (renderer == null) { continue; }
				Material[] newMaterials = new Material[renderer.materials.Length];
				for (int i = 0; i < renderer.materials.Length; i++) {
					//MaterialsHelper.materialsHelperObject // Material_AX_01 / Material_AW_01 / Material_AZ_01

					if (renderer.materials[i].name.Contains("Glass", StringComparison.InvariantCultureIgnoreCase)) {
						newMaterials[i] = replacementMaterial;
					} else {
						newMaterials[i] = renderer.materials[i];
					}
				}
				renderer.SetMaterialArray(newMaterials);
			}
		}
	}

	public class Nicki0_SetFixedHeight() : MonoBehaviour {
		public static readonly float height = /*entry height:*/-250 + (-1) + 3.3905f; // interior of living compartment is 1 coord higher than it's position; +3.3905 is the offset of the ladder
		public void Start() {
			float oldHeight = this.transform.position.y;

			Vector3 newPosition = this.transform.position;
			newPosition.y = height;
			this.transform.position = newPosition;


			this.GetComponent<ActionMovePlayer>().destination.transform.position += new Vector3(0, oldHeight - height, 0);
			this.transform.parent.Find("TopMove").GetComponent<ActionMovePlayer>().destination.transform.position += new Vector3(0, -(oldHeight - height), 0);

		}
	}

	public class Nicki0_SpawnStartingRoom : MonoBehaviour {
		public /*IEnumerator*/void Start() {
			if (this.GetComponentInParent<ConstructibleGhost>() != null) return; //yield break;

			WorldObjectAssociated woa = this.transform.root.GetComponentInChildren<WorldObjectAssociated>();

			//while (WorldObjectsHandler.Instance == null) yield return null;
			if (woa.GetWorldObject().GetLinkedWorldObject() > 0 && WorldObjectsHandler.Instance.GetWorldObjectViaId(woa.GetWorldObject().GetLinkedWorldObject()) == null) {
				woa.GetWorldObject().SetLinkedWorldObject(0);
			}
			if (woa.GetWorldObject().GetLinkedWorldObject() == 0) {
				Vector3 initialPosition = this.transform.position;

				WorldObjectsHandler.Instance.CreateAndInstantiateWorldObject(
					GroupsHandler.GetGroupViaId("pod"),
					new Vector3((float)Math.Round(initialPosition.x / 2) * 2, initialPosition.y - 3.3905f, (float)Math.Round(initialPosition.z / 2) * 2),
					Quaternion.identity, //base.gameObject.transform.rotation,
					true, false, true, false, delegate (GameObject podGO) {
						if (podGO == null) return;
						podGO.GetComponentInChildren<WorldObjectAssociatedProxy>().GetWorldObjectDetails(delegate (WorldObject podWO) {
							woa.GetWorldObject().SetLinkedWorldObject(podWO.GetId());

							podGO.GetComponentInChildren<ActionDeconstructible>().gameObject.GetComponent<BoxCollider>().enabled = false;
						});
					});
			} else {
				GameObject podGO = WorldObjectsHandler.Instance.GetWorldObjectViaId(woa.GetWorldObject().GetLinkedWorldObject()).GetGameObject();
				podGO.GetComponentInChildren<ActionDeconstructible>().gameObject.GetComponent<BoxCollider>().enabled = false;
			}
			return; // yield break;
		}
	}
	public class Nicki0_MoveStartingRoomInGhost : MonoBehaviour {
		private GameObject podGO;
		private float height = Nicki0_SetFixedHeight.height;
		public void Start() {
			if (this.GetComponentInParent<ConstructibleGhost>() == null) {
				Destroy(this);
				return;
			}

			podGO = Instantiate(GroupsHandler.GetGroupViaId("pod").GetAssociatedGameObject(), this.transform);
			podGO.AddComponent<DestroyIfNotInGhost>();
			podGO.transform.localScale *= 0.6f; //0.9f;
												// Ghost tries to be placed on colliders, pushes player out of pods etc.
			Destroy(podGO.transform.Find("TriggerDeconstruction").gameObject.GetComponent<Collider>());
			podGO.transform.Find("Container").gameObject.SetActive(false);
			Destroy(podGO.transform.Find("Volumes/TriggerOxygen").gameObject); // Could make ladder climbable that would lead to rocks
		}
		public void Update() {
			if (podGO == null) { return; }

			float oldHeight = this.transform.position.y;

			Vector3 newPosition = this.transform.position;
			newPosition.y = height;
			this.transform.position = newPosition;
			this.GetComponent<ActionMovePlayer>().destination.transform.position += new Vector3(0, oldHeight - height, 0);

			Vector3 initialPosition = this.transform.position;
			podGO.transform.position = new Vector3((float)Math.Round(initialPosition.x / 2) * 2, initialPosition.y - 3.3905f, (float)Math.Round(initialPosition.z / 2) * 2);
		}
	}

	public class Nicki0_DeleteCollider : MonoBehaviour {
		public void Start() {
			this.StartCoroutine(RemoveCollider());
		}
		private IEnumerator RemoveCollider() {
			yield return new WaitForSeconds(1);
			foreach (Collider collider in this.gameObject.GetComponents<Collider>()) {
				Destroy(collider);
			}
		}
	}



	class Nicki0_ReMoveTerrain : MonoBehaviour {
		Dictionary<Terrain, ((int, int, int, int), List<Vector2>)> holePositions = new Dictionary<Terrain, ((int, int, int, int), List<Vector2>)>();
		Dictionary<Terrain, ((int, int, int, int), List<Vector3>)> heightPositions = new Dictionary<Terrain, ((int, int, int, int), List<Vector3>)>();

		public void Start() {
			//Vector3 pos = this.transform.position + new Vector3(0, 20, 0);

			Collider collider = this.GetComponent<Collider>();

			Vector3[] corners = [
				this.transform.position,
				collider.bounds.center + new Vector3(collider.bounds.extents.x, 0, collider.bounds.extents.z),
				collider.bounds.center + new Vector3(collider.bounds.extents.x, 0, -collider.bounds.extents.z),
				collider.bounds.center + new Vector3(-collider.bounds.extents.x, 0, collider.bounds.extents.z),
				collider.bounds.center + new Vector3(-collider.bounds.extents.x, 0, -collider.bounds.extents.z),
				];

			foreach (Vector3 pos in corners) {
				foreach (RaycastHit hit in Physics.RaycastAll(pos + 2 * collider.bounds.extents.y * Vector3.up, Vector3.down, 4 * collider.bounds.extents.y, LayerMask.GetMask(new string[] { GameConfig.layerTerrainName }))) {
					// ------------------------------------------------------------------------------------------------------------------------>
					// points.Select(terrain.transform.InverseTransformPoint).ToList();
					if (hit.collider == null) continue;
					if (!hit.collider.enabled || hit.collider.gameObject == null) continue;

					if (!hit.collider.gameObject.TryGetComponent<Terrain>(out Terrain terrain)) continue;

					if (holePositions.ContainsKey(terrain) || heightPositions.ContainsKey(terrain)) { continue; }

					TerrainData td = terrain.terrainData;

					List<Vector2> holeChanges = new List<Vector2>();
					List<Vector3> heightChanges = new List<Vector3>();
					// Problem: Can't revert changes when pods next to each other are deconstructed in the wrong order => no area changes
					//Dictionary<Vector2, float> heightChanges_ForArea = new Dictionary<Vector2, float>();


					int minX_Height = Math.Max(0, (int)Math.Floor((collider.bounds.min.x - terrain.transform.position.x) / td.size.x * (td.heightmapResolution - 1)) - 1);
					int minZ_Height = Math.Max(0, (int)Math.Floor((collider.bounds.min.z - terrain.transform.position.z) / td.size.z * (td.heightmapResolution - 1)) - 1);
					int maxX_Height = Math.Min(td.heightmapResolution - 1, (int)Math.Ceiling((collider.bounds.max.x - terrain.transform.position.x) / td.size.x * (td.heightmapResolution - 1)));
					int maxZ_Height = Math.Min(td.heightmapResolution - 1, (int)Math.Ceiling((collider.bounds.max.z - terrain.transform.position.z) / td.size.z * (td.heightmapResolution - 1)));
					int arrayWidth_Height = maxX_Height - minX_Height + 1;
					int arrayHeight_Height = maxZ_Height - minZ_Height + 1;
					float[,] heightMap = td.GetHeights(minX_Height, minZ_Height, arrayWidth_Height, arrayHeight_Height);
					(int, int)[] heightAreaOffsets = [(-1, -1), (-1, 0), (-1, 1), (0, -1), (0, 1), (1, -1), (1, 0), (1, 1)];

					for (int j = 0; j < heightMap.GetLength(0); j++) {
						for (int i = 0; i < heightMap.GetLength(1); i++) {
							Vector3 pointInWorldSpace_height = new Vector3(
								((float)(minX_Height + i)) / ((float)td.heightmapResolution - 1) * td.size.x,
								heightMap[j, i] * td.size.y,
								((float)(minZ_Height + j)) / ((float)td.heightmapResolution - 1) * td.size.z
								);
							pointInWorldSpace_height += terrain.transform.position;

							float heightTop = 10E+10f;
							float heightBottom = -10E+10f;

							if (collider.Raycast(new Ray(pointInWorldSpace_height + new Vector3(0, 50, 0), Vector3.down), out RaycastHit hitTop, 50)) {
								heightTop = hitTop.point.y;
							}
							if (collider.Raycast(new Ray(pointInWorldSpace_height + new Vector3(0, -50, 0), Vector3.up), out RaycastHit hitBottom, 50)) {
								heightBottom = hitBottom.point.y;
							}
							float newHeightTop = (heightTop - terrain.transform.position.y) / td.size.y;
							float newHeightBottom = (heightBottom - terrain.transform.position.y) / td.size.y;

							if (Math.Abs(pointInWorldSpace_height.y - heightTop) <= 2 && newHeightTop >= 0 && newHeightTop <= 1) {
								heightChanges.Add(new Vector3(i, heightMap[j, i], j));
								heightMap[j, i] = Math.Min(1, newHeightTop + 0.25f / td.size.y);

								/*foreach ((int oX, int oZ) in heightAreaOffsets) {
									if (i + oX < 0 || i + oX >= heightMap.GetLength(1) || j + oZ < 0 || j + oZ >= heightMap.GetLength(0)) { continue; }

									heightChanges_ForArea.TryAdd(new Vector2(i + oX, j + oZ), Math.Min(1, newHeightTop + 0.25f / td.size.y));
								}*/
							} else if (Math.Abs(pointInWorldSpace_height.y - heightBottom) <= 3.5 && newHeightBottom >= 0 && newHeightBottom <= 1) {
								heightChanges.Add(new Vector3(i, heightMap[j, i], j));
								heightMap[j, i] = Math.Max(0, newHeightBottom - 0.25f / td.size.y);

								/*foreach ((int oX, int oZ) in heightAreaOffsets) {
									if (i + oX < 0 || i + oX >= heightMap.GetLength(1) || j + oZ < 0 || j + oZ >= heightMap.GetLength(0)) { continue; }

									heightChanges_ForArea.TryAdd(new Vector2(i + oX, j + oZ), Math.Max(0, newHeightBottom - 0.25f / td.size.y));
								}*/
							}
						}
					}
					/*foreach (Vector3 existingChange in heightChanges) {
						heightChanges_ForArea.Remove(new Vector2(existingChange.x, existingChange.z));
					}
					foreach (KeyValuePair<Vector2, float> areaChange in heightChanges_ForArea) {
						heightChanges.Add(new Vector3(areaChange.Key.x, heightMap[(int)areaChange.Key.y, (int)areaChange.Key.x], areaChange.Key.y));
						heightMap[(int)areaChange.Key.y, (int)areaChange.Key.x] = areaChange.Value;
					}*/

					td.SetHeights(minX_Height, minZ_Height, heightMap);
					heightPositions[terrain] = ((minX_Height, minZ_Height, arrayWidth_Height, arrayHeight_Height), heightChanges);

					int minX_Holes = Math.Max(0, (int)Math.Floor((collider.bounds.min.x - terrain.transform.position.x) / td.size.x * (td.holesResolution)) - 1);
					int minZ_Holes = Math.Max(0, (int)Math.Floor((collider.bounds.min.z - terrain.transform.position.z) / td.size.z * (td.holesResolution)) - 1);
					int maxX_Holes = Math.Min(td.holesResolution - 1, (int)Math.Ceiling((collider.bounds.max.x - terrain.transform.position.x) / td.size.x * (td.holesResolution)));
					int maxZ_Holes = Math.Min(td.holesResolution - 1, (int)Math.Ceiling((collider.bounds.max.z - terrain.transform.position.z) / td.size.z * (td.holesResolution)));
					int arrayWidth_Holes = maxX_Holes - minX_Holes + 1;
					int arrayHeight_Holes = maxZ_Holes - minZ_Holes + 1;
					bool[,] holeMap = td.GetHoles(minX_Holes, minZ_Holes, arrayWidth_Holes, arrayHeight_Holes);
					float[,] terrainHeight = td.GetHeights(0, 0, td.heightmapResolution, td.heightmapResolution);

					(int, int)[] cornerOffsets = [(0, 0), (0, 1), (1, 0), (1, 1)];

					for (int j = 0; j < holeMap.GetLength(0); j++) {
						for (int i = 0; i < holeMap.GetLength(1); i++) {
							/*
							 * OLD SYSTEM: Checks center; unreliable
							 */
							/*float minY = Math.Min(Math.Min(terrainHeight[minZ_Holes + j, minX_Holes + i], terrainHeight[minZ_Holes + j, minX_Holes + i + 1]), Math.Min(terrainHeight[minZ_Holes + j + 1, minX_Holes + i], terrainHeight[minZ_Holes + j + 1, minX_Holes + i + 1]));
							float maxY = Math.Max(Math.Max(terrainHeight[minZ_Holes + j, minX_Holes + i], terrainHeight[minZ_Holes + j, minX_Holes + i + 1]), Math.Max(terrainHeight[minZ_Holes + j + 1, minX_Holes + i], terrainHeight[minZ_Holes + j + 1, minX_Holes + i + 1]));

							float averageY = (maxY + minY) / 2;
							Vector3 pointInWorldSpace_hole = new Vector3(
								((float)(minX_Holes + i) + 0.5f) / ((float)td.holesResolution) * td.size.x,
								averageY * td.size.y, // (terrainHeight[minZ_Holes + j, minX_Holes + i] + terrainHeight[minZ_Holes + j + 1, minX_Holes + i + 1]) / 2f * td.size.y,
								((float)(minZ_Holes + j) + 0.5f) / ((float)td.holesResolution) * td.size.z
								) + terrain.transform.position;
							bool isInsideCollider = (collider.ClosestPoint(pointInWorldSpace_hole) - pointInWorldSpace_hole).sqrMagnitude < 0.01f;
							bool overwrite = Math.Abs(maxY - minY) < 2 && (Math.Abs(pointInWorldSpace_hole.y - collider.bounds.max.y) < 0.5f || Math.Abs(pointInWorldSpace_hole.y - collider.bounds.min.y) < 0.5f);

							if (isInsideCollider && !overwrite) {
								holeMap[j, i] = false; // might be old, but think about it again! -> // &= because otherwise other holes would be closed
								holeChanges.Add(new Vector2(i, j));
							}*/

							bool isCornerInsideCollider = false;
							bool isCornerBelowCollider = false;
							bool isCornerAboveCollider = false;
							foreach ((int oX, int oZ) in cornerOffsets) {
								Vector3 cornerPointInWorldSpace_hole = new Vector3(
									((float)(minX_Holes + i + oX)) / ((float)td.holesResolution) * td.size.x,
									terrainHeight[minZ_Holes + j + oZ, minX_Holes + i + oX] * td.size.y,
									((float)(minZ_Holes + j + oZ)) / ((float)td.holesResolution) * td.size.z
									) + terrain.transform.position;
								//(collider.ClosestPoint(cornerPointInWorldSpace_hole) - cornerPointInWorldSpace_hole).sqrMagnitude < 0.01f;
								Vector3 closestPoint = collider.ClosestPoint(cornerPointInWorldSpace_hole);
								float ok_distance = 0.5f;
								isCornerInsideCollider |= closestPoint == cornerPointInWorldSpace_hole
									&& Math.Abs(cornerPointInWorldSpace_hole.x - collider.bounds.min.x) > ok_distance
									&& Math.Abs(cornerPointInWorldSpace_hole.x - collider.bounds.max.x) > ok_distance
									&& Math.Abs(cornerPointInWorldSpace_hole.z - collider.bounds.min.z) > ok_distance
									&& Math.Abs(cornerPointInWorldSpace_hole.z - collider.bounds.max.z) > ok_distance
									;
								isCornerBelowCollider |= cornerPointInWorldSpace_hole.y < closestPoint.y && cornerPointInWorldSpace_hole.y < collider.bounds.center.y && cornerPointInWorldSpace_hole.x == closestPoint.x && cornerPointInWorldSpace_hole.z == closestPoint.z;
								isCornerAboveCollider |= cornerPointInWorldSpace_hole.y > closestPoint.y && cornerPointInWorldSpace_hole.y > collider.bounds.center.y && cornerPointInWorldSpace_hole.x == closestPoint.x && cornerPointInWorldSpace_hole.z == closestPoint.z;
							}
							if (isCornerInsideCollider || (isCornerAboveCollider && isCornerBelowCollider)) {
								holeMap[j, i] = false;
								holeChanges.Add(new Vector2(i, j));
							}

						}
					}
					td.SetHoles(minX_Holes, minZ_Holes, holeMap);
					holePositions[terrain] = ((minX_Holes, minZ_Holes, arrayWidth_Holes, arrayHeight_Holes), holeChanges);

				}
			}
		}

		public void OnDestroy() {
			foreach (KeyValuePair<Terrain, ((int, int, int, int), List<Vector2>)> kvp in holePositions) {
				Terrain terrain = kvp.Key;
				((int, int, int, int), List<Vector2>) bounds = kvp.Value;
				(int x, int y, int width, int height) origin = bounds.Item1;
				List<Vector2> indicesToChange = bounds.Item2;

				if (terrain == null || terrain.gameObject == null) continue;

				bool[,] holeMap = terrain.terrainData.GetHoles(origin.x, origin.y, origin.width, origin.height);
				foreach (Vector2 pos in indicesToChange) {
					holeMap[(int)pos.y, (int)pos.x] = true;
				}
				terrain.terrainData.SetHoles(origin.x, origin.y, holeMap);
			}

			foreach (KeyValuePair<Terrain, ((int, int, int, int), List<Vector3>)> kvp in heightPositions) {
				Terrain terrain = kvp.Key;
				((int, int, int, int), List<Vector3>) bounds = kvp.Value;
				(int x, int y, int width, int height) origin = bounds.Item1;
				List<Vector3> indicesToChange = bounds.Item2;

				if (terrain == null || terrain.gameObject == null) continue;

				float[,] heightMap = terrain.terrainData.GetHeights(origin.x, origin.y, origin.width, origin.height);
				foreach (Vector3 pos in indicesToChange) {
					heightMap[(int)pos.z, (int)pos.x] = pos.y;
				}
				terrain.terrainData.SetHeights(origin.x, origin.y, heightMap);
			}
		}
	}
}

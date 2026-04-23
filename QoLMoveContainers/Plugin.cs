// Copyright 2025-2026 Nicolas Schäfer & Contributors
// Licensed under Apache License, Version 2.0

using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using SpaceCraft;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nicki0.QoLMoveContainers {

	[BepInPlugin("Nicki0.theplanetcraftermods.QoLMoveContainers", "(QoL) Move Containers", PluginInfo.PLUGIN_VERSION)]
	public class Plugin : BaseUnityPlugin {

		private static ManualLogSource log;
		private static Plugin Instance;

		private void Awake() {
			// Plugin startup logic
			log = Logger;
			Instance = this;

			if (LibCommon.ModVersionCheck.Check(this, Logger.LogInfo, out bool hashError, out string repoURL)) {
				LibCommon.ModVersionCheck.NotifyUser(this, hashError, repoURL, Logger.LogInfo);
			}



			Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
			Harmony.CreateAndPatchAll(typeof(Plugin));
		}

		static Dictionary<Group, Group> moveGroups = new Dictionary<Group, Group>();
		static WorldObject woToMove;
		[HarmonyPrefix]
		[HarmonyPatch(typeof(ActionOpenable), nameof(ActionOpenable.OnAction))]
		static bool AO_OA(ActionOpenable __instance) {
			if (Managers.GetManager<PlayersManager>().GetActivePlayerController().GetPlayerInputDispatcher().IsPressingAccessibilityKey() && Keyboard.current.altKey.isPressed) {
				WorldObjectAssociated worldObjectAssociated = __instance.GetComponentInParent<WorldObjectAssociated>();
				WorldObjectAssociatedProxy proxy = __instance.GetComponentInParent<WorldObjectAssociatedProxy>();
				InventoryAssociatedProxy invProxy = __instance.GetComponentInParent<InventoryAssociatedProxy>();

				if (worldObjectAssociated != null) {
					HandleWorldObject(worldObjectAssociated.GetWorldObject());
				} else if (proxy != null) {
					proxy.GetWorldObjectDetails(HandleWorldObject);
				} else if (invProxy != null) {
					invProxy.GetInventory(delegate (Inventory _, WorldObject wo) { HandleWorldObject(wo); });
				} else {
					return true;
				}

				return false;
			} else if (Managers.GetManager<PlayersManager>().GetActivePlayerController().GetPlayerBuilder().GetIsGhostExisting()) {
				return false;
			}
			woToMove = null;
			return true;
		}
		static void HandleWorldObject(WorldObject wo) {
			Group originalGroup = wo.GetGroup();
			if (!moveGroups.ContainsKey(originalGroup)) {
				Group containerCopy = new GroupConstructible((GroupDataConstructible)(originalGroup.GetGroupData()));
				containerCopy.SetRecipe(new Recipe(new List<GroupDataItem>()));
				moveGroups[originalGroup] = containerCopy;
			}
			if (Managers.GetManager<PlayersManager>().GetActivePlayerController().GetPlayerBuilder().SetNewGhost(moveGroups[originalGroup], null)) {
				Managers.GetManager<WindowsHandler>()?.CloseAllWindows();
			}

			woToMove = wo;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(WorldObjectsHandler), nameof(WorldObjectsHandler.CreateAndInstantiateWorldObject))]
		static bool WorldObjectsHandler_CreateAndInstantiateWorldObject(Group group, Vector3 position, Quaternion rotation) {
			if (moveGroups.Values.Contains(group)) {
				woToMove.SetPositionAndRotation(position, rotation, woToMove.GetPlanetHash());
				woToMove.GetGameObject().transform.rotation = rotation;
				woToMove.GetGameObject().transform.position = position;

				PlayerBuilder pb = Managers.GetManager<PlayersManager>().GetActivePlayerController().GetPlayerBuilder();

				if (woToMove.GetGameObject().TryGetComponent<GroupNetworkBase>(out GroupNetworkBase groupNetworkBase)) {
					groupNetworkBase.StartDisolveAnimationClientRpc();
				}

				PlayerBuilder.IsDuringConstruction = false;
				AccessTools.FieldRefAccess<PlayerBuilder, ConstructibleGhost>(pb, "_ghost") = null;

				return false;
			}
			return true;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(PlanetNetworkLoader), nameof(PlanetNetworkLoader.SwitchToPlanet))]
		static void PlanetNetworkLoader_SwitchToPlanet() {
			Managers.GetManager<PlayersManager>().GetActivePlayerController().GetPlayerBuilder().InputOnCancelAction();
		}


	}
}

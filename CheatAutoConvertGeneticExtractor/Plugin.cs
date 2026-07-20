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
using System.Collections.ObjectModel;
using System.Diagnostics;
using Unity.Netcode;


namespace Nicki0.CheatAutoConvertGeneticExtractor {
	[BepInPlugin("Nicki0.theplanetcraftermods.CheatAutoConvertGeneticExtractor", "(Cheat) Auto Convert Genetic Extractor", PluginInfo.PLUGIN_VERSION)]
	public class Plugin : BaseUnityPlugin {

		private static ConfigEntry<bool> config_enable;
		private static ConfigEntry<float> config_runEveryXSeconds;
		private static ConfigEntry<int> config_ProcessExtractorsPerFrame;
		private static ConfigEntry<bool> config_debug;

		static ManualLogSource log;
		static Plugin Instance;

		private void Awake() {
			// Plugin startup logic

			if (LibCommon.ModVersionCheck.Check(this, Logger.LogInfo, out bool hashError, out string repoURL)) {
				LibCommon.ModVersionCheck.NotifyUser(this, hashError, repoURL, Logger.LogInfo);
			}

			log = Logger;
			Instance = this;

			config_enable = Config.Bind<bool>("Config", "enabled", true, "enable mod");
			config_runEveryXSeconds = Config.Bind<float>("Config", "runEveryXSeconds", 1.0f, "Run the genetic extraction every X seconds.");
			config_ProcessExtractorsPerFrame = Config.Bind<int>("Config", "processNExtractorsPerFrame", 5, "Number of genetic extractors that are processed per frame.");
			config_debug = Config.Bind<bool>("Config", "debug", false, "print debug messages");

			Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
			Harmony.CreateAndPatchAll(typeof(Plugin));
		}
		public void Update() {
			if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) {
				return;
			}
			if (config_enable.Value) AutoConvertInventories();
		}

		private static List<int> inventoryIDs = new List<int>();

		[HarmonyPrefix] // enable ACs to take from the inventory
		[HarmonyPatch(typeof(StaticDataHandler), "LoadStaticData")]
		private static void StaticDataHandler_LoadStaticData(List<GroupData> ___groupsData) {
			GroupDataConstructible genExtractor1 = ___groupsData.Find(e => e.id == "GeneticExtractor1") as GroupDataConstructible;
			if (genExtractor1 != null) {
				genExtractor1.logisticInterplanetaryType = DataConfig.LogisticInterplanetaryType.EnabledOnAllInventories;
			}
		}


		private static int AutoConvertInventoriesCounter = 0;
		private static int CurrentNumberOfGenExtractors = 0;
		private static Stopwatch sw;
		private static void AutoConvertInventories() {
			if (
				AutoConvertInventoriesCounter++ < Math.Ceiling(CurrentNumberOfGenExtractors / (float)(Math.Max(1, config_ProcessExtractorsPerFrame.Value)))
				||
				(sw != null && sw.IsRunning && sw.ElapsedMilliseconds < 1000.0f * config_runEveryXSeconds.Value)
				) {
				return; // only run every few frames and only if the previous routine finished.
			}

			if (config_debug.Value) { log.LogInfo("running trait conversion: " + sw?.ElapsedMilliseconds); }

			sw?.Stop();
			sw = Stopwatch.StartNew();

			if (GeneticTraitHandler.Instance == null
				|| GeneticTraitHandler.Instance.GetAllAvailableTraits() == null
				|| WorldObjectsHandler.Instance == null
				) { return; }

			inventoryIDs.Clear();

			foreach (WorldObject wo in WorldObjectsHandler.Instance.GetConstructedWorldObjects()) {
				if (wo.GetGroup().GetId() == "GeneticExtractor1") {
					inventoryIDs.Add(wo.GetLinkedInventoryId());
				}
			}

			CurrentNumberOfGenExtractors = inventoryIDs.Count;

			UnityEngine.Coroutine coroutine = Instance.StartCoroutine(ProcessGeneticExtractors());
		}

		private static IEnumerator ProcessGeneticExtractors() {
			int currentlyProcessedGeneticExtractorCount = 0;
			for (int inventoryID_Ctr = 0; inventoryID_Ctr < inventoryIDs.Count; inventoryID_Ctr++) {
				if (++currentlyProcessedGeneticExtractorCount > config_ProcessExtractorsPerFrame.Value) {
					currentlyProcessedGeneticExtractorCount = 0;
					yield return null;
				}

				int invId = inventoryIDs[inventoryID_Ctr];

				if (InventoriesHandler.Instance == null || GeneticTraitHandler.Instance == null) yield break; // game was left

				Inventory inv = InventoriesHandler.Instance.GetInventoryById(invId);
				if (inv == null) { continue; }

				// From UiWindowDNAExtractor.OnClickExtractTraits:
				ReadOnlyCollection<WorldObject> insideWorldObjects = inv.GetInsideWorldObjects();
				for (int i = insideWorldObjects.Count - 1; i > -1; i--) {
					WorldObject worldObject = insideWorldObjects[i];
					GroupItem groupItem = worldObject.GetGroup() as GroupItem;
					if (groupItem != null) {
						GeneticTraitData associatedGeneticTrait = GeneticTraitHandler.Instance.GetGeneticTraitExtractedFromGroup(groupItem);
						if (associatedGeneticTrait != null) {
							InventoriesHandler.Instance.RemoveItemFromInventory(worldObject, inv, true, delegate (bool success) {
								if (success) {
									/*
									 * this._groupsConverted.Add(worldObject.GetGroup());
									 * this._traitsConverted.Add(associatedGeneticTrait);
									 * THEY ARE REMOVED TO PREVENT THE ANIMATION FROM PLAYING!!!
									 */
									GeneticTraitHandler.Instance.CreateNewTraitWorldObject(associatedGeneticTrait, inv);
								}
							});
						}
					}
				}
			}
			yield break;
		}
	}
}

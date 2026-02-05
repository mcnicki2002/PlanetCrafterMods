// Copyright 2025-2026 Nicolas Schäfer & Contributors
// Licensed under Apache License, Version 2.0

using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using SpaceCraft;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nicki0.FeatTerrainHeightTool {

	[BepInPlugin("Nicki0.theplanetcraftermods.FeatTerrainHeightTool", "(Feat) Terrain Height Tool", PluginInfo.PLUGIN_VERSION)]
	public class Plugin : BaseUnityPlugin {

		static ManualLogSource log;
		//private static readonly int SaveStateId = SaveState.GenerateId(typeof(Plugin));

		private static readonly int StateDataFormat = 1;
		private static SaveState saveState;

		private void Awake() {
			// Plugin startup logic
			log = Logger;

			if (LibCommon.ModVersionCheck.Check(this, Logger.LogInfo)) {
				LibCommon.ModVersionCheck.NotifyUser(this, Logger.LogInfo);
			}

			saveState = new SaveState(typeof(Plugin), StateDataFormat);

			Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
			Harmony.CreateAndPatchAll(typeof(Plugin));
		}

		private static bool switchToolIsActive = false;

		public void FixedUpdate() {// called at a fixed interval
			if (switchToolIsActive) {
				ProcessTerrainModification();
			}
		}

		//List<int> collectorInstIDs = new List<int>();
		public void Update() {
			if (Keyboard.current.ctrlKey.IsPressed() && Keyboard.current.tKey.wasPressedThisFrame && Keyboard.current.tKey.IsPressed()) {
				switchToolIsActive = !switchToolIsActive;
			}


			/*
			 * Does not work because once the objects are hidden, they get a new id
			 * 
			if (Keyboard.current.pKey.wasPressedThisFrame) {
				if (Physics.Raycast(Managers.GetManager<PlayersManager>().GetActivePlayerController().GetAimController().GetAimRay(), out RaycastHit hit, 100)) {
					collectorInstIDs.Add(hit.collider.gameObject.GetInstanceID());
					log.LogMessage(hit.collider.gameObject.GetInstanceID());
				}
			}
			if (Keyboard.current.lKey.wasPressedThisFrame) {
				log.LogWarning("-----");
				foreach (int id in collectorInstIDs) {
					log.LogError(id);
					//GameObject go = (GameObject) Resources.InstanceIDToObject(id);

					GameObject go = (GameObject)typeof(UnityEngine.Object).GetMethod("FindObjectFromInstanceID", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
		.Invoke(null, new object[] { id });

					log.LogInfo(go == null);
					if (go != null) go.SetActive(!go.activeSelf);
				}
			}*/
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(BaseHudHandler), "UpdateHud")]
		private static void BaseHudHandler_UpdateHud(BaseHudHandler __instance) {
			if (switchToolIsActive && (Managers.GetManager<PlayersManager>()?.GetActivePlayerController().GetPlayerCanAct().GetCanMove() ?? false)) {
				__instance.textPositionDecoration.text += " - Terrain Height Tool Active";
			}
		}

		private static Func<float[,], string> EncodeFloatArray = delegate (float[,] array) {
			return array.GetLength(0) + "x" + array.GetLength(1) + "#" + string.Join("*", array.Cast<float>().Select(e => e.ToString(CultureInfo.InvariantCulture)).ToArray());
		};
		private static Func<string, float[,]> DecodeFloatArray = delegate (string s) {
			string[] sizeAndValues = s.Split("#");
			string[] size = sizeAndValues[0].Split("x");
			int sizex = int.Parse(size[0]);
			int sizey = int.Parse(size[1]);
			float[,] result = new float[sizex, sizey];
			string[] values = sizeAndValues[1].Split("*");
			for (int i = 0; i < sizex; i++) {
				for (int j = 0; j < sizey; j++) {
					result[i, j] = float.Parse(values[i * sizey + j], CultureInfo.InvariantCulture);
				}
			}
			return result;
		};

		private static Dictionary<string, Dictionary<string, float[,]>> heightsOfSaves = new Dictionary<string, Dictionary<string, float[,]>>();
		private static void ProcessTerrainModification() {
			string saveFileName = Managers.GetManager<SavedDataHandler>().GetCurrentSaveFileName();
			if (!(Keyboard.current.ctrlKey.IsPressed() && (Mouse.current.leftButton.IsPressed() || Mouse.current.rightButton.IsPressed()))) return;
			//SaveState.GetAndCreateStateObject(SaveStateId, out WorldObject so);

			if (!heightsOfSaves.ContainsKey(saveFileName)) {
				heightsOfSaves[saveFileName] = GetSaveState();
			}
			Dictionary<string, float[,]> terrainHeights = heightsOfSaves[saveFileName];

			float heightChange = (Mouse.current.rightButton.IsPressed() ? -1 : 1) * 0.001f;

			PlayerMainController pmc = Managers.GetManager<PlayersManager>().GetActivePlayerController();

			RaycastHit[] hist = Physics.RaycastAll(pmc.GetAimController().GetAimRay(), 1000);
			foreach (RaycastHit hit in hist) {
				if (hit.collider == null) continue;

				if (!hit.collider.gameObject.TryGetComponent<Terrain>(out Terrain terrain)) continue;

				TerrainData td = terrain.terrainData;
				float[,] heights;
				if (terrainHeights.ContainsKey(hit.collider.gameObject.name)) {
					heights = terrainHeights[hit.collider.gameObject.name];
				} else {
					heights = td.GetHeights(0, 0, td.heightmapResolution, td.heightmapResolution);
				}
				int x = (int)(Mod(hit.point.x - terrain.transform.position.x, 1000) / 1000 * td.heightmapResolution);
				int z = (int)(Mod(hit.point.z - terrain.transform.position.z, 1000) / 1000 * td.heightmapResolution);
				int radius = 5;
				if (Keyboard.current.altKey.isPressed) radius = 25;
				float oldCenterHeight = heights[z, x];
				float newCenterHeight = heights[z, x] + heightChange;

				Dictionary<string, Terrain> changedTerrains = new Dictionary<string, Terrain>();

				for (int i = x - radius; i <= x + radius; i++) {
					for (int j = z - radius; j <= z + radius; j++) {
						float distance = new Vector2(i - x, j - z).magnitude;

						if (0 <= i && i < td.heightmapResolution && 0 <= j && j < td.heightmapResolution) {
							if (distance > radius) continue;

							float newHeight = heights[j, i] + heightChange * (1 - Mathf.Pow(distance / radius, 1.25f)) /* * Mathf.Abs((newCenterHeight - heights[j, i]) / heightChange)*/;
							if (heightChange > 0) {
								if ((false || newHeight <= newCenterHeight) && (newHeight * 600 <= pmc.transform.position.y || !Keyboard.current.shiftKey.isPressed)) {
									heights[j, i] = newHeight;
								}
							} else {
								if (newHeight >= newCenterHeight) {
									heights[j, i] = newHeight;
								}
							}
						} else {
							Terrain tempTerrain = terrain;
							int tempRes = tempTerrain.terrainData.heightmapResolution;
							int tj = j;
							int ti = i;
							if (tj < 0) {
								tempTerrain = tempTerrain.bottomNeighbor;
								tj++;
							} else if (tj >= tempRes) {
								tempTerrain = tempTerrain.topNeighbor;
								tj--;
							}
							if (tempTerrain == null) continue;
							if (ti < 0) {
								tempTerrain = tempTerrain.leftNeighbor;
								ti++;
							} else if (ti >= tempRes) {
								tempTerrain = tempTerrain.rightNeighbor;
								ti--;
							}
							if (tempTerrain == null) continue;

							distance = new Vector2(ti - x, tj - z).magnitude;
							if (distance > radius) continue;

							float[,] tempHeight;
							if (terrainHeights.ContainsKey(tempTerrain.gameObject.name)) {
								tempHeight = terrainHeights[tempTerrain.gameObject.name];
							} else {
								tempHeight = tempTerrain.terrainData.GetHeights(0, 0, tempRes, tempRes);
							}

							float tempNewHeight = tempHeight[(j + tempRes) % tempRes, (i + tempRes) % tempRes] + heightChange * (1 - Mathf.Pow(distance / radius, 1.25f));
							if (heightChange > 0) {
								if (tempNewHeight <= newCenterHeight && (tempNewHeight * 600 <= pmc.transform.position.y || !Keyboard.current.shiftKey.isPressed)) {
									tempHeight[(j + tempRes) % tempRes, (i + tempRes) % tempRes] = tempNewHeight;
								}
							} else {
								if (tempNewHeight >= newCenterHeight) {
									tempHeight[(j + tempRes) % tempRes, (i + tempRes) % tempRes] = tempNewHeight;
								}
							}

							(heightsOfSaves[saveFileName])[tempTerrain.gameObject.name] = tempHeight;
							changedTerrains[tempTerrain.gameObject.name] = tempTerrain;
						}
					}
				}

				foreach (Terrain terr in changedTerrains.Values) {
					terr.terrainData.SetHeights(0, 0, (heightsOfSaves[saveFileName])[terr.gameObject.name]);
				}

				td.SetHeights(0, 0, heights);

				(heightsOfSaves[saveFileName])[hit.collider.gameObject.name] = heights;
			}
		}


		[HarmonyPrefix]
		[HarmonyPatch(typeof(SavedDataHandler), nameof(SavedDataHandler.SaveWorldData))]
		private static void SavedDataHandler_SaveWorldData(SavedDataHandler __instance) {
			//SaveState.GetAndCreateStateObject(SaveStateId, out WorldObject so);
			//SaveState.SetDictionaryData<string, float[,]>(so, heightsOfSaves[__instance.GetCurrentSaveFileName()], TFunc: EncodeFloatArray);
			saveState.SetData(heightsOfSaves[__instance.GetCurrentSaveFileName()]);
		}
		[HarmonyPostfix]
		[HarmonyPatch(typeof(PlanetLoader), "HandleDataAfterLoad")]
		static void PlanetLoader_HandleDataAfterLoad_terrainEdit() {
			string saveFileName = Managers.GetManager<SavedDataHandler>().GetCurrentSaveFileName();

			if (!heightsOfSaves.ContainsKey(saveFileName)) {
				//SaveState.GetAndCreateStateObject(SaveStateId, out WorldObject so);
				heightsOfSaves[saveFileName] = GetSaveState();
				//SaveState.GetDictionaryData<string, float[,]>(so, s => s, DecodeFloatArray);
			}

			Dictionary<string, float[,]> terrainHeights = heightsOfSaves[saveFileName];
			foreach (Terrain terrain in GameObject.Find("World/Terrains").GetComponentsInChildren<Terrain>()) {
				if (terrainHeights.TryGetValue(terrain.gameObject.name, out float[,] heights)) {
					terrain.terrainData.SetHeights(0, 0, heights);
				}
			}
		}

		private static Dictionary<string, float[,]> GetSaveState() {
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
							saveState.SetData(new Dictionary<string, float[,]>());
							return new Dictionary<string, float[,]>();
					}
					break;
				case SaveState.ERROR_CODE.INVALID_JSON:
					log.LogError("Can't convert data format! Resetting data.");
					saveState.SetData(new Dictionary<string, float[,]>());
					break;
				case SaveState.ERROR_CODE.STATE_OBJECT_MISSING:
					saveState.SetData(new Dictionary<string, float[,]>());
					return new Dictionary<string, float[,]>();
			}

			switch (saveState.GetData(out Dictionary<string, float[,]> data)) {
				case SaveState.ERROR_CODE.SUCCESS:
					return data;
				default:
					log.LogError("Can't load data! Resetting data.");
					saveState.SetData(new Dictionary<string, float[,]>());
					return new Dictionary<string, float[,]>();
			}
		}

		private static float Mod(float v, float m) {
			return (v % m + m) % m;
		}
	}
}
// --- Old FeatTerrainHeightTool --->
/*private static void DoTerrainStuff() {
	//DoTerrainStuffPoints();
	DoTerrainStuffArray();
}

static Func<string, Vector2> stringToVector2 = static delegate (string s) {
	string[] splitString = s.Split(':');
	return new Vector2(float.Parse(splitString[0]), float.Parse(splitString[1]));
};

private static void DoTerrainStuffPoints() {
	if (Keyboard.current.ctrlKey.IsPressed() && (Mouse.current.leftButton.IsPressed() || Mouse.current.rightButton.IsPressed())) {
		SaveState.GetAndCreateStateObject(100, out WorldObject so);
		List<Vector2> allChanges = SaveState.GetListData<Vector2>(so, stringToVector2, separator: ";");

		float upwards = (Mouse.current.rightButton.IsPressed() ? -1 : 1) * 0.001f;
		PlayerMainController pmc = Managers.GetManager<PlayersManager>().GetActivePlayerController();
		Ray ray = pmc.GetAimController().GetAimRay();
		RaycastHit[] hist = Physics.RaycastAll(ray, 1000);
		foreach (RaycastHit hit in hist) {
			if (hit.collider == null) continue;

			if (hit.collider.gameObject.TryGetComponent<Terrain>(out Terrain terrain)) {
				Vector2 hitPos = new Vector2(hit.point.x, hit.point.z);
				allChanges.Add(hitPos);
				ModifyTerrain(terrain, hitPos, upwards);
			}
		}

		SaveState.SetListData<Vector2>(so, allChanges, (v) => v.x + ":" + v.y, separator: ";");
	}

	if (Keyboard.current.f15Key.IsPressed() && Keyboard.current.f15Key.wasPressedThisFrame) {
		SaveState.GetAndCreateStateObject(100, out WorldObject so);

		foreach (Vector2 v in SaveState.GetListData<Vector2>(so, stringToVector2, separator: ";")) {
			foreach (Terrain t in GetTerrainsFromPos(v)) {
				ModifyTerrain(t, v, 0.001f);
			}
		}
	}
}
private static List<Terrain> GetTerrainsFromPos(Vector2 pos) {
	List<Terrain> terrains = new List<Terrain>();
	foreach (RaycastHit hit in Physics.RaycastAll(new Vector3(pos.x, 500, pos.y), Vector3.down)) {
		if (hit.collider == null) { continue; }

		if (hit.collider.gameObject.TryGetComponent<Terrain>(out Terrain terrain)) {
			terrains.Add(terrain);
		}
	}
	return terrains;
}
private static void ModifyTerrain(Terrain terrain, Vector2 pos, float heightChange) {
	TerrainData td = terrain.terrainData;
	float[,] heights = td.GetHeights(0, 0, td.heightmapResolution, td.heightmapResolution);
	int x = (int)(((pos.x % 1000) + 1000) % 1000 / 1000 * td.heightmapResolution);
	int z = (int)(((pos.y % 1000) + 1000) % 1000 / 1000 * td.heightmapResolution);
	int radius = 5;
	for (int i = Mathf.Max(0, x - radius); i <= Mathf.Min(x + radius, td.heightmapResolution - 1); i++) {
		for (int j = Mathf.Max(0, z - radius); j <= Mathf.Min(z + radius, td.heightmapResolution - 1); j++) {
			float distance = new Vector2(i - x, j - z).magnitude;
			if (distance <= radius) {
				heights[j, i] += heightChange;
			}
		}
	}

	td.SetHeights(0, 0, heights);
}*/
// <--- Old FeatTerrainHeightTool ---

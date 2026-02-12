// Copyright 2025-2026 Nicolas Schäfer & Contributors
// Licensed under Apache License, Version 2.0

using BepInEx.Logging;
using HarmonyLib;
using SpaceCraft;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nicki0 {

	internal class MaterialsHelper {
		static ManualLogSource log;

		class Nicki0_MaterialsHelper : MonoBehaviour {
			public Dictionary<string, Material> materialDictionary;
			public Dictionary<string, Material> completeMaterialDictionary;
			public Dictionary<string, Material> completeMaterialDictionary2;
		}
		static GameObject materialsHelperObject;
		public static void InitMaterialsHelper(ManualLogSource pLog) {
			log = pLog;

			materialsHelperObject = GameObject.Find("Nicki0_MaterialsHelperObject");
			if (materialsHelperObject == null) {
				materialsHelperObject = new GameObject("Nicki0_MaterialsHelperObject");

				materialsHelperObject.AddComponent<Nicki0_MaterialsHelper>();

				GameObject.DontDestroyOnLoad(materialsHelperObject);

				Harmony.CreateAndPatchAll(typeof(MaterialsHelper));
			}
		}


		public static bool ApplyGameMaterials(GameObject toFix, bool normalizeName = false, bool fromCompleteCollection = false, bool setSharedMaterials = false) {
			if (materialsHelperObject == null) {
				Console.WriteLine("[Fatal] MaterialsHelper not initialized");
				return false;
			}
			if (materialsHelperObject.GetComponent<Nicki0_MaterialsHelper>().materialDictionary == null) {
				log.LogFatal("Materials not yet initialized");
				return false;
			}

			Nicki0_MaterialsHelper materialsHelper = materialsHelperObject.GetComponent<Nicki0_MaterialsHelper>();

			foreach (Renderer mr in toFix.GetComponentsInChildren<MeshRenderer>()) {
				Material[] materials = mr.GetSharedMaterialArray();

				for (int i = 0; i < materials.Length; i++) {
					if (fromCompleteCollection) {
						if (materialsHelper.completeMaterialDictionary.TryGetValue(normalizeName ? materials[i].name.CanonicalizeString() : materials[i].name, out Material gameMaterial)) {
							materials[i] = gameMaterial;
						}
					} else if (materialsHelper.materialDictionary.TryGetValue(normalizeName ? materials[i].name.CanonicalizeString() : materials[i].name, out Material gameMaterial)) {
						materials[i] = gameMaterial;
					}
				}

				if (setSharedMaterials) {
					mr.SetSharedMaterials(materials.ToList());
				} else {
					mr.SetMaterialArray(materials);
				}
			}

			return true;
		}

		[HarmonyPrefix]
		[HarmonyPriority(Priority.First)]
		[HarmonyPatch(typeof(StaticDataHandler), "LoadStaticData")]
		private static void StaticDataHandler_LoadStaticData(List<GroupData> ___groupsData) {
			Nicki0_MaterialsHelper materialsHelper = materialsHelperObject.GetComponent<Nicki0_MaterialsHelper>();
			if (materialsHelper.materialDictionary != null) {
				return;
			}
			materialsHelper.materialDictionary = new Dictionary<string, Material>();
			materialsHelper.completeMaterialDictionary = new Dictionary<string, Material>();
			materialsHelper.completeMaterialDictionary2 = new Dictionary<string, Material>();

			// --- Add materials from GroupDataConstructable ---
			// ~18ms
			foreach (GroupData gd in ___groupsData) {
				if (gd == null || gd.associatedGameObject == null) continue;

				//foreach (MeshRenderer renderer in gd.associatedGameObject.GetComponentsInChildren<MeshRenderer>()) {
				foreach (Renderer renderer in gd.associatedGameObject.GetComponentsInChildren<Renderer>()) {
					if (renderer == null) continue;

					foreach (Material material in renderer.GetSharedMaterialArray()) {
						if (material == null) continue;
						materialsHelper.materialDictionary.TryAdd(material.name, material);
					}
				}
			}

			// ~0.4ms
			foreach (MethodInfo mi in AccessTools.GetDeclaredMethods(typeof(VisualsResourcesHandler))) {
				if (mi.ReturnType == typeof(Material) && mi.GetParameters().Length == 0) {
					Material returnedMaterial = mi.Invoke(Managers.GetManager<VisualsResourcesHandler>(), []) as Material;
					if (returnedMaterial == null || string.IsNullOrEmpty(returnedMaterial.name)) continue;
					materialsHelper.materialDictionary.TryAdd(returnedMaterial.name, returnedMaterial);
				}
			}
			

			/*
			// --- Add materials from procedural wrecks --- Does not add any additional Materials ---
			//ProceduralInstancesHandler.GeneratorData[] _generators
			Type generatorDataType = AccessTools.FirstInner(typeof(ProceduralInstancesHandler), x => x.Name.Contains("GeneratorData"));
			Array generatorArray = (Array)(AccessTools.GetDeclaredFields(typeof(ProceduralInstancesHandler)).Where(x => x.Name == "_generators").First().GetValue(ProceduralInstancesHandler.Instance));
			FieldInfo GOProperty = AccessTools.Field(generatorDataType, "generator");
			for (int i = 0; i < generatorArray.Length; i++) {
				GameObject go = ((GameObject)GOProperty.GetValue(generatorArray.GetValue(i)));
				if (go == null) continue;

				foreach (MeshRenderer renderer in go.GetComponentsInChildren<MeshRenderer>()) {
					//foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>()) {
					if (renderer == null) continue;

					foreach (Material material in renderer.GetSharedMaterialArray()) {
						if (material == null) continue;
						materialsHelper.materialDictionary.TryAdd(material.name, material);
					}
				}
			}*/




			/*
			// --- Add materials from all Scenes ---
			for (int i = 0; i < SceneManager.sceneCount; i++) {
				Scene scene = SceneManager.GetSceneAt(i);
				if (scene == null) { continue; }
				foreach (GameObject go in scene.GetRootGameObjects()) {
					if (go == null) continue;
					foreach (Renderer r in go.GetComponentsInChildren<Renderer>()) {
						if (r == null) continue;
						foreach (Material m in r.sharedMaterials) {
							if (m == null) continue;
							materialsHelper.materialDictionary.TryAdd(m.name, m);
						}
					}
				}
			}

			


			// --- Add materials from MaterialList objects ---
			foreach (MaterialList ml in UnityEngine.Object.FindObjectsByType(typeof(MaterialList), FindObjectsSortMode.None)) {
				if (ml == null) continue;

				foreach (Material m in ml.materials) {
					materialsHelper.materialDictionary.TryAdd(m.name, m);
				}
			}
			*/

			// --- Add materials of all Materials ---
			// ~0.6 ms
			foreach (Material m in Resources.FindObjectsOfTypeAll(typeof(Material))) {
				if (m == null) continue;
				materialsHelper.completeMaterialDictionary2.TryAdd(m.name, m);
			}
			
			// ~70ms
			/*foreach (UnityEngine.Object obj in Resources.FindObjectsOfTypeAll<UnityEngine.Object>()) {
				if (obj is Material m) {
					materialsHelper.completeMaterialDictionary.TryAdd(m.name, m);
				}
			}*/
		}

	}
	public static class StringExtension {
		public static string CanonicalizeString(this string str) {
			return str.Replace("(Instance)", "").Replace("(Clone)", "").Trim();
		}
	}
}

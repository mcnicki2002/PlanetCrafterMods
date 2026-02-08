// Copyright 2025-2026 Nicolas Schäfer & Contributors
// Licensed under Apache License, Version 2.0

using BepInEx.Logging;
using HarmonyLib;
using SpaceCraft;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nicki0 {

	internal class MaterialsHelper {
		static ManualLogSource log;

		class Nicki0_MaterialsHelper : MonoBehaviour {
			public Dictionary<string, Material> materialDictionary;
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

		public static bool ApplyGameMaterials(GameObject toFix) {
			if (materialsHelperObject == null) {
				Console.WriteLine("[Fatal] MaterialsHelper not initialized");
				return false;
			}
			if (materialsHelperObject.GetComponent<Nicki0_MaterialsHelper>().materialDictionary == null) {
				log.LogFatal("Materials not yet initialized");
				return false;
			}

			Nicki0_MaterialsHelper materialsHelper = materialsHelperObject.GetComponent<Nicki0_MaterialsHelper>();

			foreach (MeshRenderer mr in toFix.GetComponentsInChildren<MeshRenderer>()) {
				Material[] materials = mr.GetSharedMaterialArray();

				for (int i = 0; i < materials.Length; i++) {
					if (materialsHelper.materialDictionary.TryGetValue(materials[i].name, out Material gameMaterial)) {
						materials[i] = gameMaterial;
					}
				}

				mr.SetMaterialArray(materials);
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

			foreach (GroupData gd in ___groupsData) {
				if (gd == null || gd.associatedGameObject == null) continue;

				foreach (MeshRenderer mr in gd.associatedGameObject.GetComponentsInChildren<MeshRenderer>()) {
					if (mr == null) continue;

					foreach (Material material in mr.GetSharedMaterialArray()) {
						if (material == null) continue;
						materialsHelper.materialDictionary.TryAdd(material.name, material);
					}
				}
			}
		}
	}
}

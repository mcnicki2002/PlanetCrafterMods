// Copyright 2025-2025 Nicolas Schäfer & Contributors
// Licensed under Apache License, Version 2.0

using BepInEx;
using HarmonyLib;
using SpaceCraft;
using TMPro;
using UnityEngine;

namespace Nicki0.FeatLargeScreens;

[BepInPlugin("Nicki0.theplanetcraftermods.FeatLargeScreens", "(Feat) Large Screens", PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin {
	private void Awake() {
		// Plugin startup logic
		Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
		Harmony.CreateAndPatchAll(typeof(Plugin));
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(WorldObjectsHandler), nameof(WorldObjectsHandler.InstantiateWorldObject))]
	private static void WorldObjectsHandler_InstantiateWorldObject(WorldObject worldObject) {
		sizeSign(worldObject);
	}
	[HarmonyPostfix]
	[HarmonyPatch(typeof(UiWindowTextInput), nameof(UiWindowTextInput.OnChangeText))]
	public static void UiWindowTextInput_OnChangeText(ref WorldObjectText ___worldObjectText, ref TMP_InputField ___inputField) {
		TextProxy textProxy = (TextProxy)AccessTools.Field(typeof(WorldObjectText), "_proxy").GetValue(___worldObjectText);
		WorldObject worldObject = textProxy.GetComponent<WorldObjectAssociated>().GetWorldObject();
		sizeSign(worldObject, ___inputField.text);
	}
	private static float originalLocalScaleY = 0;
	private static void sizeSign(WorldObject worldObject, string txt = null) {
		if (worldObject.GetGroup().GetId() == "Sign") {
			float factor = 1;
			if (worldObject.GetText() != null) {
				string[] splittext = (txt == null ? worldObject.GetText() : txt).Split("!");
				if (splittext.Length > 1) {
					if (float.TryParse(splittext[1], out float parsedFactor)) {
						if (parsedFactor >= 0.1) {
							factor = parsedFactor;
						}
					}
				}
			}

			worldObject.GetGameObject().transform.localScale = new Vector3(1, factor, 1);
			Transform texttransform = worldObject.GetGameObject().transform.Find("Container/Text/TV_Big_Screen_01/Screen/Canvas/Text (TMP)");
			if (texttransform != null) {
				if (originalLocalScaleY == 0) originalLocalScaleY = texttransform.localScale.y;
				texttransform.localScale = new Vector3(texttransform.localScale.x, originalLocalScaleY / factor, texttransform.localScale.z);
			}
		}
	}
	[HarmonyPostfix]
	[HarmonyPatch(typeof(UiWindowTextInput), nameof(UiWindowTextInput.SetTextWorldObject))]
	public static void UiWindowTextInput_SetTextWorldObject(ref TMP_InputField ___inputField) {
		___inputField.characterLimit = 200; // Increase character limit
	}
}

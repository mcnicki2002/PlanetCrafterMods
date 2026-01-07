// Copyright 2025-2026 Nicolas Schäfer & Contributors
// Licensed under Apache License, Version 2.0

using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using SpaceCraft;
using System;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace Nicki0.FeatLargeScreens;

[BepInPlugin("Nicki0.theplanetcraftermods.FeatLargeScreens", "(Feat) Large Screens", PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin {

	private static FieldInfo field_WorldObjectText_proxy;

	private static ConfigEntry<float> config_FontSize;

	private void Awake() {
		// Plugin startup logic
		field_WorldObjectText_proxy = AccessTools.Field(typeof(WorldObjectText), "_proxy");

		config_FontSize = Config.Bind<float>("General", "FontSize", 26, "Font size of Signs");

		Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
		Harmony.CreateAndPatchAll(typeof(Plugin));
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(WorldObjectsHandler), nameof(WorldObjectsHandler.InstantiateWorldObject))]
	private static void WorldObjectsHandler_InstantiateWorldObject(WorldObject worldObject) {
		SizeSign(worldObject);
	}
	[HarmonyPostfix]
	[HarmonyPatch(typeof(UiWindowTextInput), nameof(UiWindowTextInput.OnChangeText))]
	public static void UiWindowTextInput_OnChangeText(ref WorldObjectText ___worldObjectText, ref TMP_InputField ___inputField) {
		TextProxy textProxy = (TextProxy)field_WorldObjectText_proxy.GetValue(___worldObjectText);
		WorldObject worldObject = textProxy.GetComponent<WorldObjectAssociated>().GetWorldObject();
		SizeSign(worldObject, ___inputField.text);
	}
	private static float originalLocalScaleY = 0;
	private static void SizeSign(WorldObject worldObject, string txt = null) {
		if (worldObject.GetGroup().GetId() != "Sign") {
			return;
		}
		float factor = 1;
		if (txt == null && worldObject.GetText() != null) {
			txt = worldObject.GetText();
		}
		if (txt != null) {
			string[] splittext = txt.Split("!");
			if (splittext.Length > 1) {
				if (float.TryParse(splittext[1], out float parsedFactor)) {
					if (parsedFactor >= 0.1 && parsedFactor <= 100) {
						factor = parsedFactor;
					}
				}
			}
		}

		TextMeshProUGUI tmpUGui = worldObject.GetGameObject().GetComponentInChildren<TextMeshProUGUI>();
		if (tmpUGui != null) {
			tmpUGui.fontSize = config_FontSize.Value;
		}

		worldObject.GetGameObject().transform.localScale = new Vector3(1, factor, 1);
		Transform texttransform = worldObject.GetGameObject().transform.Find("Container/Text/TV_Big_Screen_01/Screen/Canvas/Text (TMP)");
		if (texttransform != null) {
			if (originalLocalScaleY == 0) originalLocalScaleY = texttransform.localScale.y;
			texttransform.localScale = new Vector3(texttransform.localScale.x, originalLocalScaleY / factor, texttransform.localScale.z);// ScaleYVector(texttransform.localScale, 1/factor);
		}
	}
	[HarmonyPostfix]
	[HarmonyPatch(typeof(UiWindowTextInput), nameof(UiWindowTextInput.SetTextWorldObject))]
	public static void UiWindowTextInput_SetTextWorldObject(TMP_InputField ___inputField, WorldObjectText ___worldObjectText) {
		if (___inputField.characterLimit < 125) {
			___inputField.characterLimit = 125; // Increase character limit
		}
	}
}

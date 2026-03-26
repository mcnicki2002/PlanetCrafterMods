// Copyright 2025-2026 Nicolas Schäfer & Contributors
// Licensed under Apache License, Version 2.0

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SpaceCraft;
using System.Reflection;
using UnityEngine;

namespace Nicki0.UiSoundConfig {

	[BepInPlugin("Nicki0.theplanetcraftermods.UiSoundConfig", "(UI) Sound Config", PluginInfo.PLUGIN_VERSION)]
	public class Plugin : BaseUnityPlugin {

		private static ManualLogSource log;
		private static Plugin Instance;

		private static ConfigEntry<float> config_volume_PlayUiHover;
		private static ConfigEntry<float> config_volume_PlayUiMove;
		private static ConfigEntry<float> config_volume_PlayUiOpen;
		private static ConfigEntry<float> config_volume_PlayUiClose;
		private static ConfigEntry<float> config_volume_PlayUiSelectElement;
		private static ConfigEntry<float> config_volume_PlayAlertLow;
		private static ConfigEntry<float> config_volume_PlayAlertCritical;
		private static ConfigEntry<float> config_volume_PlayCheckTutorial;
		private static ConfigEntry<float> config_volume_PlayEnergyLack;
		private static ConfigEntry<float> config_volume_PlayEnergyRestored;
		private static ConfigEntry<float> config_volume_PlayDropObject;
		private static ConfigEntry<float> config_volume_PlayCantDo;
		private static ConfigEntry<float> config_volume_PlayTeleport;


		private static MethodInfo method_GlobalAudioHandler_CanPlaySound;

		private void Awake() {
			// Plugin startup logic
			log = Logger;
			Instance = this;

			if (LibCommon.ModVersionCheck.Check(this, Logger.LogInfo)) {
				LibCommon.ModVersionCheck.NotifyUser(this, Logger.LogInfo);
			}

			config_volume_PlayUiHover = Config.Bind("Volume", "volumeMultiplier_PlayUiHover", 1.0f, "Multiplier for the volume of the sound PlayUiHover");
			config_volume_PlayUiMove = Config.Bind("Volume", "volumeMultiplier_PlayUiMove", 1.0f, "Multiplier for the volume of the sound PlayUiMove");
			config_volume_PlayUiOpen = Config.Bind("Volume", "volumeMultiplier_PlayUiOpen", 1.0f, "Multiplier for the volume of the sound PlayUiOpen");
			config_volume_PlayUiClose = Config.Bind("Volume", "volumeMultiplier_PlayUiClose", 1.0f, "Multiplier for the volume of the sound PlayUiClose");
			config_volume_PlayUiSelectElement = Config.Bind("Volume", "volumeMultiplier_PlayUiSelectElement", 1.0f, "Multiplier for the volume of the sound PlayUiSelectElement");
			config_volume_PlayAlertLow = Config.Bind("Volume", "volumeMultiplier_PlayAlertLow", 1.0f, "Multiplier for the volume of the sound PlayAlertLow");
			config_volume_PlayAlertCritical = Config.Bind("Volume", "volumeMultiplier_PlayAlertCritical", 1.0f, "Multiplier for the volume of the sound PlayAlertCritical");
			config_volume_PlayCheckTutorial = Config.Bind("Volume", "volumeMultiplier_PlayCheckTutorial", 1.0f, "Multiplier for the volume of the sound PlayCheckTutorial");
			config_volume_PlayEnergyLack = Config.Bind("Volume", "volumeMultiplier_PlayEnergyLack", 1.0f, "Multiplier for the volume of the sound PlayEnergyLack");
			config_volume_PlayEnergyRestored = Config.Bind("Volume", "volumeMultiplier_PlayEnergyRestored", 1.0f, "Multiplier for the volume of the sound PlayEnergyRestored");
			config_volume_PlayDropObject = Config.Bind("Volume", "volumeMultiplier_PlayDropObject", 1.0f, "Multiplier for the volume of the sound PlayDropObject");
			config_volume_PlayCantDo = Config.Bind("Volume", "volumeMultiplier_PlayCantDo", 1.0f, "Multiplier for the volume of the sound PlayCantDo");
			config_volume_PlayTeleport = Config.Bind("Volume", "volumeMultiplier_PlayTeleport", 1.0f, "Multiplier for the volume of the sound PlayTeleport");


			method_GlobalAudioHandler_CanPlaySound = AccessTools.Method(typeof(GlobalAudioHandler), "CanPlaySound");

			Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
			Harmony.CreateAndPatchAll(typeof(Plugin));
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(GlobalAudioHandler), nameof(GlobalAudioHandler.PlayUiHover))]
		private static bool GlobalAudioHandler_PlayUiHover(AudioSource ___soundContainerUi, AudioResourcesHandler ___audioResourcesHandler) {
			___soundContainerUi.PlayOneShot(___audioResourcesHandler.uiHoverSound, config_volume_PlayUiHover.Value);
			return false;
		}
		[HarmonyPrefix]
		[HarmonyPatch(typeof(GlobalAudioHandler), nameof(GlobalAudioHandler.PlayUiMove))]
		private static bool GlobalAudioHandler_PlayUiMove(AudioSource ___soundContainerUi, AudioResourcesHandler ___audioResourcesHandler) {
			___soundContainerUi.PlayOneShot(___audioResourcesHandler.uiTransferSound, config_volume_PlayUiMove.Value);
			return false;
		}
		[HarmonyPrefix]
		[HarmonyPatch(typeof(GlobalAudioHandler), nameof(GlobalAudioHandler.PlayUiOpen))]
		private static bool GlobalAudioHandler_PlayUiOpen(AudioSource ___soundContainerUi, AudioResourcesHandler ___audioResourcesHandler) {
			___soundContainerUi.PlayOneShot(___audioResourcesHandler.uiOpenSound, config_volume_PlayUiOpen.Value);
			return false;
		}
		[HarmonyPrefix]
		[HarmonyPatch(typeof(GlobalAudioHandler), nameof(GlobalAudioHandler.PlayUiClose))]
		private static bool GlobalAudioHandler_PlayUiClose(AudioSource ___soundContainerUi, AudioResourcesHandler ___audioResourcesHandler) {
			___soundContainerUi.PlayOneShot(___audioResourcesHandler.uiCloseSound, config_volume_PlayUiClose.Value);
			return false;
		}
		[HarmonyPrefix]
		[HarmonyPatch(typeof(GlobalAudioHandler), nameof(GlobalAudioHandler.PlayUiSelectElement))]
		private static bool GlobalAudioHandler_PlayUiSelectElement(AudioSource ___soundContainerUi, AudioResourcesHandler ___audioResourcesHandler) {
			___soundContainerUi.PlayOneShot(___audioResourcesHandler.uiSelectElement, config_volume_PlayUiSelectElement.Value);
			return false;
		}
		[HarmonyPrefix]
		[HarmonyPatch(typeof(GlobalAudioHandler), nameof(GlobalAudioHandler.PlayAlertLow))]
		private static bool GlobalAudioHandler_PlayAlertLow(AudioSource ___soundContainerAlert, AudioResourcesHandler ___audioResourcesHandler) {
			___soundContainerAlert.PlayOneShot(___audioResourcesHandler.alertLowSound, config_volume_PlayAlertLow.Value);
			return false;
		}
		[HarmonyPrefix]
		[HarmonyPatch(typeof(GlobalAudioHandler), nameof(GlobalAudioHandler.PlayAlertCritical))]
		private static bool GlobalAudioHandler_PlayAlertCritical(AudioSource ___soundContainerAlert, AudioResourcesHandler ___audioResourcesHandler) {
			___soundContainerAlert.PlayOneShot(___audioResourcesHandler.alertCriticalSound, config_volume_PlayAlertCritical.Value);
			return false;
		}
		[HarmonyPrefix]
		[HarmonyPatch(typeof(GlobalAudioHandler), nameof(GlobalAudioHandler.PlayCheckTutorial))]
		private static bool GlobalAudioHandler_PlayCheckTutorial(AudioSource ___soundContainerAlert, AudioResourcesHandler ___audioResourcesHandler) {
			___soundContainerAlert.PlayOneShot(___audioResourcesHandler.alertTutorialCheck, config_volume_PlayCheckTutorial.Value);
			return false;
		}
		[HarmonyPrefix]
		[HarmonyPatch(typeof(GlobalAudioHandler), nameof(GlobalAudioHandler.PlayEnergyLack))]
		private static bool GlobalAudioHandler_PlayEnergyLack(AudioSource ___soundContainerAlert, AudioResourcesHandler ___audioResourcesHandler) {
			___soundContainerAlert.PlayOneShot(___audioResourcesHandler.energyLack, config_volume_PlayEnergyLack.Value);
			return false;
		}
		[HarmonyPrefix]
		[HarmonyPatch(typeof(GlobalAudioHandler), nameof(GlobalAudioHandler.PlayEnergyRestored))]
		private static bool GlobalAudioHandler_PlayEnergyRestored(AudioSource ___soundContainerAlert, AudioResourcesHandler ___audioResourcesHandler) {
			___soundContainerAlert.PlayOneShot(___audioResourcesHandler.energyRestored, config_volume_PlayEnergyRestored.Value);
			return false;
		}
		[HarmonyPrefix]
		[HarmonyPatch(typeof(GlobalAudioHandler), nameof(GlobalAudioHandler.PlayDropObject))]
		private static bool GlobalAudioHandler_PlayDropObject(GlobalAudioHandler __instance, AudioSource ___soundContainerUi, AudioResourcesHandler ___audioResourcesHandler) {
			if ((bool)method_GlobalAudioHandler_CanPlaySound.Invoke(__instance, [])) {
				___soundContainerUi.PlayOneShot(___audioResourcesHandler.drop, config_volume_PlayDropObject.Value);
			}
			return false;
		}
		[HarmonyPrefix]
		[HarmonyPatch(typeof(GlobalAudioHandler), nameof(GlobalAudioHandler.PlayCantDo))]
		private static bool GlobalAudioHandler_PlayCantDo(GlobalAudioHandler __instance, AudioSource ___soundContainerUi, AudioResourcesHandler ___audioResourcesHandler) {
			if ((bool)method_GlobalAudioHandler_CanPlaySound.Invoke(__instance, [])) {
				___soundContainerUi.PlayOneShot(___audioResourcesHandler.cantDo, config_volume_PlayCantDo.Value);
			}
			return false;
		}
		[HarmonyPrefix]
		[HarmonyPatch(typeof(GlobalAudioHandler), nameof(GlobalAudioHandler.PlayTeleport))]
		private static bool GlobalAudioHandler_PlayTeleport(AudioSource ___soundContainerUi, AudioResourcesHandler ___audioResourcesHandler) {
			___soundContainerUi.PlayOneShot(___audioResourcesHandler.teleport, config_volume_PlayTeleport.Value);
			return false;
		}
	}
}

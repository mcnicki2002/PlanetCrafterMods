// Copyright 2025-2026 Nicolas Schäfer & Contributors
// Licensed under Apache License, Version 2.0

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SpaceCraft;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

namespace Nicki0.UiChangeMusic {

	[BepInPlugin("Nicki0.theplanetcraftermods.UiChangeMusic", "(UI) Change Music", PluginInfo.PLUGIN_VERSION)]
	public class Plugin : BaseUnityPlugin {

		private static ManualLogSource log;
		private static Plugin Instance;

		public static ConfigEntry<bool> config_addInsteadOfReplace;

		private void Awake() {
			// Plugin startup logic
			log = Logger;
			Instance = this;

			if (LibCommon.ModVersionCheck.Check(this, Logger.LogInfo)) {
				LibCommon.ModVersionCheck.NotifyUser(this, Logger.LogInfo);
			}

			config_addInsteadOfReplace = Config.Bind<bool>("General", "addInsteadOfReplace", false, "Add music instead of replacing it.");

			Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
			Harmony.CreateAndPatchAll(typeof(Plugin));
		}
		// --- add music --->
		[HarmonyPrefix]
		[HarmonyPatch(typeof(MusicsHandler), "InitMusicHandler")]
		static void MusicsHandler_InitMusicHandler(MusicsHandler __instance, List<MusicData> availableTracks) {
			string[] musicFiles = Directory.GetFiles(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

			List<(DownloadHandlerAudioClip, UnityWebRequest, string)> downloadhandlers = [];

			foreach (string musicPath in musicFiles) {
				if (File.Exists(musicPath)) {
					AudioType audioType;
					if (musicPath.EndsWith("mp3", System.StringComparison.InvariantCultureIgnoreCase)) audioType = AudioType.MPEG;
					else if (musicPath.EndsWith("ogg", System.StringComparison.InvariantCultureIgnoreCase)) audioType = AudioType.OGGVORBIS;
					else if (musicPath.EndsWith("wav", System.StringComparison.InvariantCultureIgnoreCase)) audioType = AudioType.WAV;
					else if (musicPath.EndsWith("dll", System.StringComparison.InvariantCultureIgnoreCase)) continue;
					else {
						log.LogWarning($"Unknown audio type for file {musicPath}");
						continue;
					}

					DownloadHandlerAudioClip dh = new DownloadHandlerAudioClip("file://" + musicPath, audioType);
					dh.compressed = true;
					UnityWebRequest wr = new UnityWebRequest("file://" + musicPath, "GET", dh, null);
					wr.SendWebRequest();

					downloadhandlers.Add((dh, wr, Path.GetFileName(musicPath)));
				} else log.LogError($"music file \"{musicPath}\" not found");
			}
			if (downloadhandlers.Count == 0) {
				log.LogWarning("No music files found!");
				return;
			}
			if (!config_addInsteadOfReplace.Value) availableTracks.Clear();

			while (downloadhandlers.Count > 0) {
				for (int i = downloadhandlers.Count - 1; i >= 0; i--) {
					(DownloadHandlerAudioClip dh, UnityWebRequest wr, string name) = downloadhandlers[i];
					if (!dh.isDone) continue;


					MusicData md = (MusicData)ScriptableObject.CreateInstance(typeof(MusicData));
					if (wr.responseCode == 200) {
						md.musicTrack = dh.audioClip;
					} else log.LogError("music not found with http code " + wr.responseCode);

					md.name = name;
					md.musicTrack.name = name;
					availableTracks.Add(md);
					log.LogInfo($"Added music file \"{md.musicTrack.name}\"");
					downloadhandlers.RemoveAt(i);
				}
			}
		}
		// <--- add music ---

	}

}

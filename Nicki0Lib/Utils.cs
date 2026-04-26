using BepInEx;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Nicki0 {
	internal class Utils {
		public static IEnumerator ExecuteLater(Action toExecute, int waitFrames = 1, int waitSeconds = 0) {
			if (waitSeconds > 0) yield return new WaitForSeconds(waitSeconds);
			for (int i = 0; i < waitFrames; i++) yield return new WaitForEndOfFrame();
			toExecute.Invoke();
		}
	}
	static class Extensions {
		public static bool IsNewVersion(this BaseUnityPlugin plugin, out Version ver) {
			try {
				if (File.Exists(plugin.Config.ConfigFilePath)) {
					ver = new Version((File.ReadLines(plugin.Config.ConfigFilePath).FirstOrDefault(e => !string.IsNullOrEmpty(e)) ?? "").Split("v").Last());
					return ver < plugin.Info.Metadata.Version;
				}
			} catch { }
			ver = null;
			return false;
		}
	}
}

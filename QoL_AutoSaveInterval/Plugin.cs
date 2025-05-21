using SpaceCraft;

using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;

namespace AutoSaveInterval
{
	[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
	public class Plugin : BaseUnityPlugin
	{
		static ManualLogSource log;
		public static ConfigEntry<float> time;
		
		private void Awake()
		{
			// Plugin startup logic
			log = Logger;
			
			time = Config.Bind<float>("General", "saveTimeInMinutes", 10, "Time until an auto-save happens. 0 disables auto-saves.");
			
			Harmony.CreateAndPatchAll(typeof(Plugin));
			Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
		}
		[HarmonyPrefix]
		[HarmonyPatch(typeof(SessionController), "Start")]
		public static void SessionController_Start(ref float ____autoSaveIntervalInMin) {
			float timeToAutosave = time.Value;
			if (timeToAutosave <= 0.001) {
				timeToAutosave = 525600000f;
			}
			____autoSaveIntervalInMin = timeToAutosave;
			log.LogInfo("Changed auto save interval to " + timeToAutosave + " minutes");
		}
	}
}

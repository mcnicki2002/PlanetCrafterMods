// Copyright 2025-2026 Nicolas Schäfer & Contributors
// Licensed under Apache License, Version 2.0

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using HSVPicker;
using SpaceCraft;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Tessera;
using UnityEngine;

namespace Nicki0.ItemWallAquarium {

	[BepInPlugin("Nicki0.theplanetcraftermods.ItemWallAquarium", "(Item) Wall Aquarium", PluginInfo.PLUGIN_VERSION)]
	public class Plugin : BaseUnityPlugin {

		private static ManualLogSource log;
		private static Plugin Instance;

		private static ConfigEntry<string> config_windowColor;
		private static ConfigEntry<float> config_lodMultiplier;
		private static ConfigEntry<bool> config_cleanMoss;

		private void Awake() {
			// Plugin startup logic
			log = Logger;
			Instance = this;

			config_windowColor = Config.Bind<string>("General", "windowColor", "0.5754, 0.9245, 0.4405, 0", "Set the color of the windows via RGBA values. Can be changed without restart.");
			config_windowColor.SettingChanged += Config_windowColor_SettingChanged;
			config_lodMultiplier = Config.Bind<float>("General", "lodMultiplier", 1.5f, "Distance multiplier at which the deco disappears. Increase to see the deco from further away, decrease for slightly better performance. Restart game to apply changes.");
			config_cleanMoss = Config.Bind<bool>("General", "cleanMoss", true, "Removes two large moss stripes from the windows. Restart game to apply changes.");

			if (LibCommon.ModVersionCheck.Check(this, Logger.LogInfo)) {
				LibCommon.ModVersionCheck.NotifyUser(this, Logger.LogInfo);
			}

			Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
			Harmony.CreateAndPatchAll(typeof(Plugin));
		}

		private void Config_windowColor_SettingChanged(object sender, EventArgs e) {
			Color? newColor = GetColor();
			if (!newColor.HasValue) {
				return;
			}

			if (GroupsHandler.GetAllGroups() != null) {
				Group wallAquariumGroup = GroupsHandler.GetGroupViaId(WallAquariumId);
				if (wallAquariumGroup != null) {
					SetColorOnPanel(wallAquariumGroup.GetGroupData().associatedGameObject, newColor.Value);
				}
			}

			foreach (Panel panel in FindObjectsByType<Panel>(FindObjectsSortMode.None)) {
				if (panel == null) { continue; }
				if (panel.GetSubPanelType() != WallAquariumSubPanelType) { continue; }

				GameObject panelGameObject = null;
				for (int i = 0; i < panel.transform.childCount; i++) {
					if (panel.transform.GetChild(i) != null && panel.transform.GetChild(i).gameObject.name.Contains(WallAquariumId)) {
						panelGameObject = panel.transform.GetChild(i).gameObject;
						break;
					}
				}
				if (panelGameObject != null) {
					SetColorOnPanel(panelGameObject, newColor.Value);
				}
			}
		}
		private static Color? GetColor() {
			List<float> clr = config_windowColor.Value.Split(",").Select(e => e.Trim()).Select(e => float.TryParse(e, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out float v) ? v : 0).ToList();
			if (clr.Count == 4) {
				return new Color(clr[0], clr[1], clr[2], clr[3]);
			}
			return null;
		}
		private static void SetColorOnPanel(GameObject go, Color newColor) {
			go.transform.Find("Structure/Roof_Window_01").GetComponent<MeshRenderer>().materials.Where(e => e.name.Contains("Glass")).First().color = newColor;
		}

		private static readonly string WallAquariumId = "Nicki0_WallAquarium";
		private static readonly DataConfig.BuildPanelSubType WallAquariumSubPanelType = (DataConfig.BuildPanelSubType)140_001;

		[HarmonyPrefix]
		[HarmonyPatch(typeof(StaticDataHandler), "LoadStaticData")]
		private static void StaticDataHandler_LoadStaticData(List<GroupData> ___groupsData) {
			GroupDataConstructible gdc_WallAquarium;
			if (!___groupsData.Where(x => x.id == WallAquariumId).Any()) {
				gdc_WallAquarium = ScriptableObject.CreateInstance<GroupDataConstructible>();//new GroupDataConstructible();
				gdc_WallAquarium.id = WallAquariumId;
				gdc_WallAquarium.unlockingWorldUnit = DataConfig.WorldUnitType.Terraformation;
				gdc_WallAquarium.unlockingValue = 4_000_000;
				gdc_WallAquarium.groupCategory = DataConfig.GroupCategory.BaseBuilding;
				gdc_WallAquarium.recipeIngredients = [
					___groupsData.Find(e => e.id == "Aluminium") as GroupDataItem,
					___groupsData.Find(e => e.id == "Cobalt") as GroupDataItem,
					___groupsData.Find(e => e.id == "WaterBottle1") as GroupDataItem,
					___groupsData.Find(e => e.id == "Algae1Seed") as GroupDataItem
					];

				Texture2D texture = new Texture2D(2, 2);
				ImageConversion.LoadImage(texture, Properties.Resources.WallAquarium);

				gdc_WallAquarium.icon = Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100.0f);
				gdc_WallAquarium.unlockInPlanets = [];
				gdc_WallAquarium.secondaryInventoriesSize = [];
				gdc_WallAquarium.terraStageRequirements = [];
				gdc_WallAquarium.notAllowedPlanetsRequirement = [];

				gdc_WallAquarium.associatedGameObject = Instantiate(GetAquarium());
				DontDestroyOnLoad(gdc_WallAquarium.associatedGameObject);
				gdc_WallAquarium.associatedGameObject.SetName(gdc_WallAquarium.id);
				gdc_WallAquarium.associatedGameObject.transform.position += new Vector3(0, -1_000, 0); // move instantiated object away from playing area

				// ConstraintSamePanel requires a ConstructibleGhost component. Otherwise, it destroys itself.
				ConstructibleGhost cg = gdc_WallAquarium.associatedGameObject.AddComponent<ConstructibleGhost>();
				ConstraintSamePanel csp = gdc_WallAquarium.associatedGameObject.AddComponent<ConstraintSamePanel>();
				Destroy(cg);

				csp.panelType = DataConfig.BuildPanelType.Wall;
				csp.panelSubType = WallAquariumSubPanelType;
				foreach (Transform t in gdc_WallAquarium.associatedGameObject.transform) {
					t.localPosition = new Vector3(0.01f, 2, -3);
				}
				Transform t_wallTop = gdc_WallAquarium.associatedGameObject.transform.Find("Structure/Wall_Coin_Simple_01 (3)");
				t_wallTop.localScale = new Vector3(0.118f, 1.106f, 0.499f);
				t_wallTop.localPosition = new Vector3(-1.009f, 2, -2.765073f);
				BoxCollider bc = gdc_WallAquarium.associatedGameObject.GetComponent<BoxCollider>();
				BoxCollider bcNew = gdc_WallAquarium.associatedGameObject.transform.GetChild(0).gameObject.AddComponent<BoxCollider>();
				bcNew.center = bc.center;
				bcNew.size = bc.size;
				Destroy(bc);

				if (config_cleanMoss.Value) {
					Destroy(gdc_WallAquarium.associatedGameObject.transform.Find("Decoration/Moss").gameObject);
				}

				foreach (LODGroup lodgroup in gdc_WallAquarium.associatedGameObject.GetComponentsInChildren<LODGroup>()) {
					lodgroup.size *= config_lodMultiplier.Value;
				}

				gdc_WallAquarium.associatedGameObject.transform.Find("Structure").gameObject.AddComponent<Nicki0_ActionConfigColor>();

				___groupsData.Add(gdc_WallAquarium);
			} else {
				gdc_WallAquarium = (GroupDataConstructible)___groupsData.Where(x => x.id == WallAquariumId).First();
			}
			// 0.5754f, 0.9245f, 0.4405f, 0
			Color? newColor = GetColor();
			if (newColor != null) {
				SetColorOnPanel(gdc_WallAquarium.associatedGameObject, newColor.Value);
			}

		}
		class Nicki0_ActionConfigColor : ActionColorWorldObject {
			public override void OnAction() {
				if (!Managers.GetManager<PlayersManager>().GetActivePlayerController().GetPlayerInputDispatcher().IsPressingAccessibilityKey()) {
					return;
				}
				if (Managers.GetManager<PlayersManager>().GetActivePlayerController().GetMultitool().GetState() == DataConfig.MultiToolState.Deconstruct) {
					return;
				}
				UiWindowColorPicker colorPickerWindow = (UiWindowColorPicker)Managers.GetManager<WindowsHandler>().OpenAndReturnUi(DataConfig.UiType.ColorPicker);
				ColorPicker picker = colorPickerWindow.GetComponentInChildren<ColorPicker>();
				picker.onValueChanged.RemoveAllListeners();
				Color? newColor = GetColor();
				if (newColor != null) {
					picker.CurrentColor = newColor.Value;
				} else {
					picker.CurrentColor = new Color(0.5754f, 0.9245f, 0.4405f, 0);
				}
				picker.onValueChanged.AddListener(delegate (Color color) {
					config_windowColor.Value =
						color.r.ToString(CultureInfo.InvariantCulture) + ", " +
						color.g.ToString(CultureInfo.InvariantCulture) + ", " +
						color.b.ToString(CultureInfo.InvariantCulture) + ", " +
						color.a.ToString(CultureInfo.InvariantCulture);
				});
			}
			public override void OnHover() {
				if (Managers.GetManager<PlayersManager>().GetActivePlayerController().GetPlayerInputDispatcher().IsPressingAccessibilityKey()) {
					base.OnHover();
				}
			}
			private static IEnumerator ExecuteLater(Action toExecute, int waitFrames = 1) {
				for (int i = 0; i < waitFrames; i++) yield return new WaitForEndOfFrame();
				toExecute.Invoke();
			}
		}
		static GameObject GetAquarium() {
			ProceduralInstancesHandler pih = ProceduralInstancesHandler.Instance;
			if (pih == null) {
				pih = FindFirstObjectByType<ProceduralInstancesHandler>();
			}
			Array generatorArray = (Array)(AccessTools.GetDeclaredFields(typeof(ProceduralInstancesHandler)).Where(x => x.Name == "_generators").First().GetValue(pih));

			Type generatorDataType = AccessTools.FirstInner(typeof(ProceduralInstancesHandler), x => x.Name.Contains("GeneratorData"));
			FieldInfo GOProperty = AccessTools.Field(generatorDataType, "generator");

			for (int i = 0; i < generatorArray.Length; i++) {
				GameObject go = ((GameObject)GOProperty.GetValue(generatorArray.GetValue(i)));
				if (go == null) { continue; }
				TesseraGenerator tg = go.GetComponentInChildren<TesseraGenerator>();
				if (tg == null) { continue; }
				foreach (CountConstraint cc in tg.GetComponents<CountConstraint>()) {
					if (cc == null || cc.tiles == null) { continue; }
					foreach (TesseraTileBase ttb in cc.tiles) {
						if (ttb == null) { continue; }
						if (ttb.GetName().Contains("Tile5x5x2_Lounge", StringComparison.InvariantCultureIgnoreCase)) {
							foreach (RandomizePropData rpd in ttb.gameObject.GetComponentsInChildren<RandomizePropData>()) {
								IEnumerable<GameObject> enumer = rpd.props.Select(e => e.prop).Where(e => e.name == "WallAquarium");
								if (!enumer.Any()) { continue; }
								GameObject aquariumObject = enumer.First();
								if (aquariumObject != null) {
									return aquariumObject;
								}
							}
						}
					}
				}
			}
			throw new Exception("WallAquarium not found! Please report this as a bug!");
		}
		[HarmonyPrefix]
		[HarmonyPatch(typeof(PanelsResources), nameof(PanelsResources.GetPanelGameObject))]
		[HarmonyPatch(typeof(PanelsResources), nameof(PanelsResources.GetPanelGroupConstructible))]
		static void PR_GPGO_GPGC(PanelsResources __instance) {
			if (__instance.panelsSubtypes.Contains(WallAquariumSubPanelType)) return;

			Group aquariumGroup = GroupsHandler.GetGroupViaId(WallAquariumId);

			__instance.panelsSubtypes.Add(WallAquariumSubPanelType);
			__instance.panelsGroupItems.Add((GroupDataConstructible)aquariumGroup.GetGroupData());
			__instance.panelsGameObjects.Add(aquariumGroup.GetAssociatedGameObject());
		}
		[HarmonyPrefix]
		[HarmonyPatch(typeof(Panel), "SetPanel")]
		static void Panel_SetPanel(List<DataConfig.BuildPanelSubType> ____deconstructiblePanelsType) {
			if (!____deconstructiblePanelsType.Contains((DataConfig.BuildPanelSubType)140_001)) {
				____deconstructiblePanelsType.Add((DataConfig.BuildPanelSubType)140_001);
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(Localization), "LoadLocalization")]
		static void Localization_LoadLocalization(Dictionary<string, Dictionary<string, string>> ___localizationDictionary) {
			if (___localizationDictionary.TryGetValue(Localization.GetDefaultLangageCode(), out Dictionary<string, string> dict)) {
				dict[GameConfig.localizationGroupNameId + WallAquariumId] = "Aquarium Wall";
				dict[GameConfig.localizationGroupDescriptionId + WallAquariumId] = "Wall with aquarium, as seen in procedural wrecks.";
			}
		}
	}
}

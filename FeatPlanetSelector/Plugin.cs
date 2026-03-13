// Copyright 2025-2026 Nicolas Schäfer & Contributors
// Licensed under Apache License, Version 2.0

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SpaceCraft;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nicki0.FeatPlanetSelector {

	[BepInPlugin("Nicki0.theplanetcraftermods.FeatPlanetSelector", "(Feat) Planet Selector", PluginInfo.PLUGIN_VERSION)]
	public class Plugin : BaseUnityPlugin {

		private static ManualLogSource log;
		private static Plugin Instance;

		public static ConfigEntry<double> config_angle;

		private void Awake() {
			// Plugin startup logic
			log = Logger;
			Instance = this;

			if (LibCommon.ModVersionCheck.Check(this, Logger.LogInfo)) {
				LibCommon.ModVersionCheck.NotifyUser(this, Logger.LogInfo);
			}

			config_angle = Config.Bind<double>("General", "sunAngle", -24.73, "Angle of the sun in the planet viewer");

			Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
			Harmony.CreateAndPatchAll(typeof(Plugin));
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(WindowsHandler), nameof(WindowsHandler.GetWindowViaUiId))]
		private static void WindowsHandler_GetWindowViaUiId(DataConfig.UiType uiId, List<UiWindow> ____allUiWindows, ref UiWindow __result) {
			if (uiId == UiWindowPlanetSelector.PlanetSelectorUiType && __result == null) {
				UiWindowPlanetSelector planetSelector = Managers.GetManager<WindowsHandler>().GetComponentInChildren<UiWindowPlanetSelector>();
				if (planetSelector == null) {
					GameObject UiWindowPlanetSelectorGameObject = new GameObject("UiWindowPlanetSelector");
					planetSelector = UiWindowPlanetSelectorGameObject.AddComponent<UiWindowPlanetSelector>();
					UiWindowPlanetSelectorGameObject.transform.parent = Managers.GetManager<WindowsHandler>().transform;
				}
				____allUiWindows.Add(planetSelector);
				__result = planetSelector;
			}
		}
		[HarmonyPrefix]
		[HarmonyPatch(typeof(StaticDataHandler), "LoadStaticData")]
		private static void StaticDataHandler_LoadStaticData_NewWindow(List<GroupData> ___groupsData) {
			GameObject go = ___groupsData.Find(e => e.id == "PlanetViewer1").associatedGameObject;
			ActionOpenUi aoui = go.AddComponent<ActionOpenPlanetSelector>();
		}
		[HarmonyPostfix]
		[HarmonyPatch(typeof(MachinePlanetChanger), "OnEnable")]
		private static void MachinePlanetChanger_OnEnable(MachinePlanetChanger __instance, GameObject ____planetContainer) {
			if (__instance.TryGetComponent<ConstructibleGhost>(out _)) return;

			__instance.StartCoroutine(ExecuteLater(delegate () {
				if (__instance.TryGetComponent<ConstructibleGhost>(out _)) return;

				__instance.GetComponent<WorldObjectAssociatedProxy>().GetWorldObjectDetails(delegate (WorldObject wo) {
					if (wo != null && wo.GetPlanetLinkedHash() != 0) {

						PlanetData pd = Managers.GetManager<PlanetLoader>().planetList.GetPlanetFromIdHash(wo.GetPlanetLinkedHash());
						if (pd == null) return;

						MachinePlanetChanger mpc = wo.GetGameObject().GetComponent<MachinePlanetChanger>();

						GameObjects.DestroyAllChildren(____planetContainer, false);
						GameObject newPlanetObject = Instantiate<GameObject>(pd.GetPlanetSpaceView(), ____planetContainer.transform);
						foreach (Transform childGO in newPlanetObject.GetComponentsInChildren<Transform>()) childGO.gameObject.layer = 0;
						PlanetChanger planetChanger = newPlanetObject.GetComponentInChildren<PlanetChanger>();
						planetChanger.Init();
						WorldUnit worldUnit = Managers.GetManager<WorldUnitsHandler>()?.GetUnit(DataConfig.WorldUnitType.Terraformation, pd.id);
						planetChanger.SetColors(worldUnit == null ? 0 : (float)worldUnit.GetValue());


						LightSource lightSource = planetChanger.GetComponent<LightSource>();
						Vector3? newSunPosition = GetNewSunPosition(lightSource, pd);
						if (newSunPosition.HasValue) {
							lightSource.Sun.transform.localPosition = newSunPosition.Value;
						}
					}
				});
			}));
			__instance.StartCoroutine(UpdateState(__instance));
		}
		private static IEnumerator ExecuteLater(Action toExecute) {
			yield return new WaitForEndOfFrame();
			toExecute.Invoke();
		}
		private static IEnumerator UpdateState(MachinePlanetChanger mpc) {
			WaitForSeconds wait = new WaitForSeconds(10f);
			yield return new WaitForSeconds(1f);
			while (mpc != null && !mpc.TryGetComponent<ConstructibleGhost>(out _)) {
				mpc.GetComponent<WorldObjectAssociatedProxy>().GetWorldObjectDetails(delegate (WorldObject wo) {
					if (wo != null) {
						PlanetData pd = Managers.GetManager<PlanetLoader>().planetList.GetPlanetFromIdHash(wo.GetPlanetLinkedHash());
						if (pd == null) { return; }
						WorldUnit worldUnit = Managers.GetManager<WorldUnitsHandler>()?.GetUnit(DataConfig.WorldUnitType.Terraformation, pd.id);
						double value = (worldUnit == null) ? 0 : worldUnit.GetValue();
						mpc.GetComponentInChildren<PlanetChanger>()?.SetColors((float)value);


						LightSource lightSource = mpc.GetComponentInChildren<LightSource>();
						Vector3? newSunPosition = GetNewSunPosition(lightSource, pd);
						if (newSunPosition.HasValue) {
							lightSource.Sun.transform.localPosition = newSunPosition.Value;
						}
					}
				});

				yield return wait;
			}
		}
		public static Vector3? GetNewSunPosition(LightSource lightSource, PlanetData pd) {
			if (lightSource != null && lightSource.Sun != null && pd != null) {
				Transform sunTransform = lightSource.Sun.transform;

				double alphaAsRad = config_angle.Value * Math.PI / 180.0;
				// lightSource.Sun would be null if the line below would be null
				Vector3 newSunPosition = (pd.GetPlanetSpaceView().GetComponentInChildren<LightSource>().Sun.transform).localPosition * 1.0f; // to definitely copy vector
				newSunPosition.y = 0; // calc vector on horizontal plane
				newSunPosition.Normalize(); // -"-
				newSunPosition *= (float)Math.Cos(alphaAsRad); // scale x-z distance
				newSunPosition.y = -(float)Math.Sin(alphaAsRad); // set y height
				newSunPosition *= sunTransform.localPosition.magnitude; // move to final position

				return newSunPosition;
			}
			return null;
		}
	}

	public class ActionOpenPlanetSelector : ActionOpenUi {
		public override void OnAction() {
			if (!base.enabled) {
				return;
			}
			UiWindowPlanetSelector planetSelector = (UiWindowPlanetSelector)Managers.GetManager<WindowsHandler>().OpenAndReturnUi(UiWindowPlanetSelector.PlanetSelectorUiType);
			this.GetComponent<WorldObjectAssociatedProxy>().GetWorldObjectDetails(planetSelector.SetWorldObject);
			planetSelector.ChangeTitle(Localization.GetLocalizedString("GROUP_NAME_PlanetViewer1"));
		}
		public void Awake() {
			AccessTools.FieldRefAccess<ActionOpenUi, DataConfig.UiType>(this, "uiType") = UiWindowPlanetSelector.PlanetSelectorUiType;
		}
	}

	public class UiWindowPlanetSelector : UiWindow {

		public static readonly DataConfig.UiType PlanetSelectorUiType = (DataConfig.UiType)140;

		private WorldObject PlanetViewer;

		public override DataConfig.UiType GetUiIdentifier() {
			this._uiIdentifier = PlanetSelectorUiType;
			return this._uiIdentifier;
		}

		public override void OnOpen() {
			base.OnOpen();
		}

		public override void OnClose() {
			PlanetViewer = null;
			base.OnClose();
		}

		public void SetWorldObject(WorldObject wo) {
			this.PlanetViewer = wo;
		}
		public WorldObject GetWorldObject() => this.PlanetViewer;

		public void Awake() {
			GameObject grid = new GameObject("Grid");
			grid.transform.parent = this.transform;

			UiWindowSystemView uiwsv = Managers.GetManager<WindowsHandler>().GetWindowViaUiId(DataConfig.UiType.SystemView) as UiWindowSystemView;
			Image border = uiwsv.uiPlanetList.uiPlanetInformationsButtonGameObject.GetComponentInChildren<Button>().GetComponent<Image>();

			UiWindowCraft uiwc = Managers.GetManager<WindowsHandler>().GetWindowViaUiId(DataConfig.UiType.Craft) as UiWindowCraft;
			GameObject title = Instantiate(uiwc.GetComponent<UiWindowCraft>().title.gameObject, this.transform);
			title.SetName("Title");
			this.title = title.GetComponent<TextMeshProUGUI>();

			Instantiate(uiwc.GetComponentInChildren<EventCloseAllUi>(true).gameObject, this.transform);

			foreach (PlanetData pd in Managers.GetManager<PlanetLoader>().planetList.GetPlanetList()) {
				string name = Readable.GetPlanetLabel(pd);
				GameObject menuObject = new GameObject(pd.id);
				menuObject.transform.parent = grid.transform;
				menuObject.transform.localPosition = Vector3.zero;

				menuObject.AddComponent<Image>().sprite = border.sprite;

				GameObject labelObject = Instantiate(uiwc.GetComponent<UiWindowCraft>().title.gameObject, menuObject.transform);
				labelObject.transform.localPosition = Vector3.zero;
				TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
				label.text = name;
				label.GetComponent<LocalizedText>().textId = "Planet_" + pd.id;

				menuObject.AddComponent<EventHoverIncrease>().SetHoverGroupEvent(0.1f * Vector3.one);
				EventsHelpers.AddTriggerEvent(menuObject, EventTriggerType.PointerClick, new Action<EventTriggerCallbackData>(delegate (EventTriggerCallbackData d) {
					MachinePlanetChanger mpc = this.GetWorldObject().GetGameObject().GetComponent<MachinePlanetChanger>();
					GameObject planetContainer = AccessTools.FieldRefAccess<MachinePlanetChanger, GameObject>(mpc, "_planetContainer");

					GameObjects.DestroyAllChildren(planetContainer, false);
					GameObject newPlanetObject = Instantiate<GameObject>(pd.GetPlanetSpaceView(), planetContainer.transform);
					foreach (Transform childGO in newPlanetObject.GetComponentsInChildren<Transform>()) childGO.gameObject.layer = 0;
					PlanetChanger planetChanger = newPlanetObject.GetComponentInChildren<PlanetChanger>();
					planetChanger.Init();
					WorldUnit worldUnit = Managers.GetManager<WorldUnitsHandler>().GetUnit(DataConfig.WorldUnitType.Terraformation, pd.id);
					planetChanger.SetColors(worldUnit == null ? 0 : (float)worldUnit.GetValue());


					LightSource lightSource = planetChanger.GetComponent<LightSource>();
					Vector3? newSunPosition = Plugin.GetNewSunPosition(lightSource, pd);
					if (newSunPosition.HasValue) {
						lightSource.Sun.transform.localPosition = newSunPosition.Value;
					}


					this.GetWorldObject().SetPlanetLinkedHash(pd.GetPlanetHash());
				}), new EventTriggerCallbackData(pd));

				menuObject.SetActive(true);
			}

			GridLayoutGroup glg = grid.AddComponent<GridLayoutGroup>();
			Canvas canvas = this.gameObject.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			this.gameObject.AddComponent<CanvasScaler>();
			this.gameObject.AddComponent<GraphicRaycaster>();

			glg.constraintCount = 4;
			glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
			glg.spacing = new Vector2(10, 10);

			glg.cellSize = new Vector2((Screen.width * (3.0f / 4.0f) - (glg.constraintCount - 1) * glg.spacing.x) / glg.constraintCount, 100);
			float width = glg.cellSize.x * glg.constraintCount + (glg.constraintCount - 1) * glg.spacing.x;
			grid.transform.localPosition = new Vector3(-width / 2, 0, 0);
		}
	}
}

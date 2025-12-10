// Copyright 2025-2025 Nicolas Schäfer & Contributors
// Licensed under Apache License, Version 2.0

using BepInEx;
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

		private void Awake() {
			// Plugin startup logic
			log = Logger;
			Instance = this;

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
			Instance.StartCoroutine(ExecuteLater(delegate () {
				__instance.GetComponent<WorldObjectAssociatedProxy>().GetWorldObjectDetails(delegate (WorldObject wo) {
					if (wo != null && wo.GetPlanetLinkedHash() != 0) {

						PlanetData pd = Managers.GetManager<PlanetLoader>().planetList.GetPlanetFromIdHash(wo.GetPlanetLinkedHash());
						if (pd == null) return;

						MachinePlanetChanger mpc = wo.GetGameObject().GetComponent<MachinePlanetChanger>();

						GameObjects.DestroyAllChildren(____planetContainer, false);
						PlanetChanger componentInChildren = Instantiate<GameObject>(pd.GetPlanetSpaceView(), ____planetContainer.transform).GetComponentInChildren<PlanetChanger>();
						componentInChildren.Init();
						double? value = Managers.GetManager<WorldUnitsHandler>()?.GetUnit(DataConfig.WorldUnitType.Terraformation, pd.id)?.GetValue();
						if (value.HasValue) componentInChildren.SetColors((float)value);
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
			while (mpc != null) {
				mpc.GetComponent<WorldObjectAssociatedProxy>().GetWorldObjectDetails(delegate (WorldObject wo) {
					if (wo != null) {
						PlanetData pd = Managers.GetManager<PlanetLoader>().planetList.GetPlanetFromIdHash(wo.GetPlanetLinkedHash());
						double? value = Managers.GetManager<WorldUnitsHandler>()?.GetUnit(DataConfig.WorldUnitType.Terraformation, pd?.id ?? null)?.GetValue();
						if (!value.HasValue) {
							return;
						}
						mpc.GetComponentInChildren<PlanetChanger>()?.SetColors((float)value);
					}
				});

				yield return wait;
			}
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

			Instantiate(uiwc.GetComponentInChildren<EventCloseAllUi>().gameObject, this.transform);

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

				menuObject.AddComponent<EventHoverIncrease>().SetHoverGroupEvent(default);
				EventsHelpers.AddTriggerEvent(menuObject, EventTriggerType.PointerClick, new Action<EventTriggerCallbackData>(delegate (EventTriggerCallbackData d) {
					MachinePlanetChanger mpc = this.GetWorldObject().GetGameObject().GetComponent<MachinePlanetChanger>();
					GameObject planetContainer = AccessTools.FieldRefAccess<MachinePlanetChanger, GameObject>(mpc, "_planetContainer");

					GameObjects.DestroyAllChildren(planetContainer, false);
					PlanetChanger componentInChildren = Instantiate<GameObject>(pd.GetPlanetSpaceView(), planetContainer.transform).GetComponentInChildren<PlanetChanger>();
					componentInChildren.Init();
					componentInChildren.SetColors((float)Managers.GetManager<WorldUnitsHandler>().GetUnit(DataConfig.WorldUnitType.Terraformation, null).GetValue());

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

			glg.cellSize = new Vector2((Screen.width * (3.0f/4.0f) - (glg.constraintCount - 1) * glg.spacing.x) / glg.constraintCount, 100);
			float width = glg.cellSize.x * glg.constraintCount + (glg.constraintCount - 1) * glg.spacing.x;
			grid.transform.localPosition = new Vector3(-width/2, 0, 0);
		}
	}
}

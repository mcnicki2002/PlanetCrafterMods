// Copyright 2025-2026 Nicolas Schäfer & Contributors
// Licensed under Apache License, Version 2.0

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SpaceCraft;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nicki0.UiMoreUnits;


[BepInPlugin("Nicki0.theplanetcraftermods.UiMoreUnits", "(UI) More Units", PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin {
	private static DataConfig.WorldUnitType[] allWorldUnits = Enum.GetValues(typeof(DataConfig.WorldUnitType)).Cast<DataConfig.WorldUnitType>().Where(x => x != DataConfig.WorldUnitType.Null).ToArray();

	private static ConfigEntry<bool> configActive;
	private static ConfigEntry<bool> configDebug;
	private static ConfigEntry<EXPONENT_DISPLAY> configExponentDisplay;
	private static ConfigEntry<bool> configPrintUnitsWhenStarting;
	private static Dictionary<DataConfig.WorldUnitType, ConfigEntry<string>> unitConfigs = new Dictionary<DataConfig.WorldUnitType, ConfigEntry<string>>();

	static ManualLogSource log;

	private enum EXPONENT_DISPLAY { NORMAL, /*SCIENTIFIC, SCIENTIFIC_E,*/ EXPONENT, EXPONENT_E }

	private class UNIT_PREFIX {
		private int INDEX_NONE = -1;
		public readonly string[] PREFIX_LIST;

		public UNIT_PREFIX(string[] pPrefix_List) {
			PREFIX_LIST = pPrefix_List;
		}

		public int GetNoneIndex() {
			if (INDEX_NONE == -1) INDEX_NONE = Array.IndexOf(PREFIX_LIST, "");
			return INDEX_NONE;
		}
	}
	private class SI_UNIT_PREFIX : UNIT_PREFIX {
		public static readonly int q = -10, r = -9, y = -8, z = -7, a = -6, f = -5, p = -4, n = -3, µ = -2, m = -1, NONE = 0, k = 1, M = 2, G = 3, T = 4, P = 5, E = 6, Z = 7, Y = 8, R = 9, Q = 10;
		public SI_UNIT_PREFIX() : base(new string[] { "q", "r", "y", "z", "a", "f", "p", "n", "µ", "m", "", "k", "M", "G", "T", "P", "E", "Z", "Y", "R", "Q" }) { }
	}
	private class LARGE_NUMBERS_UNIT_PREFIX : UNIT_PREFIX {
		public LARGE_NUMBERS_UNIT_PREFIX() : base(new string[]{"", "k", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No", "Dc", "Ud", "Dd", "Td", "Qad", "Qid", "Sxd", "Spd", "Ocd", "Nod", "Vg", "Uvg", "Dvg", "Tvg", "Qavg", "Qivg", "Sxvg",
																"Spvg", "Ocvg", "Novg", "Tg", "Utg", "Dtg", "Ttg", "Qatg", "Qitg"}) { }
	}
	private class UnitAttr {
		public string unit;
		public int offset = 0;
		public List<string> prefix = null;
		public List<string> hardcodedUnits = null;
		public UNIT_PREFIX unit_prefix = null;

		public UnitAttr(string pUnit, UNIT_PREFIX pUnit_prefix = null) {
			this.unit = pUnit;
			unit_prefix = ((pUnit_prefix == null) ? new SI_UNIT_PREFIX() : pUnit_prefix);
		}

		public string ToUnitString() {
			List<string> builtUnits = new List<string>();
			if (this.prefix != null) builtUnits.AddRange(prefix);
			if (hardcodedUnits != null) {
				builtUnits.AddRange(hardcodedUnits);
			} else {
				builtUnits.AddRange(BuildUnitPrefixElements(this.unit, this.offset));
			}
			return string.Join(",", builtUnits);
		}

		private List<string> BuildUnitPrefixElements(string pUnit, int pOffset = 1) { // pOffset = 1 such that the elements start with k, ...
			List<string> builtUnitPrefixElements = new List<string>();

			for (int i = this.unit_prefix.GetNoneIndex() + pOffset; i < this.unit_prefix.PREFIX_LIST.Length; i++) builtUnitPrefixElements.Add(this.unit_prefix.PREFIX_LIST[i] + pUnit);

			return builtUnitPrefixElements;
		}

		public string BaseUnit() => (prefix != null && prefix.Count >= 1) ? prefix.First() : this.unit;
		
		//public int BaseOffset() => this.offset - (prefix?.Count ?? 0);
	}

	private static UnitAttr mass = new UnitAttr("t") { prefix = new List<string>() { "g", "kg" } };
	private static Dictionary<DataConfig.WorldUnitType, UnitAttr> units = new() {
		{ DataConfig.WorldUnitType.Terraformation,      new UnitAttr("Ti") },
		{ DataConfig.WorldUnitType.Oxygen,              new UnitAttr("mol")/*{ hardcodedUnits = "ppq,ppt,ppb,ppm,\u2030, ,k,M,G,T,P".Split(',').ToList() }*/ }, // because >1 mil ppm is impossible
		{ DataConfig.WorldUnitType.Heat,                new UnitAttr("°C" /*"K~"*/){ offset = SI_UNIT_PREFIX.p } }, // fits better for melting ice
		{ DataConfig.WorldUnitType.Pressure,            new UnitAttr("Pa"){ offset = SI_UNIT_PREFIX.n } },
		{ DataConfig.WorldUnitType.Biomass, mass },
		{ DataConfig.WorldUnitType.Plants, mass },
		{ DataConfig.WorldUnitType.Insects, mass },
		{ DataConfig.WorldUnitType.Animals, mass },
		{ DataConfig.WorldUnitType.Energy,              new UnitAttr("W"){ offset = SI_UNIT_PREFIX.k } },
		{ DataConfig.WorldUnitType.SystemTerraformation,new UnitAttr("SysTi", new LARGE_NUMBERS_UNIT_PREFIX()) },
		{ DataConfig.WorldUnitType.Purification,		new UnitAttr("Pu") }
	};
	private static Dictionary<DataConfig.WorldUnitType, UnitAttr> UnitDict = new Dictionary<DataConfig.WorldUnitType, UnitAttr>();

	/*
	 *	Oxygen: "ppq", "ppt", "ppb", "ppm"
	 *	Heat: "pK", "nK", "μK", "mK"
	 *	Pressure: "nPa", "μPa", "mPa", "Pa"
	 *	Energy: "kW"
	 *	Terraformation: "Ti", "kTi", "MTi", "GTi", "TTi", "PTi", "ETi", "ZTi", "YTi", "RTI", "QTI"
	 *	Plants: "g", "kg", "t", "kt", "mt", "Gt"
	 *	Insects: "g", "kg", "t", "kt", "mt", "Gt"
	 *	Animals: "g", "kg", "t", "kt", "mt", "Gt"
	 *	Biomass: "g", "kg", "t", "kt", "mt", "Gt", "Tt"
	 *	SystemTerraformation: "SysTi"
	 *	Purification: "Pu", "kPu", "MPu", "GPu", "TTPu", "PPu", "EPu", "ZPu"
	 */
	private void Awake() {
		// Plugin startup logic
		log = Logger;

		configActive = Config.Bind("General", "Enabled", true, "Enable mod");
		configDebug = Config.Bind("General", "Debug", false, "Enable Debug");
		//configExponent = Config.Bind("General", "ShowExponent", false, "Show Exponent instead of Unit");
		configExponentDisplay = Config.Bind("General", "ExponentDisplay", EXPONENT_DISPLAY.NORMAL, "NORMAL = Show the configured units; SCIENTIFIC = Show exponent relative to the prefix-less unit; EXPONENT = Show exponent relative to the base unit; Add '_E' to use 1.0E+1 instead of 1.0*10^1");
		configPrintUnitsWhenStarting = Config.Bind("General", "PrintUnitsWhenStarting", false, "Print all default units before setting the modded ones");

		foreach (KeyValuePair<DataConfig.WorldUnitType, UnitAttr> kvp in units) {
			unitConfigs[kvp.Key] = Config.Bind("Units", kvp.Key.ToString(), kvp.Value.ToUnitString(), "Unit for " + kvp.Key.ToString());
		}

		// Test for new WorldUnitTypes
		foreach (DataConfig.WorldUnitType wut in allWorldUnits) {
			if (!units.ContainsKey(wut)) {
				log.LogWarning("Missing unit definition for " + wut);
			}
		}

		Harmony.CreateAndPatchAll(typeof(Plugin));
		Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(WorldUnit), MethodType.Constructor, new Type[] { typeof(List<string>), typeof(DataConfig.WorldUnitType) })]
	public static void WorldUnit_ctor(ref List<string> unitLabels, DataConfig.WorldUnitType worldUnitType) {
		SetUnits(ref unitLabels, worldUnitType);
	}

	public static void SetUnits(ref List<string> unitLabels, DataConfig.WorldUnitType unitType) {
		if (!configActive.Value) return;

		if (configPrintUnitsWhenStarting.Value) log.LogInfo(unitType.ToString() + ": \"" + string.Join("\", \"", unitLabels) + "\"");

		if (unitConfigs.TryGetValue(unitType, out ConfigEntry<string> currentConfig)) {
			unitLabels = currentConfig.Value.Split(',').ToList();

			if (configDebug.Value) Console.WriteLine("Unit set for: " + unitType.ToString());
		}
	}

	//[HarmonyPrefix] // taken from v1.509
	//[HarmonyPatch(typeof(WorldUnit), nameof(WorldUnit.GetDisplayStringForValue), new Type[] {typeof(List<string>), typeof(double), typeof(bool), typeof(int), typeof(bool)})]
	//public static bool disabled_WorldUnit_GetDisplayStringForValue(List<string> unitLabels, double givenValue, bool roundFinalNum, int labelIndex, bool useExponent, ref string __result) {
	//	if (!configActive.Value || !configDisplayStringForValue.Value) return true;

	//	if (/*show SysTi as log value*/false && unitConfigs.TryGetValue(DataConfig.WorldUnitType.SystemTerraformation, out ConfigEntry<string> systiConfig)) {
	//		string systiLabel = systiConfig.Value.Split(',')[0];
	//		if (unitLabels.First() == systiLabel) {
	//			__result = string.Format("{0:0.00}", Math.Log(givenValue, 10)) + " " + systiLabel;
	//			return false;
	//		}
	//	}

	//	int unitIndex = ((labelIndex != 0) ? Math.Min(Math.Max(0, (int)Math.Floor(Math.Log(givenValue * 1.000005, 1000.0))), unitLabels.Count - 1) : labelIndex); // *1.000005 prevents 999.999... from being displayed as 1000.00
	//	double givenValueAdapted = ((unitIndex < 1) ? givenValue : (givenValue / Math.Pow(1000.0, (double)unitIndex)));
	//	string text = (roundFinalNum ? "{0:0}" : "{0:0.00}");
	//	__result = string.Format((unitIndex < 1) ? text : ((unitIndex == unitLabels.Count - 1 && Math.Round(givenValueAdapted, 2) >= 1000.0) ? "{0:0.##E+0}"/*"{0:0.00E+0}"*//*"{0:E2}"*/ : "{0:0.00}"), givenValueAdapted) + " " + unitLabels[unitIndex];
	//	return false;
	//}

	private static DataConfig.WorldUnitType SelectedWorldUnitType = DataConfig.WorldUnitType.Null;
	[HarmonyPrefix]
	[HarmonyPatch(typeof(WorldUnit), nameof(WorldUnit.GetDisplayStringForValue), new Type[] { typeof(double), typeof(bool), typeof(int) })]
	public static void Pre_WorldUnit_GetDisplayStringForValue(DataConfig.WorldUnitType ____unitType) {
		SelectedWorldUnitType = ____unitType;
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(SaveFileDisplayer), nameof(SaveFileDisplayer.SetData))]
	public static void Pre_SaveFileDisplayer_SetData() {
		SelectedWorldUnitType = DataConfig.WorldUnitType.Terraformation;
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(WorldUnit), nameof(WorldUnit.GetDisplayStringForValue), new Type[] { typeof(List<string>), typeof(double), typeof(bool), typeof(int), typeof(bool) })]
	public static bool Static_WorldUnit_GetDisplayStringForValue(List<string> unitLabels, double givenValue, /*bool roundFinalNum, int labelIndex, bool useExponent,*/ ref string __result) {
		if (!configActive.Value) return true;

		/*if (SelectedWorldUnitType == DataConfig.WorldUnitType.Null) {
			if (configDebug.Value) log.LogWarning("Static_WorldUnit_GetDisplayStringForValue called from " + new System.Diagnostics.StackTrace(true).ToString());
			return true;
		}*/ // Doesn't work properly because of inlining of the setting methods... 

		switch (configExponentDisplay.Value) {
			case EXPONENT_DISPLAY.NORMAL:
				return true;
			case EXPONENT_DISPLAY.EXPONENT_E:
				__result = string.Format("{0:0.##E+0}", givenValue) + " " + unitLabels[0];
				return false;
			case EXPONENT_DISPLAY.EXPONENT:
				__result = string.Format("{0:0.##E+0}", givenValue).Replace("+", "").Replace("E", "*10^") + " " + unitLabels[0];
				return false;
			// doesn't work properly:
			/*case EXPONENT_DISPLAY.SCIENTIFIC_E:
				UnitAttr ua_e = units[SelectedWorldUnitType];
				__result = string.Format("{0:0.##E+0}", givenValue * MathF.Pow(1000, ua_e.BaseOffset())) + " " + ua_e.BaseUnit();
				return false;
			case EXPONENT_DISPLAY.SCIENTIFIC:
				UnitAttr ua = units[SelectedWorldUnitType];
				__result = string.Format("{0:0.##E+0}", givenValue * MathF.Pow(1000, ua.BaseOffset())).Replace("+", "").Replace("E", "*10^") + " " + ua.BaseUnit();
				return false;*/
		}
		return true;
	}
	[HarmonyPostfix]
	[HarmonyPatch(typeof(WorldUnit), nameof(WorldUnit.GetDisplayStringForValue), new Type[] { typeof(double), typeof(bool), typeof(int) })]
	public static void Post_WorldUnit_GetDisplayStringForValue() {
		SelectedWorldUnitType = DataConfig.WorldUnitType.Null;
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(SaveFileDisplayer), nameof(SaveFileDisplayer.SetData))]
	public static void Post_SaveFileDisplayer_SetData() {
		SelectedWorldUnitType = DataConfig.WorldUnitType.Null;
	}


}
/* Units pre refractor / pre v1.507dev:
EnergyUnits = kW,MW,GW,TW,PW,EW,ZW,YW,RW,QW

TerraformationUnits = Ti,kTi,MTi,GTi,TTi,PTi,ETi,ZTi,YTi,RTi,QTi

OxygenUnits = mol,kmol,Mmol,Gmol,Tmol,Pmol,Emol,Zmol,Ymol,Rmol,Qmol

HeatUnits = p°C,n°C,μ°C,m°C,°C,k°C,M°C,G°C,T°C,P°C,E°C,Z°C,Y°C,R°C,Q°C

PressureUnits = nPa,μPa,mPa,Pa,kPa,MPa,GPa,TPa,PPa,EPa,ZPa,YPa,RPa,QPa

BiomassUnits = g,kg,t,kt,Mt,Gt,Tt,Pt,Et,Zt,Yt,Rt,Qt

PlantsUnits = g,kg,t,kt,Mt,Gt,Tt,Pt,Et,Zt,Yt,Rt,Qt

InsectsUnits = g,kg,t,kt,Mt,Gt,Tt,Pt,Et,Zt,Yt,Rt,Qt

AnimalsUnits = g,kg,t,kt,Mt,Gt,Tt,Pt,Et,Zt,Yt,Rt,Qt
*/
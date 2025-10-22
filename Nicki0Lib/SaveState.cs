// Copyright 2025-2025 Nicolas Schäfer & Contributors
// Licensed under Apache License, Version 2.0

using BepInEx;
using SpaceCraft;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;

namespace Nicki0 {

	public class SaveState {
		private static readonly int Nicki0IdSpace = 140_0000_0;
		/// <summary>
		/// Method <c>GenerateId</c> Generate an id
		/// </summary>
		public static int GenerateId(Type PluginType) {
			return GenerateId(MetadataHelper.GetMetadata(PluginType).GUID);
		}
		/// <summary>
		/// Method <c>GenerateId</c> Generate an id
		/// </summary>
		public static int GenerateId(string id) {
			return Nicki0IdSpace + ((id.GetStableHashCode() % 10000 + 10000) % 10000) * 10;
		}

		public static bool GetAndCreateStateObject(int id, out WorldObject stateObject) {
			stateObject = WorldObjectsHandler.Instance?.GetWorldObjectViaId(id);
			if (stateObject == null) {
				stateObject = WorldObjectsHandler.Instance?.CreateNewWorldObject(GroupsHandler.GetGroupViaId("Container2"), id);
				stateObject?.SetText("");
			}
			stateObject?.SetDontSaveMe(false);
			return stateObject != null;
		}
		public static bool GetStateObject(int id, out WorldObject stateObject) {
			stateObject = WorldObjectsHandler.Instance?.GetWorldObjectViaId(id);
			stateObject?.SetDontSaveMe(false);
			return stateObject != null;
		}
		public static void SetDictionaryListData(WorldObject wo, Dictionary<string, List<string>> data) {
			wo.SetText(string.Join(";", data.Keys.Select(e => e + ":" + string.Join(",", data[e]))));
		}
		public static void SetDictionaryListData<K, T>(WorldObject wo, Dictionary<K, IEnumerable<T>> data, Func<K, string> KFunc = null, Func<T, string> TFunc = null) {
			KFunc ??= e => e.ToString();
			TFunc ??= e => e.ToString();
			wo.SetText(string.Join(";", data.Keys.Select(k => KFunc(k) + ":" + string.Join(",", data[k].Select(TFunc)))));
		}
		public static Dictionary<string, List<string>> GetDictionaryListData(WorldObject wo) {
			Dictionary<string, List<string>> dict = new Dictionary<string, List<string>>();
			foreach (string keyAndList in wo.GetText().Split(";")) {
				string[] keyAndListArray = keyAndList.Split(":");
				Assert.IsTrue(keyAndListArray.Count() == 2, nameof(SaveState.GetDictionaryListData).ToString() + ": Wrong data format: there may only be one key and list");
				dict[keyAndListArray[0]] = new List<string>(keyAndListArray[1].Split(","));
			}
			return dict;
		}

		public static void SetDictionaryData<K, T>(WorldObject wo, Dictionary<K, T> data, Func<K, string> KFunc = null, Func<T, string> TFunc = null) {
			KFunc ??= e => e.ToString();
			TFunc ??= e => e.ToString();
			wo.SetText(string.Join(";", data.Keys.Select(k => KFunc(k) + ":" + TFunc(data[k]))));
		}
		public static Dictionary<K, T> GetDictionaryData<K, T>(WorldObject wo, Func<string, K> KFunc, Func<string, T> TFunc) {
			string text = wo.GetText();
			Dictionary<K, T> dict = new Dictionary<K, T>();
			if (string.IsNullOrEmpty(text)) return dict;

			foreach (string keyAndValue in text.Split(";")) {
				string[] keyAndValueArray = keyAndValue.Split(":");
				Assert.IsTrue(keyAndValueArray.Count() == 2, nameof(SaveState.GetDictionaryData).ToString() + ": Wrong data format: there may only be one key and list");
				dict[KFunc(keyAndValueArray[0])] = TFunc(keyAndValueArray[1]);
			}
			return dict;
		}

		public static void SetListData<T>(WorldObject wo, List<T> data, Func<T, string> TFunc = null, string separator = ",") {
			TFunc ??= e => e.ToString();
			wo.SetText(string.Join(separator, data.Select(TFunc)));
		}
		public static List<T> GetListData<T>(WorldObject wo, Func<string, T> TFunc, string separator = ",") {
			return wo.GetText().Split(separator).Select(TFunc).ToList();
		}
		public static List<string> GetListData(WorldObject wo) => GetListData<string>(wo, e => e);
	}
}

// Copyright 2025-2026 Nicolas Schäfer & Contributors
// Licensed under Apache License, Version 2.0

using BepInEx;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SpaceCraft;
using System;
using System.ComponentModel;

namespace Nicki0 {

	public class SaveState {

		/*
		 * TODO: 
		 * 
		 * 
		 * 
		 */

		/// <summary>
		/// 0: individual
		/// 1: json
		/// </summary>
		private static readonly int StateFormatVersion = 1;
		// private readonly JsonSerializerSettings serializerSettings = new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, NullValueHandling = NullValueHandling.Ignore };

		class State {
			public int stateFormatVersion; // State Format Version
			public int dataFormatVersion; // Data Format Version
			public JToken data; // Data
		}

		public enum ERROR_CODE {
			SUCCESS,
			INVALID_JSON,
			OLD_DATA_FORMAT,
			NEWER_DATA_FORMAT,
			STATE_OBJECT_MISSING // <- can be used if the object has to be initialized; remember: Can happen in played/progressed save file
		}

		Type PluginType;
		int DataFormatVersion;

		public readonly int id;

		public SaveState(Type pPluginType, int pDataFormatVersion, int? pId = null) {
			PluginType = pPluginType;
			DataFormatVersion = pDataFormatVersion;


			id = (pId.HasValue ? pId.Value : GenerateId(PluginType));

			BepInPlugin bip = MetadataHelper.GetMetadata(PluginType);
			Log(bip.Name + " uses id " + id);
		}


		private static readonly int Nicki0IdSpace = 140_0000_0;
		/// <summary>
		/// Method <c>GenerateId</c> Generate an id
		/// </summary>
		private static int GenerateId(Type PluginType) {
			// Console.WriteLine("--- " + MetadataHelper.GetMetadata(PluginType).Name + " uses id " + GenerateId(MetadataHelper.GetMetadata(PluginType).GUID) + " ---");
			return GenerateId(MetadataHelper.GetMetadata(PluginType).GUID);
		}
		/// <summary>
		/// Method <c>GenerateId</c> Generate an id
		/// </summary>
		private static int GenerateId(string id) {
			return Nicki0IdSpace + ((id.GetStableHashCode() % 10000 + 10000) % 10000) * 10;
		}

		public ERROR_CODE GetDataFormatVersion(out int dataFormatVersion) {
			if (GetStateObject(out WorldObject wo)) {
				try {
					State state = JsonConvert.DeserializeObject<State>(wo.GetText()/*, serializerSettings*/);

					if (state == null) {
						dataFormatVersion = default;
						return ERROR_CODE.INVALID_JSON;
					}

					dataFormatVersion = state.dataFormatVersion;

					if (dataFormatVersion < this.DataFormatVersion) return ERROR_CODE.OLD_DATA_FORMAT;
					if (dataFormatVersion > this.DataFormatVersion) return ERROR_CODE.NEWER_DATA_FORMAT;
					return ERROR_CODE.SUCCESS;
				} catch {
					dataFormatVersion = default;
					return ERROR_CODE.INVALID_JSON;
				}
			}
			dataFormatVersion = default;
			return ERROR_CODE.STATE_OBJECT_MISSING;
		}

		private bool GetStateObject(out WorldObject stateObject) {
			stateObject = WorldObjectsHandler.Instance?.GetWorldObjectViaId(id);
			stateObject?.SetDontSaveMe(false);
			return stateObject != null;
		}

		private bool CreateStateObject(out WorldObject stateObject) {
			stateObject = WorldObjectsHandler.Instance?.GetWorldObjectViaId(id);
			if (stateObject != null) {
				stateObject.SetDontSaveMe(false);
				return true; // object exists, so it can be accessed
			} else {
				stateObject = WorldObjectsHandler.Instance.CreateNewWorldObject(GroupsHandler.GetGroupViaId("Container2"), id);
				stateObject?.SetDontSaveMe(false);
				return stateObject != null;
			}
		}

		public bool SetData<T>(T data) {
			if (data == null) {
				Log("data is null!");
				return false;
			}

			BepInPlugin bip = MetadataHelper.GetMetadata(PluginType);
			State state = new State();
			state.stateFormatVersion = StateFormatVersion;
			state.dataFormatVersion = this.DataFormatVersion;
			state.data = JToken.FromObject(data);
			/*using (SHA256 sha = SHA256.Create()) {
				byte[] hashValue = sha.ComputeHash(Encoding.UTF8.GetBytes(state.data));
				state.dhash = Convert.ToBase64String(hashValue);
			}*/

			string serializedData = JsonConvert.SerializeObject(state/*, serializerSettings*/);
			if (serializedData.Contains("@") || serializedData.Contains("|")) {
				Log("String contains invalid characters");
				return false;
			}

			if (GetStateObject(out WorldObject wo)) {
				wo.SetText(serializedData);
				return true;
			} else if (WorldObjectsHandler.Instance != null) {
				if (CreateStateObject(out WorldObject newWO)) {
					newWO.SetText(serializedData);
					return true;
				} else {
					return false;
				}
			}
			return false;
		}

		public ERROR_CODE GetData<T>(out T data) {
			if (GetStateObject(out WorldObject wo)) {
				try {
					State state = JsonConvert.DeserializeObject<State>(wo.GetText() ?? ""/*, serializerSettings*/);
					if (state == null) {
						data = default(T);
						return ERROR_CODE.INVALID_JSON;
					}

					if (state.stateFormatVersion != StateFormatVersion) {
						/*
						 * TODO: Implement handling of older versions
						 */
					}

					if (state.dataFormatVersion != this.DataFormatVersion) {
						data = default(T);
						return state.dataFormatVersion < this.DataFormatVersion ? ERROR_CODE.OLD_DATA_FORMAT : ERROR_CODE.NEWER_DATA_FORMAT;
					}

					data = state.data.ToObject<T>();//JsonConvert.DeserializeObject<T>(state.data);

					return data != null ? ERROR_CODE.SUCCESS : ERROR_CODE.INVALID_JSON;
				} catch {
					data = default(T);
					return ERROR_CODE.INVALID_JSON;
				}
			} else {
				data = default(T);
				return ERROR_CODE.STATE_OBJECT_MISSING;
			}
		}

		private void Log(string message) {
			Console.WriteLine("[SaveState] " + message);
		}
	}
}

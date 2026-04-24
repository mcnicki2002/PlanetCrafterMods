using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace UpdateNexusModsWithAPI {
	partial class Program {
		// --- Get mod --->
		class GetMod_Response {
			public GetMod_Data data { get; set; }
		}
		class GetMod_Data {
			public string id { get; set; }
			public string game_scoped_id { get; set; }
			public string game_id { get; set; }
			public string name { get; set; }
		}
		static async Task GetMod(ModFileInfo mod) {
			using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, $"https://api.nexusmods.com/v3/games/{mod.game_domain}/mods/{mod.game_scoped_id}")) {
				requestMessage.Headers.Add("apiKey", apiKey);

				HttpResponseMessage response = await client.SendAsync(requestMessage);
				Console.WriteLine(await response.Content.ReadAsStringAsync());
				response.EnsureSuccessStatusCode();
				response.Headers.PrintLimit();

				GetMod_Response resp = await response.Content.ReadFromJsonAsync<GetMod_Response>();

				mod.getMod___id = resp.data.id;
			}
		}
		// <--- Get mod ---

		// --- Get mod file update groups --->
		class GetModFileUpdateGroups_Response {
			public GetModFileUpdateGroups_Data data { get; set; }
		}
		class GetModFileUpdateGroups_Data {
			public GetModFileUpdateGroups_Groups[] groups { get; set; }
		}
		class GetModFileUpdateGroups_Groups {
			public string id { get; set; }
			public string name { get; set; }
			public bool is_active { get; set; }
			public string last_file_uploaded_at { get; set; }
			public int versions_count { get; set; }
			public int archived_count { get; set; }
			public int removed_count { get; set; }
		}
		static async Task GetModFileUpdateGroups(ModFileInfo mod) {
			using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, $"https://api.nexusmods.com/v3/mods/{mod.getMod___id}/file-update-groups")) {
				requestMessage.Headers.Add("apiKey", apiKey);

				HttpResponseMessage response = await client.SendAsync(requestMessage);
				Console.WriteLine(await response.Content.ReadAsStringAsync());
				response.EnsureSuccessStatusCode();
				response.Headers.PrintLimit();

				GetModFileUpdateGroups_Response resp = await response.Content.ReadFromJsonAsync<GetModFileUpdateGroups_Response>();

				//mod.getMod___id = resp.data.id;
			}
		}
		// <--- Get mod file update groups ---

		// --- Get file update group versions --->
		class GetFileUpdateGroupVersions_Response {
			public GetFileUpdateGroupVersions_Data data { get; set; }
		}
		class GetFileUpdateGroupVersions_Data {
			public GetFileUpdateGroupVersions_Versions[] versions { get; set; }
		}
		class GetFileUpdateGroupVersions_Versions {
			public string id { get; set; }
			public string position { get; set; }
			public GetFileUpdateGroupVersions_File file { get; set; }
		}
		public class GetFileUpdateGroupVersions_File {
			public string id { get; set; }
			public string game_scoped_id { get; set; }
			public string name { get; set; }
			public string version { get; set; }
			public string category { get; set; }
			public string uploaded_at { get; set; }
		}
		static async Task<string> GetFileUpdateGroupVersions(ModFileInfo mod) {
			using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, $"https://api.nexusmods.com/v3/file-update-groups/{mod.updateGroupId}/versions")) {
				requestMessage.Headers.Add("apiKey", apiKey);

				HttpResponseMessage response = await client.SendAsync(requestMessage);
				Console.WriteLine(await response.Content.ReadAsStringAsync());
				response.EnsureSuccessStatusCode();
				response.Headers.PrintLimit();

				GetFileUpdateGroupVersions_Response resp = await response.Content.ReadFromJsonAsync<GetFileUpdateGroupVersions_Response>();

				return resp.data.versions.First().file.version;
			}
		}
		// <--- Get file update group versions ---
	}
}

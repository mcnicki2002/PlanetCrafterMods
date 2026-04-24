using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace UpdateNexusModsWithAPI {
	partial class Program {
		// --- Get mod file --->
		class GetModFile_Response {
			public GetModFile_Data data { get; set; }
		}
		class GetModFile_Data {
			public string id { get; set; }
			public string game_scoped_id { get; set; }
			public string game_id { get; set; }
			public string mod_game_scoped_id { get; set; }
			public GetModFile_UpdateGroupVersion update_group_version { get; set; }

		}
		class GetModFile_UpdateGroupVersion {
			public string position { get; set; }
			public string group_id { get; set; }
		}

		static async Task GetModFile(ModFileInfo mod) {
			using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, $"https://api.nexusmods.com/v3/games/{mod.game_domain}/mod-files/{mod.game_scoped_id}")) {
				requestMessage.Headers.Add("apiKey", apiKey);

				HttpResponseMessage response = await client.SendAsync(requestMessage);
				Console.WriteLine(await response.Content.ReadAsStringAsync());
				response.EnsureSuccessStatusCode();
				response.Headers.PrintLimit();

				GetModFile_Response resp = await response.Content.ReadFromJsonAsync<GetModFile_Response>();

				//mod.getMod___id = resp.data.id;
			}
		}
		// <--- Get mod file ---



		// --- Create a new update group version (updates a mod file) --->
		enum FILECATEGORY {
			main,
			optional,
			miscellaneous
		}
		class CreateNewUpdateGroupVersion_Config {
			public string upload_id { get; set; }
			public string name { get; set; }
			public string description { get; set; }
			public string version { get; set; }
			public string file_category { get; set; }
		}
		class CreateNewUpdateGroupVersion_Response {
			public CreateNewUpdateGroupVersion_Data data { get; set; }
		}
		class CreateNewUpdateGroupVersion_Data {
			public string id { get; set; }
			public string game_scoped_id { get; set; }
			public string name { get; set; }
			public string file_category { get; set; }
		}
		static async Task CreateNewUpdateGroupVersion(ModFileInfo mod) {
			using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, $"https://api.nexusmods.com/v3/mod-file-update-groups/{mod.updateGroupId}/versions")) {
				requestMessage.Headers.Add("apiKey", apiKey);

				CreateNewUpdateGroupVersion_Config cfg = new CreateNewUpdateGroupVersion_Config() {
					upload_id = mod.upload___id,
					name = Path.GetFileNameWithoutExtension(mod.file.FullName) + (mod.devBranch ? "-DevBranch" : ""),
					description = mod.devBranch ? $"For Dev-Branch {GAME_VERSION}" : "",
					version = mod.version,
					file_category = (mod.devBranch ? FILECATEGORY.miscellaneous : FILECATEGORY.main).ToString()
				};

				string content = JsonSerializer.Serialize<CreateNewUpdateGroupVersion_Config>(cfg);
				Console.WriteLine($"Sent content: {content}");

				requestMessage.Content = new StringContent(content, Encoding.UTF8, "application/json");

				HttpResponseMessage response = await client.SendAsync(requestMessage);
				Console.WriteLine(await response.Content.ReadAsStringAsync());
				response.EnsureSuccessStatusCode();
				response.Headers.PrintLimit();

				CreateNewUpdateGroupVersion_Response resp = await response.Content.ReadFromJsonAsync<CreateNewUpdateGroupVersion_Response>();
			}
		}
		// <--- Create a new update group version (updates a mod file) ---
	}
}

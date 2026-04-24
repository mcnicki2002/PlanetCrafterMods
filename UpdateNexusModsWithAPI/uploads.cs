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
		// --- Get upload --->
		class GetUpload_Response {
			public GetUpload_Data data { get; set; }
		}
		class GetUpload_Data {
			public string id { get; set; }
			public GetUpload_User user { get; set; }
			public string state { get; set; }
		}
		class GetUpload_User {
			public string id { get; set; }
		}
		static async Task<string> GetUpload(ModFileInfo mod) {
			using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, $"https://api.nexusmods.com/v3/uploads/{mod.upload___id}")) {
				requestMessage.Headers.Add("apiKey", apiKey);

				HttpResponseMessage response = await client.SendAsync(requestMessage);
				Console.WriteLine(await response.Content.ReadAsStringAsync());
				response.EnsureSuccessStatusCode();
				response.Headers.PrintLimit();

				GetUpload_Response resp = await response.Content.ReadFromJsonAsync<GetUpload_Response>();
				return resp.data.state;
			}
		}
		// <--- Get upload ---

		// --- Create Upload --->
		class CreateUpload_Response {
			public CreateUpload_Data data { get; set; }
		}
		class CreateUpload_Data {
			public string id { get; set; }
			public CreateUpload_User user { get; set; }
			public string state { get; set; }
			public string presigned_url { get; set; }
		}
		class CreateUpload_User {
			public string id { get; set; }
		}
		class CreateUpload_Config {
			public string size_bytes { get; set; }
			public string filename { get; set; }
		}
		static async Task CreateUpload(ModFileInfo mod) {
			using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, $"https://api.nexusmods.com/v3/uploads")) {
				requestMessage.Headers.Add("apiKey", apiKey);

				CreateUpload_Config cfg = new CreateUpload_Config() {
					size_bytes = mod.file.Length.ToString(),
					filename = mod.file.Name
				};
				string content = JsonSerializer.Serialize(cfg);
				Console.WriteLine($"Sent content for CreateUpload: {content} ");
				requestMessage.Content = new StringContent(content, Encoding.UTF8, "application/json");

				HttpResponseMessage response = await client.SendAsync(requestMessage);
				Console.WriteLine(await response.Content.ReadAsStringAsync());
				response.EnsureSuccessStatusCode();
				response.Headers.PrintLimit();

				CreateUpload_Response resp = await response.Content.ReadFromJsonAsync<CreateUpload_Response>();

				mod.upload___id = resp.data.id;
				mod.upload___presigned_url = resp.data.presigned_url;
			}
		}
		static async Task PutUpload(ModFileInfo mod) {

			using (FileStream fs = mod.file.OpenRead()) {
				StreamContent content = new StreamContent(fs);
				content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
				HttpResponseMessage response = await client.PutAsync(mod.upload___presigned_url, content);
				Console.WriteLine(await response.Content.ReadAsStringAsync());
				response.EnsureSuccessStatusCode();
				response.Headers.PrintLimit();
			}
		}
		// <--- Create Upload ---

		// --- Finalize upload --->
		static async Task FinalizeUpload(ModFileInfo mod) {

			using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, $"https://api.nexusmods.com/v3/uploads/{mod.upload___id}/finalise")) {
				requestMessage.Headers.Add("apiKey", apiKey);

				HttpResponseMessage response = await client.SendAsync(requestMessage);
				Console.WriteLine(await response.Content.ReadAsStringAsync());
				response.EnsureSuccessStatusCode();
				response.Headers.PrintLimit();

				/* TODO: parse answer */
			}
		}
		// <--- Finalize upload ---
	}
}

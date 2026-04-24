using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace UpdateNexusModsWithAPI {
	partial class Program {

		// --- Config --->
		public static bool pushForDevBranch = true;
		public static string GAME_VERSION = "v2.007";
		public static string configFilePath = "D:/NexusCFG.json";
		// <--- Config ---


		static string apiKey = "";
		public static string author_prefix = "";
		public static readonly string GAME_DOMAIN = "planetcrafter";


		static HttpClient client = new HttpClient();

		class ModFileInfo {
			public ModFileInfo(Configs_Mod mod, string folderPath, string version) : this(Path.Combine(folderPath, mod.file), version/*mod.version*/, mod.game_scoped_id, mod.updateGroupId, mod.forDevBranch) { }
			public ModFileInfo(string pathToFile, string version, string game_scoped_id, string updateGroupId, bool forDevBranch) {
				this.game_domain = GAME_DOMAIN;
				this.game_scoped_id = game_scoped_id;
				this.devBranch = forDevBranch;
				this.file = new FileInfo(pathToFile);
				this.updateGroupId = updateGroupId;
				this.version = version;
			}
			public FileInfo file;
			public bool devBranch;
			public string updateGroupId;
			public string version;

			public string game_domain;
			public string game_scoped_id;

			public string getMod___id;

			public string upload___id;
			public string upload___presigned_url;
		}


		class Configs_Mod {
			public string file { get; set; }
			//public string version { get; set; }
			public string game_scoped_id { get; set; }
			public string updateGroupId { get; set; }
			public bool forDevBranch { get; set; }
		}
		class Configs {
			public string apiKey { get; set; }
			public string authorPrefix { get; set; }
			public string zipPath { get; set; }
			public string projectsPath { get; set; }
			public Configs_Mod[] Mods { get; set; }
		}

		private static void Main(string[] args) {



			FileInfo configFile = new FileInfo(configFilePath);
			if (!configFile.Exists) {
				Console.WriteLine("config file is missing");

				string text = JsonSerializer.Serialize(new Configs {
					apiKey = "fdas",
					authorPrefix = "YourName-",
					zipPath = "D:/ZIPs",
					projectsPath = "D:/",
					Mods = new Configs_Mod[] {
					new Configs_Mod() {
						file = "TESTFILE.zip",
						game_scoped_id = "999",
						updateGroupId = "3141593",
						forDevBranch = true
					}
				}
				}, new JsonSerializerOptions() { WriteIndented = true });
				Console.WriteLine($"Example config file:\n{text}");

				return;
			}

			string configFileContent = File.ReadAllText(configFile.FullName);

			Configs config = JsonSerializer.Deserialize<Configs>(configFileContent);
			Console.WriteLine(config.apiKey);

			apiKey = config.apiKey;
			author_prefix = config.authorPrefix;

			string projectsDir = Path.GetFullPath(config.projectsPath);
			Console.WriteLine($"workdir: {projectsDir}");


			Dictionary<string, ModInfos> modInfos = LoadModInfos(projectsDir, author_prefix);
			foreach (KeyValuePair<string, ModInfos> inf in modInfos) {
				Console.WriteLine($"{inf.Key} : {inf.Value.AssemblyName} : {inf.Value.Version}");
			}






			List<ModFileInfo> modFileInfos = new List<ModFileInfo>();
			foreach (Configs_Mod el in config.Mods) {
				if (el.forDevBranch != pushForDevBranch) { continue; }

				if (!modInfos.TryGetValue(el.file, out ModInfos info)) {
					Console.WriteLine($"Didn't find version for {el.file}");
					continue;
				}
				modFileInfos.Add(new ModFileInfo(el, config.zipPath, info.Version));
			}


			foreach (ModFileInfo modFileInfo in modFileInfos) {
				RunAsync(modFileInfo).GetAwaiter().GetResult();
			}
		}

		class ModInfos {
			public string AssemblyName;
			public string Version;
		}
		static Dictionary<string, ModInfos> LoadModInfos(string projectDirPath, string zipPrefix) {
			Dictionary<string, ModInfos> modInfos = new Dictionary<string, ModInfos>();


			// Modified from ZapModVersions by akarnokd
			string versionTag = "<Version>(.+?)</Version>";
			Regex versionRegex = new Regex(versionTag);

			string workdir = projectDirPath;
			Console.WriteLine("Checking projects in " + workdir);

			HashSet<string> excludedMods = ["XTestPlugins", "ZipRest"];

			foreach (string dir in Directory.EnumerateDirectories(workdir)) {
				string d = Path.GetFileName(dir);
				string csprojFile = Path.Combine(dir, Path.GetFileName(dir) + ".csproj");

				if (excludedMods.Contains(d) || !File.Exists(csprojFile)) { continue; }
				if (!File.Exists(csprojFile)) { continue; }

				string csproj = File.ReadAllText(csprojFile);

				Match m3 = versionRegex.Match(csproj);
				if (m3.Success) {
					Console.WriteLine(csprojFile);
					string assName = Regex.Match(csproj,
							@"<AssemblyName\s*>(.*?)</AssemblyName>",
							RegexOptions.IgnoreCase | RegexOptions.Singleline).Groups[1].Value;

					string dirName = Regex.Match(csproj,
							@"<Description\s*>(.*?)</Description>",
							RegexOptions.IgnoreCase | RegexOptions.Singleline).Groups[1].Value;

					Match deployMatch = Regex.Match(csproj,
							@"<Deploy\s*>(.*?)</Deploy>",
							RegexOptions.IgnoreCase | RegexOptions.Singleline);
					if (!m3.Success || (author_prefix == "Nicki0-" && !deployMatch.Groups[1].Value.Contains("true", StringComparison.InvariantCultureIgnoreCase))) {
						Console.WriteLine($"{csprojFile} is not deployed");
						continue;
					}

					modInfos.Add(zipPrefix + assName + ".zip", new ModInfos() {
						AssemblyName = assName,
						Version = m3.Groups[1].Value
					});
				}
			}


			return modInfos;
		}

		static async Task RunAsync(ModFileInfo mod) {

			//await GetMod(mod);
			//await GetUpload(mod);


			//await GetModFileUpdateGroups(mod);
			string latestNexusVersion = await GetFileUpdateGroupVersions(mod);
			if (new Version(latestNexusVersion) >= new Version(mod.version)) {
				Console.WriteLine($"For mod {mod.file.Name}: Nexus version {latestNexusVersion} >= local version {mod.version}");
				return;
			}



			await CreateUpload(mod);
			await PutUpload(mod);
			await FinalizeUpload(mod);
			do {
				await Task.Delay(1000);
			} while (!(await GetUpload(mod)).Contains("available"));
			await CreateNewUpdateGroupVersion(mod);

		}



	}

	static class Extension {
		public static void PrintLimit(this HttpResponseHeaders header) {
			if (header == null) { return; }

			header.TryGetValues("x-rl-hourly-remaining", out IEnumerable<string> hourlyRemaining);
			header.TryGetValues("x-rl-hourly-limit", out IEnumerable<string> hourlyLimit);
			header.TryGetValues("x-rl-daily-remaining", out IEnumerable<string> dailyRemaining);
			header.TryGetValues("x-rl-daily-limit", out IEnumerable<string> dailyLimit);

			if (hourlyRemaining != null && hourlyLimit != null && dailyRemaining != null && dailyLimit != null) {
				Console.WriteLine($"{hourlyRemaining.First()} / {hourlyLimit.First()} ::: {dailyRemaining.First()} / {dailyLimit.First()}");
			}
		}
	}
}
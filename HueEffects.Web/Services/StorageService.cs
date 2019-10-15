using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Hosting;

namespace HueEffects.Web.Services
{
	public class StorageService
	{
		private readonly IWebHostEnvironment _environment;

		public StorageService(IWebHostEnvironment hostingEnvironment)
		{
			_environment = hostingEnvironment;
		}

		public async Task SaveConfig<TConfig>(TConfig config) => await File.WriteAllTextAsync(GetPath<TConfig>(), JsonConvert.SerializeObject(config, Formatting.Indented));

		public async Task<TConfig> LoadConfig<TConfig>() where TConfig : new()
		{
			var path = GetPath<TConfig>();
			if (File.Exists(path))
			{
				var s = await File.ReadAllTextAsync(path);
				return JsonConvert.DeserializeObject<TConfig>(s);
			}
			else
				return new TConfig();
		}

		private string GetPath<TConfig>() => Path.Combine(_environment.ContentRootPath, typeof(TConfig).Name + ".json");
	}
}

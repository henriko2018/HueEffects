using System.Threading.Tasks;
using System.IO;
using HueEffects.Web.Models;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace HueEffects.Web.Services
{
    public interface IStorageService
    {
        Task SaveConfig(EffectConfig config);
        Task<TConfig> LoadConfig<TConfig>() where TConfig : EffectConfig, new();
    }

    public class StorageService : IStorageService
    {
		private readonly IWebHostEnvironment _environment;
        private readonly ILogger<StorageService> _logger;

        public StorageService(IWebHostEnvironment hostingEnvironment, ILogger<StorageService> logger)
        {
            _environment = hostingEnvironment;
            _logger = logger;
        }

        public async Task SaveConfig(EffectConfig config)
        {
            var path = GetPath(config);
            var s = JsonConvert.SerializeObject(config, Formatting.Indented);
            await File.WriteAllTextAsync(path, s);
            _logger.LogDebug("Config saved to {path}: {config}", path, s);
        }

        public async Task<TConfig> LoadConfig<TConfig>() where TConfig : EffectConfig, new()
		{
            var config = new TConfig();
			var path = GetPath(config);
			if (File.Exists(path))
			{
				var s = await File.ReadAllTextAsync(path);
                _logger.LogDebug("Config loaded from {path}: {config}", path, s);
                return JsonConvert.DeserializeObject<TConfig>(s);
			}

            return config;
        }

		private string GetPath(EffectConfig config) => Path.Combine(_environment.ContentRootPath, config.GetType().Name + ".json");
	}
}

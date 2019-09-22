using System.Threading;
using System.Threading.Tasks;
using HueEffects.Web.EffectHandlers;
using Microsoft.Extensions.Logging;

namespace HueEffects.Web.Services
{
	public class BackgroundService : Microsoft.Extensions.Hosting.BackgroundService
    {
        private readonly ILogger<BackgroundService> _logger;
		private readonly StorageService _storageService;

		public EffectHandler ActiveHandler { get; private set; }

		public BackgroundService(ILogger<BackgroundService> logger, StorageService storageService)
        {
            _logger = logger;
			_storageService = storageService;
			_logger.LogDebug("ctor");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Started and waiting for cancellation.");
            try
            {
                await Task.Delay(-1, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("Received stop signal.");
				ActiveHandler.Stop();
                _logger.LogInformation("Finished cleaning up.");
            }
        }

        public async Task StartEffect<TConfig>(TConfig config, EffectHandler handler)
        {
			await _storageService.SaveConfig(config);

			// Stop previous handler if there is one
			if (ActiveHandler != null)
			{
				ActiveHandler.Stop();
				ActiveHandler = null;
			}

			handler.Start();
			ActiveHandler = handler;
		}
	}
}

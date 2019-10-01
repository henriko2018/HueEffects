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
        private CancellationTokenSource _effectCancellationTokenSource;

		public EffectHandler ActiveHandler { get; private set; }

		public BackgroundService(ILogger<BackgroundService> logger, StorageService storageService)
        {
            _logger = logger;
			_storageService = storageService;
			_logger.LogDebug("ctor");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("Started and waiting for stop signal.");
				await Task.Delay(-1, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("Received stop signal.");
                // Cancel effect handler if there is one.
                _effectCancellationTokenSource?.Cancel();
				_logger.LogInformation("Finished cleaning up.");
            }
        }

        public async Task StartEffect<TConfig>(TConfig config, EffectHandler handler)
        {
			await _storageService.SaveConfig(config);

			// Cancel previous handler if there is one
			_effectCancellationTokenSource?.Cancel();

			// Start the new handler
			_effectCancellationTokenSource = new CancellationTokenSource();
			handler.Start(_effectCancellationTokenSource.Token);
            ActiveHandler = handler;
        }
	}
}

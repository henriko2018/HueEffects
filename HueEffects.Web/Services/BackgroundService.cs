using System.Threading;
using System.Threading.Tasks;
using HueEffects.Web.EffectHandlers;
using HueEffects.Web.Models;
using Microsoft.Extensions.Logging;
using Q42.HueApi.Interfaces;

namespace HueEffects.Web.Services
{
	public class BackgroundService : Microsoft.Extensions.Hosting.BackgroundService
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<BackgroundService> _logger;
		private readonly StorageService _storageService;
        private CancellationTokenSource _effectCancellationTokenSource;
        private readonly ILocalHueClient _hueClient;

        public EffectHandler ActiveHandler { get; private set; }

		public BackgroundService(ILoggerFactory loggerFactory, StorageService storageService, ILocalHueClient hueClient)
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<BackgroundService>();
			_storageService = storageService;
            _hueClient = hueClient;
            _logger.LogDebug("ctor");
        }

        /// <summary>
        /// Called by ASP.NET Core during start-up.
        /// Waits for stop signal from a cancellation token.
        /// When that is received, cancels and effects and exits.
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("Started.");
                await ResumeEffect();
                _logger.LogInformation("Waiting for stop signal.");
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

        /// <summary>
        /// Called by the web controller to start an effect (after first stopping any active effect).
        /// Configuration and active state is persisted to file.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="handler"></param>
        /// <returns></returns>
        public async Task StartEffect(EffectConfig config, EffectHandler handler)
        {
            // Cancel previous handler if there is one
			_effectCancellationTokenSource?.Cancel();

			// Start the new handler
			_effectCancellationTokenSource = new CancellationTokenSource();
			handler.Start(_effectCancellationTokenSource.Token);
            ActiveHandler = handler;

            // Save config including state
            var otherConfig = config.GetType() == typeof(XmasEffectConfig)
                ? (EffectConfig)await _storageService.LoadConfig<WarmupEffectConfig>()
                : (EffectConfig)await _storageService.LoadConfig<XmasEffectConfig>();
            otherConfig.Active = false;
            config.Active = true;
            await _storageService.SaveConfig(otherConfig);
            await _storageService.SaveConfig(config);
        }

        /// <summary>
        /// Resume active effect after restart.
        /// </summary>
        /// <returns></returns>
        private async Task ResumeEffect()
        {
            var xmasEffectConfig = await _storageService.LoadConfig<XmasEffectConfig>();
            if (xmasEffectConfig.Active)
                await StartEffect(xmasEffectConfig, new XmasHandler(xmasEffectConfig, _loggerFactory, _hueClient));
            var warmupEffectConfig = await _storageService.LoadConfig<WarmupEffectConfig>();
            if (warmupEffectConfig.Active)
                await StartEffect(warmupEffectConfig, new WarmupHandler(warmupEffectConfig, _loggerFactory, _hueClient));
        }
    }
}

using System.Threading;
using System.Threading.Tasks;
using HueEffects.Web.EffectHandlers;
using HueEffects.Web.Models;
using Microsoft.Extensions.Logging;

namespace HueEffects.Web.Services
{
    public class BackgroundService : Microsoft.Extensions.Hosting.BackgroundService
    {
        private readonly ILogger<BackgroundService> _logger;

        public BackgroundService(ILogger<BackgroundService> logger)
        {
            _logger = logger;
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
                // TODO: Stop handler
                _logger.LogInformation("Finished cleaning up.");
            }
        }

        public Task StartEffect(EffectsConfig config, EffectHandler handler)
        {
            // TODO: Move code from HomeController.UseEffect.
            return Task.CompletedTask;
        }
    }
}

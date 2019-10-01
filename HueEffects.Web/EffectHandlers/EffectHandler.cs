using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Q42.HueApi;
using Q42.HueApi.Interfaces;

namespace HueEffects.Web.EffectHandlers
{
    public abstract class EffectHandler
    {
        protected readonly ILocalHueClient HueClient;
        private Thread _thread;
        private readonly ILogger _logger;
        protected CancellationToken CancellationToken { get; private set; }

        protected EffectHandler(ILocalHueClient hueClient, ILoggerFactory loggerFactory)
        {
            HueClient = hueClient;
            _logger = loggerFactory.CreateLogger<EffectHandler>();
        }

        public void Start(CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;
#pragma warning disable 4014
            _thread = new Thread(() => DoWork()) { IsBackground = true, Name = nameof(EffectHandler) };
#pragma warning restore 4014
            _thread.Start();
        }

        protected abstract Task DoWork();

		protected async Task SwitchOn(IReadOnlyCollection<string> lightIds, int? colorTemp = null)
		{
			_logger.LogDebug("Switching on light(s) {lights}...", string.Join(',', lightIds));
			var command = new LightCommand { On = true, ColorTemperature = colorTemp };
			await HueClient.SendCommandAsync(command, lightIds);
		}

		protected async Task SwitchOff(IReadOnlyCollection<string> lightIds)
		{
			_logger.LogDebug("Switching off light(s) {lights}...", string.Join(',', lightIds));
			var command = new LightCommand { On = false };
			await HueClient.SendCommandAsync(command, lightIds);
		}

        protected async Task UpdateColorTemp(int ct, IReadOnlyCollection<string> lightIds)
        {
            _logger.LogDebug("Setting color temp to {ct} for light(s) {lights}...", ct, string.Join(',', lightIds));
            var command = new LightCommand { ColorTemperature = ct};
            await HueClient.SendCommandAsync(command, lightIds);
        }

        protected async Task<Light[]> GetLights(string groupId)
        {
            var group = await HueClient.GetGroupAsync(groupId);
            var tasks = group.Lights.Select(id => HueClient.GetLightAsync(id));
            var lights = await Task.WhenAll(tasks);
            return lights;
        }
    }
}
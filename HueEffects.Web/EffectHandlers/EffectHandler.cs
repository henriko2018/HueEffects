﻿using System;
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
            _thread = new Thread(() => DoWork()) { IsBackground = true, Name = nameof(EffectHandler) };
            _thread.Start();
        }

        protected abstract Task DoWork();

		protected async Task SwitchOn(IReadOnlyCollection<string> lightIds, int? colorTemp = null, byte? brightness = null)
		{
			_logger.LogDebug("Switching on light(s) {lights}...", string.Join(',', lightIds));
			var command = new LightCommand { On = true, ColorTemperature = colorTemp, Brightness = brightness};
			await HueClient.SendCommandAsync(command, lightIds);
            // Check that they are really switched on
            foreach (var lightId in lightIds)
            {
                var light = await HueClient.GetLightAsync(lightId);
                if (!light.State.On)
                    _logger.LogError("Failed to switch on light " + lightId);
            }
        }

		protected async Task SwitchOff(IReadOnlyCollection<string> lightIds)
		{
			_logger.LogDebug("Switching off light(s) {lights}...", string.Join(',', lightIds));
			var command = new LightCommand { On = false };
			await HueClient.SendCommandAsync(command, lightIds);
            // Check that they are really switched off
            foreach (var lightId in lightIds)
            {
                var light = await HueClient.GetLightAsync(lightId);
                if (light.State.On)
                    _logger.LogError("Failed to switch off light " + lightId);
            }
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

        protected async void FireAndForget(Task task)
        {
            try
            {
                await task;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception caught in FireAndForget");
            }
        }
    }
}
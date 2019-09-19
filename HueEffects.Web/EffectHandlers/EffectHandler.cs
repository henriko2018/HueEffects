using System;
using System.Collections.Generic;
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
        protected bool StopFlag;
        private readonly ILogger _logger;

        protected EffectHandler(ILocalHueClient hueClient, ILoggerFactory loggerFactory)
        {
            HueClient = hueClient;
            _logger = loggerFactory.CreateLogger<EffectHandler>();
        }

        public void Start()
        {
#pragma warning disable 4014
            _thread = new Thread(() => DoWork()) { IsBackground = true, Name = nameof(XmasHandler) };
#pragma warning restore 4014
            _thread.Start();
        }

        public virtual void Stop()
        {
            StopFlag = true;
        }

        protected abstract Task DoWork();

        protected async Task UpdateColorTemp(int ct, IReadOnlyCollection<string> lightIds)
        {
            _logger.LogDebug("Setting color temp to {ct} for lights {lights}...", ct, string.Join(',', lightIds));
            var command = new LightCommand { ColorTemperature = ct};
            await HueClient.SendCommandAsync(command, lightIds);
        }

        protected async Task<List<string>> GetLightsWithCapability(string groupId, Func<LightCapabilities, bool> condition)
        {
            var lights = new List<string>();
            var group = await HueClient.GetGroupAsync(groupId);
            foreach (var lightId in @group.Lights)
            {
                var light = await HueClient.GetLightAsync(lightId);
                if (condition(light.Capabilities))
                    lights.Add(light.Id);
            }

            return lights;
        }
    }
}